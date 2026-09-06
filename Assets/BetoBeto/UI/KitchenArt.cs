using UnityEngine;

namespace BetoBeto.UI
{
    public static class KitchenArt
    {
        static readonly Sprite[] fruits = new Sprite[4];
        static Sprite hudChef, hudControls, hudEscape, hudIngredientCard, hudPause;
        static Sprite hudProgressBadge, hudRecipeFrame, hudScore, hudTime;
        static Sprite tart;

        public static Sprite HudChef => Load(ref hudChef, "HudChef");
        public static Sprite HudControls => Load(ref hudControls, "HudControls");
        public static Sprite HudEscape => Load(ref hudEscape, "HudEscape");
        public static Sprite HudIngredientCard => Load(ref hudIngredientCard, "HudIngredientCard");
        public static Sprite HudPause => Load(ref hudPause, "HudPause");
        public static Sprite HudProgressBadge => Load(ref hudProgressBadge, "HudProgressBadge");
        public static Sprite HudRecipeFrame => Load(ref hudRecipeFrame, "HudRecipeFrame");
        public static Sprite HudScore => Load(ref hudScore, "HudScore");
        public static Sprite HudTime => Load(ref hudTime, "HudTime");
        public static Sprite Tart => Load(ref tart, "TartBase");
        public static Sprite Fruit(int kind)
        {
            if (fruits[kind] == null)
                fruits[kind] = Make("FruitIcons", new Rect(kind % 2 * .5f, kind < 2 ? .5f : 0, .5f, .5f));
            return fruits[kind];
        }

        static Sprite Load(ref Sprite sprite, string path)
        {
            if (sprite == null) sprite = Make(path, new Rect(0, 0, 1, 1));
            return sprite;
        }

        static Sprite Make(string path, Rect uv)
        {
            var texture = Resources.Load<Texture2D>("UI/" + path);
            if (texture == null) return null;
            return Sprite.Create(texture, new Rect(uv.x * texture.width, uv.y * texture.height,
                uv.width * texture.width, uv.height * texture.height), new Vector2(.5f, .5f), 100);
        }
    }
}
