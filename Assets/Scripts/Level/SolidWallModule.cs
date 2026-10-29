using UnityEngine;

namespace MoriEchoes
{
    public sealed class SolidWallModule : MonoBehaviour
    {
        public bool IsOpen { get; private set; }
        public bool HasIllustration { get; private set; }
        public Vector2 Size { get; private set; }
        private GameManager game;
        private BoxCollider2D blocker;
        private Transform panels;
        private SpriteRenderer status;
        private float retraction;

        public void Initialize(GameManager owner, Vector2 size)
        {
            game = owner; Size = size;
            blocker = GetComponent<BoxCollider2D>();
            if (blocker == null) blocker = gameObject.AddComponent<BoxCollider2D>();
            blocker.size = size;
            ThemeProfile theme = game.Level.Theme;
            panels = new GameObject("ArmoredBulkheadPanels").transform;
            panels.SetParent(transform, false);
            Sprite illustration = StageSurfaceArt.Resolve(game, game.Level);
            HasIllustration = illustration != null;
            int count = Mathf.Max(1, Mathf.CeilToInt(size.y / 1.7f));
            float height = size.y / count;
            for (int index = 0; index < count; index++)
            {
                float offset = -size.y * 0.5f + height * (index + 0.5f);
                SpriteRenderer panel = game.Art.Renderer("OpaqueArmorPlate_" + index, panels,
                    illustration != null ? illustration : game.Art.Side.Concrete,
                    illustration != null ? Color.white : Color.Lerp(theme.structure, Color.white, 0.25f),
                    illustration != null ? new Vector2(size.x / illustration.bounds.size.x, height / illustration.bounds.size.y) : new Vector2(size.x, height), 40);
                panel.transform.localPosition = new Vector3(0, offset, 0);
                if (illustration == null)
                {
                    Detail("InsetPanel", new Vector2(0, offset), new Vector2(size.x * 0.72f, height * 0.7f), theme.structure * 0.65f, 41);
                    Detail("PlateBevel", new Vector2(-size.x * 0.34f, offset), new Vector2(0.055f, height * 0.7f), theme.structure * 2.2f, 42);
                    Detail("PanelSeam", new Vector2(0, offset - height * 0.47f), new Vector2(size.x, 0.045f), Color.black, 42);
                }
                for (int side = -1; side <= 1; side += 2)
                {
                    SpriteRenderer bolt = game.Art.Renderer("RecessedFastener", panels, game.Art.Disc,
                        new Color(0.55f, 0.65f, 0.72f), Vector2.one * 0.08f, 44);
                    bolt.transform.localPosition = new Vector3(side * size.x * 0.36f, offset + height * 0.32f, 0);
                }
            }
            status = Detail("LockStatusStrip", new Vector2(0, 0), new Vector2(size.x * 0.14f, size.y * 0.75f), theme.danger, 45);
            status.enabled = !HasIllustration;
        }

        private SpriteRenderer Detail(string label, Vector2 position, Vector2 size, Color tint, int order)
        {
            SpriteRenderer renderer = game.Art.Renderer(label, panels, game.Art.Square, tint, size, order);
            renderer.transform.localPosition = position;
            return renderer;
        }

        public void Open()
        {
            if (IsOpen) return;
            IsOpen = true; blocker.enabled = false;
            status.color = game.Level.Theme.accent;
            game.Effects.Burst(transform.position, game.Level.Theme.accent, 8);
        }

        public void Close()
        {
            IsOpen = false; blocker.enabled = true; retraction = 0;
            panels.localPosition = Vector3.zero;
            status.color = game.Level.Theme.danger;
        }

        private void Update()
        {
            if (game == null || !game.IsPlaying) return;
            retraction = Mathf.MoveTowards(retraction, IsOpen ? 1 : 0, Time.deltaTime * 1.6f);
            panels.localPosition = Vector3.up * (Size.y + 0.2f) * Mathf.SmoothStep(0, 1, retraction);
        }
    }
}
