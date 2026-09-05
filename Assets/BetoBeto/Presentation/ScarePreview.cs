using BetoBeto.Core;
using BetoBeto.Player;
using UnityEngine;
using UnityEngine.Rendering;

namespace BetoBeto.Presentation
{
    public sealed class ScarePreview : MonoBehaviour
    {
        GameController game;
        GhostController player;
        LineRenderer range, charge;
        Transform visual;
        Vector3 restScale;
        readonly Vector3[] corners = { new Vector3(-.48f, 0, -.48f), new Vector3(.48f, 0, -.48f), new Vector3(.48f, 0, .48f), new Vector3(-.48f, 0, .48f) };

        public void Initialize(GameController controller, GhostController ghost)
        {
            game = controller; player = ghost;
            range = MakeLine("Scare range", .045f, true);
            charge = MakeLine("Scare charge", .075f, false);
            visual = transform.Find("Visual");
            if (visual != null) restScale = visual.localScale;
        }
        LineRenderer MakeLine(string label, float width, bool loop)
        {
            var go = new GameObject(label);
            go.transform.SetParent(transform, false);
            var line = go.AddComponent<LineRenderer>();
            line.sharedMaterial = game.assets.effectMaterial;
            line.useWorldSpace = true; line.loop = loop; line.widthMultiplier = width;
            line.numCapVertices = 3; line.numCornerVertices = 3;
            line.shadowCastingMode = ShadowCastingMode.Off; line.receiveShadows = false;
            return line;
        }
        void LateUpdate()
        {
            bool visible = game.Session.State == GameState.Playing && GamepadControls.Ready;
            bool charged = player.IsCharging && ScareRules.IsCharged(player.ChargeSeconds);
            range.enabled = visible;
            charge.enabled = visible && player.IsCharging;
            if (!visible) return;
            Color color = GameFeedback.Lavender;
            if (player.Charge01 >= 1) color = Color.Lerp(color, Color.white, .4f + .3f * Mathf.Sin(Time.time * 16));
            range.startColor = range.endColor = color;
            charge.startColor = charge.endColor = Color.white;
            if (charged)
            {
                DrawArc(range, game.Board.Data.World(player.Cell, .065f), player.ScareRadius + .04f, 1, 64);
            }
            else
            {
                range.positionCount = 4;
                Vector3 point = game.Board.Data.World(player.Cell, .065f);
                Vector3 stretch = Vector3.one;
                if (game.Board.Data.Contains(player.ScareTarget))
                {
                    point = (point + game.Board.Data.World(player.ScareTarget, .065f)) * .5f;
                    stretch = new Vector3(1 + Mathf.Abs(player.Facing.x), 1, 1 + Mathf.Abs(player.Facing.y));
                }
                for (int i = 0; i < corners.Length; i++) range.SetPosition(i, point + Vector3.Scale(corners[i], stretch));
            }
            if (player.IsCharging)
                DrawArc(charge, transform.position + Vector3.up * .15f, .65f, player.Charge01, 65);
            if (visual != null)
            {
                float amount = player.IsCharging ? player.Charge01 : 0;
                float pulse = Mathf.Sin(Time.time * (8 + 12 * amount)) * .025f * amount;
                Vector3 scale = Vector3.Scale(restScale, new Vector3(1 + amount * .18f + pulse, 1 + amount * .1f - pulse, 1 + amount * .18f + pulse));
                visual.localScale = Vector3.Lerp(visual.localScale, scale, game.Feedback.SimulationDelta * 18);
            }
        }
        static void DrawArc(LineRenderer line, Vector3 center, float radius, float fraction, int points)
        {
            line.positionCount = points;
            for (int i = 0; i < points; i++)
            {
                float angle = i / (float)(line.loop ? points : points - 1) * Mathf.PI * 2 * fraction;
                line.SetPosition(i, center + new Vector3(Mathf.Sin(angle), 0, Mathf.Cos(angle)) * radius);
            }
        }
    }
}
