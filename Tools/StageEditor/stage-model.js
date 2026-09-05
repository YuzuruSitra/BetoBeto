(function (root, factory) {
  const api = factory();
  if (typeof module === "object" && module.exports) module.exports = api;
  else root.StageModel = api;
})(typeof globalThis !== "undefined" ? globalThis : this, function () {
  "use strict";
  const sample = {
  "version": 1,
  "name": "はじめてのキッチン",
  "dessert": "フルーツタルト",
  "width": 16,
  "height": 9,
  "rows": [
    "###P########P###",
    "#1.....CXC....2#",
    "#..............#",
    "#JCCCX....XCCCJ#",
    "#1.C...JJ...C.2#",
    "#....4.JJ.3....#",
    "#.......G......#",
    "#4.....H......3#",
    "##.####..####.##"
  ],
  "recipe": {
    "strawberry": 5,
    "blueberry": 4,
    "orange": 3,
    "melon": 1
  },
  "escapeLimit": 10,
  "spawnInterval": 3.6,
  "iceLifetime": 5,
  "droolLifetime": 10,
  "cookieHits": 3,
  "cookieRespawnSeconds": 20,
  "sconeRespawnSeconds": 5,
  "movingShredderSpeed": 1,
  "freezerSeconds": 3,
  "frozenSpeedMultiplier": 0.35
};
  const symbols = ".#PXGJCHV1234F";
  const gimmickDefaults = { cookieHits: 3, cookieRespawnSeconds: 20, sconeRespawnSeconds: 5, movingShredderSpeed: 1, freezerSeconds: 3, frozenSpeedMultiplier: .35 };
  const isScone = c => '1234'.includes(c);
  const isShredder = c => 'XHV'.includes(c);
  const blocksConnectivity = c => c === '#' || c === 'J' || c === 'P';
  const clone = value => {
    const copy = JSON.parse(JSON.stringify(value));
    if (copy && typeof copy === 'object' && !Array.isArray(copy) && copy.sconeRespawnSeconds === undefined) {
      if (copy.cookieRespawnSeconds === undefined || copy.cookieRespawnSeconds === 5) copy.cookieRespawnSeconds = gimmickDefaults.cookieRespawnSeconds;
      copy.sconeRespawnSeconds = gimmickDefaults.sconeRespawnSeconds;
    }
    if (copy && Array.isArray(copy.rows)) copy.rows = copy.rows.map(row => typeof row === 'string' ? row.replaceAll('E', '.') : row);
    return copy;
  };
  function validate(s) {
    const errors = [];
    if (!s || typeof s !== "object" || Array.isArray(s)) return ["ステージJSONはオブジェクトである必要があります。"];
    if (s.version !== 1) errors.push("対応していないJSONバージョンです。");
    if (!Number.isInteger(s.width) || s.width < 16 || s.width > 32 || !Number.isInteger(s.height) || s.height < 9 || s.height > 18) errors.push("盤面は幅16〜32、高さ9〜18にしてください。");
    if (typeof s.name !== "string" || !s.name.trim()) errors.push("ステージ名を入力してください。");
    if (typeof s.dessert !== "string" || !s.dessert.trim()) errors.push("スイーツ名を入力してください。");
    if (!Array.isArray(s.rows) || s.rows.length !== s.height) return errors.concat("行数が高さと一致しません。");
    let pipes = 0, shredders = 0, ghosts = 0;
    const pipeCells = [];
    s.rows.forEach((row, y) => {
      if (typeof row !== "string" || row.length !== s.width) { errors.push((y + 1) + "行目の幅が一致しません。"); return; }
      Array.from(row).forEach((c, x) => {
        if (!symbols.includes(c) && c !== 'E') errors.push("不明な記号: " + c);
        if (c === "P") { pipes++; pipeCells.push([x, y]); if (y !== 0) errors.push("パイプは最上段に置いてください。"); }
        if (isShredder(c)) shredders++;
        if (c === "G") ghosts++;
      });
    });
    if (pipes < 2 || pipes > 3) errors.push("パイプは2〜3基必要です。");
    if (shredders < 4 || shredders > 8) errors.push("シュレッダーは4〜8箇所必要です。");
    if (ghosts !== 1) errors.push("ゴースト開始位置は1箇所必要です。");
    if (!Number.isInteger(s.escapeLimit) || s.escapeLimit < 1 || s.escapeLimit > 99) errors.push("脱出上限は1〜99にしてください。");
    for (const [key, min, max, label] of [["spawnInterval", .5, 30, "出現間隔"], ["iceLifetime", 1, 30, "氷の寿命"], ["droolLifetime", 1, 60, "よだれの寿命"]]) {
      if (!Number.isFinite(s[key]) || s[key] < min || s[key] > max) errors.push(label + "は" + min + "〜" + max + "秒にしてください。");
    }
    const hits = s.cookieHits === undefined ? gimmickDefaults.cookieHits : s.cookieHits;
    if (!Number.isInteger(hits) || hits < 1 || hits > 10) errors.push('クッキーの耐久は1〜10回にしてください。');
    for (const [key, min, max, label] of [['cookieRespawnSeconds', 1, 30, 'クッキーの復帰時間'], ['sconeRespawnSeconds', 1, 30, 'スコーンの復帰時間'], ['movingShredderSpeed', .25, 3, '移動シュレッダーの速度'], ['freezerSeconds', .5, 10, '凍結時間'], ['frozenSpeedMultiplier', .1, .9, '凍結中の速度倍率']]) {
      const value = s[key] === undefined ? gimmickDefaults[key] : s[key];
      if (!Number.isFinite(value) || value < min || value > max) errors.push(label + 'は' + min + '〜' + max + 'にしてください。');
    }
    const recipe = s.recipe, keys = ["strawberry", "blueberry", "orange", "melon"];
    if (!recipe || keys.some(k => !Number.isInteger(recipe[k]) || recipe[k] < 0) || keys.reduce((total, k) => total + recipe[k], 0) < 1 || keys.reduce((total, k) => total + recipe[k], 0) > 200) errors.push("必要フルーツ数は各0以上、合計1〜200にしてください。");
    if (errors.length === 0) {
      for (const [px, py] of pipeCells) {
        const queue = [[px, py]], seen = new Set([px + "," + py]);
        let shredder = false, exit = false;
        for (let i = 0; i < queue.length; i++) {
          const [x, y] = queue[i], c = s.rows[y][x];
          if (isShredder(c)) shredder = true;
          if (c !== 'P' && (y === 0 || y === s.height - 1 || x === 0 || x === s.width - 1)) exit = true;
          for (const [dx, dy] of [[1, 0], [-1, 0], [0, 1], [0, -1]]) {
            if (c === 'P' && (dx !== 0 || dy !== 1)) continue;
            const nx = x + dx, ny = y + dy, id = nx + "," + ny;
            if (nx >= 0 && ny >= 0 && nx < s.width && ny < s.height && !blocksConnectivity(s.rows[ny][nx]) && !seen.has(id)) { seen.add(id); queue.push([nx, ny]); }
          }
        }
        if (!shredder) errors.push("パイプ(" + px + "," + py + ")からシュレッダーへ到達できません。");
        if (!exit) errors.push("パイプ(" + px + "," + py + ")から盤外へ到達できません。");
      }
    }
    return Array.from(new Set(errors));
  }
  function paint(s, x, y, symbol) {
    if (x < 0 || y < 0 || x >= s.width || y >= s.height || !symbols.includes(symbol)) return false;
    if (symbol === "P" && y !== 0) return false;
    if (s.rows[y][x] === symbol) return false;
    if (symbol === "G") s.rows = s.rows.map(row => row.replaceAll("G", "."));
    const chars = Array.from(s.rows[y]); chars[x] = symbol; s.rows[y] = chars.join("");
    return true;
  }
  function resize(s, width, height) {
    if (!Number.isInteger(width) || !Number.isInteger(height) || width < 16 || width > 32 || height < 9 || height > 18) throw new Error("幅16〜32、高さ9〜18の整数を指定してください。");
    const result = clone(s);
    result.rows = Array.from({ length: height }, (_, y) => Array.from({ length: width }, (_, x) => y < s.height && x < s.width ? s.rows[y][x] : (x === 0 || y === 0 || x === width - 1 || y === height - 1 ? "#" : ".")).join(""));
    result.width = width; result.height = height;
    return result;
  }
  function blank() {
    const s = clone(sample); s.name = "新しいキッチン";
    s.rows = Array.from({ length: s.height }, (_, y) => Array.from({ length: s.width }, (_, x) => x === 0 || y === 0 || x === s.width - 1 || y === s.height - 1 ? "#" : ".").join(""));
    for (const [x, y, c] of [[3, 0, "P"], [12, 0, "P"], [5, 3, "X"], [10, 3, "X"], [4, 6, "X"], [11, 6, "X"], [7, 5, "G"], [7, 8, "."], [8, 8, "."]]) paint(s, x, y, c);
    return s;
  }
  return { sample, clone, validate, paint, resize, blank, gimmickDefaults, isScone, isShredder, blocksConnectivity };
});
