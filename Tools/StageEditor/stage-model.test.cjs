const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const M = require('./stage-model.js');
const source = JSON.parse(fs.readFileSync(path.join(__dirname, '../../Assets/BetoBeto/Stages/kitchen-01.json'), 'utf8'));
let passed = 0;
function test(name, body) { body(); passed++; console.log('PASS ' + name); }
test('Bundled sample exactly matches Unity JSON', () => assert.deepEqual(M.sample, source));
test('Sample and blank layouts are valid', () => { assert.deepEqual(M.validate(M.sample), []); assert.deepEqual(M.validate(M.blank()), []); });
test('JSON round trip preserves data', () => assert.deepEqual(JSON.parse(JSON.stringify(M.sample)), source));
test('Moving the ghost leaves exactly one start', () => { const s = M.clone(source); M.paint(s, 4, 4, 'G'); assert.equal(s.rows.join('').split('G').length - 1, 1); });
test('Pipe and exit reject interior placement', () => { const s = M.clone(source); assert.equal(M.paint(s, 5, 5, 'P'), false); assert.equal(M.paint(s, 5, 5, 'E'), false); });
test('Invalid JSON structures and missing rules are rejected', () => { for (const s of [null, [], {}, { ...source, rows: ['bad'] }, { ...source, recipe: null }, { ...source, spawnInterval: null }, { ...source, escapeLimit: 1.5 }]) assert.ok(M.validate(s).length > 0); });
test('Disconnected pipes are rejected', () => { const s = M.clone(source); s.rows[1] = '#'.repeat(16); assert.ok(M.validate(s).some(e => e.includes('到達できません'))); });
test('Resize preserves interior cells and allows undo snapshot', () => { const s = M.clone(source), snapshot = JSON.stringify(s), larger = M.resize(s, 24, 14); assert.equal(larger.rows[5][7], 'G'); assert.equal(larger.rows.length, 14); assert.equal(larger.rows[13].length, 24); assert.deepEqual(JSON.parse(snapshot), source); });
test('Unsupported dimensions are rejected', () => { assert.throws(() => M.resize(source, 33, 18)); assert.throws(() => M.resize(source, 16.5, 9)); });
test('Undo restores a painted stroke without mutating sample', () => { const s = M.clone(source), before = JSON.stringify(s); M.paint(s, 1, 1, '#'); assert.notDeepEqual(s, source); assert.deepEqual(JSON.parse(before), source); });
test('All gimmick symbols can be painted and round trip', () => {
  const s = M.blank(); for (const [i, c] of Array.from('JC1234F').entries()) assert.equal(M.paint(s, i + 2, 2, c), true);
  M.paint(s, 5, 3, 'H'); M.paint(s, 10, 3, 'V');
  assert.deepEqual(M.validate(s), []); assert.deepEqual(JSON.parse(JSON.stringify(s)), s);
});
test('Moving blades count toward the four-to-eight blade limit', () => {
  const s = M.blank(); s.rows = s.rows.map(row => row.replaceAll('X', 'H'));
  assert.deepEqual(M.validate(s), []); M.paint(s, 1, 1, 'V'); assert.deepEqual(M.validate(s), []);
  for (let x = 2; x <= 5; x++) M.paint(s, x, 1, 'V');
  assert.ok(M.validate(s).some(e => e.includes('4〜8')));
});
test('Older version 1 maps use default gimmick parameters', () => {
  const s = M.clone(source); for (const key of Object.keys(M.gimmickDefaults)) delete s[key];
  assert.deepEqual(M.validate(s), []);
});
test('Invalid gimmick timings and multipliers are rejected', () => {
  for (const [key, value] of [['cookieHits', 1.5], ['cookieRespawnSeconds', 0], ['movingShredderSpeed', 4], ['freezerSeconds', null], ['frozenSpeedMultiplier', 1]])
    assert.ok(M.validate({ ...source, [key]: value }).length > 0, key);
});
test('Connectivity sees jelly and scones as solid but cookies as breakable', () => {
  const s = M.blank(); s.rows[1] = 'J'.repeat(16); assert.ok(M.validate(s).some(e => e.includes('到達できません')));
  s.rows[1] = '1'.repeat(16); assert.ok(M.validate(s).some(e => e.includes('到達できません')));
  s.rows[1] = 'C'.repeat(16); assert.deepEqual(M.validate(s), []);
});
test('Both playable examples keep their compact dimensions and include all five gimmicks', () => {
  const second = JSON.parse(fs.readFileSync(path.join(__dirname, '../../Assets/BetoBeto/Stages/kitchen-02.json'), 'utf8'));
  assert.deepEqual([source.width, source.height, second.width, second.height], [16, 9, 20, 12]);
  for (const stage of [source, second]) {
    assert.deepEqual(M.validate(stage), []);
    for (const group of ['J', 'C', 'HV', '1234', 'F']) assert.ok(Array.from(stage.rows.join('')).some(c => group.includes(c)), group);
  }
});
console.log(`${passed} tests passed.`);
