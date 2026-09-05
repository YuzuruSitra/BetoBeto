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
        public Vector2Int TravelDirection => moving ? moveDirection : Direction;
        GameController game;
        FruitMotionVfx motion;
        ActorFacing visualFacing;
        readonly Dictionary<Vector2Int, int> visits = new Dictionary<Vector2Int, int>();
        readonly HashSet<FruitAgent> slideContacts = new HashSet<FruitAgent>();
        bool moving, preferLeft, needsObstacleTurn;
        Vector2Int moveDirection = Directions.Down;
        float spawnDelay = .65f, immunity, stunned, stunDuration, recoil, fleeRemaining;
        Vector3 stunOrigin;
        SlideCombo combo;
        SlideCombo pendingCombo;
        Vector2Int pendingDirection;

        // All members of a collision chain share its speed and score multiplier.
        sealed class SlideCombo
        {
            public int Count;
            public SlideCombo MergedInto;
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
            slideContacts.RemoveWhere(ContactSeparated);
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
                if (stunned <= 0 && pendingCombo != null)
                {
                    var group = pendingCombo;
                    var direction = pendingDirection;
                    pendingCombo = null;
                    StartSlide(direction, group, true);
                }
                return;
            }
            float timeLeft = dt;
            for (int step = 0; step < 16 && timeLeft > .00001f && !Removed && game.Session.State == GameState.Playing; step++)
            {
                if (!moving && !ChooseNext()) return;
                bool walkingCookie = !Sliding && CanBumpCookie(TargetCell);
                bool returningInsidePipe = Cell == TargetCell && game.Board.Pipes.Contains(Cell);
                if (!Sliding && game.Board.BlocksWalking(TargetCell, moveDirection) && !walkingCookie && !returningInsidePipe)
                {
                    StopAtWall(); return;
                }
                bool obstacle = walkingCookie || (Sliding && game.Board.BlocksSliding(TargetCell, moveDirection));
                Vector3 target = game.Board.Data.World(TargetCell);
                if (obstacle) target -= new Vector3(moveDirection.x, 0, -moveDirection.y) * .62f;
                float distance = Vector3.Distance(transform.position, target);
                float speed = CurrentSpeed;
                float travel = Mathf.Min(timeLeft * speed, distance);
                Vector3 from = transform.position;
                Vector3 to = Vector3.MoveTowards(from, target, travel);
                bool contact = game.FindSlideContact(this, from, to, out var slideSource, out var slideTarget, out float hit);
                if (contact) { to = Vector3.Lerp(from, to, hit); travel *= hit; }
                transform.position = to;
                timeLeft -= travel / speed;
                if (game.Gimmicks.CheckMovingBlade(this, from, transform.position)) return;
                if (!game.Board.Data.Contains(game.Board.Data.Cell(transform.position))) { game.EscapeFruit(this); return; }
                if (contact && slideTarget.JoinSlide(slideSource))
                {
                    if (game.Feedback.HitStopped) return;
                    // Joining a chain can reverse this mover and change its destination mid-segment.
                    continue;
                }
                if (distance > travel + .001f) break;
                if (obstacle)
                {
                    if (walkingCookie)
                    {
                        bool broken = game.Gimmicks.HitCookie(TargetCell, this);
                        motion.Impact(.65f);
                        if (!broken)
                        {
                            // Recoil before the alternating turn; never hit this cookie again while waiting to turn.
                            Stun(.2f, .06f);
                            TargetCell = Cell; moving = false; needsObstacleTurn = true;
                        }
                        // Breaking the cookie preserves the heading, current segment and next turn preference.
                        return;
                    }
                    if (game.Board.Jellies.Contains(TargetCell))
                    {
                        var jelly = TargetCell;
                        Redirect(-moveDirection);
                        MoveToCell(Cell);
                        game.Feedback.Ricochet(this, jelly, false);
                    }
                    else if (game.Board.HasIceWall(TargetCell))
                    {
                        StopForIceWall(TargetCell); return;
                    }
                    else if (game.Board.Cookies.ContainsKey(TargetCell))
                    {
                        if (!game.Gimmicks.HitCookie(TargetCell, this)) { StopAtWall(false); return; }
                    }
                    else if (game.Board.HasScone(TargetCell))
                    {
                        // The two straight sides stop a slide at the edge, but still take one hit.
                        if (!game.Gimmicks.HitScone(TargetCell)) { StopAtWall(); return; }
                        game.Feedback.CookieImpact(this, TargetCell, true, 0);
                    }
                    else { StopAtWall(); return; }
                    if (game.Feedback.HitStopped) return;
                    continue;
                }
                Cell = TargetCell; moving = false;
                visits[Cell] = VisitCount(Cell) + 1;
                if (!game.Board.Data.Contains(Cell)) { game.EscapeFruit(this); return; }
                if (Sliding && game.Board.TouchesShredder(Cell, transform.position)) { HitShredder(); if (Removed || stunned > 0) return; }
                if (game.Board.HasScone(Cell) && game.Board.Scones.TryGetValue(Cell, out int turns))
                {
                    if (Sliding && game.Gimmicks.HitScone(Cell)) game.Feedback.CookieImpact(this, Cell, true, 0);
                    else
                    {
                        Redirect(GimmickRules.SconeReflection(moveDirection, turns));
                        game.Feedback.Ricochet(this, Cell, true, Sliding ? game.Board.SconeHitsLeft[Cell] : -1);
                    }
                }
                if (game.Board.Freezers.Contains(Cell)) Freeze();
                if (game.Board.Drool.ContainsKey(Cell)) BeginSlide(Direction, 1);
                if (game.Feedback.HitStopped) return;
            }
        }
        bool ChooseNext()
        {
            if (game.Board.Pipes.Contains(Cell))
            {
                // The supply valve only lets newly spawned fruit out towards the board.
                Direction = Directions.Down;
                visualFacing.Face(Direction);
                if (needsObstacleTurn && BlocksWalkingFromCell(Cell + Direction)) return false;
                if (!Sliding && BlocksWalkingFromCell(Cell + Direction) && !CanBumpCookie(Cell + Direction)) return false;
                needsObstacleTurn = false;
            }
            else if (!Sliding)
            {
                var next = needsObstacleTurn
                    ? FruitNavigation.ChooseTurn(Cell, Direction, preferLeft, BlocksWalkingFromCell)
                    : CanBumpCookie(Cell + Direction) ? Direction
                    : FruitNavigation.Choose(kind, Cell, Direction, preferLeft, BlocksWalkingFromCell, VisitCount);
                if (next == Vector2Int.zero) return false;
                // Alternate relative to the last actual quarter turn. A dead-end reversal does not consume it.
                if (next == Directions.Right(Direction)) preferLeft = true;
                else if (next == Directions.Left(Direction)) preferLeft = false;
                needsObstacleTurn = false;
                Direction = next;
                visualFacing.Face(Direction);
            }
            MoveToCell(Cell + Direction);
            return true;
        }
        void MoveToCell(Vector2Int target)
        {
            TargetCell = target;
            Vector3 delta = game.Board.Data.World(target) - transform.position;
            moveDirection = delta.sqrMagnitude < .000001f ? moveDirection
                : Mathf.Abs(delta.x) > Mathf.Abs(delta.z)
                    ? (delta.x > 0 ? Vector2Int.right : Vector2Int.left)
                    : (delta.z > 0 ? Vector2Int.down : Vector2Int.up);
            moving = true;
        }
        bool BlocksWalkingFromCell(Vector2Int cell) => game.Board.BlocksWalking(cell, cell - Cell);
        bool CanBumpCookie(Vector2Int cell) => kind == FruitKind.Orange
            && game.Board.Cookies.TryGetValue(cell, out var cookie) && !cookie.Broken;
        void Redirect(Vector2Int direction)
        {
            Direction = direction;
            visualFacing.Face(Direction);
            if (Sliding) motion.Ricochet();
        }
        public void Freeze()
        {
            if (Removed || spawnDelay > 0) return;
            bool fresh = !IsFrozen;
            FrozenRemaining = game.Board.Data.freezerSeconds;
            if (fresh) game.Feedback.FreezeFruit(this);
        }
        public void StopForIceWall(Vector2Int cell)
        {
            if (Removed || !Sliding || !game.Board.HasIceWall(cell)) return;
            // A newly raised wall may overlap a fruit that was already on the water.
            if (Cell == cell)
            {
                var safe = cell - Direction;
                if (!game.Board.Data.Contains(safe) || game.Board.Blocked(safe) || game.Board.HasShredder(safe))
                {
                    safe = cell;
                    foreach (var direction in Directions.All)
                    {
                        var candidate = cell + direction;
                        if (game.Board.Data.Contains(candidate) && !game.Board.Blocked(candidate) && !game.Board.HasShredder(candidate))
                        { safe = candidate; break; }
                    }
                }
                Cell = safe;
                if (safe != cell) transform.position = Vector3.Lerp(game.Board.Data.World(safe), game.Board.Data.World(cell), .38f);
            }
            game.Gimmicks.HitIceWall(cell, this);
            StopAtWall(false);
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
            if (game.Board.Pipes.Contains(Cell)) away = Directions.Down;
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
            MoveToCell(anchor);
            moving = (transform.position - game.Board.Data.World(anchor)).sqrMagnitude > .0001f;
            if (!moving) Cell = anchor;
            stunned = 0; needsObstacleTurn = false; pendingCombo = null;
            fleeRemaining = Sliding ? 0 : ScareRules.FleeSeconds;
            visualFacing.Face(Direction);
            motion.Scare();
            game.Feedback.FruitScared(this);
            // A scare aimed at the adjacent blade commits to a dive without snapping the fruit to the tile center.
            if (!Sliding && game.Board.HasShredder(game.Board.Data.Cell(transform.position) + Direction))
                StartSlide(Direction, new SlideCombo { Count = 1 }, false);
            else if (!moving && !Sliding && game.Board.Drool.ContainsKey(Cell)) BeginSlide(Direction, 1);
            return true;
        }
        public bool BeginSlide(Vector2Int direction, int chain)
        {
            return StartSlide(direction, new SlideCombo { Count = Mathf.Max(1, chain) }, false);
        }
        public bool JoinSlide(FruitAgent source)
        {
            if (!CanJoinSlide(source)) return false;
            if (stunned > 0)
            {
                // Remember the impact without restarting the recoil or global hit-stop.
                pendingCombo = source.combo;
                pendingDirection = source.TravelDirection;
                motion.Impact(.6f);
            }
            else if (!StartSlide(source.TravelDirection, source.combo, true)) return false;
            slideContacts.Add(source);
            source.slideContacts.Add(this);
            return true;
        }
        bool ContactSeparated(FruitAgent other)
        {
            if (other == null || other.Removed) return true;
            Vector3 delta = other.transform.position - transform.position;
            return delta.x * delta.x + delta.z * delta.z > .81f;
        }
        public bool CanJoinSlide(FruitAgent source) => source != null && source != this && !source.Removed
            && source.Sliding && !source.IsStunned && source.combo != null
            && !slideContacts.Contains(source) && !source.slideContacts.Contains(this)
            && !Removed && spawnDelay <= 0 && (!Sliding || combo != source.combo);
        bool StartSlide(Vector2Int direction, SlideCombo group, bool collision)
        {
            // A queued impact follows any merges that happened while the fruit was recoiling.
            while (group.MergedInto != null) group = group.MergedInto;
            // Wall recoil must finish before a new slide can begin. Otherwise overlapping followers
            // repeatedly cancel the stun and slam this fruit into the same wall, restarting hit-stop.
            // Shredder immunity protects the melon's health; it must not disable fruit contact.
            if (Removed || spawnDelay > 0 || stunned > 0 || direction.sqrMagnitude != 1
                || (Sliding && (!collision || combo == group))) return false;
            pendingCombo = null;
            SlideCombo previous = combo;
            if (game.Board.Pipes.Contains(Cell)) direction = Directions.Down;
            // Preserve the travelled segment instead of snapping over a tile's arrival callback.
            // In particular, a fruit entering a scone still has to reach and react to that scone.
            if (moving)
            {
                Vector2Int oldCell = Cell, oldTarget = TargetCell;
                int dot = direction.x * moveDirection.x + direction.y * moveDirection.y;
                Vector2Int anchor;
                if (dot > 0) anchor = oldTarget;
                else if (dot < 0) anchor = oldCell;
                else anchor = (transform.position - game.Board.Data.World(oldTarget)).sqrMagnitude
                    < (transform.position - game.Board.Data.World(oldCell)).sqrMagnitude ? oldTarget : oldCell;
                // Cell remains the last reached centre, even when returning along the same segment.
                MoveToCell(anchor);
            }
            else
            {
                Cell = game.Board.Data.Cell(transform.position);
                if ((transform.position - game.Board.Data.World(Cell)).sqrMagnitude > .000001f) MoveToCell(Cell);
            }
            bool added = false;
            if (collision && previous != null && previous != group)
            {
                // Merge history once, while preserving the headings of the other branch members.
                foreach (var member in previous.Members)
                {
                    if (group.Members.Add(member)) { group.Count++; added = true; }
                    if (member != null && member.combo == previous) member.combo = group;
                }
                previous.MergedInto = group;
            }
            if (group.Members.Add(this) && collision) { group.Count++; added = true; }
            combo = group;
            Direction = direction;
            Sliding = true; stunned = 0; fleeRemaining = 0; needsObstacleTurn = false;
            visualFacing.Face(Direction);
            motion.StartSlide();
            if (game.Board.Freezers.Contains(game.Board.Data.Cell(transform.position))) Freeze();
            // Rejoining after separation moves the fruit again without farming chain count or hit-stop.
            game.OnSlide(this, collision && added);
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
            if (!BlocksWalkingFromCell(safeCell)) Cell = safeCell;
            else
            {
                foreach (var direction in Directions.All)
                    if (game.Board.Data.Contains(Cell + direction) && !BlocksWalkingFromCell(Cell + direction)) { Cell += direction; break; }
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
