using System.Collections.Generic;
using UnityEngine;

namespace MoriEchoes
{
    public sealed class LevelManager : MonoBehaviour
    {
        public ThemeProfile Theme { get; private set; }
        public SegmentDefinition[] Segments { get; private set; }
        public int SegmentIndex { get; private set; }
        public int Stage => game.SelectedStage;
        // 0-4 横版 / 5-7 竖井向上 / 8 海面游泳 / 9 水下浮沉。
        public bool IsVerticalStage => Stage >= 5 && Stage <= 7;
        public bool IsSwimStage => Stage >= 8 && Stage <= 9;
        public bool IsUnderwaterStage => Stage == 9;
        public VerticalStageDirector Vertical { get; private set; }
        public SwimStageDirector Swim { get; private set; }
        public int Deaths { get; private set; }
        public int Chips { get; private set; }
        public int Seed { get; private set; }
        public bool EncounterStarted { get; private set; }
        public bool GateLocked { get; private set; }
        public bool BossSpawned { get; private set; }
        public EnemyAI Boss { get; private set; }
        public SecretBranchDirector SecretRoutes { get; private set; }
        public Vector2 Checkpoint { get; private set; } = new Vector2(3, 1);
        public int CheckpointSegment { get; private set; }
        public float Length => Vertical != null ? Vertical.DepthLength : Segments[Segments.Length - 1].End;
        public string Rating { get; private set; } = "—";
        public string Objective => BossSpawned ? Theme.boss + " / 击破三相核心"
            : IsVerticalStage ? "向上攀升 / 击破闸门 · 敌潮自上方降临"
            : IsUnderwaterStage ? "水下浮沉 / 上下浮动推进 · 攻击暗流敌群"
            : IsSwimStage ? "海面游泳 / 向前推进 · 浪涌跳跃"
            : "向右推进 / 火力压制";
        public float SlowProjectiles { get; set; } = 1;
        private GameManager game;
        private Transform geometry;
        private Transform checkpointFlag;
        private float checkpointHealth;
        private int[] checkpointAmmo;
        private readonly List<EnemyAI> encounter = new List<EnemyAI>();

        public void Initialize(GameManager owner)
        {
            game = owner; Theme = game.Config.themes[Stage];
            Seed = System.Environment.TickCount & int.MaxValue;
            if (IsVerticalStage)
            {
                Vertical = gameObject.AddComponent<VerticalStageDirector>();
                Vertical.Initialize(game, this);
                Segments = Vertical.CreateSegments();
                SecretRoutes = gameObject.AddComponent<SecretBranchDirector>();
                SecretRoutes.Initialize(game, this);
                SecretRoutes.BuildVerticalBranch();
                game.Environment = gameObject.AddComponent<SceneFeedback>();
                game.Environment.Initialize(game);
                Vertical.BindReactiveProps(game.Environment);
                Checkpoint = Vertical.StartPoint;
                CheckpointSegment = SegmentIndex = 0;
                return;
            }
            if (IsSwimStage)
            {
                Swim = gameObject.AddComponent<SwimStageDirector>();
                Swim.Initialize(game, this);
                Segments = Swim.CreateSegments();
                game.Environment = gameObject.AddComponent<SceneFeedback>();
                game.Environment.Initialize(game);
                Swim.BindReactiveProps(game.Environment);
                Checkpoint = Swim.StartPoint;
                CheckpointSegment = SegmentIndex = 0;
                return;
            }
            var random = new System.Random(Seed);
            const int combatSegments = 7;
            Segments = new SegmentDefinition[combatSegments + 1];
            for (int index = 0; index < Segments.Length; index++)
                Segments[index] = new SegmentDefinition
                {
                    start = index * 34, width = index == combatSegments ? 38 : 34,
                    layout = random.Next(3), boss = index == combatSegments
                };
            geometry = new GameObject("StageGeometry_" + Theme.title).transform;
            geometry.SetParent(transform, false);
            SecretRoutes = gameObject.AddComponent<SecretBranchDirector>();
            SecretRoutes.Initialize(game, this);
            var builder = geometry.gameObject.AddComponent<SegmentBuilder>();
            builder.Build(game, this);
            Sprite terminal = game.Config.scenerySprites != null && game.Config.scenerySprites.Length > 1 ? game.Config.scenerySprites[1] : null;
            checkpointFlag = game.Art.Renderer("CheckpointFlag", geometry, terminal != null ? terminal : game.Art.Square,
                terminal != null ? Color.white : Theme.accent, terminal != null ? Vector2.one : new Vector2(0.08f, 2.6f), 20).transform;
            checkpointFlag.position = Checkpoint + Vector2.down * 0.3f;
            CheckpointSegment = SegmentIndex = 0;
        }

        public void SaveInitialCheckpoint()
        {
            checkpointHealth = game.Player.Health.Current;
            checkpointAmmo = game.Player.Weapon.CaptureAmmo();
        }

        private void Update()
        {
            if (!game.IsPlaying || game.Player == null || SecretRoutes != null && SecretRoutes.Inside) return;
            if (Vertical != null) { Vertical.Tick(); return; }
            if (Swim != null) { Swim.Tick(); return; }
            SegmentDefinition segment = Segments[SegmentIndex];
            float position = game.Player.transform.position.x;
            if (!EncounterStarted && position >= segment.start + 5) BeginEncounter();
            if (!EncounterStarted || segment.boss) return;
            GateLocked = false;
            // 软推进：不再用"清场"解锁。越过相机左界的旧敌静默回收，把"向前冲"的主动权还给玩家。
            Camera cam = game.CameraRig.Camera;
            float leftBound = cam.transform.position.x - cam.orthographicSize * cam.aspect - 4f;
            for (int i = encounter.Count - 1; i >= 0; i--)
            {
                EnemyAI enemy = encounter[i];
                if (enemy == null || enemy.Health.IsDead) { encounter.RemoveAt(i); continue; }
                if (enemy.transform.position.x < leftBound) { enemy.Recycle(); encounter.RemoveAt(i); }
            }
            if (position >= segment.End - 1.8f && SegmentIndex < Segments.Length - 1)
            {
                SegmentIndex++;
                CheckpointSegment = SegmentIndex;
                Checkpoint = new Vector2(Segments[SegmentIndex].start + 2, 1);
                checkpointFlag.position = Checkpoint + Vector2.down * 0.3f;
                checkpointHealth = Mathf.Max(game.Player.Health.Current, game.Player.Health.Maximum * 0.5f);
                checkpointAmmo = game.Player.Weapon.CaptureAmmo();
                game.Player.Health.Restore(12);
                EncounterStarted = false;
                game.Hud.Announce("推进 / 第 " + (SegmentIndex + 1) + " 区", 2);
            }
        }

        private void BeginEncounter()
        {
            EncounterStarted = true; GateLocked = true;
            SegmentDefinition segment = Segments[SegmentIndex];
            if (segment.boss)
            {
                SetBoss(Spawn(EnemyKind.Boss, new Vector2(segment.End - 6, 1.8f)));
                game.Hud.Announce("BOSS // " + Theme.boss, 3);
                return;
            }
            EncounterDirector.SpawnEncounter(game, this, segment, SegmentIndex, encounter);
            game.Hud.Announce("交战区 " + (SegmentIndex + 1) + " / " + EncounterHint(), 3);
        }

        private string EncounterHint()
        {
            switch (Stage)
            {
                case 0: return "后翼伏击 + 高空炮蜂 / 跳过炮线、落石，利用油桶";
                case 1: return "三向工蜂围压 / 跳过冲程、穿越传送带，打断维修";
                case 2: return "上下层冰翼夹攻 / 离开蓄力线、跃过震波与冰刺";
                case 3: return "空地错层追击 / 蹲过激光、跳过栅格、用掩体断线";
                default: return "前后夹阵 + 空投轰炸 / 交替跳跃、蹲伏和横向闪避";
            }
        }

        public EnemyAI Spawn(EnemyKind kind, Vector2 position)
        {
            if (game.Enemies.Count >= game.Config.maximumEnemies) return null;
            var entity = new GameObject(kind == EnemyKind.Boss ? Theme.boss : EnemyRoster.Name(Stage, kind));
            entity.transform.SetParent(transform, false); entity.transform.position = position;
            EnemyAI enemy = entity.AddComponent<EnemyAI>();
            enemy.Initialize(game, kind, Stage + 1);
            return enemy;
        }

        internal void SetVerticalSegment(int index, Vector2 checkpoint)
        {
            SegmentIndex = Mathf.Clamp(index, 0, Segments.Length - 1);
            CheckpointSegment = SegmentIndex;
            Checkpoint = checkpoint;
            checkpointHealth = Mathf.Max(game.Player.Health.Current, game.Player.Health.Maximum * 0.55f);
            checkpointAmmo = game.Player.Weapon.CaptureAmmo();
            EncounterStarted = true;
        }

        internal void SetVerticalCheckpoint(int index, Vector2 checkpoint)
        {
            CheckpointSegment = Mathf.Clamp(index, 0, Segments.Length - 2);
            Checkpoint = checkpoint;
            checkpointHealth = Mathf.Max(game.Player.Health.Current, game.Player.Health.Maximum * 0.55f);
            checkpointAmmo = game.Player.Weapon.CaptureAmmo();
        }

        internal void SetBoss(EnemyAI boss)
        {
            Boss = boss;
            BossSpawned = boss != null;
            EncounterStarted = true;
            GateLocked = boss != null;
        }

        internal void SetSwimSegment(int index, Vector2 checkpoint)
        {
            SegmentIndex = Mathf.Clamp(index, 0, Segments.Length - 1);
            CheckpointSegment = SegmentIndex;
            Checkpoint = checkpoint;
            checkpointHealth = Mathf.Max(game.Player.Health.Current, game.Player.Health.Maximum * 0.55f);
            checkpointAmmo = game.Player.Weapon.CaptureAmmo();
            EncounterStarted = true;
        }

        public void CollectChip()
        {
            Chips++;
            if (Chips == 3) CampaignProgress.Write("Skin." + Stage, 1);
            game.Hud.Announce("隐藏芯片 " + Chips + " / 3" + (Chips == 3 ? " / 武器配色解锁" : ""), 2);
        }

        public void Retry()
        {
            GetComponent<WeaponCapsuleDirector>()?.ClearAll();
            Deaths++;
            foreach (EnemyAI enemy in game.Enemies.ToArray())
                if (enemy != null) { enemy.gameObject.SetActive(false); Destroy(enemy.gameObject); }
            game.Enemies.Clear(); encounter.Clear(); Boss = null; BossSpawned = false;
            game.Projectiles.ClearAll(); game.Pickups.ClearAll(); game.Effects.ClearAll();
            game.Experience.Track.LoseUnspentHalf();
            SegmentIndex = CheckpointSegment; EncounterStarted = GateLocked = false; SlowProjectiles = 1;
            GetComponent<EncounterHazardDirector>()?.ResetForCheckpoint();
            SecretRoutes?.ResetForRetry();
            Vertical?.ResetForRetry(CheckpointSegment);
            Swim?.ResetForRetry(CheckpointSegment);
            float maximum = game.Player.Health.Maximum;
            game.Player.Health.Initialize(Team.Player, maximum);
            game.Player.Health.ConfigurePlayerDamage(game.Config);
            game.Player.Health.SetCurrent(Mathf.Clamp(checkpointHealth, 1, maximum));
            game.Player.Motor.ResetAt(Checkpoint);
            game.Player.Weapon.RestoreAmmo(checkpointAmmo);
            game.CameraRig.ResetFollow();
        }

        public void RecordClear()
        {
            Rating = CampaignProgress.Grade(Deaths, game.Elapsed, game.Kills, game.StyleScore);
            CampaignProgress.Unlock(Stage + 2);
            string key = "Best." + Stage + "." + game.Difficulty;
            int rank = Rating == "S" ? 4 : Rating == "A" ? 3 : Rating == "B" ? 2 : 1;
            CampaignProgress.Write(key, Mathf.Max(CampaignProgress.Read(key), rank));
            string timeKey = "Time." + Stage + "." + game.Difficulty;
            int best = CampaignProgress.Read(timeKey, int.MaxValue);
            CampaignProgress.Write(timeKey, Mathf.Min(best, Mathf.CeilToInt(game.Elapsed * 100)));
            if (Rating == "S") CampaignProgress.Write("Palette." + game.CharacterId, 1);
        }
    }
}
