using UnityEngine;

namespace Betobeto.Fruits
{
    [DisallowMultipleComponent]
    public sealed class FruitExpressionSwitcher : MonoBehaviour
    {
        public enum Expression { Normal, Surprised }
        [SerializeField] private SkinnedMeshRenderer normal;
        [SerializeField] private SkinnedMeshRenderer surprised;
        [SerializeField] private Expression expression;

        private void Reset() { FindParts(); Apply(); }
        private void Awake() { FindParts(); Apply(); }
        private void OnValidate() { FindParts(); Apply(); }

        private void FindParts()
        {
            foreach (var part in GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (part.name == "Expression_Normal") normal = part;
                if (part.name == "Expression_Surprised") surprised = part;
            }
        }

        public void SetExpression(Expression value)
        {
            expression = value;
            Apply();
        }

        // Convenient entry point for Animation Events and UnityEvents: 0 / 1.
        public void SetExpressionIndex(int index)
        {
            SetExpression(index == 1 ? Expression.Surprised : Expression.Normal);
        }

        private void Apply()
        {
            if (normal != null) normal.enabled = expression == Expression.Normal;
            if (surprised != null) surprised.enabled = expression == Expression.Surprised;
        }
    }
}
