import { renderIcon } from './icons';
export function setupMedia(send: (type: string, payload: object) => number | false, releaseAll: () => void) {
  const section = document.getElementById('media-controls')!;
  const select = document.getElementById('media-profile') as HTMLSelectElement;
  const note = document.getElementById('media-note')!;
  const feedback = document.getElementById('media-feedback')!;
  const buttons = [...document.querySelectorAll<HTMLButtonElement>('[data-media]')];
  const quickKeyboard = document.getElementById('quick-keyboard') as HTMLButtonElement;
  let profile = 'General', enabled = false, saving = false;
  function apply(value: string) {
    if (!['General', 'YouTube', 'VLC', 'Stremio'].includes(value) || saving) return;
    profile = value; select.value = value;
    for (const button of buttons) {
      button.hidden = (value === 'General' && ['seekBack', 'seekForward', 'fullscreen'].includes(button.dataset.media!)) || (value === 'Stremio' && button.dataset.media === 'previous');
      if (button.dataset.media === 'previous') button.querySelector('.button-label')!.textContent = value === 'YouTube' ? 'Previous (playlist)' : 'Previous';
      if (button.dataset.media === 'next') button.querySelector('.button-label')!.textContent = value === 'Stremio' ? 'Next episode' : 'Next';
    }
    renderIcon(document.getElementById('profile-icon')!, value === 'Stremio' ? 'stremio' : 'media');
    note.textContent = value === 'General' ? 'Active media session · Windows volume' : 'Focus the player on your PC.';
    document.getElementById('media-help')!.textContent = value === 'General' ? 'Windows media keys target the active media session; volume controls system volume. Seek and fullscreen need an app profile.'
      : value === 'YouTube' ? 'YouTube shortcuts: click the PC video player first; focus its seek bar for volume. Previous needs a playlist. Other browser players may use different keys.'
      : value === 'Stremio' ? 'Focus Stremio’s built-in player on the PC. Seek amount follows its settings. Next episode needs an available next episode; Previous has no supported shortcut. Use Touchpad or Navigation to browse, and Keyboard for search.'
      : 'Focus VLC on the PC. These controls use its default hotkeys; customized hotkeys may differ. Seek amount follows VLC settings.';
  }
  select.addEventListener('change', async () => {
    if (!enabled || saving) return;
    const requested = select.value; saving = true; select.disabled = true;
    for (const button of buttons) button.disabled = true;
    releaseAll(); feedback.textContent = 'Saving profile…';
    try {
      const response = await fetch('/api/settings/media', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ profile: requested }), cache: 'no-store', signal: AbortSignal.timeout(5000) });
      if (!response.ok) throw new Error();
      const result = await response.json(); saving = false; apply(result.profile); feedback.textContent = 'Profile saved on the PC.';
    } catch { saving = false; apply(profile); feedback.textContent = 'Could not save profile. Reconnect and try again.'; }
    select.disabled = !enabled; for (const button of buttons) button.disabled = !enabled;
  });
  for (const button of buttons) button.addEventListener('click', () => {
    if (!enabled || saving) return;
    releaseAll(); if (!send('media.action', { profile, action: button.dataset.media! })) feedback.textContent = 'Disconnected. No action queued.';
  });
  apply('General');
  return {
    profile: apply,
    setEnabled(value: boolean) { enabled = value; section.inert = !value; section.classList.toggle('disabled', !value); select.disabled = !value || saving; quickKeyboard.disabled = !value; for (const button of buttons) button.disabled = !value || saving; },
  };
}
