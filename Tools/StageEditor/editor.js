(() => {
  'use strict';
  const M = window.StageModel, $ = id => document.getElementById(id);
  const canvas = $('canvas'), ctx = canvas.getContext('2d'), wrap = $('canvas-wrap');
  const storageKey = 'betobeto.stage.v1';
  let stage = M.clone(M.sample), brush = '#', undo = [], redo = [], drawing = false, panning = false, space = false;
  let last = null, hover = null, strokeBefore = null, zoom = 1, baseCell = 40, pan = { x: 0, y: 0 }, dimensions = { w: 1, h: 1 }, toastTimer;
  try { const saved = localStorage.getItem(storageKey); if (saved) { const candidate = JSON.parse(saved); if (Array.isArray(candidate.rows) && candidate.rows.length === candidate.height && candidate.rows.every(row => typeof row === 'string' && row.length === candidate.width) && candidate.width >= 16 && candidate.width <= 32 && candidate.height >= 9 && candidate.height <= 18 && candidate.recipe) stage = candidate; } } catch (_) {}
  const fields = ['name', 'dessert', 'width', 'height', 'escapeLimit', 'spawnInterval', 'droolLifetime'];
  const ingredients = ['strawberry', 'blueberry', 'orange', 'melon'];
  function sync() {
    for (const key of fields) $(key).value = stage[key];
    for (const key of ingredients) $(key).value = stage.recipe[key];
    update();
  }
  function commit(before) {
    if (before !== JSON.stringify(stage)) { undo.push(before); if (undo.length > 60) undo.shift(); redo = []; }
    update();
  }
  function update() {
    $('map-name').textContent = stage.name; $('dimensions').textContent = stage.width + ' × ' + stage.height;
    $('undo').disabled = !undo.length; $('redo').disabled = !redo.length;
    const errors = M.validate(stage);
    $('validation').classList.toggle('invalid', errors.length > 0);
    $('validation-title').textContent = errors.length ? errors.length + '項目を調整してください' : '✓ Unityに読み込めるレイアウトです';
    $('errors').replaceChildren(...errors.map(error => { const li = document.createElement('li'); li.textContent = error; return li; }));
    const content = stage.rows.join('');
    const count = symbol => Array.from(content).filter(c => c === symbol).length;
    $('placement-counts').textContent = '壁 ' + count('#') + '　パイプ ' + count('P') + '　罠 ' + count('X') + '　出口 ' + count('E');
    try { localStorage.setItem(storageKey, JSON.stringify(stage)); $('save-state').textContent = 'この端末に自動保存'; } catch (_) { $('save-state').textContent = '自動保存不可 · JSONで保存してください'; }
    draw();
  }
  function history(backward) {
    const source = backward ? undo : redo, destination = backward ? redo : undo;
    if (!source.length) return;
    destination.push(JSON.stringify(stage)); stage = JSON.parse(source.pop()); sync();
  }
  function fit() { zoom = 1; pan = { x: 0, y: 0 }; resizeCanvas(); }
  function resizeCanvas() {
    dimensions = { w: wrap.clientWidth, h: wrap.clientHeight };
    const dpr = window.devicePixelRatio || 1;
    canvas.width = Math.round(dimensions.w * dpr); canvas.height = Math.round(dimensions.h * dpr);
    ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
    baseCell = Math.min((dimensions.w - 70) / stage.width, (dimensions.h - 125) / stage.height);
    draw();
  }
  function transform() {
    const cell = baseCell * zoom;
    return { cell, x: (dimensions.w - stage.width * cell) / 2 + pan.x, y: (dimensions.h - stage.height * cell) / 2 + pan.y + 12 };
  }
  function local(event) { const rect = canvas.getBoundingClientRect(); return { x: event.clientX - rect.left, y: event.clientY - rect.top }; }
  function grid(point) { const t = transform(); return { x: Math.floor((point.x - t.x) / t.cell), y: Math.floor((point.y - t.y) / t.cell) }; }
  function roundRect(x, y, w, h, radius, color) {
    ctx.fillStyle = color; ctx.beginPath(); ctx.roundRect(x, y, w, h, Math.max(0, radius)); ctx.fill();
  }
  function draw() {
    const { w, h } = dimensions, t = transform(), c = t.cell;
    ctx.clearRect(0, 0, w, h); ctx.fillStyle = '#193b4b'; ctx.fillRect(0, 0, w, h);
    ctx.fillStyle = '#244958';
    for (let x = 16; x < w; x += 23) for (let y = 16; y < h; y += 23) { ctx.beginPath(); ctx.arc(x, y, .65, 0, Math.PI * 2); ctx.fill(); }
    roundRect(t.x - 10, t.y - 10, stage.width * c + 20, stage.height * c + 20, 13, '#244e60');
    for (let y = 0; y < stage.height; y++) for (let x = 0; x < stage.width; x++) {
      const px = t.x + x * c, py = t.y + y * c, symbol = stage.rows[y][x];
      roundRect(px + 1, py + 1, c - 2, c - 2, c * .09, (x + y) % 2 ? '#669db3' : '#6ca4b9');
      const margin = c * .09, size = c * .82;
      if (symbol === '#') {
        roundRect(px + margin, py + margin + c * .04, size, size, c * .13, '#ac7649');
        roundRect(px + margin, py + margin, size, size - c * .05, c * .13, '#e8bc80');
        ctx.fillStyle = '#b78655';
        for (let i = 0; i < 3; i++) for (let j = 0; j < 3; j++) { ctx.beginPath(); ctx.arc(px + c * (.3 + i * .2), py + c * (.27 + j * .2), Math.max(1, c * .026), 0, 7); ctx.fill(); }
      } else if (symbol === 'P') {
        roundRect(px + c * .24, py + c * .08, c * .52, c * .84, c * .15, '#a1d6d0');
        roundRect(px + c * .18, py + c * .06, c * .64, c * .13, c * .045, '#f0d092');
        roundRect(px + c * .18, py + c * .8, c * .64, c * .13, c * .045, '#f0d092');
        ctx.fillStyle = '#487c77'; ctx.font = 'bold ' + c * .49 + 'px sans-serif'; ctx.textAlign = 'center'; ctx.textBaseline = 'middle'; ctx.fillText('↓', px + c * .5, py + c * .5);
      } else if (symbol === 'X') {
        roundRect(px + margin, py + margin, size, size, c * .13, '#e69cac');
        roundRect(px + c * .2, py + c * .2, c * .6, c * .6, c * .07, '#4c6470');
        ctx.save(); ctx.translate(px + c * .5, py + c * .5); ctx.fillStyle = '#cbdde0';
        for (let i = 0; i < 8; i++) { ctx.rotate(Math.PI / 4); ctx.fillRect(-c * .06, -c * .28, c * .14, c * .3); }
        ctx.fillStyle = '#f8e0ac'; ctx.beginPath(); ctx.arc(0, 0, c * .105, 0, 7); ctx.fill(); ctx.restore();
      } else if (symbol === 'E') {
        roundRect(px + c * .06, py + c * .06, c * .88, c * .88, c * .09, '#304d5c');
        for (let i = 1; i <= 3; i++) roundRect(px + c * (.17 + i * .15), py + c * .18, c * .035, c * .54, c * .01, '#9fbec6');
        roundRect(px + c * .28, py + c * .83, c * .44, c * .08, c * .015, '#eca8b6');
      } else if (symbol === 'G') {
        roundRect(px + c * .22, py + c * .13, c * .56, c * .7, c * .26, '#fff6ed');
        ctx.fillStyle = '#374f60';
        for (const offset of [.39, .61]) { ctx.beginPath(); ctx.ellipse(px + c * offset, py + c * .38, c * .035, c * .064, 0, 0, 7); ctx.fill(); }
        roundRect(px + c * .31, py + c * .56, c * .38, c * .23, c * .06, '#c9a8c2');
      }
    }
    ctx.fillStyle = '#86a8b3'; ctx.font = '9px system-ui'; ctx.textAlign = 'center'; ctx.textBaseline = 'middle';
    for (let x = 0; x < stage.width; x++) ctx.fillText(x, t.x + (x + .5) * c, t.y + stage.height * c + 23);
    for (let y = 0; y < stage.height; y++) ctx.fillText(y, t.x - 23, t.y + (y + .5) * c);
    if (hover && hover.x >= 0 && hover.y >= 0 && hover.x < stage.width && hover.y < stage.height) {
      ctx.strokeStyle = '#fff4d7'; ctx.lineWidth = 2; ctx.strokeRect(t.x + hover.x * c + 2, t.y + hover.y * c + 2, c - 4, c - 4);
      ctx.fillStyle = '#fff7e820'; ctx.fillRect(t.x + hover.x * c, t.y + hover.y * c, c, c);
    }
    $('zoom').textContent = Math.round(zoom * 100) + '%';
  }
  function selectBrush(symbol) { brush = symbol; document.querySelectorAll('.tool').forEach(button => button.classList.toggle('active', button.dataset.brush === brush)); }
  function toast(message) { clearTimeout(toastTimer); $('toast').textContent = message; $('toast').classList.add('visible'); toastTimer = setTimeout(() => $('toast').classList.remove('visible'), 2600); }
  function paintAt(cell, symbol) {
    if (!cell || cell.x < 0 || cell.y < 0 || cell.x >= stage.width || cell.y >= stage.height) return;
    if (symbol === 'P' && cell.y !== 0) { toast('パイプは最上段に配置します'); return; }
    if (symbol === 'E' && cell.x !== 0 && cell.y !== 0 && cell.x !== stage.width - 1 && cell.y !== stage.height - 1) { toast('出口は外周に配置します'); return; }
    M.paint(stage, cell.x, cell.y, symbol);
  }
  function paintLine(a, b, symbol) {
    if (!a) { paintAt(b, symbol); return; }
    const distance = Math.max(Math.abs(b.x - a.x), Math.abs(b.y - a.y));
    for (let i = 0; i <= distance; i++) { const ratio = distance === 0 ? 1 : i / distance; paintAt({ x: Math.round(a.x + (b.x - a.x) * ratio), y: Math.round(a.y + (b.y - a.y) * ratio) }, symbol); }
  }
  function endStroke() { if (drawing && strokeBefore !== null) commit(strokeBefore); drawing = panning = false; strokeBefore = last = null; canvas.style.cursor = space ? 'grab' : 'crosshair'; }
  function zoomAt(factor, point = { x: dimensions.w / 2, y: dimensions.h / 2 }) {
    const before = transform(), gx = (point.x - before.x) / before.cell, gy = (point.y - before.y) / before.cell;
    zoom = Math.min(3.5, Math.max(.45, zoom * factor));
    const after = transform(); pan.x += point.x - (after.x + gx * after.cell); pan.y += point.y - (after.y + gy * after.cell); draw();
  }
  canvas.addEventListener('contextmenu', e => e.preventDefault());
  canvas.addEventListener('pointerdown', e => {
    e.preventDefault(); canvas.focus(); canvas.setPointerCapture(e.pointerId);
    if (e.button === 1 || (space && e.button === 0)) { panning = true; last = local(e); canvas.style.cursor = 'grabbing'; return; }
    if (e.button !== 0 && e.button !== 2) return;
    drawing = true; strokeBefore = JSON.stringify(stage); last = grid(local(e)); paintAt(last, e.button === 2 ? '.' : brush); draw();
  });
  canvas.addEventListener('pointermove', e => {
    const point = local(e); hover = grid(point); $('coords').textContent = 'X ' + hover.x + '   Y ' + hover.y + '   · 原点：左上';
    if (panning) { pan.x += point.x - last.x; pan.y += point.y - last.y; last = point; }
    else if (drawing) { paintLine(last, hover, (e.buttons & 2) ? '.' : brush); last = hover; }
    draw();
  });
  canvas.addEventListener('pointerup', endStroke); canvas.addEventListener('pointercancel', endStroke); canvas.addEventListener('lostpointercapture', endStroke);
  canvas.addEventListener('pointerleave', () => { if (!drawing && !panning) { hover = null; draw(); } });
  canvas.addEventListener('wheel', e => { e.preventDefault(); zoomAt(Math.exp(-e.deltaY * .0015), local(e)); }, { passive: false });
  window.addEventListener('blur', () => { space = false; endStroke(); });
  document.querySelectorAll('.tool').forEach(button => button.addEventListener('click', () => selectBrush(button.dataset.brush)));
  $('undo').onclick = () => history(true); $('redo').onclick = () => history(false); $('fit').onclick = fit;
  $('zoom-in').onclick = () => zoomAt(1.15); $('zoom-out').onclick = () => zoomAt(1 / 1.15);
  document.addEventListener('keydown', e => {
    if (e.target.matches('input,textarea,select')) return;
    if ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === 'z') { e.preventDefault(); history(!e.shiftKey); }
    else if ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === 'y') { e.preventDefault(); history(false); }
    else if (e.code === 'Space') { e.preventDefault(); space = true; canvas.style.cursor = 'grab'; }
    else if ('123456'.includes(e.key) && e.key.length === 1) selectBrush(['#', '.', 'P', 'X', 'E', 'G'][Number(e.key) - 1]);
    else if (e.key.toLowerCase() === 'f') fit();
  });
  document.addEventListener('keyup', e => { if (e.code === 'Space') { space = false; if (!panning) canvas.style.cursor = 'crosshair'; } });
  for (const key of fields.filter(k => k !== 'width' && k !== 'height')) $(key).addEventListener('change', () => {
    const before = JSON.stringify(stage); stage[key] = key === 'name' || key === 'dessert' ? $(key).value : Number($(key).value); commit(before);
  });
  for (const key of ingredients) $(key).addEventListener('change', () => { const before = JSON.stringify(stage); stage.recipe[key] = Number($(key).value); commit(before); });
  $('resize').onclick = () => { try { const before = JSON.stringify(stage); stage = M.resize(stage, Number($('width').value), Number($('height').value)); commit(before); sync(); fit(); toast('サイズを適用しました。Ctrl+Zで元に戻せます'); } catch (e) { toast(e.message); } };
  function replace(next) { const before = JSON.stringify(stage); stage = next; commit(before); sync(); fit(); }
  $('new').onclick = () => { replace(M.blank()); toast('新しい盤面を作成しました。Ctrl+Zで元に戻せます'); };
  $('sample').onclick = () => { replace(M.clone(M.sample)); toast('サンプルを読み込みました'); };
  $('load').onclick = () => $('file').click();
  $('file').addEventListener('change', async () => {
    const file = $('file').files[0]; if (!file) return;
    try {
      if (file.size > 1024 * 1024) throw new Error('JSONは1MB以下にしてください。');
      const next = JSON.parse(await file.text()), errors = M.validate(next);
      if (errors.length) throw new Error(errors.join(' / '));
      replace(next); toast('JSONを読み込みました');
    } catch (e) { toast('読み込み失敗: ' + e.message); }
    $('file').value = '';
  });
  $('export').onclick = () => {
    const errors = M.validate(stage); if (errors.length) { toast('書き出す前に、下の検証項目を修正してください'); return; }
    const url = URL.createObjectURL(new Blob([JSON.stringify(stage, null, 2) + '\n'], { type: 'application/json' }));
    const a = document.createElement('a'); a.href = url; a.download = (stage.name.replace(/[\\/:*?"<>|]/g, '-') || 'kitchen') + '.json';
    document.body.appendChild(a); a.click(); a.remove(); setTimeout(() => URL.revokeObjectURL(url), 1000); toast('JSONを書き出しました');
  };
  new ResizeObserver(resizeCanvas).observe(wrap);
  sync(); fit();
})();
