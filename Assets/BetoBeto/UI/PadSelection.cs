using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BetoBeto.UI
{
    [RequireComponent(typeof(Outline))]
    public sealed class PadSelection : MonoBehaviour, ISelectHandler, IDeselectHandler
    {
        Outline outline;
        void Awake()
        {
            outline = GetComponent<Outline>();
            outline.effectColor = new Color(.31f, .86f, .77f, 1);
            outline.effectDistance = new Vector2(4, -4);
            outline.enabled = false;
        }
        public void OnSelect(BaseEventData data) { outline.enabled = true; }
        public void OnDeselect(BaseEventData data) { outline.enabled = false; }
    }
}
