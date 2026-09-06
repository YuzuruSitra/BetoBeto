using UnityEngine;

namespace BetoBeto.UI
{
    /// <summary>All artwork and UI use this centered, aspect-preserving design canvas.</summary>
    public static class KitchenLayout
    {
        public static readonly Vector2 DesignSize = new Vector2(1600, 900);
        public static readonly Rect Board = new Rect(134, 108, 894, 650);
        // Screen-space footprint of the real towel behind the recipe, in bottom-left design coordinates.
        public static readonly Rect RecipeCloth = new Rect(1130, 318, 462, 415);

        public static Rect Viewport(int width, int height)
            => Viewport(Board, width, height);

        public static Rect Viewport(Rect area, int width, int height)
        {
            float scale = Mathf.Min(width / DesignSize.x, height / DesignSize.y);
            var margin = (new Vector2(width, height) - DesignSize * scale) * .5f;
            return new Rect((margin.x + area.x * scale) / width, (margin.y + area.y * scale) / height,
                area.width * scale / width, area.height * scale / height);
        }

        public static Vector2 FeedbackPosition(Vector3 viewport)
        {
            return new Vector2(Board.x + Mathf.Clamp(viewport.x, 180f / Board.width, 1 - 180f / Board.width) * Board.width,
                Board.y + Mathf.Clamp(viewport.y, .08f, .90f) * Board.height);
        }
    }
}
