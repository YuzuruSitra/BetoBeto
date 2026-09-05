using System.Collections.Generic;
using BetoBeto.Core;
using BetoBeto.Presentation;
using BetoBeto.Player;
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
        public bool IsFleeing => fleeRemaining > 0;
        public bool IsFrozen => FrozenRemaining > 0;
        public float FrozenRemaining { get; private set; }
        public bool ShredderImmune => immunity > 0;
        public float Speed => kind == FruitKind.Blueberry ? 2.15f : kind == FruitKind.Orange ? 1.05f : 1.45f;
        public float SlidingSpeed => 7.5f + Mathf.Min(Chain - 1, 6) * .65f;
        public float CurrentSpeed => (Sliding ? SlidingSpeed : Speed) * (IsFrozen ? game.Board.Data.frozenSpeedMultiplier : 1);
        public Vector3 Forward => new Vector3(Direction.x, 0, -Direction.y);
        GameController game;
        FruitMotionVfx motion;
        ActorFacing visualFacing;
        readonly Dictionary<Vector2Int, int> visits = new Dictionary<Vector2Int, int>();
        bool moving, preferLeft, directionalScare;
        float spawnDelay = .65f, immunity, stunned, stunDuration, recoil, fleeRemaining;
        Vector2Int scareSource;
        Vector3 stunOrigin;
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
            visualFacing = gameObject.AddComponent<ActorFacing>();
            visualFacing.Initialize(game.assets.effectMaterial, false);
            motion = gameObject.AddComponent<FruitMotionVfx>();
            motion.Initialize(game, this, transform.Find("Visual"));
            visits[cell] = 1;
        }
        public void Tick(float dt)
        {
            if (Removed || dt <= 0) return;
            immunity = Mathf.Max(0, immunity - dt);
            fleeRemaining = Mathf.Max(0, fleeRemaining - dt);
            FrozenRemaining = Mathf.Max(0, FrozenRemaining - dt);
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
                transform.position = Vector3.Lerp(stunOrigin, game.Board.Data.World(Cell), progress) - Forward * (Mathf.Sin(progress * Mathf.PI) * recoil);
                return;
            }
            float timeLeft = dt;
            for (int step = 0; step < 16 && timeLeft > .00001f && !Removed && game.Session.State == GameState.Playing; step++)
            {
                if (!moving && !ChooseNext()) return;
                if (!Sliding && game.Board.BlocksWalking(TargetCell))
                {
                    StopAtWall(); return;
                }
                bool obstacle = Sliding && game.Board.BlocksSliding(TargetCell);
                Vector3 target = game.Board.Data.World(TargetCell);
                if (obstacle) target -= Forward * .62f;
                float distance = Vector3.Distance(transform.position, target);
                float speed = CurrentSpeed;
                float travel = Mathf.Min(timeLeft * speed, distance);
                Vector3 from = transform.position;
                transform.position = Vector3.MoveTowards(transform.position, target, travel);
                timeLeft -= travel / speed;
                if (game.Gimmicks.CheckMovingBlade(this, from, transform.position)) return;
                if (Sliding) game.PropagateSlide(this);
                if (distance > travel + .001f) break;
                if (obstacle)
                {
                    if (game.Board.Jellies.Contains(TargetCell))
                    {
                        var jelly = TargetCell;
                        Redirect(-Direction);
                        TargetCell = Cell; moving = true;
                        game.Feedback.Ricochet(this, jelly, false);
                    }
                    else if (game.Board.Cookies.ContainsKey(TargetCell))
                    {
                        if (!game.Gimmicks.HitCookie(TargetCell, this)) { StopAtWall(false); return; }
                    }
                    else { StopAtWall(); return; }
                    if (game.Feedback.HitStopped) return;
                    continue;
                }
                Cell = TargetCell; moving = false;
                visits[Cell] = VisitCount(Cell) + 1;
                if (!game.Board.Data.Contains(Cell) || game.Board.Exits.Contains(Cell)) { game.EscapeFruit(this); return; }
                if (Sliding && game.Board.TouchesShredder(Cell, transform.position)) { HitShredder(); if (Removed || stunned > 0) return; }
                if (Sliding && game.Board.Scones.TryGetValue(Cell, out int turns))
                {
                    Redirect(GimmickRules.SconeReflection(Direction, turns));
                    game.Feedback.Ricochet(this, Cell, true);
                }
                if (game.Board.Freezers.Contains(Cell)) Freeze();
                if (game.Board.Drool.ContainsKey(Cell)) BeginSlide(Direction, 1);
                if (game.Feedback.HitStopped) return;
            }
        }
        bool ChooseNext()
        {
            if (!Sliding)
            {
                var next = IsFleeing
                    ? FruitNavigation.ChooseFlee(Cell, scareSource, Direction, game.Board.BlocksWalking, !directionalScare)
                    : FruitNavigation.Choose(kind, Cell, Direction, preferLeft, game.Board.BlocksWalking, VisitCount);
                if (next == Vector2Int.zero) return false;
                if (next != Direction)
                {
                    if (kind == FruitKind.Orange && next.x != 0) preferLeft = !preferLeft;
                    else if (kind != FruitKind.Orange) preferLeft = !preferLeft;
                }
                Direction = next;
                visualFacing.Face(Direction);
            }
            TargetCell = Cell + Direction;
            moving = true; return true;
        }
        void Redirect(Vector2Int direction)
        {
            Direction = direction;
            visualFacing.Face(Direction);
            motion.Ricochet();
        }
        public void Freeze()
        {
            if (Removed || spawnDelay > 0) return;
            bool fresh = !IsFrozen;
            FrozenRemaining = game.Board.Data.freezerSeconds;
            if (fresh) game.Feedback.FreezeFruit(this);
        }
        void StopAtWall(bool feedback = true)
        {
            moving = false;
            if (Sliding)
            {
                if (feedback) game.Feedback.WallImpact(this);
                motion.Impact(1);
                Stun(.48f, .23f);
            }
            else transform.position = game.Board.Data.World(Cell);
            Sliding = false; TargetCell = Cell; combo = null;
        }
        void Stun(float duration, float distance)
        {
            stunned = stunDuration = duration;
            recoil = distance;
            stunOrigin = transform.position;
        }
        int VisitCount(Vector2Int cell) => visits.TryGetValue(cell, out int count) ? count : 0;
        public bool Scare(Vector3 source, Vector2Int? fleeDirection = null)
        {
            if (Removed || spawnDelay > 0) return false;
            Vector3 delta = transform.position - source;
            Vector2Int away = fleeDirection ?? ScareRules.Away(new Vector2(delta.x, -delta.z), Direction);
            if (away.sqrMagnitude != 1) return false;
            // Finish or reverse the current segment without teleporting across a tile.
            // A perpendicular turn starts at the nearer end of that same segment.
            Vector2Int anchor = Cell;
            if (moving)
            {
                int dot = away.x * Direction.x + away.y * Direction.y;
                if (dot > 0 || (dot == 0 && (transform.position - game.Board.Data.World(TargetCell)).sqrMagnitude
                    < (transform.position - game.Board.Data.World(Cell)).sqrMagnitude)) anchor = TargetCell;
            }
            Direction = away;
            TargetCell = anchor;
            moving = (transform.position - game.Board.Data.World(anchor)).sqrMagnitude > .0001f;
            if (!moving) Cell = anchor;
            stunned = 0;
            scareSource = game.Board.Data.Cell(source);
            directionalScare = fleeDirection.HasValue;
            fleeRemaining = Sliding ? 0 : ScareRules.FleeSeconds;
            visualFacing.Face(Direction);
            motion.Scare();
            game.Feedback.FruitScared(this);
            // A scare aimed at the adjacent blade commits to a dive without snapping the fruit to the tile center.
            if (!Sliding && game.Board.HasShredder(game.Board.Data.Cell(transform.position) + Direction))
                StartSlide(Direction, new SlideCombo { Count = 1 }, false, false);
            else if (!moving && !Sliding && game.Board.Drool.ContainsKey(Cell)) BeginSlide(Direction, 1);
            return true;
        }
        public bool BeginSlide(Vector2Int direction, int chain)
        {
            return StartSlide(direction, new SlideCombo { Count = Mathf.Max(1, chain) }, false);
        }
        public bool JoinSlide(FruitAgent source)
        {
            if (!source.Sliding || source.combo == null) return false;
            return StartSlide(source.Direction, source.combo, true);
        }
        bool StartSlide(Vector2Int direction, SlideCombo group, bool collision, bool snapToCell = true)
        {
            if (Removed || spawnDelay > 0 || Sliding || immunity > 0 || direction.sqrMagnitude != 1) return false;
            Cell = game.Board.Data.Cell(transform.position);
            if (snapToCell) transform.position = game.Board.Data.World(Cell);
            if (group.Members.Add(this) && collision) group.Count++;
            combo = group;
            Direction = direction; Sliding = true; moving = false; stunned = 0; fleeRemaining = 0;
            visualFacing.Face(Direction);
            motion.StartSlide();
            // A collision can snap a walking fruit onto the plate before its normal arrival callback.
            if (game.Board.Freezers.Contains(Cell)) Freeze();
            game.OnSlide(this, collision);
            return true;
        }
        public void HitShredder()
        {
            // Walking contact can never collect ingredients, including editor-driven calls.
            if (Removed || !Sliding || immunity > 0 || !game.Board.TouchesShredder(Cell, transform.position)) return;
            if (--Health <= 0) { game.HarvestFruit(this); return; }
            immunity = 1.1f;
            game.Feedback.MelonImpact(this);
            motion.Impact(1.15f);
            Sliding = false; moving = false; combo = null;
            Cell = game.Board.Data.Cell(transform.position);
            var safeCell = Cell - Direction;
            if (!game.Board.BlocksWalking(safeCell)) Cell = safeCell;
            else
            {
                foreach (var direction in Directions.All)
                    if (game.Board.Data.Contains(Cell + direction) && !game.Board.BlocksWalking(Cell + direction)) { Cell += direction; break; }
            }
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
