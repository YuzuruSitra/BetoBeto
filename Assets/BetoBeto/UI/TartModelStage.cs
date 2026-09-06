using System.Collections.Generic;
using BetoBeto.Audio;
using UnityEngine;

namespace BetoBeto.UI
{
    /// <summary>The recipe tart as real geometry. Earned ingredients fall in from above and settle onto the shell.</summary>
    public sealed class TartModelStage : MonoBehaviour
    {
        public const string ModelPath = "FruitTart_Assembled";
        // Three slots per fruit kind, back to front, matching TartPreview.IsToppingCollected.
        public static readonly string[,] ToppingParts = {
            { "Strawberry_1 BerryHalf_1", "Strawberry_2 BerryHalf_2", "Strawberry_3 BerryHalf_3 TopMint" },
            { "Blueberry_1 Blueberry_2", "Blueberry_3 Blueberry_4", "Blueberry_5" },
            { "Mandarin_1", "Mandarin_2", "Mandarin_3" },
            { "Melon_1 Melon_2", "Melon_3", "Melon_4" }
        };
        const float FallSeconds = .45f, SettleSeconds = .34f, StaggerSeconds = .08f;

        sealed class Piece
        {
            public Transform Model;
            public Vector3 RestPosition, RestScale;
            public Quaternion RestRotation, EntryRotation;
            public float Delay, Age, Height, Fall;
            public bool Landed, Silent;
        }

        /// <summary>Local units the ingredients fall through; the preview camera sets it above its own frame.</summary>
        public float dropHeight = .3f;
        public float swayDegrees = 6.5f, swaySeconds = 7.5f;
        readonly List<Piece>[,] toppings = new List<Piece>[4, 3];
        readonly bool[,] collected = new bool[4, 3];
        readonly List<Piece> falling = new List<Piece>();
        static float lastLandingSound = -1;
        Quaternion restRotation = Quaternion.identity;
        float sway;

        public bool Ready { get; private set; }
        /// <summary>World bounds with every ingredient at rest, so framing never depends on the live recipe.</summary>
        public Bounds RestBounds { get; private set; }

        void Awake() { restRotation = transform.localRotation; }

        public bool Build()
        {
            restRotation = transform.localRotation;
            var prefab = Resources.Load<GameObject>(ModelPath);
            if (prefab == null)
            {
                Debug.LogWarning("Recipe tart model missing from Resources: " + ModelPath);
                return false;
            }
            var model = Instantiate(prefab, transform);
            model.name = "Assembled tart";
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = new Quaternion(0,0.264462173f,0,0.964396119f);
            // The preview is decoration only: nothing here may take part in physics or picking.
            foreach (var collider in model.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            foreach (var body in model.GetComponentsInChildren<Rigidbody>(true)) body.isKinematic = true;
            var parts = new Dictionary<string, Transform>();
            foreach (Transform child in model.transform) parts[child.name] = child;
            RestBounds = Measure(model);
            for (int kind = 0; kind < 4; kind++)
                for (int slot = 0; slot < 3; slot++)
                {
                    var pieces = new List<Piece>();
                    foreach (string name in ToppingParts[kind, slot].Split(' '))
                    {
                        if (!parts.TryGetValue(name, out var part))
                        {
                            Debug.LogWarning("Recipe tart model has no ingredient named " + name);
                            continue;
                        }
                        pieces.Add(new Piece
                        {
                            Model = part,
                            RestPosition = part.localPosition,
                            RestRotation = part.localRotation,
                            RestScale = part.localScale,
                            Delay = pieces.Count * StaggerSeconds
                        });
                        part.gameObject.SetActive(false);
                    }
                    toppings[kind, slot] = pieces;
                }
            Ready = true;
            return true;
        }
        static Bounds Measure(GameObject model)
        {
            var renderers = model.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return new Bounds(model.transform.position, Vector3.one * .2f);
            var bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }
        /// <summary>Reveals or clears one ingredient group; a new group drops in unless the caller asks for it placed.
        /// Returns whether the group changed.</summary>
        public bool SetCollected(int kind, int slot, bool earned, bool instant = false)
        {
            if (!Ready || collected[kind, slot] == earned) return false;
            collected[kind, slot] = earned;
            foreach (var piece in toppings[kind, slot])
            {
                falling.Remove(piece);
                piece.Model.gameObject.SetActive(earned);
                Rest(piece);
                if (!earned || instant) continue;
                piece.Height = dropHeight; piece.Fall = FallSeconds; piece.Silent = false;
                piece.EntryRotation = piece.RestRotation * Quaternion.Euler(
                    Random.Range(-34f, 34f), Random.Range(-180f, 180f), Random.Range(-34f, 34f));
                Begin(piece);
            }
            return true;
        }
        /// <summary>A count that does not finish a group still shows on the tart: its ingredients give a small hop.</summary>
        public void Nudge(int kind)
        {
            if (!Ready) return;
            for (int slot = 0; slot < 3; slot++)
            {
                if (!collected[kind, slot]) continue;
                foreach (var piece in toppings[kind, slot])
                {
                    if (falling.Contains(piece)) continue;
                    piece.Height = dropHeight * .12f; piece.Fall = .16f; piece.Silent = true;
                    piece.EntryRotation = piece.RestRotation;
                    Begin(piece);
                }
            }
        }
        void Begin(Piece piece)
        {
            piece.Age = 0; piece.Landed = false;
            Place(piece, 1);
            falling.Add(piece);
        }
        public bool IsCollected(int kind, int slot) => collected[kind, slot];
        public bool IsDropping(int kind, int slot)
        {
            if (!Ready) return false;
            foreach (var piece in toppings[kind, slot]) if (falling.Contains(piece)) return true;
            return false;
        }
        public IReadOnlyList<Transform> Pieces(int kind, int slot)
        {
            var models = new List<Transform>();
            if (Ready) foreach (var piece in toppings[kind, slot]) models.Add(piece.Model);
            return models;
        }
        public void ShowAll()
        {
            for (int kind = 0; kind < 4; kind++)
                for (int slot = 0; slot < 3; slot++)
                    SetCollected(kind, slot, true, true);
        }
        void Update()
        {
            // The preview keeps its own beat: the kitchen's hit stop and pauses must not freeze it.
            float delta = Time.unscaledDeltaTime;
            sway += delta;
            transform.localRotation = restRotation *
                Quaternion.Euler(0, Mathf.Sin(sway * (2 * Mathf.PI / swaySeconds)) * swayDegrees, 0);
            for (int i = falling.Count - 1; i >= 0; i--)
                if (Animate(falling[i], delta)) falling.RemoveAt(i);
        }
        /// <summary>Advances one ingredient; returns true once it has settled.</summary>
        bool Animate(Piece piece, float delta)
        {
            piece.Age += delta;
            float time = piece.Age - piece.Delay;
            if (time < 0) return false;
            if (time < piece.Fall)
            {
                float fall = time / piece.Fall;
                // Released from rest, so the ingredient accelerates into the tart instead of drifting down.
                Place(piece, 1 - fall * fall);
                piece.Model.localRotation = Quaternion.Slerp(piece.EntryRotation, piece.RestRotation, fall * fall);
                piece.Model.localScale = piece.RestScale;
                return false;
            }
            if (!piece.Landed)
            {
                piece.Landed = true;
                if (!piece.Silent && GameAudio.Instance != null && Time.unscaledTime - lastLandingSound > .07f)
                {
                    lastLandingSound = Time.unscaledTime;
                    GameAudio.Instance.Play("chocolate");
                }
            }
            float settle = Mathf.Clamp01((time - piece.Fall) / SettleSeconds);
            if (settle >= 1) { Rest(piece); return true; }
            float squash = Mathf.Sin(settle * Mathf.PI) * (1 - settle) * .28f;
            piece.Model.localPosition = piece.RestPosition;
            piece.Model.localRotation = piece.RestRotation;
            piece.Model.localScale = Vector3.Scale(piece.RestScale,
                new Vector3(1 + squash * .55f, 1 - squash, 1 + squash * .55f));
            return false;
        }
        void Place(Piece piece, float height)
        {
            piece.Model.localPosition = piece.RestPosition + Vector3.up * (piece.Height * height);
        }
        static void Rest(Piece piece)
        {
            piece.Model.localPosition = piece.RestPosition;
            piece.Model.localRotation = piece.RestRotation;
            piece.Model.localScale = piece.RestScale;
        }
    }
}
