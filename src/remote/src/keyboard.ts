import { type Capabilities, unavailable, labels } from './capabilities';
export function setupKeyboard(send: (type: string, payload: object) => number | false, releaseAll: () => void) {
  const sections = [document.getElementById('keyboard-controls')!, document.getElementById('navigation-controls')!];
  const field = document.getElementById('remote-text') as HTMLTextAreaElement;
  const submit = document.getElementById('send-text') as HTMLButtonElement;
  const feedback = document.getElementById('text-feedback')!;
  const modifiers = [...document.querySelectorAll<HTMLButtonElement>('[data-modifier]')];
  const held = new Set<string>();
  const repeatTimers = new Map<string, ReturnType<typeof setTimeout>>();
  let capabilities: Capabilities = unavailable;
  let enabled = false, composing = false, pending = 0, lastSubmitted = 0;
  let ackDeadline: ReturnType<typeof setTimeout> | undefined;
  function render() {
    for (const modifier of modifiers) modifier.setAttribute('aria-pressed', String(held.has(modifier.dataset.modifier!)));
    submit.disabled = !enabled || !capabilities.text || composing || pending !== 0 || field.value.length === 0 || field.value.length > 256;
    field.disabled = !enabled || !capabilities.text || pending !== 0;
    for (const button of document.querySelectorAll<HTMLButtonElement>('[data-key], [data-modifier]')) button.disabled = !enabled || !capabilities.keyboard;
    for (const button of document.querySelectorAll<HTMLButtonElement>('[data-shortcut]')) button.disabled = !enabled || !capabilities.shortcuts;
  }
  function reset() {
    held.clear(); for (const timer of repeatTimers.values()) clearTimeout(timer); repeatTimers.clear(); render();
  }
  function endKey(key: string) {
    clearTimeout(repeatTimers.get(key)); repeatTimers.delete(key);
    if (held.delete(key) && enabled) send('key.up', { key });
    render();
  }
  for (const modifier of modifiers) modifier.addEventListener('click', () => {
    if (!enabled || !capabilities.keyboard) return;
    const key = modifier.dataset.modifier!;
    if (held.has(key)) endKey(key);
    else if (send('key.down', { key })) { held.add(key); render(); }
  });
  for (const button of document.querySelectorAll<HTMLButtonElement>('[data-key]')) {
    const key = button.dataset.key!;
    function begin() {
      if (!enabled || !capabilities.keyboard || held.has(key)) return;
      if (!send('key.down', { key })) return;
      held.add(key);
      function repeat() {
        if (!held.has(key) || !enabled) return;
        if (!send('key.down', { key })) { releaseAll(); return; }
        repeatTimers.set(key, setTimeout(repeat, 125));
      }
      if (button.dataset.repeat === 'true') repeatTimers.set(key, setTimeout(repeat, 400));
    }
    button.addEventListener('pointerdown', event => { event.preventDefault(); button.setPointerCapture(event.pointerId); begin(); });
    button.addEventListener('pointerup', () => endKey(key));
    button.addEventListener('pointercancel', releaseAll);
    button.addEventListener('lostpointercapture', () => endKey(key));
    button.addEventListener('keydown', event => { if (event.key === ' ' || event.key === 'Enter') { event.preventDefault(); if (!event.repeat) begin(); } });
    button.addEventListener('keyup', event => { if (event.key === ' ' || event.key === 'Enter') { event.preventDefault(); endKey(key); } });
    button.addEventListener('blur', () => endKey(key));
  }
  for (const button of document.querySelectorAll<HTMLButtonElement>('[data-shortcut]')) button.addEventListener('click', () => {
    if (!enabled || !capabilities.shortcuts) return; releaseAll(); send('shortcut', { action: button.dataset.shortcut! });
  });
  field.addEventListener('focus', releaseAll);
  field.addEventListener('compositionstart', () => { composing = true; render(); });
  field.addEventListener('compositionend', () => { composing = false; render(); });
  field.addEventListener('input', render);
  document.getElementById('text-form')!.addEventListener('submit', event => {
    event.preventDefault(); if (submit.disabled || composing) return;
    const text = field.value; releaseAll();
    const sequence = send('text.insert', { text });
    if (!sequence) { feedback.textContent = 'Disconnected. Nothing queued; reconnect before sending.'; return; }
    pending = sequence; lastSubmitted = sequence; feedback.textContent = 'Sending…'; render();
    ackDeadline = setTimeout(() => {
      pending = 0; feedback.textContent = `Delivery uncertain. Check the ${labels(capabilities).computer} before sending again; this text will not be replayed.`; render();
    }, 5000);
  });
  return {
    setCapabilities(value: Capabilities) { capabilities = value; render(); },
    reset, held: () => held.size > 0,
    setEnabled(value: boolean) {
      if (!value) {
        reset(); clearTimeout(ackDeadline);
        if (pending) feedback.textContent = `Connection lost during sending. Check the ${labels(capabilities).computer} before retrying; nothing will be replayed.`;
        pending = 0; lastSubmitted = 0;
      }
      enabled = value; for (const section of sections) { section.inert = !value; section.classList.toggle('disabled', !value); } render();
    },
    acknowledge(sequence: number) {
      if (!Number.isSafeInteger(sequence) || sequence <= 0 || sequence > lastSubmitted) return false;
      if (sequence !== pending) return true; // Ignore late acknowledgement rather than replay or clear newer edits.
      clearTimeout(ackDeadline); pending = 0; field.value = ''; feedback.textContent = `Sent to the ${labels(capabilities).computer}.`; render(); return true;
    },
  };
}
