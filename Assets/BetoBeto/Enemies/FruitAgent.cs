using System.Collections.Generic;
using BetoBeto.Core;
using BetoBeto.Presentation;
using BetoBeto.Stage;
using UnityEngine;

namespace BetoBeto.Enemies
{
    public sealed class FruitAgent : MonoBehaviour
    {
        public FruitKind kind;
        public Vector2Int Cell { get; private set; }
        public Vector2Int TargetCell { get; private set; }
        public Vector2Int Direction { get; private set; } = Directions.Down;
        public bool Sliding { get; private set; }
        public int Health { get; private set; }
        public int Chain => combo == null ? 1 : combo.Count;
        public bool Removed { get; private set; }
        public bool IsStunned => stunned > 0;
        public float Speed => kind == FruitKind.Blueberry ? 2.15f : kind == FruitKind.Orange ? 1.05f : 1.45f;
        public float SlidingSpeed => 7.5f + Mathf.Min(Chain - 1, 6) * .65f;
        public Vector3 Forward => new Vector3(Direction.x, 0, -Direction.y);
        GameController game;
        FruitMotionVfx motion;
        readonly Dictionary<Vector2Int, int> visits = new Dictionary<Vector2Int, int>();
        bool moving, preferLeft;
        float spawnDelay = .65f, immunity, stunned, stunDuration, recoil;
        SlideCombo combo;

        // All members of a collision chain share its speed and score multiplier.
        sealed class SlideCombo
        {
            public int Count;
            public readonly HashSet<FruitAgent> Members = new HashSet<FruitAgent>();
        }

        public void Initialize(GameController controller, Vector2Int cell, bool turnLeft)
        {
            game = controller; Cell = TargetCell = cell; preferLeft = turnLeft;
            Health = kind == FruitKind.Melon ? 2 : 1;
            transform.position = game.Board.Data.World(cell);
            motion = gameObject.AddComponent<FruitMotionVfx>();
            motion.Initialize(game, this, transform.Find("Visual"));
            visits[cell] = 1;
        }
        public void Tick(float dt)
        {
            if (Removed || dt <= 0) return;
            immunity = Mathf.Max(0, immunity - dt);
            if (spawnDelay > 0)
            {
                spawnDelay = Mathf.Max(0, spawnDelay - dt);
                transform.position = game.Board.Data.World(Cell) + Vector3.up * (spawnDelay * 2.2f);
                return;
            }
            if (stunned > 0)
            {
                stunned = Mathf.Max(0, stunned - dt);
                float progress = 1 - stunned / stunDuration;
                transform.position = game.Board.Data.World(Cell) - Forward * (Mathf.Sin(progress * Mathf.PI) * recoil);
                return;
            }
            float budget = dt * (Sliding ? SlidingSpeed : Speed);
            for (int step = 0; step < 16 && budget > 0 && !Removed && game.Session.State == GameState.Playing; step++)
            {
                if (!moving && !ChooseNext()) return;
                if (Sliding ? game.Board.Blocked(TargetCell) : game.Board.BlocksWalking(TargetCell))
                {
                    StopAtWall(); return;
                }
                Vector3 target = game.Board.Data.World(TargetCell);
                float distance = Vector3.Distance(transform.position, target);
                float travel = Mathf.Min(budget, distance);
                transform.position = Vector3.MoveTowards(transform.position, target, travel);
                budget -= travel;
                if (Sliding) game.PropagateSlide(this);
                if (distance > travel + .001f) break;
                Cell = TargetCell; moving = false;
                visits[Cell] = VisitCount(Cell) + 1;
                if (!game.Board.Data.Contains(Cell) || game.Board.Exits.Contains(Cell)) { game.EscapeFruit(this); return; }
                if (Sliding && game.Board.Shredders.Contains(Cell)) { HitShredder(); if (Removed || stunned > 0) return; }
                if (game.Board.Drool.ContainsKey(Cell)) BeginSlide(Direction, 1);
            }
        }
        bool ChooseNext()
        {
            if (!Sliding)
            {
                var next = FruitNavigation.Choose(kind, Cell, Direction, preferLeft, game.Board.BlocksWalking, VisitCount);
                if (next == Vector2Int.zero) return false;
                if (next != Direction)
                {
                    if (kind == FruitKind.Orange && next.x != 0) preferLeft = !preferLeft;
                    else if (kind != FruitKind.Orange) preferLeft = !preferLeft;
                }
                Direction = next;
            }
            TargetCell = Cell + Direction;
            if (Sliding ? game.Board.Blocked(TargetCell) : game.Board.BlocksWalking(TargetCell)) { StopAtWall(); return false; }
            moving = true; return true;
        }
        void StopAtWall()
        {
            transform.position = game.Board.Data.World(Cell);
            moving = false;
            if (Sliding)
            {
                game.Feedback.WallImpact(this, game.Board.Ice.ContainsKey(TargetCell));
                motion.Impact(1);
                Stun(.48f, .23f);
            }
            Sliding = false; TargetCell = Cell; combo = null;
        }
        void Stun(float duration, float distance)
        {
            stunned = stunDuration = duration;
            recoil = distance;
        }
        int VisitCount(Vector2Int cell) => visits.TryGetValue(cell, out int count) ? count : 0;
        public bool BeginSlide(Vector2Int direction, int chain)
        {
            return StartSlide(direction, new SlideCombo { Count = Mathf.Max(1, chain) }, false);
        }
        public bool JoinSlide(FruitAgent source)
        {
            if (!source.Sliding || source.combo == null) return false;
            return StartSlide(source.Direction, source.combo, true);
        }
        bool StartSlide(Vector2Int direction, SlideCombo group, bool collision)
        {
            if (Removed || spawnDelay > 0 || Sliding || immunity > 0 || direction.sqrMagnitude != 1) return false;
            Cell = game.Board.Data.Cell(transform.position);
            transform.position = game.Board.Data.World(Cell);
            if (group.Members.Add(this) && collision) group.Count++;
            combo = group;
            Direction = direction; Sliding = true; moving = false; stunned = 0;
            motion.StartSlide();
            game.OnSlide(this, collision);
            return true;
        }
        public void HitShredder()
        {
            // Walking contact can never collect ingredients, including editor-driven calls.
            if (Removed || !Sliding || immunity > 0 || !game.Board.Shredders.Contains(Cell)) return;
            if (--Health <= 0) { game.HarvestFruit(this); return; }
            immunity = 1.1f;
            game.Feedback.MelonImpact(this);
            motion.Impact(1.15f);
            Sliding = false; moving = false; combo = null;
            var safeCell = Cell - Direction;
            if (!game.Board.BlocksWalking(safeCell)) Cell = safeCell;
            TargetCell = Cell;
            transform.position = game.Board.Data.World(Cell);
            Stun(.5f, .2f);
            game.Notify("メロンの皮が割れた！ もう一度よだれで滑らせよう", 2.3f);
        }
        public void MarkRemoved()
        {
            Removed = true;
            motion.DetachTrails();
            gameObject.SetActive(false);
            Destroy(gameObject);
        }
    }
}
