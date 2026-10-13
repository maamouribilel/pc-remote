import { type Capabilities, unavailable, labels as platformLabels } from './capabilities';
import { renderIcon } from './icons';

type Api = (path: string, body?: object) => Promise<Record<string, any>>;
type App = { id: string; name: string; profile: string; available: boolean };
type Volume = { available: boolean; level: number; muted: boolean };
type PendingPower = { action: string; executeAt: string };
const labels: Record<string, string> = { sleep: 'Sleep', restart: 'Restart', shutdown: 'Shut down' };

export function setupPc(api: Api, release: () => void, profile: (value: string) => void, selectTab: (id: string) => void) {
  const panel = document.getElementById('pc-controls')!;
  const apps = document.getElementById('launcher-apps')!;
  const feedback = document.getElementById('pc-feedback')!;
  const volume = document.getElementById('system-volume') as HTMLInputElement;
  const volumeValue = document.getElementById('system-volume-value')!;
  const volumeNote = document.getElementById('system-volume-note')!;
  const volumeFeedback = document.getElementById('volume-feedback')!;
  const dialog = document.getElementById('power-dialog') as HTMLDialogElement;
  const confirm = document.getElementById('confirm-power') as HTMLButtonElement;
  const cancel = document.getElementById('cancel-power') as HTMLButtonElement;
  const countdown = document.getElementById('power-countdown')!;
  const countdownText = document.getElementById('power-countdown-text')!;
  let capabilities: Capabilities = unavailable;
  let enabled = false, session = '', loading = false, launching = false, preparing = false, savingVolume = false, editingVolume = false;
  let latestVolume: Volume = { available: false, level: 0, muted: false };
  let pending: PendingPower | null = null;
  let ticket: { token: string; session: string; expires: number; action: string } | undefined;

  function update() {
    panel.inert = !enabled; panel.classList.toggle('disabled', !enabled);
    volume.disabled = !enabled || !capabilities.volume || !latestVolume.available || savingVolume;
    for (const button of apps.querySelectorAll<HTMLButtonElement>('button')) button.disabled = !enabled || !capabilities.launcher || launching || button.dataset.available !== 'true';
    for (const button of document.querySelectorAll<HTMLButtonElement>('[data-power]')) button.disabled = !enabled || !capabilities.powerActions.includes(button.dataset.power!) || preparing || pending !== null;
    confirm.disabled = !enabled || preparing || !ticket || ticket.expires <= Date.now() || ticket.session !== session;
    cancel.disabled = !enabled || !pending;
    countdown.hidden = !pending;
    if (pending) countdownText.textContent = `${labels[pending.action] ?? 'Power action'} in ${Math.max(0, Math.ceil((Date.parse(pending.executeAt) - Date.now()) / 1000))} seconds. Keep this app open, or cancel below.`;
  }
  function applyVolume(value: Volume) {
    latestVolume = value;
    if (!editingVolume && !savingVolume) {
      volume.value = String(value.level); volumeValue.textContent = value.available ? `${value.level}%` : '—';
      volumeNote.textContent = !value.available ? `${platformLabels(capabilities).system} audio output is unavailable.` : value.muted ? `${platformLabels(capabilities).system} is muted. Moving the slider unmutes it.` : `${platformLabels(capabilities).system} output volume · separate from the player’s volume`;
    }
    update();
  }
  async function request(path: string, body: object) {
    if (!enabled || !session) throw new Error('Reconnect before trying again.');
    const current = session;
    const result = await api(path, { session: current, ...body });
    if (!enabled || current !== session) throw new Error('Connection changed. Check the PC before trying again.');
    if (!result.ok) throw new Error(result.error === 'audio_unavailable' ? `${platformLabels(capabilities).system} audio output is unavailable.`
      : result.error === 'confirmation_expired' ? 'Confirmation expired. Choose the action again.'
      : result.error === 'launch_rate_limit' ? 'Wait a moment before opening another app.'
      : result.error === 'power_pending' ? 'Cancel the pending power action first.'
      : 'The PC could not complete this action. Check the app, connection and permissions.');
    return result;
  }
  async function refresh() {
    if (!enabled || loading) return;
    const current = session; loading = true;
    try {
      const result = await api('/api/pc');
      if (!enabled || session !== current) return;
      if (!result.ok) throw new Error();
      apps.replaceChildren();
      for (const entry of result.apps as App[]) {
        const button = document.createElement('button'); button.type = 'button'; button.className = 'launcher-app'; button.dataset.available = String(entry.available);
        const icon = document.createElement('span'); renderIcon(icon, entry.profile === 'Stremio' ? 'stremio' : entry.profile === 'VLC' ? 'media' : entry.profile === 'YouTube' ? 'browser' : 'apps');
        const text = document.createElement('span'); text.className = 'launcher-name'; text.textContent = entry.name;
        const hint = document.createElement('small'); hint.textContent = entry.available ? `Open on ${platformLabels(capabilities).computer}` : 'App not found';
        const arrow = document.createElement('span'); renderIcon(arrow, 'chevronRight');
        button.append(icon, text, hint, arrow);
        button.addEventListener('click', async () => {
          if (launching || !enabled || !capabilities.launcher) return;
          release(); launching = true; feedback.textContent = `Opening ${entry.name}…`; update();
          try {
            const response = await request('/api/pc/launch', { id: entry.id });
            profile(response.profile); selectTab('media-controls');
            volumeFeedback.textContent = response.focused ? `${response.name} is ready on the ${platformLabels(capabilities).computer}.` : `${response.name} opened. If it remains behind another window, select it on the ${platformLabels(capabilities).computer}.`;
          } catch (reason) { feedback.textContent = String(reason).replace(/^Error: /, ''); }
          finally { launching = false; update(); }
        });
        apps.append(button);
      }
      document.getElementById('launcher-empty')!.hidden = result.apps.length > 0;
      sync(result);
    } catch { if (enabled) feedback.textContent = 'Could not load apps. Reopen this tab to try again.'; }
    finally { loading = false; update(); if (enabled && current !== session) void refresh(); }
  }
  volume.addEventListener('input', () => { editingVolume = true; volumeValue.textContent = `${volume.value}%`; });
  volume.addEventListener('change', async () => {
    if (!enabled || !capabilities.volume || savingVolume) return;
    release(); savingVolume = true; editingVolume = false; update(); volumeFeedback.textContent = `Setting ${platformLabels(capabilities).system} volume…`;
    try {
      const result = await request('/api/pc/volume', { level: Number(volume.value) });
      savingVolume = false; applyVolume(result.volume); volumeFeedback.textContent = `${platformLabels(capabilities).system} volume set to ${result.volume.level}%.`;
    } catch (reason) { savingVolume = false; applyVolume(latestVolume); volumeFeedback.textContent = String(reason).replace(/^Error: /, ''); }
    update();
  });
  for (const button of document.querySelectorAll<HTMLButtonElement>('[data-power]')) button.addEventListener('click', async () => {
    if (!enabled || !capabilities.powerActions.includes(button.dataset.power!) || preparing || pending) return;
    release(); preparing = true; ticket = undefined; feedback.textContent = ''; update();
    try {
      const result = await request('/api/pc/power/prepare', { action: button.dataset.power });
      ticket = { token: result.token, action: result.action, session, expires: Date.parse(result.expires) };
      document.getElementById('power-title')!.textContent = `${labels[result.action]} this ${platformLabels(capabilities).computer}?`;
      document.getElementById('power-explanation')!.textContent = result.action === 'sleep'
        ? `The ${platformLabels(capabilities).computer} will sleep after a 30-second countdown. The remote will disconnect. Wake it locally to reconnect.`
        : `Save your work first. After a 30-second countdown, ${platformLabels(capabilities).system} will request this action. Apps with unsaved work may need your attention.`;
      confirm.textContent = `${labels[result.action]} in 30 seconds`;
      dialog.showModal();
    } catch (reason) { feedback.textContent = String(reason).replace(/^Error: /, ''); }
    finally { preparing = false; update(); }
  });
  confirm.addEventListener('click', async () => {
    if (!ticket || preparing || !enabled) return;
    preparing = true; update();
    try {
      const result = await request('/api/pc/power/confirm', { token: ticket.token });
      pending = result.power; ticket = undefined; dialog.close(); feedback.textContent = 'Power action scheduled.';
    } catch (reason) { ticket = undefined; dialog.close(); feedback.textContent = String(reason).replace(/^Error: /, ''); }
    finally { preparing = false; update(); }
  });
  document.getElementById('dismiss-power')!.addEventListener('click', () => { ticket = undefined; dialog.close(); update(); });
  dialog.addEventListener('cancel', () => { ticket = undefined; update(); });
  cancel.addEventListener('click', async () => {
    try { await request('/api/pc/power/cancel', {}); pending = null; feedback.textContent = 'Power action cancelled.'; update(); }
    catch (reason) { feedback.textContent = String(reason).replace(/^Error: /, ''); }
  });
  function sync(value: Record<string, any>) {
    if (value.volume) applyVolume(value.volume);
    if ('power' in value) pending = value.power;
    if (value.powerMessage) feedback.textContent = value.powerMessage;
    update();
  }
  document.getElementById('tab-pc')!.addEventListener('click', refresh);
  setInterval(update, 500);
  return {
    sync,
    setCapabilities(value: Capabilities) { capabilities = value; update(); },
    setConnection(value: boolean, id = '') {
      const changed = value && (!enabled || id !== session);
      enabled = value; session = value ? id : '';
      if (!value) { ticket = undefined; pending = null; editingVolume = false; dialog.close(); }
      update(); if (changed) void refresh();
    },
  };
}
