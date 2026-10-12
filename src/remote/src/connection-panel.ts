export function setupConnectionPanel(release: () => void) {
  const dialog = document.getElementById('connection-dialog') as HTMLDialogElement;
  const details = document.getElementById('connection-details') as HTMLDetailsElement;
  const trigger = document.getElementById('open-settings')!;
  let automatic = false;
  function open(forConnection = false) {
    if (dialog.open) return;
    automatic = forConnection;
    release(); details.open = true; dialog.showModal();
  }
  function close() { if (dialog.open) dialog.close(); }
  trigger.addEventListener('click', () => open());
  document.getElementById('close-settings')!.addEventListener('click', close);
  dialog.addEventListener('close', () => { release(); trigger.focus({ preventScroll: true }); });
  return { open, close, closeAutomatic: () => { if (automatic) close(); } };
}
