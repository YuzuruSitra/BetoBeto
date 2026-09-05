using UnityEngine;
using UnityEngine.UI;

namespace BetoBeto.UI
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class SconeMapGraphic : MaskableGraphic
    {
        public int turns;
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            var rect = rectTransform.rect;
            var rotation = Quaternion.Euler(0, 0, -turns * 90);
            Vector3[] corners = { new Vector3(-.5f, .5f), new Vector3(.5f, .5f), new Vector3(-.5f, -.5f) };
            foreach (var corner in corners)
            {
                Vector3 p = rotation * corner;
                mesh.AddVert(new Vector3(rect.center.x + p.x * rect.width, rect.center.y + p.y * rect.height), color, Vector2.zero);
            }
            mesh.AddTriangle(0, 1, 2);
        }
    }
}
