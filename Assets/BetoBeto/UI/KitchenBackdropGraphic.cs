using UnityEngine;
using UnityEngine.UI;

namespace BetoBeto.UI
{
    /// <summary>Draws the illustrated surround without covering the live 3D camera.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class KitchenBackdropGraphic : MaskableGraphic
    {
        public bool showBoard;
        Texture2D artwork;
        public override Texture mainTexture => artwork != null ? artwork : base.mainTexture;

        protected override void OnEnable()
        {
            // Keep a managed reference: Single scene loads unload assets after the new UI is created.
            artwork = Resources.Load<Texture2D>("UI/KitchenBackdrop");
            base.OnEnable();
            SetAllDirty();
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            if (!showBoard) { Quad(mesh, new Rect(0, 0, 1600, 900)); return; }
            var hole = KitchenLayout.Board;
            Quad(mesh, new Rect(0, 0, 1600, hole.yMin));
            Quad(mesh, new Rect(0, hole.yMax, 1600, 900 - hole.yMax));
            Quad(mesh, new Rect(0, hole.yMin, hole.xMin, hole.height));
            Quad(mesh, new Rect(hole.xMax, hole.yMin, 1600 - hole.xMax, hole.height));
        }
        void Quad(VertexHelper mesh, Rect r)
        {
            int n = mesh.currentVertCount;
            var offset = rectTransform.rect.min;
            mesh.AddVert(offset + new Vector2(r.xMin, r.yMin), color, new Vector2(r.xMin / 1600, r.yMin / 900));
            mesh.AddVert(offset + new Vector2(r.xMin, r.yMax), color, new Vector2(r.xMin / 1600, r.yMax / 900));
            mesh.AddVert(offset + new Vector2(r.xMax, r.yMax), color, new Vector2(r.xMax / 1600, r.yMax / 900));
            mesh.AddVert(offset + new Vector2(r.xMax, r.yMin), color, new Vector2(r.xMax / 1600, r.yMin / 900));
            mesh.AddTriangle(n, n + 1, n + 2); mesh.AddTriangle(n, n + 2, n + 3);
        }
    }
}
