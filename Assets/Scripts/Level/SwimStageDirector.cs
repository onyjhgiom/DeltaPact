using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace MoriEchoes
{
    // 水面/水下关卡驱动：横向推进，但玩家处于水中。
    // 第 9 关为海面，第 10 关为深渊；两者使用独立运动边界。
    public sealed class SwimStageDirector : MonoBehaviour
    {
        private const float SectionWidth = 30f;
        public const int Sections = 8;

        private readonly List<SpriteRenderer> reactiveProps = new List<SpriteRenderer>();
        private readonly List<SpriteRenderer> bubbles = new List<SpriteRenderer>();
        private readonly List<SpriteRenderer> godRays = new List<SpriteRenderer>();
        private readonly List<SpriteRenderer> waveCrests = new List<SpriteRenderer>();
        private readonly List<PlatformSurface> floatingPlatforms = new List<PlatformSurface>();
        private readonly List<SpriteRenderer> paintedBackdrops = new List<SpriteRenderer>();
        public IReadOnlyList<PlatformSurface> FloatingPlatforms => floatingPlatforms;
        private GameManager game;
        private LevelManager level;
        private Transform scenery;
        private Light2D playerRimLight;
        private SpriteRenderer surfaceBand;
        private int activeSection = -1;
        private int spawnedThisSection;
        private float nextSpawn;
        private float sectionClock;
        private float nextAmbientBubble;
        private EnemyAI finalBoss;
        private float depletedClock;
        private float pressureClock;
        private float nextRefill;
        private float ambientCenterX;

        public bool Underwater { get; private set; }
        public float SurfaceY { get; private set; }
        public float MinY { get; private set; }
        public float MaxY { get; private set; }
        public float CourseLength => SectionWidth * Sections;
        public Vector2 StartPoint { get; private set; }
        public int ActiveSection => activeSection;
        public int SpawnedEnemies { get; private set; }
        public float Oxygen { get; private set; } = 100;
        public int SurvivalHits { get; private set; }
        public float NormalizedDepth(float y) => Mathf.InverseLerp(MaxY, MinY, y);
        public float SurfaceHeightAt(float x) => SurfaceY + Mathf.Sin(Time.time * 1.7f + x * 0.4f) * 0.22f;
        public void RestoreOxygen(float amount) { Oxygen = Mathf.Clamp(Oxygen + amount, 0, 100); }

        public void Initialize(GameManager owner, LevelManager campaign)
        {
            game = owner; level = campaign;
            Underwater = level.IsUnderwaterStage;
            if (Underwater) { MinY = -7f; MaxY = 8f; SurfaceY = MaxY; }
            else { SurfaceY = 2f; MinY = SurfaceY - 2.2f; MaxY = SurfaceY + 6.2f; }
            StartPoint = new Vector2(3f, Underwater ? (MinY + MaxY) * 0.5f : SurfaceY);
            BuildEnvironment();
        }

        public SegmentDefinition[] CreateSegments()
        {
            var segments = new SegmentDefinition[Sections];
            for (int index = 0; index < Sections; index++)
                segments[index] = new SegmentDefinition
                {
                    start = index * SectionWidth,
                    width = SectionWidth,
                    layout = index % 3,
                    boss = index == Sections - 1
                };
            return segments;
        }

        private void BuildEnvironment()
        {
            scenery = new GameObject("SwimStage_SetDressing_" + level.Theme.title).transform;
            scenery.SetParent(level.transform, false);
            ThemeProfile theme = level.Theme;
            float midX = CourseLength * 0.5f;

            var globalLightObject = new GameObject("Swim_GlobalLight");
            globalLightObject.transform.SetParent(scenery, false);
            Light2D globalLight = globalLightObject.AddComponent<Light2D>();
            globalLight.lightType = Light2D.LightType.Global;
            globalLight.color = Color.Lerp(Color.white, theme.accent, Underwater ? 0.16f : 0.07f);
            globalLight.intensity = Underwater ? 0.82f : 0.96f;

            var rimObject = new GameObject("Swim_PlayerRimLight");
            rimObject.transform.SetParent(scenery, false);
            playerRimLight = rimObject.AddComponent<Light2D>();
            playerRimLight.lightType = Light2D.LightType.Point;
            playerRimLight.color = theme.accent;
            playerRimLight.intensity = Underwater ? 0.9f : 0.6f;
            playerRimLight.pointLightOuterRadius = 5.2f;

            // 大背景：整片水域/天空渐变底
            float spanY = (MaxY - MinY) + 18f;
            float midY = (MinY + MaxY) * 0.5f;
            game.Art.Renderer("Swim_BackdropGradient", scenery, game.Art.SideSky,
                new Color(theme.sky.r, theme.sky.g, theme.sky.b, 1f), new Vector2(CourseLength + 60, spanY), -1000)
                .transform.position = new Vector2(midX, midY);

            if (Underwater) BuildUnderwater(theme, midX, midY);
            else BuildSurface(theme, midX);
            if (!Underwater) BuildFloatingPlatforms(theme);
            if (theme.background != null)
            {
                for (int panel = -1; panel <= 1; panel++)
                {
                    SpriteRenderer backdrop = game.Art.Renderer("Swim_IllustratedBackdrop_" + panel, scenery, theme.background,
                        Color.white, Vector2.one * (64 / theme.background.bounds.size.x), -90);
                    paintedBackdrops.Add(backdrop);
                }
            }

            // 远处漂浮剪影（两层，简单视差感）
            for (int index = 0; index < 26; index++)
            {
                float x = 4 + index * (CourseLength / 26f);
                float depth = Underwater ? MinY + 1.5f + (index % 4) * 2.2f : SurfaceY - 1.2f - (index % 3) * 1.4f;
                Sprite silhouette = theme.Architecture(index % 2 + 1) ?? (index % 3 == 0 ? game.Art.Diamond : game.Art.Glow);
                SpriteRenderer prop = game.Art.Renderer("Swim_Distant_" + index, scenery, silhouette,
                    new Color(theme.structure.r, theme.structure.g, theme.structure.b, 0.28f),
                    new Vector2(0.7f + (index % 3) * 0.5f, 1.6f + (index % 4) * 0.9f), -60);
                prop.transform.position = new Vector2(x, depth);
            }
        }

        private void BuildFloatingPlatforms(ThemeProfile theme)
        {
            // Two nearby decks per group: water leap -> low deck -> upper deck -> next group.
            for (int index = 0; index < 24; index++)
            {
                bool upper = index % 2 == 1;
                float x = 14 + index / 2 * 18 + (upper ? 5.1f : 0);
                Vector2 size = new Vector2(upper ? 3.6f : 4.6f, 0.36f);
                var entity = new GameObject("SeaPontoon_" + index);
                entity.transform.SetParent(scenery, false);
                entity.transform.position = new Vector2(x, SurfaceY + (upper ? 1.05f : 0.35f));
                var collider = entity.AddComponent<BoxCollider2D>(); collider.size = size; collider.usedByEffector = true;
                var effector = entity.AddComponent<PlatformEffector2D>();
                effector.useOneWay = true; effector.useSideFriction = false;
                effector.useSideBounce = false; effector.useColliderMask = false; effector.surfaceArc = 165;
                PlatformSurface platform = entity.AddComponent<PlatformSurface>();
                platform.OneWay = platform.Buoyant = true; platform.BuoyancyPhase = index * 0.63f;
                // Versioned generated pontoon art can replace Architecture(0) without changing its collider.
                Sprite deckArt = theme.Architecture(0);
                if (deckArt != null)
                {
                    SpriteRenderer deck = game.Art.Renderer("IllustratedPontoonDeck", entity.transform, deckArt, Color.white,
                        Vector2.one * ((size.x + 0.3f) / deckArt.bounds.size.x), 12);
                    deck.transform.localPosition = Vector2.down * 0.12f;
                }
                else StageSurfaceArt.Draw(game, level, entity.transform, size, true, false, 12);
                for (int side = -1; side <= 1; side += 2)
                {
                    if (deckArt != null) continue;
                    SpriteRenderer floatBody = game.Art.Renderer("PontoonFlotationHull", entity.transform,
                        game.Art.Side.Armor, Color.Lerp(theme.structure, theme.accent, 0.2f), new Vector2(0.55f, 0.52f), 11);
                    floatBody.transform.localPosition = new Vector2(side * (size.x * 0.5f - 0.45f), -0.32f);
                }
                floatingPlatforms.Add(platform);
            }
        }

        private void BuildSurface(ThemeProfile theme, float midX)
        {
            // 天空（水面以上）
            game.Art.Renderer("Swim_Sky", scenery, game.Art.Square, theme.sky,
                new Vector2(CourseLength + 60, 30), -999).transform.position = new Vector2(midX, SurfaceY + 15f);
            // 水体（水面以下，半透明蓝）
            if (theme.background == null)
                game.Art.Renderer("Swim_WaterBody", scenery, game.Art.Square,
                    new Color(theme.distance.r, theme.distance.g, theme.distance.b, 0.5f),
                    new Vector2(CourseLength + 60, SurfaceY - MinY + 2), -20).transform.position
                    = new Vector2(midX, (SurfaceY + MinY) * 0.5f);
            // 水面高光带（波浪起伏，Tick 驱动）
            surfaceBand = game.Art.Renderer("Swim_SurfaceBand", scenery, game.Art.Glow,
                new Color(theme.accent.r, theme.accent.g, theme.accent.b, 0.85f), new Vector2(CourseLength + 60, 0.32f), 6);
            surfaceBand.transform.position = new Vector2(midX, SurfaceY);
            // 浪尖高光块
            for (int index = 0; index < 24; index++)
            {
                SpriteRenderer crest = game.Art.Renderer("Swim_WaveCrest_" + index, scenery, game.Art.Glow,
                    new Color(1f, 1f, 1f, 0.5f), new Vector2(1.6f + (index % 3) * 0.5f, 0.12f), 7);
                crest.transform.position = new Vector2(3 + index * (CourseLength / 24f), SurfaceY);
                waveCrests.Add(crest);
            }
            // 漂浮残骸/浮标
            for (int index = 0; index < 12; index++)
            {
                SpriteRenderer debris = game.Art.Renderer("Swim_Debris_" + index, scenery,
                    theme.Architecture(index % 2 == 0 ? 2 : 3) ?? (index % 2 == 0 ? game.Art.Side.Armor : game.Art.Diamond), Color.white,
                    Vector2.one * 1.1f, 4);
                debris.transform.position = new Vector2(6 + index * (CourseLength / 12f), SurfaceY + 0.15f);
                reactiveProps.Add(debris);
            }
            for (int index = 0; index < 6; index++)
            {
                SpriteRenderer ray = game.Art.Renderer("Swim_SunShaft_" + index, scenery, game.Art.Glow,
                    new Color(1, 0.94f, 0.72f, 0.16f), new Vector2(2.3f, 15), -35);
                godRays.Add(ray);
            }
        }

        private void BuildUnderwater(ThemeProfile theme, float midX, float midY)
        {
            // 深层渐变（越往下越暗）
            for (int layer = 0; layer < 5; layer++)
            {
                float t = layer / 4f;
                float y = Mathf.Lerp(MaxY, MinY, t);
                game.Art.Renderer("Swim_DepthLayer_" + layer, scenery, game.Art.Square,
                    Color.Lerp(theme.distance, theme.sky, t), new Vector2(CourseLength + 60, (MaxY - MinY) / 5f + 0.4f), -998)
                    .transform.position = new Vector2(midX, y);
            }
            // 顶部透光（水面光）
            game.Art.Renderer("Swim_SurfaceGlow", scenery, game.Art.Glow,
                new Color(theme.accent.r, theme.accent.g, theme.accent.b, 0.5f), new Vector2(CourseLength + 60, 1.4f), -30)
                .transform.position = new Vector2(midX, MaxY);
            // 透光光柱（Tick 摆动）
            for (int index = 0; index < 9; index++)
            {
                SpriteRenderer ray = game.Art.Renderer("Swim_GodRay_" + index, scenery, game.Art.Glow,
                    new Color(theme.accent.r, theme.accent.g, theme.accent.b, 0.12f), new Vector2(1.2f + (index % 3) * 0.7f, MaxY - MinY), -40);
                ray.transform.position = new Vector2(6 + index * (CourseLength / 9f), midY);
                ray.transform.rotation = Quaternion.Euler(0, 0, 12 + (index % 3) * 4);
                godRays.Add(ray);
            }
            // 海床剪影
            for (int index = 0; index < 30; index++)
            {
                Sprite rock = theme.Architecture(index % 2 == 0 ? 0 : 1);
                SpriteRenderer ridge = game.Art.Renderer("Swim_Seabed_" + index, scenery, rock ?? game.Art.Diamond,
                    rock != null ? Color.white : theme.structure * 0.9f, Vector2.one * (1.2f + (index % 4) * 0.3f), -50);
                ridge.transform.position = new Vector2(2 + index * (CourseLength / 30f), MinY - 0.2f);
                ridge.transform.rotation = Quaternion.Euler(0, 0, (index % 5 - 2) * 9);
            }
            // 热泉口发光
            for (int index = 0; index < 8; index++)
            {
                Sprite chimney = theme.Architecture(2);
                SpriteRenderer vent = game.Art.Renderer("Swim_Vent_" + index, scenery, chimney ?? game.Art.Glow,
                    chimney != null ? Color.white : new Color(theme.danger.r, theme.danger.g, theme.danger.b, 0.4f), Vector2.one * 1.5f, -45);
                vent.transform.position = new Vector2(8 + index * (CourseLength / 8f), MinY + 0.6f);
                reactiveProps.Add(vent);
            }
            // 上升气泡池
            for (int index = 0; index < 40; index++)
            {
                SpriteRenderer bubble = game.Art.Renderer("Swim_Bubble_" + index, scenery, game.Art.Ring,
                    new Color(1f, 1f, 1f, 0.35f), Vector2.one * (0.08f + (index % 4) * 0.05f), -10);
                bubble.transform.position = new Vector2(Random.Range(-18, 18), Random.Range(MinY, MaxY));
                bubbles.Add(bubble);
            }
        }

        public void BindReactiveProps(SceneFeedback feedback)
        {
            if (feedback == null) return;
            foreach (SpriteRenderer sprite in reactiveProps)
                if (sprite != null) feedback.Register(sprite, true);
        }

        public void Tick()
        {
            if (game.Player == null || !game.IsPlaying) return;
            Vector2 playerPos = game.Player.transform.position;
            if (playerRimLight != null) playerRimLight.transform.position = playerPos;
            AnimateWater(playerPos);
            TickSurvival(Time.deltaTime);

            int section = Mathf.Clamp(Mathf.FloorToInt(playerPos.x / SectionWidth), 0, Sections - 1);
            if (section != activeSection)
            {
                activeSection = section; sectionClock = 0; spawnedThisSection = 0; nextSpawn = Time.time + 0.5f;
                float cpY = Underwater ? (MinY + MaxY) * 0.5f : SurfaceY;
                level.SetSwimSegment(section, new Vector2(section * SectionWidth + 3, cpY));
                game.Hud.Announce((Underwater ? "下潜区 " : "浪区 ") + (section + 1) + " / " + Sections, 1.4f);
                if (section == Sections - 1) StartBoss();
            }
            sectionClock += Time.deltaTime;
            if (section >= Sections - 1) return;
            if (Time.time >= nextSpawn && spawnedThisSection < 8 + section / 2 && game.Enemies.Count < game.Config.maximumEnemies - 5)
            {
                float interval = Mathf.Max(0.9f, 1.5f - section * 0.08f - game.Difficulty * 0.1f);
                SpawnSwimEnemy(section);
                nextSpawn = Time.time + interval;
            }
        }

        private void AnimateWater(Vector2 playerPos)
        {
            float time = Time.time;
            float cameraX = game.CameraRig.Camera.transform.position.x;
            int tile = Mathf.FloorToInt(cameraX * 0.2f / 64);
            for (int index = 0; index < paintedBackdrops.Count; index++)
            {
                SpriteRenderer backdrop = paintedBackdrops[index];
                int column = tile + index - 1;
                backdrop.flipX = (column & 1) != 0;
                backdrop.transform.position = new Vector2(column * 64 + cameraX * 0.8f, Underwater ? 0.5f : SurfaceY);
            }
            if (surfaceBand != null)
                surfaceBand.transform.position = new Vector2(surfaceBand.transform.position.x, SurfaceY + Mathf.Sin(time * 1.4f) * 0.08f);
            for (int index = 0; index < waveCrests.Count; index++)
            {
                SpriteRenderer crest = waveCrests[index];
                if (crest == null) continue;
                Vector3 p = crest.transform.position;
                crest.enabled = Mathf.Abs(p.x - cameraX) < 25;
                crest.transform.position = new Vector3(p.x, SurfaceHeightAt(p.x), p.z);
            }
            for (int index = 0; index < godRays.Count; index++)
            {
                SpriteRenderer ray = godRays[index];
                if (ray == null) continue;
                ray.transform.position = new Vector2(cameraX + (index - (godRays.Count - 1) * 0.5f) * 5, Underwater ? (MinY + MaxY) * 0.5f : SurfaceY + 4);
                ray.transform.rotation = Quaternion.Euler(0, 0, 12 + (index % 3) * 4 + Mathf.Sin(time * 0.5f + index) * 3f);
            }
            if (Underwater)
            {
                for (int index = 0; index < bubbles.Count; index++)
                {
                    SpriteRenderer bubble = bubbles[index];
                    if (bubble == null) continue;
                    Vector3 p = bubble.transform.position + Vector3.up * (0.6f + (index % 3) * 0.25f) * Time.deltaTime;
                    p.x += cameraX - ambientCenterX;
                    if (p.y > MaxY) p.y = MinY;
                    bubble.transform.position = p;
                }
            }
            ambientCenterX = cameraX;
            if (Time.time >= nextAmbientBubble)
            {
                nextAmbientBubble = Time.time + (Underwater ? 0.22f : 0.35f);
                Vector2 source = Underwater ? new Vector2(cameraX + Random.Range(-15, 15), MinY + 0.5f)
                    : new Vector2(cameraX + Random.Range(-12, 12), SurfaceY);
                game.Effects.WaterTrail(source, Vector2.up * (Underwater ? 2 : 3), level.Theme.accent);
            }
        }

        // Oxygen is a recoverable hazard, never an instant floor kill. Vent stations recur every room.
        internal void TickSurvival(float deltaTime)
        {
            if (!Underwater || game.Player == null || game.Player.Health.IsDead) return;
            float depth = NormalizedDepth(game.Player.transform.position.y);
            Oxygen = Mathf.Max(0, Oxygen - Mathf.Lerp(0.45f, 1.05f, depth) * deltaTime);
            float ventX = 8 + Mathf.Round((game.Player.transform.position.x - 8) / SectionWidth) * SectionWidth;
            if (Mathf.Abs(game.Player.transform.position.x - ventX) < 1.6f && depth > 0.73f && Time.time >= nextRefill)
            {
                RestoreOxygen(40); nextRefill = Time.time + 8;
                game.Hud.Announce("热泉补氧 +40%", 1.6f);
                game.Effects.SkillWave(game.Player.transform.position, level.Theme.accent, 2);
            }
            depletedClock = Oxygen <= 0 ? depletedClock + deltaTime : 0;
            pressureClock = depth > 0.92f ? pressureClock + deltaTime : 0;
            if (depletedClock >= 2.5f || pressureClock >= 4)
            {
                if (game.Player.Health.TakeDamage(game.Config.playerHealth * 0.16f, Team.Enemy, false, IncomingHitKind.Hazard)) SurvivalHits++;
                depletedClock = pressureClock = 0;
            }
        }

        private void SpawnSwimEnemy(int section)
        {
            float px = game.Player.transform.position.x;
            float spawnX = Mathf.Min(px + Random.Range(13f, 19f), CourseLength - 4f);
            float spawnY = Underwater ? Random.Range(MinY + 1.5f, MaxY - 1.5f) : SurfaceY + Random.Range(-0.9f, 0.4f);
            Vector2 emergence = new Vector2(spawnX, spawnY);
            int pick = (spawnedThisSection + section + game.SelectedStage) % 5;
            EnemyKind kind = pick == 0 || pick == 4 ? EnemyKind.Elite : pick == 2 ? EnemyKind.Runner : pick == 3 ? EnemyKind.Gunner : EnemyKind.Chaser;
            EnemyAI enemy = level.Spawn(kind, emergence);
            if (enemy == null) return;
            enemy.SetCustomController();
            SwimEnemyController motor = enemy.gameObject.AddComponent<SwimEnemyController>();
            motor.Initialize(game, enemy, this, section, spawnedThisSection);
            spawnedThisSection++; SpawnedEnemies++;
            game.Effects.Telegraph(emergence, 1.1f, 0.5f, level.Theme.danger);
            game.Effects.Burst(emergence, level.Theme.accent, 6);
        }

        private void StartBoss()
        {
            if (finalBoss != null || level.BossSpawned) return;
            float by = Underwater ? (MinY + MaxY) * 0.5f : SurfaceY;
            Vector2 arena = new Vector2(CourseLength - 8f, by);
            finalBoss = level.Spawn(EnemyKind.Boss, arena);
            level.SetBoss(finalBoss);
            game.Hud.Announce((Underwater ? "深渊霸主" : "海面霸主") + " // " + game.Level.Theme.boss + " · 三相洋流", 4);
            game.Effects.BossImpact(arena, 5f);
        }

        public void SpawnBossAdd() { SpawnSwimEnemy(Sections - 2); }

        public void ResetForRetry(int checkpoint)
        {
            finalBoss = null;
            activeSection = Mathf.Clamp(checkpoint - 1, -1, Sections - 1);
            spawnedThisSection = 0;
            Oxygen = 100; depletedClock = pressureClock = 0; nextRefill = 0;
            if (scenery != null) scenery.gameObject.SetActive(true);
        }
    }
}
