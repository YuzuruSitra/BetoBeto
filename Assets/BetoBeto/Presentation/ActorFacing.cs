using BetoBeto.Stage;
using UnityEngine;
using UnityEngine.Rendering;

namespace BetoBeto.Presentation
{
    /// <summary>The model's face is local -Z; the grid's down direction is world -Z.</summary>
    public sealed class ActorFacing : MonoBehaviour
    {
        public Vector2Int Direction { get; private set; } = Directions.Down;
        public void Initialize(Material material, bool player)
        {
            var marker = new GameObject("Facing chevron").AddComponent<LineRenderer>();
            marker.transform.SetParent(transform, false);
            marker.sharedMaterial = material;
            marker.useWorldSpace = false;
            marker.positionCount = 3;
            float reach = player ? .62f : .49f;
            marker.SetPositions(new[] { new Vector3(-.14f, .045f, -reach + .14f), new Vector3(0, .045f, -reach), new Vector3(.14f, .045f, -reach + .14f) });
            marker.startWidth = marker.endWidth = player ? .065f : .04f;
            marker.startColor = marker.endColor = player ? new Color(.55f, 1, .88f, 1) : new Color(1, .94f, .73f, .75f);
            marker.numCornerVertices = 3; marker.numCapVertices = 3;
            marker.shadowCastingMode = ShadowCastingMode.Off; marker.receiveShadows = false;
            Face(Direction);
        }
        public void Face(Vector2Int direction)
        {
            if (direction == Vector2Int.zero) return;
            Direction = direction;
            transform.rotation = Quaternion.LookRotation(new Vector3(-direction.x, 0, direction.y), Vector3.up);
        }
    }
}
