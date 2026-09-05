using BetoBeto.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BetoBeto.UI
{
    /// <summary>Each ingredient group fills independently from the live recipe, including zero-goal recipes.</summary>
    public sealed class TartPreview : MonoBehaviour
    {
        readonly Image[] toppings = new Image[12];
        // Back-to-front composition in a normalized, 500 x 340 illustration.
        static readonly Vector2[] Positions = {
            new Vector2(155, 103), new Vector2(226, 88), new Vector2(293, 119),
            new Vector2(200, 144), new Vector2(269, 151), new Vector2(327, 172),
            new Vector2(348, 119), new Vector2(321, 216), new Vector2(194, 208),
            new Vector2(151, 163), new Vector2(242, 214), new Vector2(365, 194)
        };
        public void Initialize()
        {
            Add("Tart plate and custard", KitchenArt.Tart, new Vector2(250, 170), new Vector2(500, 340));
            for (int i = 0; i < toppings.Length; i++)
            {
                int kind = i / 3;
                float size = kind == 0 ? 103 : kind == 1 ? 72 : 88;
                toppings[i] = Add("Ingredient " + kind + " " + i % 3, KitchenArt.Fruit(kind), Positions[i], Vector2.one * size);
            }
        }
        Image Add(string label, Sprite sprite, Vector2 position, Vector2 size)
        {
            var image = new GameObject(label, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            image.rectTransform.SetParent(transform, false);
            image.rectTransform.anchorMin = image.rectTransform.anchorMax = new Vector2(0, 1);
            image.rectTransform.pivot = new Vector2(.5f, .5f);
            image.rectTransform.anchoredPosition = new Vector2(position.x, -position.y);
            image.rectTransform.sizeDelta = size;
            image.sprite = sprite; image.preserveAspect = true; image.raycastTarget = false;
            return image;
        }
        public static bool IsToppingCollected(int slot, int harvested, int goal)
            => goal > 0 && harvested > 0 && (slot == 2 ? harvested >= goal
                : slot < Mathf.CeilToInt(Mathf.Clamp01(harvested / (float)goal) * 2));
        public void Refresh(GameSession session)
        {
            for (int i = 0; i < toppings.Length; i++)
            {
                int kind = i / 3, goal = session.Recipe.For((FruitKind)kind);
                toppings[i].gameObject.SetActive(goal > 0);
                toppings[i].color = IsToppingCollected(i % 3, session.Harvested[kind], goal)
                    ? Color.white : new Color(.68f, .61f, .50f, .16f);
            }
        }
        public void ShowComplete()
        {
            foreach (var topping in toppings) { topping.gameObject.SetActive(true); topping.color = Color.white; }
        }
    }
}
