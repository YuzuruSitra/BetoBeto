using UnityEngine;

namespace BetoBeto.UI
{
    /// <summary>All artwork and UI use this centered, aspect-preserving design canvas.</summary>
    public static class KitchenLayout
    {
        public static readonly Vector2 DesignSize = new Vector2(1600, 900);
        public static readonly Rect Board = new Rect(134, 108, 894, 650);

        public static Rect Viewport(int width, int height)
        {
            float scale = Mathf.Min(width / DesignSize.x, height / DesignSize.y);
            var margin = (new Vector2(width, height) - DesignSize * scale) * .5f;
            return new Rect((margin.x + Board.x * scale) / width, (margin.y + Board.y * scale) / height,
                Board.width * scale / width, Board.height * scale / height);
        }

        public static Vector2 FeedbackPosition(Vector3 viewport)
        {
            return new Vector2(Board.x + Mathf.Clamp(viewport.x, 180f / Board.width, 1 - 180f / Board.width) * Board.width,
                Board.y + Mathf.Clamp(viewport.y, .08f, .90f) * Board.height);
        }
    }
}
