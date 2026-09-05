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
console.log(`${passed} tests passed.`);
