using UnityEngine;

namespace MoriEchoes
{
    // All traversable structures in a stage share its main surface illustration.
    public static class StageSurfaceArt
    {
        public static Sprite Resolve(GameManager game, LevelManager level)
        {
            int stage = level.Stage;
            if (stage < 5 && game.Config.platformSprites != null && stage < game.Config.platformSprites.Length
                && game.Config.platformSprites[stage] != null) return game.Config.platformSprites[stage];
            return level.Theme.Architecture(0) ?? game.Art.Side.Concrete;
        }

        public static void Draw(GameManager game, LevelManager level, Transform root, Vector2 size,
            bool ledge, bool ceiling = false, int order = 5)
        {
            Sprite image = Resolve(game, level);
            bool wall = size.y > size.x;
            int count = Mathf.Max(1, Mathf.CeilToInt((wall ? size.y : size.x) / (wall ? 2.6f : 5.6f)));
            float span = (wall ? size.y : size.x) / count;
            for (int index = 0; index < count; index++)
            {
                float height = ledge ? Mathf.Min(1.1f, span * image.bounds.size.y / image.bounds.size.x) : wall ? span : size.y;
                float width = wall ? size.x : span;
                SpriteRenderer panel = game.Art.Renderer("StageSurfacePanel", root, image, Color.white,
                    new Vector2(width / image.bounds.size.x, height / image.bounds.size.y), order);
                float along = -(wall ? size.y : size.x) * 0.5f + span * (index + 0.5f);
                panel.flipY = ceiling;
                panel.transform.localPosition = wall ? new Vector2(0, along)
                    : new Vector2(along, ledge ? (ceiling ? 1 : -1) * (height - size.y) * 0.5f : 0);
            }
        }
    }
}
