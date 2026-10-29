using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace MoriEchoes
{
    public sealed class VerticalStageDirector : MonoBehaviour
    {
        private const float SectionHeight = 14f;
        public const int Sections = 8;
        private readonly List<VerticalSkullGate> gates = new List<VerticalSkullGate>();
        private readonly List<VerticalHazardEmitter> hazards = new List<VerticalHazardEmitter>();
        private readonly List<SpriteRenderer> reactiveProps = new List<SpriteRenderer>();
        private GameManager game;
        private LevelManager level;
        private Transform scenery;
        private Light2D globalLight;
        private Light2D playerRimLight;
        private int activeSection = -1;
        private int spawnedThisSection;
        private float nextSpawn;
        private float sectionClock;
        private int miniBossesDefeated;
        private EnemyAI finalBoss;

        // 竖版关卡方向：true = 从下向上攀升（玩家自井底向上，Boss 在天顶）。
        public bool Ascending => true;

        public float DepthLength => SectionHeight * Sections;
        public Vector2 StartPoint => new Vector2(0, -7.5f);   // 井底起点（镜像自原向下版的 +7.5）
        public int ActiveSection => activeSection;
        public int SpawnedThisSection => spawnedThisSection;
        public int SpawnedEnemies { get; private set; }
        public int BrokenSkulls { get; private set; }
        public int GateCount => gates.Count;
        public int LiveHazards => hazards.FindAll(hazard => hazard != null && hazard.isActiveAndEnabled).Count;
        public string LayoutName => game == null ? string.Empty : game.SelectedStage switch
        {
            5 => "骨井回廊 / 交错四骸闸 / 天降骷髅军团",
            6 => "沉没圣堂 / 双侧潮汐井 / 水压喷流",
            _ => "星坠核心井 / 重力反转井壁 / 虚空裂柱"
        };

        public void Initialize(GameManager owner, LevelManager campaign)
        {
            game = owner; level = campaign;
            BuildEnvironment();
            for (int index = 0; index < Sections - 1; index++) BuildSection(index);
        }

        public SegmentDefinition[] CreateSegments()
        {
            var segments = new SegmentDefinition[Sections];
            for (int index = 0; index < Sections; index++)
                // 从井底向上：start 随段号增大（向上），boss 段在顶部。镜像自原向下版的 -index*SectionHeight。
                segments[index] = new SegmentDefinition { start = -2.4f + index * SectionHeight, width = SectionHeight, layout = (index + game.SelectedStage) % 3, boss = index == Sections - 1 };
            return segments;
        }

        private void BuildEnvironment()
        {
            scenery = new GameObject("VerticalShaft_SetDressing_" + game.Level.Theme.title).transform;
            scenery.SetParent(level.transform, false);
            ThemeProfile theme = level.Theme;
            GameObject globalLightObject = new GameObject("VerticalShaft_GlobalLight");
            globalLightObject.transform.SetParent(scenery, false);
            globalLight = globalLightObject.AddComponent<Light2D>();
            globalLight.lightType = Light2D.LightType.Global;
            globalLight.color = Color.Lerp(Color.white, theme.accent, 0.08f);
            globalLight.intensity = 0.92f;
            GameObject rimLightObject = new GameObject("VerticalShaft_PlayerRimLight");
            rimLightObject.transform.SetParent(scenery, false);
            playerRimLight = rimLightObject.AddComponent<Light2D>();
            playerRimLight.lightType = Light2D.LightType.Point;
            playerRimLight.color = theme.accent;
            playerRimLight.intensity = 0.7f;
            playerRimLight.pointLightOuterRadius = 4.6f;
            Transform backdrop = new GameObject("VerticalDepthBackdrop").transform;
            backdrop.SetParent(scenery, false);
            VerticalShaftParallax parallax = backdrop.gameObject.AddComponent<VerticalShaftParallax>();
            parallax.Camera = game.CameraRig.Camera;
            parallax.Factor = 0.12f;
            SpriteRenderer depth = game.Art.Renderer("LayeredShaftAtmosphere", backdrop, game.Art.SideSky,
                new Color(theme.sky.r * 1.2f, theme.sky.g * 1.2f, theme.sky.b * 1.4f, 0.9f),
                new Vector2(20, DepthLength + 26), -1000);
            depth.transform.position = new Vector2(0, (StartPoint.y + DepthLength - 7.2f) * 0.5f);
            if (theme.background != null)
            {
                Vector2 sourceSize = theme.background.bounds.size;
                float scale = 16f / Mathf.Max(0.01f, sourceSize.x);
                for (int section = 0; section < Sections; section++)
                {
                    float center = GateY(section) + SectionHeight * 0.5f;
                    var tile = new GameObject("GeneratedShaftBackdrop_" + section);
                    tile.transform.SetParent(scenery, false);
                    tile.transform.position = new Vector2(0, center);
                    SpriteRenderer panel = game.Art.Renderer("GeneratedShaftArtwork", tile.transform, theme.background,
                        Color.white, Vector2.one * scale, -998);
                    panel.flipX = section % 2 == 1;
                }
            }
            float shaftMid = (StartPoint.y + DepthLength - 7.2f) * 0.5f;
            for (int side = -1; side <= 1; side += 2)
            {
                AddBox("Shaft_OuterWall", new Vector2(side * 8.25f, shaftMid), new Vector2(0.55f, DepthLength + 18),
                    game.Art.Side.Concrete, theme.structure * 1.6f, true);
            }
            // 向上攀升：起点封顶在井底，Boss 平台封顶在天顶。
            AddBox("Shaft_StartCap", new Vector2(0, StartPoint.y - 1.3f), new Vector2(16.5f, 1), game.Art.Side.Concrete, theme.structure, true);
            AddBox("Shaft_BossCap", new Vector2(0, DepthLength - 2.7f), new Vector2(16.5f, 1), game.Art.Side.Concrete, theme.structure * 1.8f, true);
            // Every rung is within a normal jump; alternate landings remain horizontally connected.
            for (int step = 0; step < 54; step++)
                AddPlatform("Climb_" + step, new Vector2(step % 2 == 0 ? -1.6f : 1.6f, -6.5f + step * 2), new Vector2(4.4f, 0.4f));
            // The final landing needs full capsule clearance, not just a head touching its underside.
            AddPlatform("BossArenaFloor", new Vector2(0, 101f), new Vector2(14.8f, 0.5f));

            for (int index = 0; index < Sections; index++)
            {
                float gateY = GateY(index);
                float midpoint = gateY + SectionHeight * 0.5f;
                DrawEtchedWall(index, midpoint);
                if (index < Sections - 1)
                {
                    AddPlatform("RestLedge_A", new Vector2(index % 2 == 0 ? -4.9f : 4.9f, gateY + 4.5f), new Vector2(3.6f, 0.42f));
                    AddPlatform("RestLedge_B", new Vector2(index % 2 == 0 ? 4.9f : -4.9f, gateY + 9.2f), new Vector2(3.1f, 0.42f));
                    BuildHazard(index, gateY + 7);
                }
                AddDistantFossils(index, midpoint);
            }
            for (int index = 0; index < 56; index++)
            {
                float y = DepthLength - 12f - index * 2f;
                float side = index % 2 == 0 ? -1 : 1;
                SpriteRenderer crystal = game.Art.Renderer("Shaft_WallEmber" + index, scenery, game.Art.Glow,
                    index % 4 == 0 ? theme.danger : theme.accent, Vector2.one * 0.12f, 3);
                crystal.transform.position = new Vector2(side * (6.8f + index % 2 * 0.5f), y);
            }
        }

        private void BuildSection(int index)
        {
            float y = GateY(index);
            string gateType = game.SelectedStage == 5 ? "FillSkullGate" : game.SelectedStage == 6 ? "TidalChoirSeal" : "GravityPrismGate";
            var gate = new GameObject(gateType + "_" + index);
            gate.transform.SetParent(scenery, false); gate.transform.position = new Vector2(0, y);
            VerticalSkullGate skullGate = gate.AddComponent<VerticalSkullGate>();
            skullGate.Initialize(game, this, index, 14.7f, 1.55f, 235 + game.SelectedStage * 38 + index * 18);
            skullGate.GateBroken += OnSkullBroken;
            gates.Add(skullGate);
            for (int side = -1; side <= 1; side += 2)
            {
                SpriteRenderer vertebra = game.Art.Renderer("GraveGate_Joint", gate.transform, game.Art.Side.Armor,
                    level.Theme.structure * 1.7f, new Vector2(0.55f, 2.1f), 61);
                vertebra.transform.localPosition = new Vector2(side * 7.1f, 0);
            }
        }

        private void BuildHazard(int index, float y)
        {
            for (int lane = 0; lane < 2; lane++)
            {
                float x = lane == 0 ? -4.2f : 4.2f;
                var entity = new GameObject("VerticalTrapEmitter_" + index + "_" + lane);
                entity.transform.SetParent(scenery, false); entity.transform.position = new Vector2(x, y);
                VerticalHazardEmitter hazard = entity.AddComponent<VerticalHazardEmitter>();
                hazard.Initialize(game, level, index, lane);
                hazards.Add(hazard);
            }
        }

        private void DrawEtchedWall(int index, float y)
        {
            ThemeProfile theme = level.Theme;
            Color baseColor = Color.Lerp(theme.structure, theme.sky, 0.24f);
            if (theme.background != null) baseColor.a = 0.16f;
            AddBox("CarvedWall_Backplate", new Vector2(0, y), new Vector2(15.9f, 13.2f), game.Art.Side.Concrete, baseColor, false);
            for (int stripe = 0; stripe < 4; stripe++)
            {
                float x = -6.15f + stripe * 4.1f;
                SpriteRenderer rib = game.Art.Renderer("CryptWall_ReinforcedRib", scenery, game.Art.Side.Armor,
                    Color.Lerp(theme.structure, theme.accent, 0.24f), new Vector2(0.38f, 11.8f), -25);
                rib.transform.position = new Vector2(x, y);
            }
            Sprite crest = theme.BossOrnament(1, 3) ?? theme.Architecture(3);
            SpriteRenderer seal = game.Art.Renderer("Shaft_PhysicalWallCrest", scenery, crest,
                new Color(0.8f, 0.9f, 1, 0.4f), Vector2.one * (2.7f / crest.bounds.size.y), -18);
            seal.transform.position = new Vector2((index % 2 == 0 ? -1 : 1) * 3.6f, y + 0.1f);
            reactiveProps.Add(seal);
            Sprite module = theme.Architecture(3);
            if (module != null)
            {
                float size = 3.6f / Mathf.Max(0.1f, module.bounds.size.y);
                SpriteRenderer ornament = game.Art.Renderer("GeneratedCryptWallOrnament", scenery, module, Color.white, Vector2.one * size, -24);
                ornament.transform.position = new Vector2(index % 2 == 0 ? 6.35f : -6.35f, y);
            }
            Sprite relief = theme.BossOrnament(1, 1) ?? theme.Architecture(1);
            SpriteRenderer cairn = game.Art.Renderer("IllustratedCairnRelief", scenery, relief,
                new Color(0.7f, 0.8f, 0.9f, 0.35f), Vector2.one * (3.1f / relief.bounds.size.y), -12);
            cairn.transform.position = new Vector2(index % 2 == 0 ? 2.5f : -2.5f, y);
            reactiveProps.Add(cairn);
        }

        private void AddDistantFossils(int section, float y)
        {
            if (game.SelectedStage == 6)
            {
                Sprite relief = level.Theme.Architecture(3);
                SpriteRenderer pillar = game.Art.Renderer("DrownedChoirRelief", scenery, relief,
                    new Color(0.6f, 0.8f, 0.9f, 0.25f), Vector2.one * (5 / relief.bounds.size.y), -32);
                pillar.transform.position = new Vector2(section % 2 == 0 ? -2.6f : 2.6f, y);
            }
            else if (game.SelectedStage == 7)
            {
                for (int shard = 0; shard < 3; shard++)
                {
                    Sprite relief = level.Theme.BossOrnament(2, 2) ?? level.Theme.Architecture(3);
                    SpriteRenderer crystal = game.Art.Renderer("VoidCoffinRelief", scenery, relief,
                        new Color(level.Theme.accent.r, level.Theme.accent.g, level.Theme.accent.b, 0.25f),
                        Vector2.one * (3.8f / relief.bounds.size.y), -28);
                    crystal.transform.position = new Vector2((shard - 1) * 4.2f, y + shard % 2 * 1.5f);
                    crystal.transform.rotation = Quaternion.Euler(0, 0, shard % 2 == 0 ? 13 : -13);
                }
            }
        }

        private void AddPlatform(string label, Vector2 position, Vector2 size)
        {
            var platform = new GameObject("Vertical_" + label); platform.transform.SetParent(scenery, false); platform.transform.position = position;
            BoxCollider2D collider = platform.AddComponent<BoxCollider2D>(); collider.size = size; collider.usedByEffector = true;
            var effector = platform.AddComponent<PlatformEffector2D>(); effector.useOneWay = true; effector.surfaceArc = 165;
            effector.useColliderMask = false; effector.useSideFriction = false; effector.useSideBounce = false;
            platform.AddComponent<PlatformSurface>().OneWay = true;
            StageSurfaceArt.Draw(game, level, platform.transform, size, true);
        }

        private void AddBox(string label, Vector2 position, Vector2 size, Sprite sprite, Color tint, bool solid)
        {
            var root = new GameObject(label); root.transform.SetParent(scenery, false); root.transform.position = position;
            if (solid) root.AddComponent<BoxCollider2D>().size = size;
            if (solid)
            {
                root.AddComponent<PlatformSurface>();
                StageSurfaceArt.Draw(game, level, root.transform, size, false, false, -40);
            }
            else
            {
                SpriteRenderer visual = game.Art.Renderer("EtchedArchitecture", root.transform, sprite, tint,
                    new Vector2(size.x / sprite.bounds.size.x, size.y / sprite.bounds.size.y), -40);
                visual.transform.localPosition = Vector3.zero;
            }
        }

        private void OnSkullBroken(VerticalSkullGate gate)
        {
            BrokenSkulls++;
            game.AwardStyle(Mathf.RoundToInt(150 * game.StyleMultiplier));
            game.Player.Energy?.Add(4);
            game.Effects.Explosion(gate.transform.position, 2.5f, level.Theme.accent);
            level.SetVerticalCheckpoint(gate.Index, ClimbCheckpoint(gate.transform.position.y - 2.5f));
            string message = game.SelectedStage == 5 ? "填充骷髅闸崩裂 // 天降通路开启"
                : game.SelectedStage == 6 ? "潮汐封印碎裂 // 圣堂逆流上涌" : "引力棱晶破碎 // 深井极性改写";
            game.Hud.Announce(message, 2.2f);
        }

        public void BindReactiveProps(SceneFeedback feedback)
        {
            if (feedback == null) return;
            foreach (SpriteRenderer sprite in reactiveProps)
                if (sprite != null) feedback.Register(sprite, true);
        }

        // 向上攀升：闸门自井底向天顶依次排列（index 越大越靠上）。镜像自原向下版的 2.4 - index*SectionHeight。
        public float GateY(int index) => -2.4f + Mathf.Clamp(index, 0, Sections - 1) * SectionHeight;

        private Vector2 ClimbCheckpoint(float belowHeight)
        {
            if (belowHeight < -6.3f) return StartPoint;
            int step = Mathf.Clamp(Mathf.FloorToInt((belowHeight + 6.5f) / 2), 0, 53);
            return new Vector2(0, -6.5f + step * 2 + 1.05f);
        }

        public void Tick()
        {
            if (game.Player == null || !game.IsPlaying) return;
            if (playerRimLight != null) playerRimLight.transform.position = game.Player.transform.position;
            float y = game.Player.transform.position.y;
            int section = Mathf.Clamp(Mathf.FloorToInt((y - StartPoint.y) / SectionHeight), 0, Sections - 1);
            if (section != activeSection)
            {
                activeSection = section; sectionClock = 0; spawnedThisSection = 0; nextSpawn = Time.time + 0.45f;
                level.SetVerticalSegment(section, section == 0 ? StartPoint : ClimbCheckpoint(y - 1.1f));
                game.Hud.Announce("攀升层 " + (section + 1) + " / " + Sections, 1.4f);
                if (section == Sections - 1) StartBoss();
            }
            sectionClock += Time.deltaTime;
            if (section >= Sections - 1) return;
            if (Time.time >= nextSpawn && spawnedThisSection < 9 + section / 3 && game.Enemies.Count < game.Config.maximumEnemies - 5)
            {
                float interval = Mathf.Max(0.8f, 1.4f - section * 0.07f - game.Difficulty * 0.1f);
                SpawnGroundEnemy(section);
                nextSpawn = Time.time + interval;
            }
        }

        private void SpawnGroundEnemy(int section)
        {
            float gateY = GateY(section);
            float x = Random.Range(-6.25f, 6.25f);
            // 从天而降：在玩家上方（但低于本段闸门）生成，向玩家坠下。
            float spawnY = Mathf.Min(gateY - 1.5f, game.Player.transform.position.y + Random.Range(6f, 9f));
            Vector2 emergence = new Vector2(x, spawnY);
            int pick = (spawnedThisSection + section + game.SelectedStage) % 5;
            EnemyKind kind = pick == 0 || pick == 4 ? EnemyKind.Elite : pick == 2 ? EnemyKind.Runner : pick == 3 ? EnemyKind.Gunner : EnemyKind.Chaser;
            EnemyAI enemy = level.Spawn(kind, emergence);
            if (enemy == null) return;
            enemy.gameObject.name = EnemyRoster.Name(game.SelectedStage, kind) + "_天降涌出";
            enemy.SetCustomController();
            VerticalEnemyController motor = enemy.gameObject.AddComponent<VerticalEnemyController>();
            motor.Initialize(game, enemy, this, section, spawnedThisSection);
            spawnedThisSection++; SpawnedEnemies++;
            game.Effects.Telegraph(emergence, 1.25f, 0.5f, level.Theme.danger);
            game.Effects.Burst(emergence, level.Theme.accent, 8);
        }

        private void StartBoss()
        {
            if (finalBoss != null || level.BossSpawned) return;
            Vector2 arena = new Vector2(0, DepthLength - 7.2f);   // 天顶
            finalBoss = level.Spawn(EnemyKind.Boss, arena);
            level.SetBoss(finalBoss);
            game.Hud.Announce("竖井核心守卫 // " + game.Level.Theme.boss + " · 三重相变", 4);
            game.Effects.BossImpact(arena, 5.5f);
        }

        public void SpawnBossAdd() { SpawnGroundEnemy(Sections - 2); miniBossesDefeated++; }

        public void ResetForRetry(int checkpoint)
        {
            // Broken gates stay open at a checkpoint; respawning must never reseal the route behind the player.
            foreach (VerticalSkullGate gate in gates) if (gate != null && !gate.Broken) gate.ResetGate();
            foreach (VerticalHazardEmitter hazard in hazards) if (hazard != null) hazard.ResetEmitter();
            finalBoss = null; activeSection = Mathf.Clamp(checkpoint - 1, -1, Sections - 1); spawnedThisSection = 0;
            if (scenery != null) scenery.gameObject.SetActive(true);
        }

        public static int StageEnemyArchetypeCount => 12;
        public int BossAddsRequested => miniBossesDefeated;
    }
}
