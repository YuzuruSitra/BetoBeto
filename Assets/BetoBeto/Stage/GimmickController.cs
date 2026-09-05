using System.Collections.Generic;
using BetoBeto.Core;
using BetoBeto.Enemies;
using UnityEngine;

namespace BetoBeto.Stage
{
    /// <summary>Uses the game's simulation clock so movement, regrowth and freezing all pause together.</summary>
    public sealed class GimmickController
    {
        readonly GameController game;
        readonly StageBoard board;
        readonly Dictionary<StageObject, Vector3> homes = new Dictionary<StageObject, Vector3>();
        public GimmickController(GameController controller)
        {
            game = controller; board = game.Board;
            foreach (var mover in board.Movers)
            {
                var view = board.Objects[mover.Start];
                homes[view] = view.transform.position;
            }
            foreach (var cookie in board.Cookies) UpdateCookie(cookie.Key, cookie.Value);
        }
        public void Reset()
        {
            foreach (var pair in homes) if (pair.Key != null) pair.Key.transform.position = pair.Value;
            foreach (var pair in board.Cookies) UpdateCookie(pair.Key, new CookieState(pair.Value.MaxHits, pair.Value.RespawnSeconds));
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
            foreach (var pair in board.Cookies)
            {
                if (pair.Value.Tick(dt, Occupied(pair.Key) || board.MoverReserves(pair.Key)))
                {
                    game.ClearDrool(pair.Key);
                    game.Feedback.CookieRestored(board.Data.World(pair.Key));
                }
                UpdateCookie(pair.Key, pair.Value);
            }
            foreach (var mover in board.Movers)
            {
                bool reversed = mover.Tick(dt, board.Data.movingShredderSpeed, cell => !board.Data.Contains(cell)
                    || board.Blocked(cell) || board.Shredders.Contains(cell) || board.Pipes.Contains(cell) || board.Exits.Contains(cell)
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
        public bool HitCookie(Vector2Int cell, FruitAgent fruit)
        {
            var cookie = board.Cookies[cell];
            bool broken = cookie.Hit();
            UpdateCookie(cell, cookie);
            game.Feedback.CookieImpact(fruit, cell, broken, cookie.HitsLeft);
            return broken;
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
