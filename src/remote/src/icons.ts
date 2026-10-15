// Local, code-native SVG controls. Nothing is fetched from a CDN.
const volume = '<path d="M3 9h4l5-4v14l-5-4H3z"/><path d="M16 8a6 6 0 0 1 0 8M19 5a10 10 0 0 1 0 14"/>';
const icons: Record<string, string> = {
  apps: '<rect x="3" y="3" width="7" height="7" rx="2"/><rect x="14" y="3" width="7" height="7" rx="2"/><rect x="3" y="14" width="7" height="7" rx="2"/><rect x="14" y="14" width="7" height="7" rx="2"/>',
  pc: '<rect x="2" y="3" width="20" height="14" rx="2"/><path d="M12 17v4M7 21h10"/>',
  browser: '<circle cx="12" cy="12" r="10"/><path d="M2 12h20M12 2c6 5 6 15 0 20M12 2c-6 5-6 15 0 20"/>',
  sleep: '<path d="M20 15A9 9 0 0 1 9 4a9 9 0 1 0 11 11Z"/>',
  restart: '<path d="M20 8V3m0 5h-5M20 8a9 9 0 1 0 1 8"/>',
  power: '<path d="M12 2v10M6 5a9 9 0 1 0 12 0"/>',
  settings: '<path d="m9 3-.5 2-2 1-2-.5-2 3 1.5 1.5v3L2.5 15l2 3 2-.5 2 1 .5 2h4l.5-2 2-1 2 .5 2-3-1.5-1.5v-3L20.5 9l-2-3-2 .5-2-1L13 3z"/><circle cx="11" cy="12" r="3"/>',
  playPause: '<path fill="currentColor" stroke="none" d="M3 4v16l11-8zM17 4h3v16h-3zM23 4h3v16h-3z"/>',
  minus: '<path d="M5 12h14"/>', plus: '<path d="M5 12h14M12 5v14"/>', volume,
  volumeDown: '<path d="M2 9h3l5-4v14l-5-4H2zM14 8a6 6 0 0 1 0 8M19 12h4"/>',
  volumeUp: '<path d="M2 9h3l5-4v14l-5-4H2zM14 8a6 6 0 0 1 0 8M19 12h4M21 10v4"/>',
  mute: '<path d="M3 9h4l5-4v14l-5-4H3zM16 8l5 8M21 8l-5 8M3 3l18 18"/>',
  keyboard: '<rect x="2" y="5" width="20" height="14" rx="2"/><path d="M6 9h.5M10 9h.5M14 9h.5M18 9h.5M6 12h.5M10 12h.5M14 12h.5M18 12h.5M7 16h10"/>',
  air: '<rect x="8" y="2" width="8" height="20" rx="2"/><path d="M4.5 8a7 7 0 0 0 0 8M19.5 8a7 7 0 0 1 0 8M1.5 5a12 12 0 0 0 0 14M22.5 5a12 12 0 0 1 0 14"/>',
  tune: '<path d="M3 7h9M17 7h4M3 17h5M13 17h8"/><circle cx="14.5" cy="7" r="2.5"/><circle cx="10.5" cy="17" r="2.5"/>',
  touchpad: '<rect x="6" y="2" width="12" height="20" rx="6"/><path d="M12 3v6M7 10h10"/>',
  leftClick: '<rect x="6" y="2" width="12" height="20" rx="6"/><path d="M12 3v7M7 10h10"/><path fill="currentColor" stroke="none" d="M11 3a5 5 0 0 0-4 5v1h4z"/>',
  rightClick: '<rect x="6" y="2" width="12" height="20" rx="6"/><path d="M12 3v7M7 10h10"/><path fill="currentColor" stroke="none" d="M13 3a5 5 0 0 1 4 5v1h-4z"/>',
  doubleClick: '<rect x="5" y="2" width="12" height="20" rx="6"/><path d="M11 3v7M6 10h10"/><circle cx="17" cy="17" r="5" fill="#222a34"/><path d="M16 15h2l-2 4h2"/>',
  drag: '<path stroke-dasharray="3 3" d="M3 3h18v18H3z"/>',
  release: '<rect x="5" y="10" width="14" height="11" rx="2" fill="currentColor" stroke="none"/><path d="M9 10V7a4 4 0 0 1 8 0"/><path stroke="#222a34" d="M12 14v3"/>',
  send: '<path d="m3 10 18-7-7 18-3-8zM11 13 21 3"/>',
  enter: '<path d="M20 5v8H4m5-5-5 5 5 5"/>',
  backspace: '<path d="M9 5h12v14H9l-7-7zM12 9l5 6m0-6-5 6"/>',
  tab: '<path d="M3 8h17m-4-4 4 4-4 4M21 16H4m4-4-4 4 4 4"/>',
  shift: '<path d="m7 10 5-5 5 5M12 5v14"/>',
  windows: '<path fill="currentColor" stroke="none" d="M3 4 11 3v8H3zM13 3l8-1v9h-8zM3 13h8v8l-8-1zM13 13h8v10l-8-1z"/>',
  selectAll: '<rect x="7" y="6" width="14" height="16" rx="2"/><path d="M16 6V3H3v16h4M11 10h6M11 14h6M11 18h4"/>',
  undo: '<path d="m9 4-6 6 6 6M3 10h12a5 5 0 0 1 0 10h-3"/>',
  up: '<path fill="currentColor" stroke="none" d="m12 4 10 15H2z"/>',
  down: '<path fill="currentColor" stroke="none" d="m12 20 10-15H2z"/>',
  left: '<path fill="currentColor" stroke="none" d="m4 12 15-10v20z"/>',
  right: '<path fill="currentColor" stroke="none" d="m20 12-15-10v20z"/>',
  home: '<path d="m2 10 10-8 10 8M5 8v13h5v-7h4v7h5V8"/>',
  end: '<path d="M4 3v18M8 12h13m-8-5-5 5 5 5"/>',
  pageUp: '<path d="M12 21V3m-6 6 6-6 6 6"/>',
  pageDown: '<path d="M12 3v18m-6-6 6 6 6-6"/>',
  navigation: '<path d="m12 2 3 4-3 4-3-4zM12 14l3 4-3 4-3-4zM2 12l4-3 4 3-4 3zM14 12l4-3 4 3-4 3z"/>',
  media: '<circle cx="12" cy="12" r="10"/><path fill="currentColor" stroke="none" d="m9 7 8 5-8 5z"/>',
  stremio: '<rect x="1" y="1" width="22" height="22" rx="4" fill="#192944" stroke="none"/><path d="m5 12 7-9 8 9-8 9z" fill="#5046b0" stroke="none"/><path d="m9 5 11 7-11 7z" fill="#9364e8" stroke="none"/><path d="m11 9 5 3-5 3z" fill="white" stroke="none"/>',
  seekBack: '<path fill="currentColor" stroke="none" d="m2 12 10-8v16zm10 0 10-8v16z"/>',
  seekForward: '<path fill="currentColor" stroke="none" d="m22 12-10-8v16zm-10 0L2 4v16z"/>',
  previous: '<path d="M4 4v16"/><path fill="currentColor" stroke="none" d="m6 12 14-8v16z"/>',
  next: '<path d="M20 4v16"/><path d="m4 4 12 8-12 8z"/>',
  fullscreen: '<path d="M3 9V3h6M15 3h6v6M21 15v6h-6M9 21H3v-6"/>',
  chevronRight: '<path d="m9 5 7 7-7 7"/>',
  close: '<path d="m6 6 12 12M18 6 6 18"/>',
};
export function renderIcon(element: HTMLElement, name: string) {
  const drawing = icons[name];
  if (!drawing) throw new Error(`Unknown control icon: ${name}`);
  element.innerHTML = `<svg class="icon" viewBox="0 0 ${name === 'playPause' ? '28' : '24'} 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true" focusable="false">${drawing}</svg>`;
}
export function setupIcons() {
  for (const element of document.querySelectorAll<HTMLElement>('[data-icon]')) renderIcon(element, element.dataset.icon!);
}
