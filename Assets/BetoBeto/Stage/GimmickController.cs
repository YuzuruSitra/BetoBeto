using System.Collections.Generic;
using BetoBeto.Core;
using BetoBeto.Enemies;
using BetoBeto.Player;
using UnityEngine;

namespace BetoBeto.Stage
{
    /// <summary>Uses the game's simulation clock so movement, regrowth and freezing all pause together.</summary>
    public sealed class GimmickController
    {
        readonly GameController game;
        readonly StageBoard board;
        readonly Dictionary<StageObject, Vector3> homes = new Dictionary<StageObject, Vector3>();
        readonly Dictionary<Vector2Int, float> sconeRespawnRemaining = new Dictionary<Vector2Int, float>();
        public GimmickController(GameController controller)
        {
            game = controller; board = game.Board;
            foreach (var mover in board.Movers)
            {
                var view = board.Objects[mover.Start];
                homes[view] = view.transform.position;
            }
            foreach (var cookie in board.Cookies) UpdateCookie(cookie.Key, cookie.Value);
            foreach (var scone in board.SconeHitsLeft) UpdateScone(scone.Key, scone.Value);
            foreach (var pair in board.IceWalls) UpdateIceWall(pair.Key, pair.Value);
        }
        public void Reset()
        {
            sconeRespawnRemaining.Clear();
            foreach (var pair in homes) if (pair.Key != null) pair.Key.transform.position = pair.Value;
            foreach (var pair in board.Cookies) UpdateCookie(pair.Key, new CookieState(pair.Value.MaxHits, pair.Value.RespawnSeconds));
            foreach (var pair in board.SconeHitsLeft) UpdateScone(pair.Key, GimmickRules.SconeMaxHits);
            foreach (var pair in board.IceWalls) { pair.Value.Reset(); UpdateIceWall(pair.Key, pair.Value); }
        }
        bool Occupied(Vector2Int cell, bool walkingOnly = false)
        {
            foreach (var fruit in game.Fruits)
                if (!fruit.Removed && (!walkingOnly || !fruit.Sliding)
                    && (fruit.Cell == cell || fruit.TargetCell == cell || board.Data.Cell(fruit.transform.position) == cell)) return true;
            return false;
        }
        public void Tick(float dt)
        {
            if (dt <= 0) return;
            foreach (var pair in board.IceWalls)
            {
                if (!pair.Value.Tick(dt)) continue;
                UpdateIceWall(pair.Key, pair.Value);
                game.Feedback.IceWallMelt(pair.Key);
            }
            foreach (var pair in board.Cookies)
            {
                if (pair.Value.Tick(dt, Occupied(pair.Key) || board.MoverReserves(pair.Key)))
                {
                    game.ClearDrool(pair.Key);
                    game.Feedback.GimmickRestored(board.Data.World(pair.Key));
                }
                UpdateCookie(pair.Key, pair.Value);
            }
            foreach (var cell in board.Scones.Keys)
            {
                if (!sconeRespawnRemaining.TryGetValue(cell, out float remaining)) continue;
                remaining = Mathf.Max(0, remaining - dt);
                sconeRespawnRemaining[cell] = remaining;
                if (remaining > 0 || Occupied(cell) || board.MoverReserves(cell)) continue;
                sconeRespawnRemaining.Remove(cell);
                board.SconeHitsLeft[cell] = GimmickRules.SconeMaxHits;
                game.ClearDrool(cell);
                UpdateScone(cell, GimmickRules.SconeMaxHits);
                game.Feedback.GimmickRestored(board.Data.World(cell));
            }
            foreach (var mover in board.Movers)
            {
                bool reversed = mover.Tick(dt, board.Data.movingShredderSpeed, cell => !board.Data.Contains(cell)
                    || board.Blocked(cell) || board.Shredders.Contains(cell) || board.Pipes.Contains(cell)
                    || board.MoverReserves(cell, mover) || Occupied(cell, true));
                var view = board.Objects[mover.Start];
                view.transform.position = board.MoverWorld(mover.Position);
                if (reversed && (mover.Previous - mover.Position).sqrMagnitude > .00001f)
                    game.Feedback.Ring(view.transform.position, new Color(1, .66f, .74f), .2f, .65f, .18f);
                foreach (var fruit in game.Fruits)
                {
                    if (fruit.Removed || !fruit.Sliding) continue;
                    Vector3 a = board.MoverWorld(mover.Previous), b = board.MoverWorld(mover.Position), p = fruit.transform.position;
                    if (GimmickRules.SweepCircle(new Vector2(a.x, a.z), new Vector2(b.x, b.z), new Vector2(p.x, p.z), GimmickRules.BladeRadius, out _))
                        fruit.HitShredder();
                    if (game.Session.State != GameState.Playing || game.Feedback.HitStopped) return;
                }
            }
        }
        // Check the whole travelled segment, including a fast fruit crossing a moving blade between tile centres.
        public bool CheckMovingBlade(FruitAgent fruit, Vector3 from, Vector3 to)
        {
            if (!fruit.Sliding || fruit.ShredderImmune) return false;
            float first = float.MaxValue;
            foreach (var mover in board.Movers)
            {
                Vector3 centre = board.MoverWorld(mover.Position);
                if (GimmickRules.SweepCircle(new Vector2(from.x, from.z), new Vector2(to.x, to.z), new Vector2(centre.x, centre.z), GimmickRules.BladeRadius, out float hit))
                    first = Mathf.Min(first, hit);
            }
            if (first > 1) return false;
            fruit.transform.position = Vector3.Lerp(from, to, first);
            fruit.HitShredder();
            return fruit.Removed || fruit.IsStunned;
        }
        public int ScareIceWalls(Vector2Int source, Vector2Int facing, float chargeSeconds)
        {
            int count = 0;
            foreach (var pair in board.IceWalls)
            {
                var cell = pair.Key;
                if (!ScareRules.Contains(source, facing, cell, chargeSeconds) || !pair.Value.Raise(board.Data.iceLifetime)) continue;
                game.ClearDrool(cell);
                UpdateIceWall(cell, pair.Value);
                game.Feedback.IceWallRise(cell);
                count++;
                // Stop fruit already crossing the water when the wall rises beneath it.
                foreach (var fruit in game.Fruits)
                    if (!fruit.Removed && fruit.Sliding && board.Data.Cell(fruit.transform.position) == cell)
                        fruit.StopForIceWall(cell);
            }
            return count;
        }
        public void HitIceWall(Vector2Int cell, FruitAgent fruit)
        {
            if (!board.IceWalls.TryGetValue(cell, out var state) || !state.Raised) return;
            state.Hit();
            UpdateIceWall(cell, state);
            game.Feedback.IceWallImpact(fruit, cell);
        }
        void UpdateIceWall(Vector2Int cell, IceWallState state)
        {
            if (!board.Objects.TryGetValue(cell, out var view) || view == null) return;
            var water = view.transform.Find("Water");
            var wall = view.transform.Find("Wall");
            if (water != null) water.gameObject.SetActive(!state.Raised);
            if (wall != null)
            {
                wall.gameObject.SetActive(state.Raised);
                var cracks = wall.Find("Cracks");
                if (cracks != null) cracks.gameObject.SetActive(state.Damaged);
            }
        }
        public bool HitCookie(Vector2Int cell, FruitAgent fruit)
        {
            var cookie = board.Cookies[cell];
            bool broken = cookie.Hit();
            UpdateCookie(cell, cookie);
            game.Feedback.CookieImpact(fruit, cell, broken, cookie.HitsLeft);
            return broken;
        }
        public bool HitScone(Vector2Int cell)
        {
            if (!board.HasScone(cell)) return false;
            int hits = --board.SconeHitsLeft[cell];
            if (hits == 0) sconeRespawnRemaining[cell] = board.Data.sconeRespawnSeconds;
            UpdateScone(cell, hits);
            return hits == 0;
        }
        void UpdateScone(Vector2Int cell, int hitsLeft)
        {
            if (!board.Objects.TryGetValue(cell, out var view) || view == null) return;
            foreach (var renderer in view.GetComponentsInChildren<Renderer>(true)) renderer.enabled = hitsLeft > 0;
            for (int i = 1; i <= 2; i++)
            {
                var crack = view.transform.Find("Impact crack " + i);
                if (crack == null)
                {
                    crack = new GameObject("Impact crack " + i).transform;
                    crack.SetParent(view.transform, false);
                    var line = crack.gameObject.AddComponent<LineRenderer>();
                    line.sharedMaterial = game.assets.effectMaterial;
                    line.useWorldSpace = false; line.widthMultiplier = .024f;
                    line.startColor = line.endColor = new Color(.34f, .18f, .10f);
                    line.positionCount = 3;
                    line.SetPositions(i == 1
                        ? new[] { new Vector3(-.10f, .325f, .38f), new Vector3(-.18f, .325f, .12f), new Vector3(-.31f, .325f, .02f) }
                        : new[] { new Vector3(-.39f, .325f, -.20f), new Vector3(-.19f, .325f, -.10f), new Vector3(-.02f, .325f, .02f) });
                    line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    line.receiveShadows = false;
                }
                crack.gameObject.SetActive(hitsLeft > 0
                    && GimmickRules.SconeMaxHits - hitsLeft >= Mathf.CeilToInt(GimmickRules.SconeMaxHits * i / 3f));
            }
        }
        void UpdateCookie(Vector2Int cell, CookieState state)
        {
            if (!board.Objects.TryGetValue(cell, out var view) || view == null) return;
            var solid = view.transform.Find("Solid");
            if (solid != null)
            {
                solid.gameObject.SetActive(!state.Broken);
                for (int i = 1; i <= 2; i++)
                {
                    var crack = solid.Find("Crack " + i);
                    if (crack != null) crack.gameObject.SetActive(state.MaxHits - state.HitsLeft >= Mathf.CeilToInt(state.MaxHits * i / 3f));
                }
            }
            var regrowing = view.transform.Find("Regrowing");
            if (regrowing != null)
            {
                regrowing.gameObject.SetActive(state.Broken);
                float progress = 1 - state.Remaining / state.RespawnSeconds;
                regrowing.localScale = new Vector3(.35f + progress * .65f, 1, .35f + progress * .65f);
            }
        }
    }
}
