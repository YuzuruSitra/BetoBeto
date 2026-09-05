using System.Collections.Generic;
using System.Collections;
using BetoBeto.Audio;
using BetoBeto.Enemies;
using BetoBeto.Player;
using BetoBeto.Presentation;
using BetoBeto.Stage;
using BetoBeto.UI;
using UnityEngine;

namespace BetoBeto.Core
{
    public sealed class GameController : MonoBehaviour
    {
        public const float DroolReuseSeconds = .3f;
        public static GameController Instance { get; private set; }
        public GameAssets assets;
        public StageLayout layout;
        public Camera gameCamera;
        public GameSession Session { get; private set; }
        public StageBoard Board { get; private set; }
        public GimmickController Gimmicks { get; private set; }
        public Camera GameCamera => gameCamera;
        public GameAudio Audio { get; private set; }
        public GameHud Hud { get; private set; }
        public GameFeedback Feedback { get; private set; }
        public GhostController Player { get; private set; }
        public Transform FeedbackRoot => effects;
        public float DroolCooldown { get; private set; }
        public float Countdown { get; private set; }
        public int ActiveFruitCount => fruits.Count;
        public string Notice { get; private set; }
        public float NoticeUntil { get; private set; }
        public IReadOnlyList<FruitAgent> Fruits => fruits;
        readonly List<FruitAgent> fruits = new List<FruitAgent>();
        readonly Dictionary<Vector2Int, GameObject> droolVisuals = new Dictionary<Vector2Int, GameObject>();
        readonly List<Vector2Int> expired = new List<Vector2Int>();
        Transform actors;
        Transform effects;
        float spawnTimer;
        int spawnIndex;
        int lastWidth, lastHeight;
        GameState shownState;
        bool hadController;

        void Awake()
        {
            Instance = this;
            GameFlow.SceneReady();
            Application.targetFrameRate = 60;
            Application.runInBackground = true;
            Board = new StageBoard(layout);
            Session = new GameSession(Board.Data);
            Audio = GameAudio.GetOrCreate();
            actors = new GameObject("Actors").transform;
            effects = new GameObject("Feedback").transform;
            Feedback = gameObject.AddComponent<GameFeedback>();
            Feedback.Initialize(this);
            Hud = gameObject.AddComponent<GameHud>();
            Hud.Initialize(this);
            FitCamera();
            StartGame();
        }
        void Update()
        {
            if (Screen.width != lastWidth || Screen.height != lastHeight) FitCamera();
            if (GamepadControls.PausePressed || (Hud.ModalOpen && GamepadControls.CancelPressed)) Hud.TogglePause();
            if (hadController && !GamepadControls.Ready && Session.State == GameState.Playing) Hud.ShowOptions();
            hadController = GamepadControls.Ready;
            if (Session.State == GameState.Playing && GamepadControls.Ready)
            {
                float dt = Feedback.SimulationDelta;
                DroolCooldown = Mathf.Max(0, DroolCooldown - dt);
                Session.Elapsed += dt;
                TickDrool(dt);
                Countdown = Mathf.Max(0, Countdown - dt);
                if (Countdown <= 0)
                {
                    spawnTimer -= dt;
                    if (spawnTimer <= 0 && fruits.Count < 24 && Board.Pipes.Count > 0)
                    {
                        SpawnFruit(NextKind(), Board.Pipes[spawnIndex % Board.Pipes.Count]);
                        spawnIndex++;
                        spawnTimer = Board.Data.spawnInterval;
                    }
                    // Small shared steps keep moving traps and fruit in sync, even after a slow browser frame.
                    for (float remaining = dt; remaining > .00001f;)
                    {
                        float step = Mathf.Min(remaining, .025f);
                        remaining -= step;
                        Gimmicks.Tick(step);
                        if (Session.State != GameState.Playing || Feedback.HitStopped) break;
                        for (int i = fruits.Count - 1; i >= 0; i--)
                        {
                            if (Session.State != GameState.Playing || Feedback.HitStopped) break;
                            fruits[i].Tick(step);
                        }
                        if (Session.State != GameState.Playing || Feedback.HitStopped) break;
                    }
                    fruits.RemoveAll(f => f == null || f.Removed);
                }
            }
            if (shownState != Session.State)
            {
                shownState = Session.State;
                if (shownState == GameState.Won || shownState == GameState.Lost)
                {
                    Player.CancelScare();
                    Audio.Play(shownState == GameState.Won ? "win" : "escape");
                    if (shownState == GameState.Won) for (int i = 0; i < 4; i++) Burst(Board.Data.World(Board.PlayerStart), (FruitKind)i, 12);
                    StartCoroutine(GoToResult());
                }
            }
            Hud.Refresh();
        }
        public void StartGame()
        {
            StopAllCoroutines();
            ClearChildren(actors); ClearChildren(effects);
            Feedback.ResetFeedback(); Hud.ClearFeedback();
            fruits.Clear(); droolVisuals.Clear();
            Gimmicks?.Reset();
            Board = new StageBoard(layout);
            Gimmicks = new GimmickController(this);
            Session = new GameSession(Board.Data) { State = GameState.Playing };
            shownState = GameState.Playing;
            DroolCooldown = 0;
            spawnTimer = 0; spawnIndex = 0; Countdown = 2.8f;
            hadController = GamepadControls.Ready;
            GamepadControls.SuppressActionsUntilRelease();
            var ghost = Instantiate(assets.ghost, Board.Data.World(Board.PlayerStart), Quaternion.identity, actors);
            Player = ghost.GetComponent<GhostController>();
            Player.Initialize(this);
            Notify("よだれから連鎖！ お菓子で跳ね返して、ピンクの刃へ！", 6);
        }
        IEnumerator GoToResult()
        {
            yield return new WaitForSecondsRealtime(1.1f);
            GameFlow.Complete(Session, Board.Data);
        }
        FruitKind NextKind()
        {
            FruitKind[] sequence = { FruitKind.Strawberry, FruitKind.Blueberry, FruitKind.Orange, FruitKind.Strawberry, FruitKind.Blueberry, FruitKind.Melon, FruitKind.Orange };
            var kind = sequence[spawnIndex % sequence.Length];
            if (Session.Harvested[(int)kind] < Session.Recipe.For(kind)) return kind;
            for (int i = 0; i < 4; i++) if (Session.Harvested[i] < Session.Recipe.For((FruitKind)i)) return (FruitKind)i;
            return kind;
        }
        public FruitAgent SpawnFruit(FruitKind kind, Vector2Int cell)
        {
            var instance = Instantiate(assets.fruits[(int)kind], Board.Data.World(cell), Quaternion.identity, actors);
            var fruit = instance.GetComponent<FruitAgent>();
            fruit.Initialize(this, cell, turnLeft: false);
            fruits.Add(fruit);
            return fruit;
        }
        public bool TryScare(Vector2Int cell, Vector2Int facing, float chargeSeconds)
            => TryScare(Board.Data.World(cell), facing, chargeSeconds);
        public bool TryScare(Vector3 source, Vector2Int facing, float chargeSeconds)
        {
            if (Session.State != GameState.Playing || facing.sqrMagnitude != 1) return false;
            int count = 0;
            Vector2Int cell = Board.Data.Cell(source);
            Vector2Int? fleeDirection = ScareRules.IsCharged(chargeSeconds) ? (Vector2Int?)null : facing;
            foreach (var fruit in fruits)
                if (!fruit.Removed && ScareRules.Contains(cell, facing, Board.Data.Cell(fruit.transform.position), chargeSeconds)
                    && fruit.Scare(source, fleeDirection)) count++;
            int raised = Gimmicks.ScareIceWalls(cell, facing, chargeSeconds);
            Feedback.ScareBurst(Board.Data.World(cell), Board.Data.World(cell + facing), chargeSeconds, count);
            if (raised > 0) Notify(count > 0 ? $"{count}体をびっくり！ 氷の壁が{raised}個そり立った！" : $"水たまりから氷の壁が{raised}個そり立った！", 1.8f);
            else if (count > 0) Notify($"{count}体をびっくり！  逃げ道によだれを置こう", 1.8f);
            return true;
        }
        public bool TryPlaceDrool(Vector2Int cell)
        {
            if (Session.State != GameState.Playing || DroolCooldown > 0) return false;
            if (!Board.CanPlace(cell)) { Notify("床の上でよだれを置こう", 1.2f); return false; }
            if (!Board.Drool.ContainsKey(cell)) droolVisuals.Add(cell, Instantiate(assets.drool, Board.Data.World(cell, .025f), Quaternion.identity, effects));
            Board.Drool[cell] = Board.Data.droolLifetime;
            DroolCooldown = DroolReuseSeconds; Audio.Play("drool");
            foreach (var fruit in fruits) if (!fruit.Removed && fruit.Cell == cell) fruit.BeginSlide(fruit.Direction, 1);
            return true;
        }
        void TickDrool(float dt)
        {
            var timers = Board.Drool;
            var visuals = droolVisuals;
            expired.Clear();
            // Snapshot keys because timer values are written back while iterating.
            var keys = new List<Vector2Int>(timers.Keys);
            foreach (var cell in keys)
            {
                float remaining = timers[cell] - dt;
                timers[cell] = remaining;
                if (remaining <= 0) { expired.Add(cell); continue; }
                if (visuals.TryGetValue(cell, out var go) && go != null)
                {
                    float shrink = remaining < 1.4f ? .82f + .18f * Mathf.Abs(Mathf.Sin(remaining * 11)) : 1;
                    go.transform.localScale = Vector3.one * shrink;
                }
            }
            foreach (var cell in expired)
            {
                timers.Remove(cell);
                if (visuals.TryGetValue(cell, out var go)) Destroy(go);
                visuals.Remove(cell);
            }
        }
        public void ClearDrool(Vector2Int cell)
        {
            Board.Drool.Remove(cell);
            if (droolVisuals.TryGetValue(cell, out var go)) Destroy(go);
            droolVisuals.Remove(cell);
        }
        public void PropagateSlide(FruitAgent source)
        {
            if (source.Removed || !source.Sliding || source.IsStunned) return;
            // Keep point-contact callers working; normal movement uses the swept query below.
            for (int i = 0; i < fruits.Count; i++)
            {
                if (!FindSlideContact(source, source.transform.position, source.transform.position,
                    out var slideSource, out var target, out _) || !target.JoinSlide(slideSource)) break;
            }
        }
        public bool FindSlideContact(FruitAgent mover, Vector3 from, Vector3 to,
            out FruitAgent slideSource, out FruitAgent slideTarget, out float fraction)
        {
            slideSource = slideTarget = null; fraction = float.PositiveInfinity;
            if (mover.Removed || mover.IsStunned) return false;
            foreach (var other in fruits)
            {
                if (other == mover || other.Removed) continue;
                var source = mover.Sliding ? mover : other;
                var target = mover.Sliding ? other : mover;
                if (!target.CanJoinSlide(source)) continue;
                Vector3 centre = other.transform.position;
                if (!GimmickRules.SweepCircle(new Vector2(from.x, from.z), new Vector2(to.x, to.z),
                    new Vector2(centre.x, centre.z), .8f, out float hit) || hit >= fraction) continue;
                Vector3 contact = Vector3.Lerp(from, to, hit);
                Vector3 delta = mover.Sliding ? centre - contact : contact - centre;
                Vector2Int direction = source.TravelDirection;
                if (delta.x * direction.x - delta.z * direction.y < -.15f) continue;
                slideSource = source; slideTarget = target; fraction = hit;
            }
            return slideTarget != null;
        }
        public void OnSlide(FruitAgent fruit, bool collision)
        {
            Session.RecordChain(fruit.Chain);
            if (collision)
            {
                Feedback.ChainImpact(fruit);
                Notify($"{fruit.Chain} CHAIN!  巻き込むほど加速！", 2);
            }
            else Feedback.SlideStart(fruit);
        }
        public void HarvestFruit(FruitAgent fruit)
        {
            if (fruit.Removed || !fruit.Sliding || !Board.TouchesShredder(fruit.Cell, fruit.transform.position)) return;
            Feedback.Harvest(fruit);
            Session.Harvest(fruit.kind, fruit.Chain);
            Notify($"+{100 * fruit.Chain}   {GameHud.FruitNames[(int)fruit.kind]}を収穫！", 1.7f);
            fruit.MarkRemoved();
        }
        public void EscapeFruit(FruitAgent fruit)
        {
            if (fruit.Removed) return;
            Session.Escape(); Audio.Play("escape");
            Notify($"フルーツが逃げた！  あと{Mathf.Max(0, Session.EscapeLimit - Session.Escaped)}回", 2);
            fruit.MarkRemoved();
        }
        public void Notify(string message, float duration) { Notice = message; NoticeUntil = Time.unscaledTime + duration; }
        public void Burst(Vector3 position, FruitKind kind, int amount)
        {
            for (int i = 0; i < amount; i++)
            {
                var bit = GameObject.CreatePrimitive(i % 3 == 0 ? PrimitiveType.Cube : PrimitiveType.Sphere);
                bit.name = "Fruit confetti"; Destroy(bit.GetComponent<Collider>());
                bit.transform.SetParent(effects);
                bit.transform.position = position + Vector3.up * .45f;
                bit.transform.localScale = Vector3.one * Random.Range(.08f, .19f);
                bit.GetComponent<Renderer>().sharedMaterial = i % 4 == 0 ? assets.sparkleMaterial : assets.fruitMaterials[(int)kind];
                bit.AddComponent<BurstEffect>().Initialize(new Vector3(Random.Range(-2f, 2f), Random.Range(1.5f, 3.5f), Random.Range(-2f, 2f)), Random.Range(.5f, .9f));
            }
        }
        void FitCamera()
        {
            lastWidth = Screen.width; lastHeight = Screen.height;
            gameCamera.rect = new Rect(.013f, .12f, .755f, .745f);
            float aspect = Mathf.Max(.5f, Screen.width * gameCamera.rect.width / (Screen.height * gameCamera.rect.height));
            float vertical = Board.Data.height * .866f + 2.3f;
            gameCamera.orthographicSize = Mathf.Max(vertical * .5f, (Board.Data.width + 1.7f) / (2 * aspect));
            var target = new Vector3(0, .45f, .2f);
            gameCamera.transform.position = target + new Vector3(0, 18, -10.3923f);
            gameCamera.transform.LookAt(target);
            Feedback.SetCameraRest();
        }
        static void ClearChildren(Transform root)
        {
            for (int i = root.childCount - 1; i >= 0; i--) { root.GetChild(i).gameObject.SetActive(false); Destroy(root.GetChild(i).gameObject); }
        }
        void OnDestroy() { if (Instance == this) Instance = null; }
    }
}
