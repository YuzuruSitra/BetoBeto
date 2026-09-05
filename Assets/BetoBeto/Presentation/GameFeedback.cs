using System.Collections.Generic;
using BetoBeto.Core;
using BetoBeto.Enemies;
using BetoBeto.Player;
using BetoBeto.Stage;
using UnityEngine;
using UnityEngine.Rendering;

namespace BetoBeto.Presentation
{
    /// <summary>Impact presentation. Never changes harvest rules or uses global timeScale.</summary>
    public sealed class GameFeedback : MonoBehaviour
    {
        public static readonly Color Mint = new Color(.48f, 1, .83f);
        public static readonly Color Lavender = new Color(.84f, .67f, 1);
        GameController game;
        readonly Dictionary<Vector2Int, Transform> props = new Dictionary<Vector2Int, Transform>();
        Vector3 cameraRest;
        float cameraSize, shake, shakeRemaining, freezeUntil;
        public bool HitStopped => Time.unscaledTime < freezeUntil;
        public float SimulationDelta => HitStopped ? 0 : Mathf.Min(Time.deltaTime, .1f);

        public void Initialize(GameController controller)
        {
            game = controller;
            foreach (var prop in game.layout.GetComponentsInChildren<StageObject>())
                props[game.Board.Data.Cell(prop.transform.position)] = prop.transform;
        }
        public void SetCameraRest()
        {
            cameraRest = game.GameCamera.transform.position;
            cameraSize = game.GameCamera.orthographicSize;
        }
        public void ResetFeedback()
        {
            freezeUntil = shake = shakeRemaining = 0;
            game.GameCamera.transform.position = cameraRest;
            game.GameCamera.orthographicSize = cameraSize;
        }
        void LateUpdate()
        {
            if (game.Session.State == GameState.Paused) return;
            shakeRemaining = Mathf.Max(0, shakeRemaining - Time.unscaledDeltaTime);
            float envelope = Mathf.Clamp01(shakeRemaining / .3f);
            float t = Time.unscaledTime * 70;
            Vector3 offset = game.GameCamera.transform.right * Mathf.Sin(t) + game.GameCamera.transform.up * Mathf.Cos(t * 1.37f) * .65f;
            game.GameCamera.transform.position = cameraRest + offset * (shake * envelope * envelope);
            game.GameCamera.orthographicSize = cameraSize * (1 - .014f * envelope);
            if (shakeRemaining == 0) shake = 0;
        }
        void Kick(float strength, float freeze)
        {
            shake = Mathf.Max(shake, strength);
            shakeRemaining = .3f;
            freezeUntil = Mathf.Max(freezeUntil, Time.unscaledTime + freeze);
        }
        public void SlideStart(FruitAgent fruit)
        {
            var point = fruit.transform.position;
            Ring(point, Mint, .25f, .78f, .29f);
            Splash(point, -fruit.Forward, 8, game.assets.droolMaterial, 2.1f);
            game.Audio.Play("slide");
        }
        public void ChainImpact(FruitAgent fruit)
        {
            var point = fruit.transform.position;
            float strength = Mathf.Min(fruit.Chain, 6);
            Ring(point, Color.white, .23f, 1.05f + strength * .07f, .3f);
            Ring(point, Mint, .34f, .87f, .38f);
            Splash(point, -fruit.Forward, 10 + Mathf.Min(fruit.Chain, 6), game.assets.droolMaterial, 2.7f);
            Kick(.075f + .015f * strength, .038f);
            game.Hud.FloatMessage(point + Vector3.up * 1.9f, fruit.Chain + " CHAIN!", Mint, 30 + Mathf.Min(fruit.Chain, 6), "chain");
            game.Audio.PlayChain(fruit.Chain);
        }
        public void ScareReady(Vector3 point)
        {
            Ring(point, Color.white, .35f, 1.2f, .32f);
            game.Audio.Play("scareReady");
        }
        public void FruitScared(FruitAgent fruit)
        {
            game.Hud.FloatMessage(fruit.transform.position + Vector3.up * 1.2f, "！", Lavender, 35);
            Ring(fruit.transform.position, Lavender, .18f, .55f, .22f);
        }
        public void ScareBurst(Vector3 source, Vector3 front, float seconds, int hits)
        {
            bool charged = ScareRules.IsCharged(seconds);
            float power = ScareRules.Charge01(seconds);
            Vector3 point = charged ? source : front;
            float radius = charged ? ScareRules.Radius(seconds) : .65f;
            Ring(point, Lavender, .15f, radius, .22f + power * .22f);
            Ring(point, Color.white, .1f, radius * .88f, .3f + power * .22f);
            if (!charged) Ring(source, Lavender, .15f, .65f, .28f);
            Splash(source, charged ? Vector3.zero : (front - source).normalized, 6 + Mathf.RoundToInt(power * 12), game.assets.sparkleMaterial, 1.3f + power);
            if (hits > 0) Kick(.045f + power * .06f, .025f);
            game.Hud.FloatMessage(source + Vector3.up * 1.5f, power >= 1 ? "わあっ！！" : "わっ！", Lavender, 26 + Mathf.RoundToInt(power * 10), "scare");
            game.Audio.PlayScare(power);
        }
        public void WallImpact(FruitAgent fruit)
        {
            Vector3 point = fruit.transform.position + fruit.Forward * .43f;
            Ring(point, new Color(1, .8f, .44f), .2f, 1.4f, .33f);
            Splash(point, -fruit.Forward, 18, game.assets.sparkleMaterial, 3.6f);
            Kick(.15f + Mathf.Min(fruit.Chain, 5) * .017f, .058f);
            game.Hud.FloatMessage(point + Vector3.up * .85f, "ゴツン！", new Color(1, .88f, .61f), 29);
            Wobble(fruit.TargetCell, fruit.Forward);
            game.Audio.Play("wall");
        }
        public void Ricochet(FruitAgent fruit, Vector2Int cell, bool scone)
        {
            Vector3 point = game.Board.Data.World(cell);
            Color color = scone ? new Color(1, .78f, .38f) : new Color(.83f, .57f, 1);
            Ring(point, color, .18f, 1.25f, .28f);
            Splash(point, fruit.Forward, 10, scone ? game.assets.cookieMaterial : game.assets.jellyMaterial, 2.5f);
            Wobble(cell, fruit.Forward);
            Kick(.09f, .025f);
            game.Hud.FloatMessage(point + Vector3.up * 1.15f, scone ? "カーン！" : "ぷるん！", color, 26);
            game.Audio.Play(scone ? "scone" : "jelly");
        }
        public void CookieImpact(FruitAgent fruit, Vector2Int cell, bool broken, int hitsLeft)
        {
            Vector3 point = game.Board.Data.World(cell);
            Ring(point, new Color(1, .8f, .48f), .2f, broken ? 1.6f : .85f, .32f);
            Splash(point, fruit.Forward, broken ? 24 : 7, game.assets.cookieMaterial, broken ? 3.6f : 1.8f);
            Wobble(cell, fruit.Forward);
            Kick(broken ? .18f : .08f, broken ? .055f : .025f);
            game.Hud.FloatMessage(point + Vector3.up * 1.15f, broken ? "パリーン！" : "あと " + hitsLeft + " 回", new Color(1, .88f, .64f), broken ? 30 : 23);
            game.Audio.Play(broken ? "cookieBreak" : "wall");
        }
        public void CookieRestored(Vector3 point)
        {
            Ring(point, new Color(1, .86f, .6f), .1f, .65f, .3f);
            Splash(point, Vector3.zero, 5, game.assets.cookieMaterial, .7f);
        }
        public void FreezeFruit(FruitAgent fruit)
        {
            Vector3 point = fruit.transform.position;
            Ring(point, new Color(.6f, .92f, 1), .18f, .85f, .4f);
            Splash(point, Vector3.zero, 8, game.assets.frostMaterial, 1.3f);
            game.Hud.FloatMessage(point + Vector3.up * 1.2f, "カチン！", new Color(.72f, .96f, 1), 24);
            game.Audio.Play("freeze");
        }
        public void MelonImpact(FruitAgent fruit)
        {
            game.Burst(fruit.transform.position, fruit.kind, 18);
            Ring(fruit.transform.position, new Color(1, .85f, .5f), .3f, 1.55f, .36f);
            Kick(.22f, .07f);
            Wobble(fruit.Cell, fruit.Forward);
            game.Hud.FloatMessage(fruit.transform.position + Vector3.up, "皮が割れた！", new Color(1, .89f, .61f), 29);
            game.Audio.Play("wall");
        }
        public void Harvest(FruitAgent fruit)
        {
            game.Burst(fruit.transform.position, fruit.kind, 25 + Mathf.Min(fruit.Chain, 6) * 3);
            var color = game.assets.fruitMaterials[(int)fruit.kind].color;
            Ring(fruit.transform.position, Color.white, .3f, 1.25f, .28f);
            Ring(fruit.transform.position, color, .5f, 1.8f, .43f);
            Kick(.16f + Mathf.Min(fruit.Chain, 6) * .017f, .06f);
            Wobble(fruit.Cell, fruit.Forward);
            game.Hud.ScoreMessage(fruit.transform.position + Vector3.up, 100 * fruit.Chain);
            game.Audio.Play("harvest");
        }
        void Wobble(Vector2Int cell, Vector3 direction)
        {
            if (!props.TryGetValue(cell, out var prop) || prop == null) return;
            var wobble = prop.GetComponent<ImpactWobble>();
            if (wobble == null) wobble = prop.gameObject.AddComponent<ImpactWobble>();
            wobble.Hit(direction);
        }
        public void TrailDrop(Vector3 point, Vector3 direction)
        {
            var scale = new Vector3(.10f, .05f, .17f) * Random.Range(.7f, 1.3f);
            Piece(point + Vector3.up * .12f, scale, -direction * .7f + Vector3.up * .8f, .34f, game.assets.droolMaterial);
        }
        public void Splash(Vector3 point, Vector3 direction, int count, Material material, float force)
        {
            for (int i = 0; i < count; i++)
            {
                Vector2 random = Random.insideUnitCircle;
                Vector3 speed = (new Vector3(random.x, 0, random.y) + direction * .5f) * force;
                speed.y = Random.Range(1.2f, 3.3f);
                float size = Random.Range(.07f, .15f);
                Piece(point + Vector3.up * .35f, new Vector3(size, size * 1.4f, size), speed, Random.Range(.35f, .65f), i % 4 == 0 ? game.assets.sparkleMaterial : material);
            }
        }
        void Piece(Vector3 point, Vector3 scale, Vector3 velocity, float life, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Juicy splash"; Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(game.FeedbackRoot);
            go.transform.position = point; go.transform.localScale = scale;
            var renderer = go.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            go.AddComponent<BurstEffect>().Initialize(velocity, life);
        }
        public void Ring(Vector3 point, Color color, float start, float end, float life)
        {
            var go = new GameObject("Impact ripple");
            go.transform.SetParent(game.FeedbackRoot);
            go.transform.position = point + Vector3.up * .11f;
            go.AddComponent<ShockRing>().Initialize(game.assets.effectMaterial, color, start, end, life);
        }
    }
}
