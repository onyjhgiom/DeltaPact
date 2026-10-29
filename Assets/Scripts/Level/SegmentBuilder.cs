using UnityEngine;

namespace MoriEchoes
{
    public sealed class SegmentBuilder : MonoBehaviour
    {
        private GameManager game;
        private LevelManager level;
        private ThemeProfile theme;

        public void Build(GameManager owner, LevelManager campaign)
        {
            game = owner; level = campaign; theme = level.Theme;
            gameObject.AddComponent<SidePresentation>().Initialize(game, level);
            foreach (SegmentDefinition segment in level.Segments)
            {
                int segmentIndex = System.Array.IndexOf(level.Segments, segment);
                float start = segment.start;
                Platform("Foundation", new Vector2(start + segment.width * 0.5f, -1.3f), new Vector2(segment.width, 2.6f), false);
                for (int detail = 0; detail < 4; detail++)
                {
                    int prop = 3 + (detail + level.Stage) % 5;
                    Sprite art = theme.Architecture(3 + (detail + level.Stage) % 3);
                    if (art == null && game.Config.scenerySprites != null && prop < game.Config.scenerySprites.Length) art = game.Config.scenerySprites[prop];
                    if (art == null) continue;
                    SpriteRenderer scenery = game.Art.Renderer("ReactiveScenery_" + prop, transform, art, Color.white, Vector2.one, -10);
                    scenery.transform.localScale = Vector3.one * (2.2f / Mathf.Max(art.bounds.size.x, art.bounds.size.y));
                    scenery.transform.position = new Vector2(start + 3 + detail * 6, scenery.bounds.size.y * 0.5f - 0.05f);
                    game.Environment.Register(scenery, prop == 5);
                }
                if (segment.boss) continue;
                float offset = segment.layout * 0.25f;
                Platform("ApproachStep", new Vector2(start + 4.2f, 1.35f + offset * 0.4f), new Vector2(2.8f, 0.38f), true);
                Platform("OneWayPlatform", new Vector2(start + 8, 1.6f + offset), new Vector2(5, 0.45f), true);
                bool hasElevator = level.Stage == 3 || level.Stage == 4 && segment.layout == 1;
                if (!hasElevator)
                    Platform("MidRoute", new Vector2(start + 12, 2.35f + offset * 0.55f), new Vector2(4.1f, 0.4f), true);
                float upperHeight = 3.15f + offset * 0.9f;
                PlatformSurface upper = Platform("UpperRoute", new Vector2(start + 17, upperHeight), new Vector2(5.1f, 0.45f), true);
                level.SecretRoutes?.BuildBranch(segmentIndex, start, upperHeight + 0.225f);
                Platform("ReturnPlatform", new Vector2(start + 23, 1.55f + ((segment.layout + level.Stage) % 3) * 0.38f), new Vector2(4.2f, 0.42f), true);
                Platform("ExitStep", new Vector2(start + 29, 1.45f + ((segment.layout + level.Stage + 1) % 2) * 0.42f), new Vector2(4.2f, 0.4f), true);
                bool secretLanding = segmentIndex == 3;
                if (level.Stage == 0) upper.Crumbling = !secretLanding;
                if (level.Stage == 2) { upper.Crumbling = !secretLanding; upper.Ice = !secretLanding; }
                if (hasElevator)
                {
                    PlatformSurface lift = Platform("Elevator", new Vector2(start + 12, 0.8f), new Vector2(2.8f, 0.4f), true);
                    lift.Elevator = true;
                }
                if (level.Stage == 1 || level.Stage == 4)
                {
                    PlatformSurface belt = Platform("ConveyorBelt", new Vector2(start + 17, 0.12f), new Vector2(8, 0.2f), false);
                    belt.ConveyorSpeed = segment.layout == 1 ? -2 : 2;
                    Hazard("HydraulicPress", new Vector2(start + 20, 2.3f), new Vector2(1.2f, 3.5f), 1, segment.start * 0.13f);
                }
                if (level.Stage == 3 || level.Stage == 4)
                {
                    Hazard("TimedLaserGrid", new Vector2(start + 18, 2.2f), new Vector2(0.12f, 4), 2, start * 0.17f);
                }
                if (level.Stage == 0 || level.Stage == 4)
                {
                    SpriteRenderer barrel = Decoration("ExplosiveBarrel", new Vector2(start + 19, 0.6f), new Vector2(0.7f, 1.1f), theme.danger, 25);
                    var explosive = barrel.gameObject.AddComponent<StageInteractable>(); explosive.Game = game; explosive.ConfigureBarrel();
                    if (game.Config.scenerySprites == null || game.Config.scenerySprites.Length == 0 || game.Config.scenerySprites[0] == null)
                        Decoration("BarrelEnergyStripe", new Vector2(start + 19, 0.65f), new Vector2(0.73f, 0.13f), new Color(2, 0.8f, 0.1f), 26);
                }
                if (segment.start < 84)
                {
                    var chip = Decoration("HiddenChip", new Vector2(start + 16, 4.6f), new Vector2(0.38f, 0.5f), new Color(1.6f, 1.3f, 0.4f), 55).gameObject.AddComponent<StageInteractable>();
                    chip.Game = game; chip.Chip = true;
                }
            }
            Platform("StartBoundary", new Vector2(-1, 4), new Vector2(2, 12), false);
            Platform("EndBoundary", new Vector2(level.Length + 1, 4), new Vector2(2, 12), false);
        }

        private PlatformSurface Platform(string name, Vector2 position, Vector2 size, bool oneWay)
        {
            var root = new GameObject(name); root.transform.SetParent(transform, false); root.transform.position = position;
            var collider = root.AddComponent<BoxCollider2D>(); collider.size = size;
            if (oneWay)
            {
                collider.usedByEffector = true;
                var effector = root.AddComponent<PlatformEffector2D>();
                effector.useOneWay = true; effector.useSideFriction = false; effector.useSideBounce = false;
                effector.useColliderMask = false; effector.surfaceArc = 165;
            }
            PlatformSurface surface = root.AddComponent<PlatformSurface>(); surface.OneWay = oneWay; surface.Ice = level.Stage == 2;
            StageSurfaceArt.Draw(game, level, root.transform, size, oneWay);
            return surface;
        }

        private SpriteRenderer Decoration(string name, Vector2 position, Vector2 size, Color color, int order)
        {
            SpriteRenderer renderer = game.Art.Renderer(name, transform, game.Art.Square, color, size, order);
            int slot = name == "ExplosiveBarrel" ? 0 : name == "DoorSwitch_E" ? 1 : name == "HiddenChip" ? 2 : -1;
            Sprite themed = slot == 0 ? theme.SegmentProp(0) : slot == 1 ? theme.Architecture(1) : game.Config.pickupSprites != null && game.Config.pickupSprites.Length > 5 ? game.Config.pickupSprites[5] : null;
            Sprite selected = themed != null ? themed : game.Config.scenerySprites != null && slot >= 0 && slot < game.Config.scenerySprites.Length ? game.Config.scenerySprites[slot] : null;
            if (slot >= 0 && selected != null)
            {
                renderer.sprite = selected;
                renderer.transform.localScale = Vector3.one * Mathf.Min(size.x / Mathf.Max(0.01f, selected.bounds.size.x), size.y / Mathf.Max(0.01f, selected.bounds.size.y));
                renderer.color = Color.white;
            }
            renderer.transform.position = position; return renderer;
        }

        private void Hazard(string name, Vector2 position, Vector2 size, int kind, float phase)
        {
            var root = new GameObject(name); root.transform.SetParent(transform, false); root.transform.position = position;
            StageHazard hazard = root.AddComponent<StageHazard>();
            Sprite sprite = kind == 1 ? theme.Architecture(2) : theme.encounterEffects != null && theme.encounterEffects.Length > 3 ? theme.encounterEffects[3] : null;
            if (sprite == null) sprite = game.Art.Side.Armor;
            game.Art.Renderer("IllustratedTrapMechanism", root.transform, sprite, Color.white,
                new Vector2(Mathf.Max(size.x, 0.55f) / sprite.bounds.size.x, size.y / sprite.bounds.size.y), 45);
            hazard.DamageSize = size;
            hazard.Game = game; hazard.Kind = kind; hazard.Phase = phase;
        }
    }
}
