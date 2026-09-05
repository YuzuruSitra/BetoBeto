using UnityEngine;

namespace BetoBeto.UI
{
    public static class KitchenArt
    {
        static readonly Sprite[] fruits = new Sprite[4];
        static Sprite tart;
        public static Sprite Tart => tart != null ? tart : tart = Make("TartBase", new Rect(0, 0, 1, 1));
        public static Sprite Fruit(int kind)
        {
            if (fruits[kind] == null)
                fruits[kind] = Make("FruitIcons", new Rect(kind % 2 * .5f, kind < 2 ? .5f : 0, .5f, .5f));
            return fruits[kind];
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
