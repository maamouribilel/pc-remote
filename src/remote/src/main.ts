import { readCapabilities, renderLabels, supports, unavailable, labels, type Capabilities } from './capabilities';
import './style.css';
import { setupTouchpad } from './touchpad';
import { setupAirPointer } from './air-pointer';
import { setupKeyboard } from './keyboard';
import { setupMedia } from './media';
import { setupTabs } from './tabs';
import { setupIcons } from './icons';
import { setupConnectionPanel } from './connection-panel';
import { setupPc } from './pc';
setupIcons();
function element<T extends HTMLElement>(id: string): T {
  const value = document.getElementById(id);
  if (!value) throw new Error(`Missing UI element: ${id}`);
  return value as T;
}
const state = element('state'), dot = element('dot'), error = element('error');
const button = element<HTMLButtonElement>('test');
const latency = element('latency'), transport = element('transport');
const description = element('description');
const form = element<HTMLFormElement>('pair-form');
const link = element<HTMLInputElement>('pair-link'), name = element<HTMLInputElement>('device-name');
const submit = element<HTMLButtonElement>('pair-submit');
const takeover = element<HTMLButtonElement>('takeover'), forget = element<HTMLButtonElement>('forget');
const supplied = new URLSearchParams(location.hash.slice(1)).get('pair');
if (supplied) link.value = supplied;
if (location.hash) history.replaceState(null, '', location.pathname + location.search);
let socket: WebSocket | undefined;
let reconnect: ReturnType<typeof setTimeout> | undefined;
let heartbeat: ReturnType<typeof setInterval> | undefined;
let deadline: ReturnType<typeof setTimeout> | undefined;
let poll: ReturnType<typeof setTimeout> | undefined;
let attempts = 0, seq = 0, generation = 0;
let authenticated = false, pairing = false;
let controlReady = false, lastPing = 0;
let capabilities: Capabilities = unavailable;
// PC clock minus phone clock, from the HTTP Date header. Power deadlines are PC times.
let serverOffset = 0;
function sendControl(type: string, payload: object): number | false {
  if (!supports(capabilities, type, payload) || !controlReady || socket?.readyState !== WebSocket.OPEN) return false;
  if (socket.bufferedAmount > 16384) { socket.close(); return false; }
  socket.send(JSON.stringify({ v: 1, seq: ++seq, type, payload }));
  if (type === 'input.releaseAll') keyboard.reset();
  return seq;
}
const controls = setupTouchpad((type, payload) => Boolean(sendControl(type, payload)), () => (socket?.bufferedAmount ?? 0) > 16384);
setupAirPointer(controls);
const keyboard = setupKeyboard((type, payload) => controls.flush() ? sendControl(type, payload) : false, controls.release);
const media = setupMedia((type, payload) => controls.flush() ? sendControl(type, payload) : false, controls.release);
const tabs = setupTabs(controls.release);
const pc = setupPc(api, controls.release, media.profile, tabs.select, () => Date.now() + serverOffset);
const connectionPanel = setupConnectionPanel(controls.release);
function applyCapabilities(value: Capabilities) {
  capabilities = value; renderLabels(value); keyboard.setCapabilities(value); media.setCapabilities(value); pc.setCapabilities(value);
}
function enableControl(auth: boolean) {
  controlReady = auth; controls.setEnabled(auth && capabilities.pointer); keyboard.setEnabled(auth); media.setEnabled(auth);
  for (const button of document.querySelectorAll<HTMLButtonElement>('#mouse-controls button')) button.disabled = !auth || !capabilities.pointer;
}
const releaseButton = element<HTMLButtonElement>('release');
let pending: { sequence: number; started: number } | undefined;
// reopen=false lets heartbeats keep an explanation visible without undoing a manual close.
function display(message: string, connected = false, explanation?: string, reopen = true) {
  state.textContent = message; dot.classList.toggle('connected', connected);
  button.disabled = !connected || pending !== undefined;
  error.hidden = !explanation; error.textContent = explanation ?? '';
  releaseButton.disabled = !controlReady;
  if (explanation) { if (reopen) connectionPanel.open(true); }
  else if (controlReady) connectionPanel.closeAutomatic();
}
function clearTimers() { clearTimeout(reconnect); clearTimeout(deadline); clearInterval(heartbeat); clearTimeout(poll); }
async function api(path: string, body?: object) {
  const response = await fetch(path, { method: body === undefined ? 'GET' : 'POST',
    headers: body === undefined ? {} : { 'Content-Type': 'application/json' },
    body: body === undefined ? undefined : JSON.stringify(body), cache: 'no-store', signal: AbortSignal.timeout(5000) });
  const date = Date.parse(response.headers.get('Date') ?? '');
  if (Number.isFinite(date)) serverOffset = date - Date.now();
  // Only the global limiter means "wait a minute"; endpoint limits carry their own code.
  const result = response.status === 429 ? await response.json().catch(() => ({})) : await response.json();
  if (response.status === 429 && result.error !== 'launch_rate_limit') throw new Error('Too many requests. Wait one minute and try again.');
  return { ...result, ok: response.ok };
}
function retry() {
  clearTimeout(reconnect);
  if (!document.hidden) reconnect = setTimeout(connect, Math.min(1000 * 2 ** attempts++, 15000) + Math.random() * 300);
}
function ping() {
  if (socket?.readyState !== WebSocket.OPEN || pending) return;
  pending = { sequence: ++seq, started: performance.now() }; button.disabled = true;
  lastPing = performance.now();
  socket.send(JSON.stringify({ v: 1, seq, type: 'ping', payload: {} }));
  // A silently dead path may not fire onclose until the browser's own close timeout.
  // Detach the socket first so its late onclose is ignored, then reconnect now.
  deadline = setTimeout(() => { const current = socket; socket = undefined; disconnected('Connection timed out. Check the PC and Wi-Fi.'); current?.close(); }, 5000);
}
function disconnected(explanation?: string) {
  clearTimeout(deadline); clearInterval(heartbeat); pending = undefined;
  controlReady = false; controls.setEnabled(false); keyboard.setEnabled(false); media.setEnabled(false); pc.setConnection(false);
  transport.textContent = 'Disconnected';
  display(document.hidden ? 'Suspended' : 'Disconnected · retrying', false, explanation);
  retry();
}
function openSocket(auth: boolean, version: number) {
  const current = new WebSocket(`wss://${location.host}/${auth ? 'control' : 'diagnostics'}/ws`);
  socket = current; seq = 0; pending = undefined;
  current.onopen = () => {
    if (socket !== current || version !== generation) { current.close(); return; }
    transport.textContent = 'HTTPS + WSS'; ping(); heartbeat = setInterval(() => {
      if (performance.now() - lastPing >= (controls.held() || keyboard.held() ? 250 : 5000)) ping();
    }, 250);
  };
  current.onmessage = (event: MessageEvent<string>) => {
    if (socket !== current || version !== generation) return;
    try {
      const reply = JSON.parse(event.data);
      if (reply.v === 1 && reply.type === 'ack' && typeof reply.session === 'string' && keyboard.acknowledge(reply.seq)) return;
      if (!pending || reply.v !== 1 || reply.type !== 'pong' || reply.seq !== pending.sequence || typeof reply.session !== 'string') throw new Error();
      latency.textContent = `${Math.round(performance.now() - pending.started)} ms`;
      pending = undefined; clearTimeout(deadline); attempts = 0;
      if (auth && reply.payload?.capabilities) applyCapabilities(readCapabilities({ protocol: 1, capabilities: reply.payload.capabilities }));
      if (typeof reply.payload?.mediaProfile === 'string') media.profile(reply.payload.mediaProfile);
      pc.setConnection(auth, reply.session); if (auth) pc.sync(reply.payload ?? {});
      enableControl(auth);
      if (pairing) display('Waiting for PC approval', true);
      // Unpaired heartbeats keep the last pairing explanation visible until the person acts.
      else display(auth ? (Object.values(capabilities).some(v => v === true) || Object.values(capabilities.mediaActions).some(a => a.length) ? 'Connected securely' : 'Connected · controls unavailable') : 'Secure transport · unpaired', true,
        auth || error.hidden ? undefined : error.textContent ?? undefined, false);
    } catch { display('Invalid server reply'); current.close(); }
  };
  current.onerror = () => { if (socket === current) display('Connection failed', false, 'Check certificate trust, the Private firewall rule, and Wi-Fi client isolation.'); };
  current.onclose = event => {
    if (socket !== current || version !== generation) return;
    disconnected(event.reason === 'input_failed' ? 'Input is unavailable or failed. Check permissions and readiness in the companion.' : undefined);
  };
}
async function pollApproval(version: number) {
  try {
    const result = await api('/api/pair/poll', {});
    if (version !== generation || document.hidden) return;
    if (result.state === 'approved') { pairing = false; submit.disabled = false; connect(); return; }
    if (result.state === 'pending') {
      pairing = true; form.hidden = true; display('Waiting for PC approval', socket?.readyState === WebSocket.OPEN);
      poll = setTimeout(() => pollApproval(version), 2000); return;
    }
    pairing = false; form.hidden = false; submit.disabled = false;
    if (!['pair_invalid', 'none', 'claimed'].includes(result.state))
      display('Pairing unsuccessful', false, `${result.state}. Create a new link on the PC and try again.`);
  } catch (reason) {
    if (version !== generation) return;
    display('Approval connection lost', false, String(reason)); retry();
  }
}
async function connect(take = false) {
  controls.release(); controlReady = false; controls.setEnabled(false); keyboard.setEnabled(false); media.setEnabled(false); pc.setConnection(false);
  const version = ++generation; clearTimers(); socket?.close(); socket = undefined; pending = undefined;
  // Background retries keep Take over in place so the layout never shifts Forget under a tap.
  if (take) takeover.hidden = true;
  if (document.hidden) return;
  if (location.protocol !== 'https:') { display('HTTPS required', false, 'Use the trusted HTTPS address displayed on the PC.'); return; }
  display('Connecting…');
  try {
    const status = await api('/api/status');
    if (version !== generation || document.hidden) return;
    if (status.protocol !== 1) { applyCapabilities(unavailable); display('Unsupported companion protocol', false, 'Use a compatible companion and browser remote.'); return; }
    const wasAuthenticated = authenticated;
    authenticated = status.authenticated; form.hidden = authenticated; forget.hidden = !authenticated;
    if (!authenticated) takeover.hidden = true;
    if (authenticated && !wasAuthenticated) { (document.getElementById('connection-details') as HTMLDetailsElement).open = false; connectionPanel.close(); }
    if (!authenticated) connectionPanel.open(true);
    applyCapabilities(authenticated ? readCapabilities(status) : unavailable);
    media.profile(status.mediaProfile);
    const readiness = status.readiness?.message;
    document.getElementById('capability-note')!.textContent = authenticated ? readiness || 'Controls reflect this companion’s available capabilities.' : '';
    description.textContent = authenticated ? `Watch your ${capabilities.platform === 'macos' ? 'Mac' : 'computer'} screen. Choose an available control.` : 'Pair this browser and approve it on the PC to control it.';
    if (authenticated) {
      pairing = false;
      const claim = await api('/api/session/claim', { takeover: take });
      if (version !== generation || document.hidden) return;
      if (!claim.ok) {
        if (claim.error === 'unauthenticated') { takeover.hidden = true; display('Pairing expired or revoked'); retry(); return; }
        takeover.hidden = claim.error !== 'controller_busy';
        display(claim.error === 'paused' ? `Paused on ${labels(capabilities).computer}` : 'Another controller is active', false,
          claim.error === 'paused' ? `Resume control on the ${labels(capabilities).computer}.` : 'Take over only when you intend to replace the current controller.');
        retry(); return;
      }
      takeover.hidden = true; openSocket(true, version);
    } else { openSocket(false, version); await pollApproval(version); }
  } catch (reason) {
    if (version !== generation) return;
    takeover.hidden = true; display('Connection failed', false, String(reason)); retry();
  }
}
form.addEventListener('submit', async event => {
  event.preventDefault(); submit.disabled = true; clearTimeout(poll);
  try {
    let challenge = link.value.trim();
    if (challenge.includes('#')) challenge = new URLSearchParams(challenge.split('#')[1]).get('pair') ?? '';
    const result = await api('/api/pair/request', { challenge, name: name.value.trim() });
    link.value = '';
    if (!result.ok) throw new Error(`${result.error}. Create a new pairing link on the PC.`);
    pairing = true; form.hidden = true; display('Waiting for PC approval', socket?.readyState === WebSocket.OPEN);
    await pollApproval(generation);
  } catch (reason) { submit.disabled = false; form.hidden = false; display('Could not pair', false, String(reason)); }
});
button.addEventListener('click', ping);
takeover.addEventListener('click', () => connect(true));
forget.addEventListener('click', async () => {
  if (!window.confirm('Forget this browser? You will need to pair it with the PC again.')) return;
  try { await api('/api/logout', {}); connect(); } catch { display('Could not forget browser', false, 'Reconnect and try again.'); }
});
document.addEventListener('visibilitychange', () => {
  if (document.hidden) { controls.release(); controlReady = false; controls.setEnabled(false); keyboard.setEnabled(false); media.setEnabled(false); pc.setConnection(false); generation++; clearTimers(); socket?.close(); display('Suspended'); } else connect();
});
window.addEventListener('pagehide', () => { controls.release(); controlReady = false; controls.setEnabled(false); keyboard.setEnabled(false); media.setEnabled(false); pc.setConnection(false); generation++; clearTimers(); socket?.close(); });
window.addEventListener('pageshow', event => { if (event.persisted) connect(); });
connect();
