export function setupTabs(release: () => void) {
  const tabs = [...document.querySelectorAll<HTMLButtonElement>('button[data-panel]')];
  const main = document.querySelector('main')!;
  function select(id: string) {
    release();
    document.body.dataset.panel = id;
    (document.activeElement as HTMLElement | null)?.blur();
    for (const tab of tabs) {
      const active = tab.dataset.panel === id;
      tab.setAttribute('aria-selected', String(active)); tab.tabIndex = active ? 0 : -1;
      document.getElementById(tab.dataset.panel!)!.hidden = !active;
    }
    main.scrollTop = 0;
  }
  for (const tab of tabs) {
    tab.addEventListener('click', () => select(tab.dataset.panel!));
    tab.addEventListener('keydown', event => {
      if (!['ArrowLeft', 'ArrowRight', 'Home', 'End'].includes(event.key)) return;
      event.preventDefault();
      const index = event.key === 'Home' ? 0 : event.key === 'End' ? tabs.length - 1 : (tabs.indexOf(tab) + (event.key === 'ArrowRight' ? 1 : -1) + tabs.length) % tabs.length;
      select(tabs[index].dataset.panel!); tabs[index].focus();
    });
  }
  document.getElementById('quick-keyboard')!.addEventListener('click', () => { select('keyboard-controls'); document.getElementById('remote-text')!.focus(); });
  function viewport() {
    const view = window.visualViewport;
    document.documentElement.style.setProperty('--view-height', `${view?.height ?? innerHeight}px`);
    document.documentElement.style.setProperty('--view-top', `${view?.offsetTop ?? 0}px`);
    document.body.classList.toggle('compact', (view?.height ?? innerHeight) < 380);
  }
  window.visualViewport?.addEventListener('resize', viewport);
  window.visualViewport?.addEventListener('scroll', viewport);
  window.addEventListener('resize', viewport); viewport();
  return { select };
}
