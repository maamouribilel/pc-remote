import { quantize } from './air-math';
export type Sender = (type: string, payload: object) => boolean;
export function setupTouchpad(send: Sender, congested: () => boolean) {
  const pad = document.getElementById('touchpad')!;
  const pads = [pad, document.getElementById('air-pad')!];
  const surface = document.getElementById('mouse-controls')!;
  const drag = document.getElementById('drag') as HTMLButtonElement;
  const hint = document.getElementById('pad-hint')!;
  const dragLabel = drag.querySelector('.button-label')!;
  let enabled = false, dragging = false, multiple = false;
  let moveX = 0, moveY = 0, scrollX = 0, scrollY = 0, frame = 0;
  let start = 0, distance = 0, lastFlush = -Infinity;
  const hooks: { reset?: () => void; enabled?: (value: boolean) => void } = {};
  const carry = (value: number) => Math.abs(value) < 0.005 ? 0 : value;
  const touches = new Map<number, { x: number; y: number }>();
  const active = new Set<number>();
  let blocked = false;
  function reset() {
    touches.clear(); multiple = false; dragging = false;
    moveX = moveY = scrollX = scrollY = 0; cancelAnimationFrame(frame); frame = 0;
    drag.setAttribute('aria-pressed', 'false'); dragLabel.textContent = 'Start drag'; for (const p of pads) p.classList.remove('dragging');
    hint.textContent = 'Move · tap · two-finger scroll';
    hooks.reset?.();
  }
  function release() { if (enabled) send('input.releaseAll', {}); reset(); }
  function flush(transition = false): boolean {
    if (congested()) {
      // Keep at most one bounded aggregate, never a stale frame queue.
      if (transition) { release(); return false; }
      return true;
    }
    const now = performance.now();
    // At most one aggregate every 8 ms (≤125 frames/s) keeps high-refresh phones under the PC's message budget.
    if (!transition && now - lastFlush < 8) return true;
    const move = [quantize(moveX), quantize(moveY)], scroll = [quantize(scrollX), quantize(scrollY)];
    for (const [type, x, y] of [['pointer.move', move[0], move[1]], ['pointer.scroll', scroll[0], scroll[1]]] as const) {
      if ((x || y) && !send(type, { dx: x, dy: y })) { reset(); return false; }
    }
    // Two-decimal frames stay small; sub-0.01 remainders (slow air motion) carry over instead of being dropped.
    moveX = carry(moveX - move[0]); moveY = carry(moveY - move[1]); scrollX = carry(scrollX - scroll[0]); scrollY = carry(scrollY - scroll[1]);
    lastFlush = now; return true;
  }
  function schedule() {
    if (frame) return;
    frame = requestAnimationFrame(() => {
      frame = 0; if (!enabled) return;
      flush(); if (moveX || moveY || scrollX || scrollY) schedule();
    });
  }
  function button(button: string, action: string) {
    if (!enabled || !flush(true)) return false;
    return send('pointer.button', { button, action });
  }
  pad.addEventListener('pointerdown', event => {
    if (!enabled || event.pointerType !== 'touch') return;
    event.preventDefault(); pad.setPointerCapture(event.pointerId);
    active.add(event.pointerId); if (blocked) return;
    touches.set(event.pointerId, { x: event.clientX, y: event.clientY });
    if (touches.size === 1) { start = performance.now(); distance = 0; multiple = false; }
    if (touches.size >= 2) { flush(true); multiple = true; }
    if (touches.size >= 3) { blocked = true; release(); }
  });
  pad.addEventListener('pointermove', event => {
    const previous = touches.get(event.pointerId);
    if (!enabled || !previous) return;
    event.preventDefault();
    const dx = event.clientX - previous.x, dy = event.clientY - previous.y;
    touches.set(event.pointerId, { x: event.clientX, y: event.clientY }); distance += Math.hypot(dx, dy);
    if (touches.size === 2 && !dragging) {
      scrollX = Math.max(-1200, Math.min(1200, scrollX + dx * 1.5));
      scrollY = Math.max(-1200, Math.min(1200, scrollY + dy * 1.5));
    } else if (touches.size === 1 && !multiple) {
      moveX = Math.max(-500, Math.min(500, moveX + dx)); moveY = Math.max(-500, Math.min(500, moveY + dy));
    }
    schedule();
  });
  pad.addEventListener('pointerup', event => {
    active.delete(event.pointerId);
    if (blocked) { if (active.size === 0) blocked = false; return; }
    if (!touches.has(event.pointerId)) return;
    event.preventDefault(); flush(true); touches.delete(event.pointerId);
    if (touches.size === 0) {
      if (!multiple && !dragging && distance < 8 && performance.now() - start < 250) button('left', 'click');
      multiple = false;
    }
  });
  pad.addEventListener('pointercancel', event => { active.delete(event.pointerId); blocked = active.size > 0; release(); });
  pad.addEventListener('lostpointercapture', event => { if (touches.has(event.pointerId)) release(); });
  for (const id of ['left-click', 'right-click', 'double-click']) {
    document.getElementById(id)!.addEventListener('click', () => {
      if (dragging) return; button(id === 'right-click' ? 'right' : 'left', id === 'double-click' ? 'double' : 'click');
    });
  }
  drag.addEventListener('click', () => {
    if (button('left', dragging ? 'up' : 'down')) {
      dragging = !dragging; drag.setAttribute('aria-pressed', String(dragging));
      dragLabel.textContent = dragging ? 'End drag' : 'Start drag'; for (const p of pads) p.classList.toggle('dragging', dragging);
      hint.textContent = dragging ? 'Dragging · move, then End drag' : 'Move · tap · two-finger scroll';
    }
  });
  document.getElementById('release')!.addEventListener('click', release);
  return {
    setEnabled(value: boolean) { if (!value) { reset(); active.clear(); blocked = false; } enabled = value; surface.inert = !value; surface.classList.toggle('disabled', !value); hooks.enabled?.(value); },
    release, flush: () => flush(true), held: () => dragging,
    // Air pointer hooks: relative pixel deltas join the same per-frame aggregate; a tap reuses the left click.
    motion(dx: number, dy: number) {
      if (!enabled) return;
      moveX = Math.max(-500, Math.min(500, moveX + dx)); moveY = Math.max(-500, Math.min(500, moveY + dy)); schedule();
    },
    click: () => button('left', 'click'),
    hook(callbacks: { reset?: () => void; enabled?: (value: boolean) => void }) { Object.assign(hooks, callbacks); },
  };
}
