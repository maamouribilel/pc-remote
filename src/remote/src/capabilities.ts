export type Capabilities = {
  platform: string; pointer: boolean; keyboard: boolean; text: boolean; shortcuts: boolean;
  volume: boolean; launcher: boolean; powerActions: string[]; mediaActions: Record<string, string[]>;
};
const actions = ['playPause', 'next', 'previous', 'volumeUp', 'volumeDown', 'mute'];
export const legacy: Capabilities = { platform: 'windows', pointer: true, keyboard: true, text: true, shortcuts: true,
  volume: true, launcher: true, powerActions: ['sleep', 'restart', 'shutdown'], mediaActions: {
    General: actions, YouTube: [...actions, 'seekBack', 'seekForward', 'fullscreen'],
    VLC: [...actions, 'seekBack', 'seekForward', 'fullscreen'], Stremio: [...actions.filter(a => a !== 'previous'), 'seekBack', 'seekForward', 'fullscreen'],
  } };
export const unavailable: Capabilities = { platform: 'unknown', pointer: false, keyboard: false, text: false, shortcuts: false,
  volume: false, launcher: false, powerActions: [], mediaActions: {} };
// An absent extension on protocol v1 is the historical Windows companion contract.
// A present malformed extension fails closed, rather than enabling legacy controls.
export function readCapabilities(status: Record<string, any>): Capabilities {
  if (status.capabilities == null) return status.protocol === 1 ? legacy : unavailable;
  const c = status.capabilities;
  if (typeof c.platform !== 'string' || !['pointer', 'keyboard', 'text', 'shortcuts', 'volume', 'launcher'].every(k => typeof c[k] === 'boolean')
      || !Array.isArray(c.powerActions) || !c.powerActions.every((a: unknown) => typeof a === 'string')
      || !c.mediaActions || typeof c.mediaActions !== 'object' || Array.isArray(c.mediaActions)
      || !Object.values(c.mediaActions).every(a => Array.isArray(a) && a.every(v => typeof v === 'string'))) return unavailable;
  return c;
}
export function labels(c: Capabilities) {
  const mac = c.platform === 'macos', windows = ['windows', 'legacy-v1', 'recording'].includes(c.platform);
  return { computer: mac ? 'Mac' : windows ? 'PC' : 'computer', control: mac ? 'Control' : 'Ctrl',
    alt: mac ? 'Option' : 'Alt', meta: mac ? 'Command' : windows ? 'Win' : c.platform === 'linux' ? 'Super' : 'Meta',
    shortcut: mac ? 'Command' : 'Ctrl', switchApp: mac ? 'Command + Tab' : 'Alt + Tab', system: windows ? 'Windows' : mac ? 'Mac' : 'System' };
}
export function supports(c: Capabilities, type: string, payload: any) {
  if (type === 'ping' || type === 'input.releaseAll') return true;
  if (type.startsWith('pointer.')) return c.pointer;
  if (type.startsWith('key.')) return c.keyboard;
  if (type === 'text.insert') return c.text;
  if (type === 'shortcut') return c.shortcuts;
  if (type === 'media.action') return c.mediaActions[payload.profile]?.includes(payload.action) === true;
  return false;
}
export function renderLabels(c: Capabilities) {
  const l = labels(c);
  for (const button of document.querySelectorAll<HTMLButtonElement>('[data-modifier]')) {
    const name = ({ Control: l.control, Alt: l.alt, Windows: l.meta } as Record<string, string>)[button.dataset.modifier!];
    if (name) { const span = button.querySelector('span:last-child'); if (span) span.textContent = name; else button.textContent = name; button.setAttribute('aria-label', name); }
    const icon = button.querySelector('[data-icon="windows"]'); if (icon) (icon as HTMLElement).hidden = !['windows', 'legacy-v1', 'recording'].includes(c.platform);
  }
  for (const button of document.querySelectorAll<HTMLButtonElement>('[data-shortcut]')) {
    const key = ({ copy: 'C', paste: 'V', selectAll: 'A', undo: 'Z' } as Record<string, string>)[button.dataset.shortcut!];
    const text = key ? `${l.shortcut} + ${key}` : l.switchApp;
    const span = button.querySelector('span:last-child'); if (span) span.textContent = text; else button.textContent = text;
  }
  document.querySelector('label[for="system-volume"]')!.textContent = `${l.system} volume`;
  document.querySelector('#pc-controls h2')!.textContent = `Your ${l.computer}`;
  document.querySelector('#tab-pc span:last-child')!.textContent = c.platform === 'macos' ? 'Mac' : ['windows', 'legacy-v1', 'recording'].includes(c.platform) ? 'PC' : 'Computer';
  document.getElementById('launcher-empty')!.textContent = c.launcher ? `Add apps in the ${l.computer} companion’s settings.` : 'App launching is unavailable on this companion.';
  document.querySelector('.more-shortcuts .description')!.textContent = `${l.shortcut}+V uses the ${l.computer}'s existing clipboard.`;
  (document.querySelector('#media-profile option[value="General"]') as HTMLOptionElement).text = `General · ${l.system} media`;
}
