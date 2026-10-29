using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace MoriEchoes
{
    public sealed class ParallaxLayer : MonoBehaviour
    {
        public Camera Camera;
        public float Factor;
        private void LateUpdate()
        {
            if (Camera != null) transform.position = new Vector3(Camera.transform.position.x * (1 - Factor), Camera.transform.position.y * (1 - Factor) * 0.3f, 0);
        }
    }

    public sealed class SidePresentation : MonoBehaviour
    {
        private GameManager game;
        private LevelManager level;
        private VolumeProfile profile;
        private Light2D playerLight;
        private SpriteRenderer weather;

        public void Initialize(GameManager owner, LevelManager campaign)
        {
            game = owner; level = campaign;
            game.Environment = gameObject.AddComponent<SceneFeedback>();
            game.Environment.Initialize(game);
            ThemeProfile theme = level.Theme;
            var random = new System.Random(1000 + level.Stage);
            float[] factors = { 0.05f, 0.2f, 0.5f, 1f, 1.3f };
            for (int layerIndex = 0; layerIndex < factors.Length; layerIndex++)
            {
                var layer = new GameObject("Parallax_" + factors[layerIndex]); layer.transform.SetParent(transform, false);
                var parallax = layer.AddComponent<ParallaxLayer>(); parallax.Camera = game.CameraRig.Camera; parallax.Factor = factors[layerIndex];
                if (layerIndex == 0)
                {
                    SpriteRenderer sky = game.Art.Renderer("AtmosphereGradient", layer.transform, game.Art.SideSky, theme.sky * 0.65f, new Vector2(level.Length + 160, 65), -1000);
                    sky.transform.localPosition = new Vector3(level.Length * 0.15f, 8, 0);
                    SpriteRenderer moon = game.Art.Renderer("MoonHalo", layer.transform, game.Art.Glow, new Color(theme.accent.r, theme.accent.g, theme.accent.b, 0.35f), Vector2.one * 15, -995);
                    moon.transform.localPosition = new Vector3(10, 6.5f, 0);
                    game.Art.Renderer("Moon", layer.transform, game.Art.Disc, Color.Lerp(theme.accent, Color.white, 0.7f), Vector2.one * 3, -994).transform.localPosition = moon.transform.localPosition;
                    for (int index = 0; index < 18; index++)
                        game.Art.Renderer("CloudBand", layer.transform, game.Art.Side.Cloud, new Color(theme.accent.r, theme.accent.g, theme.accent.b, 0.12f), new Vector2(12, 3), -992)
                            .transform.localPosition = new Vector3(index * 10 - 20, 5 + (float)random.NextDouble() * 5, 0);
                    continue;
                }
                if (layerIndex == 3) continue;
                if (theme.background != null && (layerIndex == 2 || layerIndex == 4))
                {
                    Sprite[] props = game.Config.scenerySprites;
                    if (props != null && props.Length >= 8)
                        for (int index = -3; index < 30; index++)
                        {
                            int slot = layerIndex == 4 ? 3 : level.Stage >= 3 ? 7 : 4;
                            if (props[slot] == null) continue;
                            SpriteRenderer prop = game.Art.Renderer("ParallaxScenery", layer.transform, props[slot],
                                layerIndex == 4 ? new Color(0.35f, 0.45f, 0.55f, 0.7f) : new Color(0.48f, 0.65f, 0.8f, 0.6f), Vector2.one * (layerIndex == 4 ? 1.4f : 1.2f), layerIndex == 4 ? 250 : -700);
                            prop.transform.localPosition = new Vector3(index * 10, layerIndex == 4 ? -2.8f : props[slot].bounds.size.y * 0.6f, 0);
                        }
                    continue;
                }
                if (layerIndex == 1 && theme.background != null)
                {
                    float height = 18;
                    float width = height * theme.background.bounds.size.x / theme.background.bounds.size.y;
                    Vector2 scale = Vector2.one * (height / theme.background.bounds.size.y);
                    for (int tile = -2; tile < Mathf.CeilToInt(level.Length / width) + 3; tile++)
                    {
                        SpriteRenderer background = game.Art.Renderer("AnimeEnvironment", layer.transform, theme.background, Color.white, scale, -850);
                        background.transform.localPosition = new Vector3(tile * width, 5, 0);
                        background.flipX = Mathf.Abs(tile) % 2 == 1;
                    }
                    continue;
                }
                for (int index = -4; index < 38; index++)
                {
                    float width = 2 + (float)random.NextDouble() * 4;
                    float height = 3 + (float)random.NextDouble() * (layerIndex == 1 ? 8 : 6);
                    Color color = layerIndex == 4 ? new Color(theme.sky.r * 0.5f, theme.sky.g * 0.5f, theme.sky.b * 0.5f, 0.18f) : Color.Lerp(theme.distance, theme.structure, layerIndex == 1 ? 0 : 0.65f) * 1.3f;
                    if (theme.background != null && layerIndex == 2) color.a = 0.25f;
                    SpriteRenderer building = game.Art.Renderer(level.Stage == 2 ? "IceRidge" : "LayeredBuilding", layer.transform,
                        level.Stage == 2 ? game.Art.SideMountain : game.Art.Side.Architecture, color, new Vector2(width, height), layerIndex == 4 ? 250 : -900 + layerIndex * 100);
                    building.transform.localPosition = new Vector3(index * 5, height * 0.5f - (layerIndex == 4 ? 9 : 1), 0);
                    if (layerIndex == 4 || level.Stage == 2) continue;
                    for (int row = 0; row < 1; row++)
                    {
                        SpriteRenderer windows = game.Art.Renderer("WindowStrip", building.transform, game.Art.Square,
                            new Color(theme.accent.r * 1.6f, theme.accent.g * 1.6f, theme.accent.b * 1.6f, 0.7f), new Vector2(0.7f, 0.012f), building.sortingOrder + 1);
                        windows.transform.localPosition = new Vector3(0, row * 0.16f - 0.35f, 0);
                    }
                    if (level.Stage == 1)
                        game.Art.Renderer("FactoryPipe", building.transform, game.Art.Square, theme.distance * 1.5f, new Vector2(0.12f, 1.4f), building.sortingOrder + 2)
                            .transform.localPosition = new Vector3(0.3f, 0.2f, 0);
                }
            }
            var global = new GameObject("StageGlobalLight"); global.transform.SetParent(transform, false);
            Light2D light = global.AddComponent<Light2D>(); light.lightType = Light2D.LightType.Global; light.intensity = 0.85f;
            var lamp = game.Config.sideLightPrefab != null ? Instantiate(game.Config.sideLightPrefab, transform) : new GameObject("PlayerRimLight");
            lamp.name = "PlayerRimLight"; lamp.transform.SetParent(transform, false);
            playerLight = lamp.GetComponent<Light2D>() ?? lamp.AddComponent<Light2D>(); playerLight.lightType = Light2D.LightType.Point;
            playerLight.color = theme.accent; playerLight.intensity = 0.8f; playerLight.pointLightOuterRadius = 5;
            var post = new GameObject("ThemePostProcessing"); post.transform.SetParent(transform, false);
            var volume = post.AddComponent<Volume>(); volume.isGlobal = true;
            profile = ScriptableObject.CreateInstance<VolumeProfile>(); volume.sharedProfile = profile;
            Bloom bloom = profile.Add<Bloom>(true); bloom.intensity.Override(0.45f); bloom.threshold.Override(1.1f); bloom.scatter.Override(0.55f);
            Vignette vignette = profile.Add<Vignette>(true); vignette.intensity.Override(0.19f); vignette.smoothness.Override(0.4f);
            ColorAdjustments adjustments = profile.Add<ColorAdjustments>(true); adjustments.contrast.Override(9); adjustments.saturation.Override(7);
            game.CameraRig.Camera.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            weather = game.Art.Renderer("WeatherVeil", transform, game.Art.SideSky, Color.clear, new Vector2(50, 40), 450);
        }

        private void LateUpdate()
        {
            if (game.Player == null) return;
            playerLight.transform.position = game.Player.transform.position;
            weather.transform.position = (Vector2)game.CameraRig.transform.position;
            float storm = level.Stage == 2 ? Mathf.SmoothStep(0.02f, 0.22f, Mathf.Sin(Time.time * 0.35f) * 0.5f + 0.5f) : 0;
            weather.color = new Color(0.75f, 0.86f, 1, storm);
            if (level.Stage == 2 && level.Boss != null)
            {
                SideBossController boss = level.Boss.GetComponent<BossController>().Side;
                if (boss.Phase == 3) weather.color = new Color(0.01f, 0.015f, 0.04f, 0.65f + Mathf.Sin(Time.time * 2.5f) * 0.15f);
            }
        }
        private void OnDestroy()
        {
            if (profile == null) return;
            foreach (VolumeComponent component in profile.components) Destroy(component);
            Destroy(profile);
        }
    }
}
