import { type Capabilities, unavailable, labels } from './capabilities';
import { renderIcon } from './icons';
export function setupMedia(send: (type: string, payload: object) => number | false, releaseAll: () => void) {
  const section = document.getElementById('media-controls')!;
  const select = document.getElementById('media-profile') as HTMLSelectElement;
  const note = document.getElementById('media-note')!;
  const feedback = document.getElementById('media-feedback')!;
  const buttons = [...document.querySelectorAll<HTMLButtonElement>('[data-media]')];
  const quickKeyboard = document.getElementById('quick-keyboard') as HTMLButtonElement;
  let capabilities: Capabilities = unavailable;
  let profile = 'General', enabled = false, saving = false;
  function apply(value: string) {
    if (!['General', 'YouTube', 'VLC', 'Stremio'].includes(value) || saving) return;
    profile = value; select.value = value;
    for (const button of buttons) {
      button.hidden = !capabilities.mediaActions[value]?.includes(button.dataset.media!) || (value === 'General' && ['seekBack', 'seekForward', 'fullscreen'].includes(button.dataset.media!)) || (value === 'Stremio' && button.dataset.media === 'previous');
      if (button.dataset.media === 'previous') button.querySelector('.button-label')!.textContent = value === 'YouTube' ? 'Previous (playlist)' : 'Previous';
      if (button.dataset.media === 'next') button.querySelector('.button-label')!.textContent = value === 'Stremio' ? 'Next episode' : 'Next';
    }
    renderIcon(document.getElementById('profile-icon')!, value === 'Stremio' ? 'stremio' : 'media');
    note.textContent = !capabilities.mediaActions[value]?.length ? 'Media control is unavailable on this companion.' : value === 'General' ? `Active media session · ${labels(capabilities).system} volume` : `Focus the player on your ${labels(capabilities).computer}.`;
    document.getElementById('media-help')!.textContent = value === 'General' ? capabilities.platform === 'linux' ? 'General controls one eligible MPRIS player, preferring the only playing one. Ambiguous players fail; volume controls system output. Seek/fullscreen need a focused app profile.' : `${labels(capabilities).system} media keys target the active media session; volume controls system volume. Seek and fullscreen need an app profile.`
      : value === 'YouTube' ? `YouTube shortcuts: click the ${labels(capabilities).computer} video player first; focus its seek bar for volume. Previous needs a playlist. Other browser players may use different keys.`
      : value === 'Stremio' ? `Focus Stremio’s built-in player on the ${labels(capabilities).computer}. Seek amount follows its settings. Next episode needs an available next episode; Previous has no supported shortcut. Use Touchpad or Navigation to browse, and Keyboard for search.`
      : `Focus VLC on the ${labels(capabilities).computer}. These controls use its default hotkeys; customized hotkeys may differ. Seek amount follows VLC settings.`;
  }
  select.addEventListener('change', async () => {
    if (!enabled || saving) return;
    const requested = select.value; saving = true; select.disabled = true;
    for (const button of buttons) button.disabled = true;
    releaseAll(); feedback.textContent = 'Saving profile…';
    try {
      const response = await fetch('/api/settings/media', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ profile: requested }), cache: 'no-store', signal: AbortSignal.timeout(5000) });
      if (!response.ok) throw new Error();
      const result = await response.json(); saving = false; apply(result.profile); feedback.textContent = `Profile saved on the ${labels(capabilities).computer}.`;
    } catch { saving = false; apply(profile); feedback.textContent = 'Could not save profile. Reconnect and try again.'; }
    render();
  });
  for (const button of buttons) button.addEventListener('click', () => {
    if (!enabled || saving || !capabilities.mediaActions[profile]?.includes(button.dataset.media!)) return;
    releaseAll(); if (!send('media.action', { profile, action: button.dataset.media! })) feedback.textContent = 'Disconnected. No action queued.';
  });
  function render() {
    const available = Object.values(capabilities.mediaActions).some(a => a.length > 0);
    section.inert = !enabled || !available; section.classList.toggle('disabled', !enabled || !available);
    select.disabled = !enabled || !available || saving;
    for (const option of select.options) option.disabled = !capabilities.mediaActions[option.value]?.length;
    quickKeyboard.disabled = !enabled || !(capabilities.keyboard || capabilities.text);
    for (const button of buttons) button.disabled = !enabled || saving || !capabilities.mediaActions[profile]?.includes(button.dataset.media!);
  }
  apply('General');
  return {
    profile(value: string) { apply(value); render(); },
    setCapabilities(value: Capabilities) { capabilities = value; apply(profile); render(); },
    setEnabled(value: boolean) { enabled = value; render(); },
  };
}
