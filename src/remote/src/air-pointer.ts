import { AirPipeline, constants, isTap, normalizeSettings, type AirSettings, type MotionSample } from './air-math';
type Pointer = {
  motion(dx: number, dy: number): void; click(): boolean; held(): boolean; release(): void;
  hook(callbacks: { reset?: () => void; enabled?: (value: boolean) => void }): void;
};
type Mode = 'touch' | 'air';
type Sensor = 'idle' | 'probing' | 'needs-permission' | 'denied' | 'none' | 'ready';
const STORAGE_KEY = 'pc-remote.air';
const IDLE_HINT = 'Hold to point · tap to click';
// Air pointer: hold the pad and turn the phone, like a TV magic remote. Relative pixel deltas feed the
// touchpad's existing pointer.move aggregate; there is no new wire message. Sensors are read only while
// Air is selected on a visible Touchpad tab with control enabled and the page visible, and raw motion
// data never leaves the phone. Pointing never re-engages on its own: every hold is a new deliberate press.
export function setupAirPointer(pointer: Pointer) {
  const panel = document.getElementById('mouse-controls')!;
  const touchpad = document.getElementById('touchpad')!;
  const pad = document.getElementById('air-pad')!;
  const status = document.getElementById('air-status')!;
  const hint = document.getElementById('air-hint')!;
  const enable = document.getElementById('air-enable') as HTMLButtonElement;
  const useTouch = document.getElementById('air-use-touch') as HTMLButtonElement;
  const settingsButton = document.getElementById('air-settings') as HTMLButtonElement;
  const dialog = document.getElementById('air-dialog') as HTMLDialogElement;
  const speed = document.getElementById('air-speed') as HTMLInputElement;
  const speedValue = document.getElementById('air-speed-value')!;
  const invertX = document.getElementById('air-invert-x') as HTMLInputElement;
  const invertY = document.getElementById('air-invert-y') as HTMLInputElement;
  const modes = [...document.querySelectorAll<HTMLButtonElement>('#pointer-mode [data-mode]')];
  const pipeline = new AirPipeline();
  let mode: Mode = 'touch', sensor: Sensor = 'idle';
  let enabled = false, subscribed = false, holding = false;
  let pointerId = -1, start = 0, travel = 0, lastSample = 0, notice = '';
  let last = { x: 0, y: 0 };
  let probe: ReturnType<typeof setTimeout> | undefined, watchdog: ReturnType<typeof setInterval> | undefined;
  let settings = load();
  // Feel is a property of this phone's sensors, so speed/invert stay on the phone. PC sensitivity still applies.
  function load(): AirSettings { try { return normalizeSettings(JSON.parse(localStorage.getItem(STORAGE_KEY) ?? 'null')); } catch { return normalizeSettings(null); } }
  function save() { try { localStorage.setItem(STORAGE_KEY, JSON.stringify(settings)); } catch { /* blocked storage: keep the values for this page only */ } }
  // iOS 13+ exposes a static requestPermission that must run inside a user gesture; other browsers deliver events directly.
  function permission(): (() => Promise<string>) | undefined {
    if (typeof DeviceMotionEvent === 'undefined') return undefined;
    const constructor = DeviceMotionEvent as unknown as { requestPermission?: () => Promise<string> };
    return typeof constructor.requestPermission === 'function' ? () => constructor.requestPermission!() : undefined;
  }
  function angle(): number {
    const current = (screen as { orientation?: { angle?: unknown } }).orientation?.angle;
    if (typeof current === 'number') return current;
    const legacy = (window as { orientation?: unknown }).orientation;
    return typeof legacy === 'number' ? legacy : 0;
  }
  function render() {
    pad.dataset.state = holding ? 'holding' : sensor;
    pad.classList.toggle('holding', holding);
    enable.hidden = sensor !== 'needs-permission';
    useTouch.hidden = sensor !== 'denied' && sensor !== 'none';
    status.textContent = holding ? 'Pointing' : sensor === 'needs-permission' ? 'Allow motion access to point with this phone.'
      : sensor === 'denied' ? 'Motion access was declined. Reload the remote to be asked again, or use Touch.'
      : sensor === 'none' ? 'No motion sensor on this device.' : sensor === 'ready' ? 'Point the phone at the screen' : 'Checking motion sensor…';
    hint.hidden = sensor !== 'ready';
    hint.textContent = holding ? 'Turn your wrist · lift to stop' : notice || (pointer.held() ? 'Dragging · hold to move, then End drag' : IDLE_HINT);
  }
  function onMotion(event: DeviceMotionEvent) {
    const now = performance.now();
    const sample: MotionSample = { rotationRate: event.rotationRate, gravity: event.accelerationIncludingGravity, timeStamp: event.timeStamp, interval: event.interval };
    const result = pipeline.feed(sample, now, settings, angle(), holding);
    if (result === undefined) return; // no gyroscope reading in this event
    lastSample = now;
    if (sensor !== 'ready') { clearTimeout(probe); sensor = 'ready'; render(); }
    if (result && holding) pointer.motion(result.dx, result.dy);
  }
  function startProbe(delay: number, next: Sensor) {
    clearTimeout(probe);
    probe = setTimeout(() => { if (subscribed && sensor === 'probing') { sensor = next; render(); } }, delay);
  }
  function subscribe() {
    if (subscribed) return;
    subscribed = true; pipeline.reset(); notice = '';
    if (typeof DeviceMotionEvent === 'undefined') { sensor = 'none'; render(); return; }
    sensor = 'probing';
    window.addEventListener('devicemotion', onMotion);
    // iOS delivers nothing until permission is granted; elsewhere silence means no sensor.
    startProbe(permission() ? 500 : 1000, permission() ? 'needs-permission' : 'none');
    render();
  }
  function unsubscribe() {
    if (!subscribed) return;
    subscribed = false; clearTimeout(probe);
    window.removeEventListener('devicemotion', onMotion);
    stop(); sensor = 'idle'; render();
  }
  const active = () => enabled && mode === 'air' && !panel.hidden && !document.hidden;
  function update() { if (active()) subscribe(); else unsubscribe(); }
  function stop(message = '') {
    clearInterval(watchdog); watchdog = undefined;
    if (holding) {
      holding = false; notice = message;
      try { pad.releasePointerCapture(pointerId); } catch { /* capture already gone */ }
      pointerId = -1;
    }
    render();
  }
  function setMode(next: Mode) {
    if (mode === next) return;
    mode = next; pointer.release(); // releases held input and stops pointing before the surfaces swap
    for (const button of modes) { const on = button.dataset.mode === mode; button.setAttribute('aria-checked', String(on)); button.tabIndex = on ? 0 : -1; }
    touchpad.hidden = mode !== 'touch'; pad.hidden = mode !== 'air'; settingsButton.hidden = mode !== 'air';
    update();
  }
  for (const button of modes) {
    button.addEventListener('click', () => setMode(button.dataset.mode as Mode));
    button.addEventListener('keydown', event => {
      if (!['ArrowLeft', 'ArrowRight', 'ArrowUp', 'ArrowDown'].includes(event.key)) return;
      event.preventDefault();
      const next = modes[(modes.indexOf(button) + 1) % modes.length];
      setMode(next.dataset.mode as Mode); next.focus();
    });
  }
  enable.addEventListener('click', () => {
    const request = permission();
    if (!request || !subscribed) return;
    let pending: Promise<string>;
    try { pending = request(); } catch { sensor = 'denied'; render(); return; } // first call inside the gesture, no await before it
    sensor = 'probing'; render(); startProbe(10000, 'needs-permission');
    pending.then(state => {
      if (!subscribed) return;
      if (state === 'denied') { sensor = 'denied'; render(); return; }
      // 'granted', or a browser answering 'prompt' without a dialog (Chromium): samples, or their absence, decide.
      window.removeEventListener('devicemotion', onMotion); window.addEventListener('devicemotion', onMotion);
      startProbe(1000, 'none');
    }, () => { if (subscribed) { sensor = 'denied'; render(); } });
  });
  useTouch.addEventListener('click', () => setMode('touch'));
  pad.addEventListener('pointerdown', event => {
    if (!enabled || holding || sensor !== 'ready' || (event.target as Element).closest('button')) return;
    event.preventDefault();
    pointerId = event.pointerId;
    try { pad.setPointerCapture(pointerId); } catch { /* pointer cannot be captured */ }
    holding = true; start = performance.now(); travel = 0; notice = ''; lastSample = start; last = { x: event.clientX, y: event.clientY };
    pipeline.engage(start, angle());
    watchdog = setInterval(() => { if (holding && performance.now() - lastSample > constants.stallMs) stop('Motion paused · hold again'); }, 100);
    try { navigator.vibrate?.(10); } catch { /* not supported */ }
    render();
  });
  pad.addEventListener('pointermove', event => {
    if (!holding || event.pointerId !== pointerId) return;
    travel += Math.hypot(event.clientX - last.x, event.clientY - last.y); last = { x: event.clientX, y: event.clientY };
  });
  pad.addEventListener('pointerup', event => {
    if (!holding || event.pointerId !== pointerId) return;
    event.preventDefault();
    const tap = isTap(performance.now() - start, travel, pipeline.rotation);
    stop();
    if (tap) pointer.click(); // motion already sent stays; a tap adds no movement of its own
  });
  pad.addEventListener('pointercancel', event => { if (holding && event.pointerId === pointerId) stop(); });
  pad.addEventListener('lostpointercapture', event => { if (holding && event.pointerId === pointerId) stop(); });
  settingsButton.addEventListener('click', () => { stop(); dialog.showModal(); });
  document.getElementById('close-air')!.addEventListener('click', () => dialog.close());
  function renderSettings() {
    speed.value = String(settings.speed); speedValue.textContent = `${settings.speed.toFixed(1)}×`;
    invertX.checked = settings.invertX; invertY.checked = settings.invertY;
  }
  speed.addEventListener('input', () => { settings = normalizeSettings({ ...settings, speed: Number(speed.value) }); renderSettings(); save(); });
  for (const box of [invertX, invertY]) box.addEventListener('change', () => { settings = normalizeSettings({ speed: settings.speed, invertX: invertX.checked, invertY: invertY.checked }); save(); });
  renderSettings();
  pointer.hook({ reset: () => stop(), enabled: value => { enabled = value; update(); } });
  new MutationObserver(() => update()).observe(panel, { attributes: true, attributeFilter: ['hidden'] });
  document.addEventListener('visibilitychange', () => update());
  document.getElementById('drag')!.addEventListener('click', () => render());
  render();
  return { stop };
}
