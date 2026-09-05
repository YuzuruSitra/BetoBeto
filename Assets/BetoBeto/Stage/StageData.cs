using System;
using System.Collections.Generic;
using UnityEngine;

namespace BetoBeto.Stage
{
    [Serializable]
    public sealed class Recipe
    {
        public int strawberry = 5;
        public int blueberry = 4;
        public int orange = 3;
        public int melon = 1;
        public int Total => strawberry + blueberry + orange + melon;
        public int For(Core.FruitKind kind) => kind switch
        {
            Core.FruitKind.Strawberry => strawberry,
            Core.FruitKind.Blueberry => blueberry,
            Core.FruitKind.Orange => orange,
            _ => melon
        };
    }

    /// <summary>Versioned contract shared with Tools/StageEditor. Row zero is the north edge.</summary>
    [Serializable]
    public sealed class StageData
    {
        public int version = 1;
        public string name = "はじめてのキッチン";
        public string dessert = "フルーツタルト";
        public int width = 16;
        public int height = 9;
        public string[] rows;
        public Recipe recipe = new Recipe();
        public int escapeLimit = 10;
        public float spawnInterval = 3.6f;
        // Retained for version 1 JSON compatibility; the scare ability does not use this value.
        [HideInInspector] public float iceLifetime = 5f;
        public float droolLifetime = 10f;

        public bool Contains(Vector2Int cell) => cell.x >= 0 && cell.y >= 0 && cell.x < width && cell.y < height;
        public char At(Vector2Int cell) => Contains(cell) ? rows[cell.y][cell.x] : 'E';
        public Vector3 World(Vector2Int cell, float y = 0) => new Vector3(cell.x - (width - 1) * .5f, y, (height - 1) * .5f - cell.y);
        public Vector2Int Cell(Vector3 world) => new Vector2Int(Mathf.RoundToInt(world.x + (width - 1) * .5f), Mathf.RoundToInt((height - 1) * .5f - world.z));

        public List<Vector2Int> Find(char symbol)
        {
            var result = new List<Vector2Int>();
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    if (rows[y][x] == symbol) result.Add(new Vector2Int(x, y));
            return result;
        }

        public List<string> Validate()
        {
            var errors = new List<string>();
            if (version != 1) errors.Add("対応していないJSONバージョンです。");
            if (width < 16 || width > 32 || height < 9 || height > 18) errors.Add("盤面は幅16〜32、高さ9〜18にしてください。");
            if (string.IsNullOrWhiteSpace(name)) errors.Add("ステージ名を入力してください。");
            if (string.IsNullOrWhiteSpace(dessert)) errors.Add("スイーツ名を入力してください。");
            if (rows == null || rows.Length != height) { errors.Add("行数が高さと一致しません。"); return errors; }
            int pipes = 0, shredders = 0, ghosts = 0;
            for (int y = 0; y < rows.Length; y++)
            {
                if (rows[y] == null || rows[y].Length != width) { errors.Add($"{y + 1}行目の幅が一致しません。"); continue; }
                for (int x = 0; x < width; x++)
                {
                    char c = rows[y][x];
                    if (".#PXEG".IndexOf(c) < 0) errors.Add($"不明な記号: {c}");
                    if (c == 'P') { pipes++; if (y != 0) errors.Add("パイプは最上段に置いてください。"); }
                    if (c == 'X') shredders++;
                    if (c == 'G') ghosts++;
                    if (c == 'E' && x != 0 && y != 0 && x != width - 1 && y != height - 1) errors.Add("出口は外周に置いてください。");
                }
            }
            if (pipes < 2 || pipes > 3) errors.Add("パイプは2〜3基必要です。");
            if (shredders < 4 || shredders > 8) errors.Add("シュレッダーは4〜8箇所必要です。");
            if (ghosts != 1) errors.Add("ゴースト開始位置は1箇所必要です。");
            if (escapeLimit < 1 || escapeLimit > 99) errors.Add("脱出上限は1〜99にしてください。");
            if (float.IsNaN(spawnInterval) || float.IsInfinity(spawnInterval) || spawnInterval < .5f || spawnInterval > 30f) errors.Add("出現間隔は0.5〜30秒にしてください。");
            if (float.IsNaN(iceLifetime) || float.IsInfinity(iceLifetime) || iceLifetime < 1 || iceLifetime > 30) errors.Add("氷の寿命は1〜30秒にしてください。");
            if (float.IsNaN(droolLifetime) || float.IsInfinity(droolLifetime) || droolLifetime < 1 || droolLifetime > 60) errors.Add("よだれの寿命は1〜60秒にしてください。");
            if (recipe == null || recipe.strawberry < 0 || recipe.blueberry < 0 || recipe.orange < 0 || recipe.melon < 0 || recipe.Total < 1 || recipe.Total > 200)
                errors.Add("必要フルーツ数は各0以上、合計1〜200にしてください。");
            if (errors.Count == 0)
            {
                foreach (var pipe in Find('P'))
                {
                    var visited = new HashSet<Vector2Int> { pipe };
                    var pending = new Queue<Vector2Int>();
                    pending.Enqueue(pipe);
                    bool reachesShredder = false, reachesExit = false;
                    while (pending.Count > 0)
                    {
                        var c = pending.Dequeue();
                        if (At(c) == 'X') reachesShredder = true;
                        if (At(c) == 'E' || (c.y == height - 1 || c.x == 0 || c.x == width - 1)) reachesExit = true;
                        foreach (var direction in Directions.All)
                        {
                            var next = c + direction;
                            if (Contains(next) && At(next) != '#' && visited.Add(next)) pending.Enqueue(next);
                        }
                    }
                    if (!reachesShredder) errors.Add($"パイプ({pipe.x},{pipe.y})からシュレッダーへ到達できません。");
                    if (!reachesExit) errors.Add($"パイプ({pipe.x},{pipe.y})から出口へ到達できません。");
                }
            }
            return errors;
        }

        public static StageData Parse(string json)
        {
            StageData data;
            try { data = JsonUtility.FromJson<StageData>(json); }
            catch (Exception e) { throw new FormatException("JSONを読み込めません: " + e.Message); }
            if (data == null) throw new FormatException("ステージデータが空です。");
            var errors = data.Validate();
            if (errors.Count > 0) throw new FormatException(string.Join("\n", errors));
            return data;
        }
    }

    public static class Directions
    {
        public static readonly Vector2Int Down = new Vector2Int(0, 1);
        public static readonly Vector2Int[] All = { Down, Vector2Int.left, Vector2Int.right, Vector2Int.down };
        public static Vector2Int Left(Vector2Int forward) => new Vector2Int(forward.y, -forward.x);
        public static Vector2Int Right(Vector2Int forward) => new Vector2Int(-forward.y, forward.x);
    }
}
