using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MoriEchoes
{
    public enum BossAttackKind
    {
        None,
        CityBreakerPunch, CityFaultLine, CityArmourCharge,
        FactoryHydraulicClamp, FactoryFuelGeyser, FactoryMoltenHelix,
        IceSpikeHunt, IceRicochetPrism, IcePolarWind,
        CyberForbiddenBoard, CyberPolylineScan, CyberMirrorExchange,
        FinalOrbitalStars, FinalGravityCore, FinalRiftCrossSlash
    }

    public sealed class SideBossController : MonoBehaviour
    {
        private static readonly BossAttackKind[,] attackTable =
        {
            { BossAttackKind.CityBreakerPunch, BossAttackKind.CityFaultLine, BossAttackKind.CityArmourCharge },
            { BossAttackKind.FactoryHydraulicClamp, BossAttackKind.FactoryFuelGeyser, BossAttackKind.FactoryMoltenHelix },
            { BossAttackKind.IceSpikeHunt, BossAttackKind.IceRicochetPrism, BossAttackKind.IcePolarWind },
            { BossAttackKind.CyberForbiddenBoard, BossAttackKind.CyberPolylineScan, BossAttackKind.CyberMirrorExchange },
            { BossAttackKind.FinalOrbitalStars, BossAttackKind.FinalGravityCore, BossAttackKind.FinalRiftCrossSlash }
        };
        private static readonly string[,] formNames =
        {
            { "重拳铁卫", "断层破城者", "碎甲攻城兽" },
            { "液压监工", "开炉熔躯", "螺旋熔核" },
            { "棘冠霜牙", "折镜冰翼", "极寒风皇" },
            { "棋盘执行体", "折线扫描体", "镜界零点" },
            { "轨道星主", "引力蚀核", "裂隙六翼" }
        };

        public int Phase { get; private set; } = 1;
        public int StageIndex { get; private set; }
        public int FormIndex => Phase - 1;
        public string FormName => formNames[StageIndex, FormIndex];
        public int UnlockedAttackCount => Phase;
        public BossAttackKind CurrentAttack { get; private set; }
        public string AttackId => CurrentAttack.ToString();
        public int AttackIndex { get; private set; } = -1;
        public bool UltimateAttack => Phase >= 3 && CurrentAttack == attackTable[StageIndex, 2];
        public int AttacksResolved { get; private set; }
        public bool Transitioning => transitionRemaining > 0;
        public bool ExecutionWindow { get; private set; }
        public bool Attacking => attackActive && !preparing;
        public string PatternName { get; private set; } = "锁定目标";
        public Health[] Arms { get; private set; } = System.Array.Empty<Health>();
        public bool Preparing => preparing;
        public int Pattern => pattern;
        public int FieldsCreated { get; private set; }
        public bool Charging => attackActive && CurrentAttack == BossAttackKind.CityArmourCharge;
        public bool OwnerAlive => enemy != null && enemy.Health != null && !enemy.Health.IsDead && enemy.gameObject.activeInHierarchy;
        public BossField TrackingStrike => trackingStrike;
        public float SecondFormThreshold => 0.6f + DifficultyEarly;
        public float ThirdFormThreshold => 0.3f + DifficultyEarly;
        public float AttackRemaining => attackActive ? Mathf.Max(0, attackLifetime - attackElapsed) : 0;
        public int ActiveFieldCount
        {
            get
            {
                int count = 0;
                foreach (BossField field in fields) if (field != null && field.gameObject.activeSelf) count++;
                return count;
            }
        }

        private readonly List<BossField> fields = new List<BossField>();
        private GameManager game;
        private LevelManager level;
        private EnemyAI enemy;
        private Rigidbody2D body;
        private BossSignatureVfx signature;
        private BossField trackingStrike;
        private float transitionRemaining;
        private float recoveryRemaining;
        private float attackElapsed;
        private float attackLifetime;
        private float windup;
        private float direction;
        private float fixedHeight;
        private float chargeDuration;
        private Vector2 target;
        private Vector2 moveStart;
        private Vector2 moveEnd;
        private Vector2 mirrorDestination;
        private bool preparing;
        private bool attackActive;
        private bool transitionProtected;
        private bool deathEffectsPlayed;
        private bool executionStarted;
        private bool executionRunning;
        private bool anchorsCreated;
        private int attackCursor;
        private int pattern;
        private float DifficultyEarly => game != null ? Mathf.Clamp(game.Difficulty, 0, 2) * 0.04f : 0;
        private SegmentDefinition Arena => level.Segments[level.Segments.Length - 1];
        private float ArenaLeft => Arena.start + 2;
        private float ArenaRight => Arena.End - 2;

        public string GetAttackId(int index)
        {
            return index >= 0 && index < 3 ? attackTable[StageIndex, index].ToString() : string.Empty;
        }

        public void Initialize(GameManager owner, EnemyAI controller, Rigidbody2D rigidbody)
        {
            game = owner;
            level = owner.Level;
            enemy = controller;
            body = rigidbody;
            StageIndex = Mathf.Clamp(game.SelectedStage, 0, 4);
            fixedHeight = StageIndex == 2 ? 4.2f : 1.57f;
            body.position = new Vector2(Mathf.Clamp(body.position.x, ArenaLeft + 1, ArenaRight - 1), fixedHeight);
            body.gravityScale = 0;
            body.constraints = RigidbodyConstraints2D.FreezeRotation | RigidbodyConstraints2D.FreezePositionY;
            body.linearVelocity = Vector2.zero;
            recoveryRemaining = 2;
            enemy.Health.Died += OnOwnerDied;
            enemy.Health.Damaged += OnBossDamaged;
            signature = GetComponent<BossSignatureVfx>();
            if (signature == null) signature = gameObject.AddComponent<BossSignatureVfx>();
            signature.Initialize(game, this);
            PatternName = FormName + " / 观察预警边界";
            game.Effects.Encounter(body.position, 1, 5, 1.2f, 0, true);
        }

        public void Step()
        {
            if (body == null) return;
            if (executionRunning) { body.linearVelocity = Vector2.zero; return; }
            if (!isActiveAndEnabled || !OwnerAlive || game == null || level == null || game.Level != level)
            {
                StopEncounter();
                return;
            }
            if (!game.IsPlaying || Time.timeScale <= 0 || game.Player == null || game.Player.Health.IsDead || !enemy.IsActive)
            {
                body.linearVelocity = Vector2.zero;
                return;
            }
            float healthRatio = enemy.Health.Current / Mathf.Max(1, enemy.Health.Maximum);
            int requested = healthRatio <= ThirdFormThreshold ? 3 : healthRatio <= SecondFormThreshold ? 2 : 1;
            if (requested > Phase) EnterForm(requested);
            float delta = Time.fixedDeltaTime;
            if (Transitioning)
            {
                body.linearVelocity = Vector2.zero;
                transitionRemaining = Mathf.Max(0, transitionRemaining - delta);
                if (!Transitioning) ReleaseTransitionProtection();
                return;
            }
            body.linearVelocity = Vector2.zero;
            if (attackActive)
            {
                attackElapsed += delta;
                if (preparing && trackingStrike != null && trackingStrike.IsWarning)
                {
                    trackingStrike.FollowWarningAt(GetCurrentTarget());
                    target = trackingStrike.transform.position;
                }
                if (preparing && attackElapsed >= windup) ResolveAttack();
                if (CurrentAttack == BossAttackKind.CityArmourCharge && !preparing)
                {
                    float progress = Mathf.Clamp01((attackElapsed - windup) / chargeDuration);
                    body.MovePosition(Vector2.Lerp(moveStart, moveEnd, progress));
                }
                if (attackElapsed < attackLifetime) return;
                attackActive = false;
                preparing = false;
                ClearFields();
                recoveryRemaining = Phase == 3 ? 0.72f : 1.0f;
                PatternName = FormName + " / 招式收势 · 反击窗口";
            }
            recoveryRemaining = Mathf.Max(0, recoveryRemaining - delta);
            if (recoveryRemaining > 0)
            {
                if (StageIndex == 0)
                {
                    float side = body.position.x >= game.Player.transform.position.x ? 1 : -1;
                    float desired = Mathf.Clamp(game.Player.transform.position.x + side * 4.7f, ArenaLeft + 1, ArenaRight - 1);
                    float velocity = Mathf.Clamp((desired - body.position.x) * 1.5f, -2.5f, 2.5f);
                    if (body.position.x <= ArenaLeft + 1 && velocity < 0 || body.position.x >= ArenaRight - 1 && velocity > 0) velocity = 0;
                    body.linearVelocity = new Vector2(velocity, 0);
                }
                return;
            }
            BeginAttack();
        }

        private void EnterForm(int requested)
        {
            ClearFields();
            attackActive = preparing = false;
            Phase = requested;
            transitionRemaining = 2;
            recoveryRemaining = 0.65f;
            transitionProtected = true;
            enemy.Health.Invulnerable = true;
            body.linearVelocity = Vector2.zero;
            attackCursor = Phase - 1;
            AttackIndex = -1;
            CurrentAttack = BossAttackKind.None;
            PatternName = FormName + " / 形态重构";
            if (StageIndex == 4 && Phase == 3) CreateRiftAnchors();
            signature.SetForm(Phase);
            game.Hud.Announce(level.Theme.boss + " // 形态 " + Phase + " · " + FormName, 2.5f);
            game.Effects.Burst(body.position, Color.white, 25);
            game.Effects.SkillWave(body.position, level.Theme.danger, 4);
            game.Effects.Encounter(body.position, 3, 3.5f, 0.8f);
            game.CameraRig.Shake(0.3f);
            game.Audio.SetIntensity(Phase);
            game.Hud.PhaseFlash();
        }

        private void BeginAttack()
        {
            if (GetComponent<BossController>().Techniques.TryBegin()) return;
            ClearFields();
            target = GetCurrentTarget();
            direction = target.x >= body.position.x ? 1 : -1;
            AttackIndex = attackCursor % UnlockedAttackCount;
            attackCursor = (AttackIndex + 1) % UnlockedAttackCount;
            CurrentAttack = attackTable[StageIndex, AttackIndex];
            attackElapsed = 0;
            attackLifetime = 0;
            windup = 1.1f;
            preparing = attackActive = true;
            body.linearVelocity = Vector2.zero;
            switch (CurrentAttack)
            {
                case BossAttackKind.CityBreakerPunch: PlanPunch(); break;
                case BossAttackKind.CityFaultLine: PlanFaultLine(); break;
                case BossAttackKind.CityArmourCharge: PlanCharge(); break;
                case BossAttackKind.FactoryHydraulicClamp: PlanHydraulics(); break;
                case BossAttackKind.FactoryFuelGeyser: PlanFuel(); break;
                case BossAttackKind.FactoryMoltenHelix: PlanHelix(); break;
                case BossAttackKind.IceSpikeHunt: PlanIceHunt(); break;
                case BossAttackKind.IceRicochetPrism: PlanPrisms(); break;
                case BossAttackKind.IcePolarWind: PlanWind(); break;
                case BossAttackKind.CyberForbiddenBoard: PlanBoard(); break;
                case BossAttackKind.CyberPolylineScan: PlanScan(); break;
                case BossAttackKind.CyberMirrorExchange: PlanMirror(); break;
                case BossAttackKind.FinalOrbitalStars: PlanStars(); break;
                case BossAttackKind.FinalGravityCore: PlanGravity(); break;
                case BossAttackKind.FinalRiftCrossSlash: PlanRift(); break;
            }
            trackingStrike = Disc(target, UltimateAttack ? 1.35f : 1.05f, TrackingStyle(), windup, 0.42f,
                UltimateAttack ? 18 : 12);
            trackingStrike.ConfigureTargetLock(0.8f);
            attackLifetime = Mathf.Max(attackLifetime, windup + 0.3f);
            if (game.Effects.AttackTelegraphsEnabled) game.Hud.Announce(PatternName, windup + 0.7f);
        }

        private void ResolveAttack()
        {
            preparing = false;
            AttacksResolved++;
            pattern++;
            if (CurrentAttack == BossAttackKind.CyberMirrorExchange)
            {
                game.Effects.Encounter(body.position, 1, 3, 0.3f);
                body.position = mirrorDestination;
                body.linearVelocity = Vector2.zero;
            }
            signature.PlayAttack();
            game.Effects.BossSkillImpact(target, TrackingStyle(), UltimateAttack ? 4.5f : 3.2f);
            if (UltimateAttack)
                game.Effects.BossUltimate(target, StageIndex == 4 ? 22 : StageIndex == 2 ? 20 : 18);
            game.Effects.Encounter(body.position, 2, 1.8f, 0.3f);
            game.Audio.Play(SoundKind.Blast);
            game.CameraRig.Shake(CurrentAttack == BossAttackKind.CityArmourCharge ? 0.2f : 0.1f);
        }

        private void PlanPunch()
        {
            PatternName = "冲拳 / 跳过拳路，或绕到身后";
            windup = 1.05f;
            float start = body.position.x + direction * 0.8f;
            float end = Mathf.Clamp(body.position.x + direction * 6.2f, ArenaLeft, ArenaRight);
            Box(new Vector2((start + end) * 0.5f, 1.35f), new Vector2(Mathf.Abs(end - start), 1.25f),
                BossFieldStyle.Punch, windup, 0.24f, 20);
        }

        private void PlanFaultLine()
        {
            PatternName = "地裂推进 / 裂缝逐段爆开，起跳越过";
            windup = 1.1f;
            for (int index = 0; index < 4; index++)
            {
                float center = body.position.x + direction * (2.3f + index * 2.8f);
                if (center < ArenaLeft + 1 || center > ArenaRight - 1) continue;
                Box(new Vector2(center, 0.38f), new Vector2(2.75f, 0.76f), BossFieldStyle.Fracture,
                    windup + index * 0.32f, 0.42f, 18);
            }
        }

        private void PlanCharge()
        {
            PatternName = "装甲冲锋 / 锁定直道，跳跃越顶或退到身后";
            windup = 1.25f;
            chargeDuration = 0.85f;
            moveStart = new Vector2(body.position.x, fixedHeight);
            float travel = Mathf.Min(9, Mathf.Abs((direction > 0 ? ArenaRight - 1.4f : ArenaLeft + 1.4f) - moveStart.x));
            moveEnd = new Vector2(moveStart.x + direction * travel, fixedHeight);
            BossField charge = Box(moveStart, new Vector2(2.55f, 2.9f), BossFieldStyle.Charge, windup, chargeDuration, 24);
            charge.ConfigurePath(new[] { moveStart, moveEnd }, chargeDuration);
        }

        private void PlanHydraulics()
        {
            PatternName = "液压夹击 / 传送带启动，留在双压柱之间";
            windup = 1.2f;
            float center = Mathf.Clamp(target.x, ArenaLeft + 5, ArenaRight - 5);
            BossField belt = Box(new Vector2(center, 0.1f), new Vector2(9.6f, 0.2f),
                BossFieldStyle.Hydraulic, 0, windup + 0.9f, 0);
            belt.ConfigureConveyor(pattern % 2 == 0 ? 2.2f : -2.2f);
            for (int index = 0; index < 2; index++)
                Box(new Vector2(center + (index == 0 ? -2.7f : 2.7f), 1.85f), new Vector2(1.75f, 3.3f),
                    BossFieldStyle.Hydraulic, windup + index * 0.22f, 0.5f, 22);
        }

        private void PlanFuel()
        {
            PatternName = "燃料喷涌 / 离开三条喷口，避开残留燃料";
            windup = 1.2f;
            float center = Mathf.Clamp(target.x, ArenaLeft + 5, ArenaRight - 5);
            for (int index = 0; index < 3; index++)
            {
                float horizontal = center + (index - 1) * 4.1f;
                float delay = windup + index * 0.28f;
                Box(new Vector2(horizontal, 1.65f), new Vector2(1.4f, 3.3f), BossFieldStyle.Fuel, delay, 0.5f, 20);
                Box(new Vector2(horizontal, 0.14f), new Vector2(1.9f, 0.28f), BossFieldStyle.Fuel, delay + 0.55f, 1.3f, 10);
            }
        }

        private void PlanHelix()
        {
            PatternName = "熔核螺旋炮 / 炮珠沿单条螺旋轨迹飞行，穿过波腹空隙";
            windup = 1.2f;
            Vector2 origin = body.position + new Vector2(direction * 1.3f, 0.65f);
            float end = Mathf.Clamp(origin.x + direction * 14, ArenaLeft + 0.7f, ArenaRight - 0.7f);
            Vector2[] points = new Vector2[41];
            for (int index = 0; index < points.Length; index++)
            {
                float progress = index / (float)(points.Length - 1);
                points[index] = new Vector2(Mathf.Lerp(origin.x, end, progress), origin.y + Mathf.Sin(progress * Mathf.PI * 4) * 1.15f);
            }
            for (int index = 0; index < 6; index++)
            {
                BossField bead = Disc(origin, 0.28f, BossFieldStyle.Molten, windup + index * 0.23f, 2.5f, 13);
                bead.ConfigurePath(points, 2.5f, index == 0);
            }
        }

        private void PlanIceHunt()
        {
            PatternName = "冰刺围猎 / 外圈先封，中心留有安全空隙";
            windup = 1.15f;
            float center = Mathf.Clamp(target.x, ArenaLeft + 5.4f, ArenaRight - 5.4f);
            for (int index = 0; index < 4; index++)
            {
                float side = index % 2 == 0 ? -1 : 1;
                float offset = index < 2 ? 4.5f : 2.1f;
                BossField spike = Box(new Vector2(center + side * offset, 1.35f), new Vector2(1.05f, 2.7f),
                    BossFieldStyle.IceSpike, windup + (index < 2 ? 0 : 0.4f), 0.65f, 17);
                spike.FreezeSeconds = 0.22f;
            }
        }

        private void PlanPrisms()
        {
            PatternName = "反弹棱晶 / 棱晶按标线折返，远离反弹拐点";
            windup = 1.25f;
            for (int index = 0; index < 2; index++)
            {
                Vector2 origin = body.position + new Vector2(direction * 1.4f, index == 0 ? -0.5f : 0.55f);
                Vector2 heading = new Vector2(direction * 0.78f, index == 0 ? -0.63f : 0.63f).normalized;
                Vector2[] points = RicochetPath(origin, heading, 18, 3);
                BossField prism = Disc(origin, 0.3f, BossFieldStyle.Prism, windup + index * 0.5f, 2.8f, 15);
                prism.ConfigurePath(points, 2.8f);
            }
        }

        private Vector2[] RicochetPath(Vector2 origin, Vector2 heading, float distance, int bounces)
        {
            List<Vector2> points = new List<Vector2> { origin };
            Vector2 current = origin;
            for (int index = 0; index <= bounces && distance > 0.01f; index++)
            {
                float horizontal = Mathf.Abs(heading.x) < 0.0001f ? float.MaxValue
                    : ((heading.x > 0 ? ArenaRight - 0.45f : ArenaLeft + 0.45f) - current.x) / heading.x;
                float vertical = Mathf.Abs(heading.y) < 0.0001f ? float.MaxValue
                    : ((heading.y > 0 ? 5.8f : 0.45f) - current.y) / heading.y;
                float step = Mathf.Max(0, Mathf.Min(distance, Mathf.Min(horizontal, vertical)));
                current += heading * step;
                points.Add(current);
                distance -= step;
                if (distance <= 0.01f) break;
                if (horizontal <= vertical) heading.x = -heading.x;
                else heading.y = -heading.y;
            }
            return points.ToArray();
        }

        private void PlanWind()
        {
            PatternName = "极寒风切 / 先蹲过高风，再跳过低风";
            windup = 1.2f;
            for (int index = 0; index < 2; index++)
            {
                float height = index == 0 ? 1.7f : 0.42f;
                float start = index == 0 ? ArenaLeft + 1.9f : ArenaRight - 1.9f;
                float end = index == 0 ? ArenaRight - 1.9f : ArenaLeft + 1.9f;
                BossField wind = Box(new Vector2(start, height), new Vector2(3.5f, 0.42f), BossFieldStyle.Wind,
                    windup + index * 2.25f, 1.8f, 16);
                wind.ConfigurePath(new[] { new Vector2(start, height), new Vector2(end, height) }, 1.8f);
            }
        }

        private void PlanBoard()
        {
            PatternName = "数字棋盘禁区 / 红格执行，空格可站立";
            windup = 1.3f;
            int columns = 7;
            float width = (ArenaRight - ArenaLeft) / columns;
            for (int column = 0; column < columns; column++)
            {
                for (int row = 0; row < 2; row++)
                {
                    if ((column + row + pattern) % 2 != 0) continue;
                    Vector2 position = new Vector2(ArenaLeft + (column + 0.5f) * width, row == 0 ? 1.05f : 3.5f);
                    Box(position, new Vector2(width - 0.24f, 2.1f), BossFieldStyle.Digital, windup, 0.9f, 17);
                }
            }
        }

        private void PlanScan()
        {
            PatternName = "折线扫描 / 高线、斜线、低线依次执行";
            windup = 1.2f;
            float center = (ArenaLeft + ArenaRight) * 0.5f;
            Vector2[] points =
            {
                new Vector2(ArenaLeft + 1, 3.6f), new Vector2(center - 3, 3.6f),
                new Vector2(center + 3, 0.95f), new Vector2(ArenaRight - 1, 0.95f)
            };
            if (pattern % 2 != 0)
                for (int index = 0; index < points.Length; index++) points[index].x = ArenaLeft + ArenaRight - points[index].x;
            for (int index = 1; index < points.Length; index++)
            {
                Vector2 delta = points[index] - points[index - 1];
                Box((points[index] + points[index - 1]) * 0.5f, new Vector2(delta.magnitude, 0.28f),
                    BossFieldStyle.Scan, windup + (index - 1) * 0.48f, 0.3f, 18,
                    Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            }
        }

        private void PlanMirror()
        {
            PatternName = "镜像换位 / 离开红色残像，向青色落点反击";
            windup = 1.3f;
            float center = (ArenaLeft + ArenaRight) * 0.5f;
            float destination = target.x <= center ? ArenaRight - 3 : ArenaLeft + 3;
            mirrorDestination = new Vector2(destination, fixedHeight);
            Box(new Vector2(target.x, 1.6f), new Vector2(2.7f, 3.2f), BossFieldStyle.Mirror, windup, 0.3f, 19);
            if (Mathf.Abs(body.position.x - target.x) > 3)
                Box(new Vector2(body.position.x, 1.6f), new Vector2(2.7f, 3.2f), BossFieldStyle.Mirror, windup, 0.3f, 19);
            Box(mirrorDestination, new Vector2(2.5f, 3), BossFieldStyle.Mirror, windup, 0.7f, 0);
        }

        private void PlanStars()
        {
            PatternName = "轨道环星 / 进入星环内侧，或退出整条轨道";
            windup = 1.35f;
            Vector2 center = new Vector2(Mathf.Clamp(target.x, ArenaLeft + 5, ArenaRight - 5), 2.2f);
            for (int star = 0; star < 3; star++)
            {
                Vector2[] points = new Vector2[65];
                for (int index = 0; index < points.Length; index++)
                {
                    float radians = star * Mathf.PI * 2 / 3 + index / (float)(points.Length - 1) * Mathf.PI * 2;
                    points[index] = center + new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * 3.5f;
                }
                BossField satellite = Disc(points[0], 0.31f, BossFieldStyle.Orbit, windup, 3.2f, 16);
                satellite.ConfigurePath(points, 3.2f, star == 0);
            }
        }

        private void PlanGravity()
        {
            PatternName = "引力核 / 只有实心核会伤害，逆向移动或闪避脱离牵引";
            windup = 1.45f;
            Vector2 position = new Vector2(Mathf.Clamp(target.x, ArenaLeft + 3, ArenaRight - 3), 1.05f);
            BossField core = Disc(position, 0.85f, BossFieldStyle.Gravity, windup, 2.6f, 20);
            core.ConfigurePull(5.2f, 12);
        }

        private void PlanRift()
        {
            PatternName = "裂隙交叉斩 / 离开交点；击破两侧锚点可各消除一道斩击";
            windup = 1.25f;
            Vector2 center = new Vector2(Mathf.Clamp(target.x, ArenaLeft + 4.8f, ArenaRight - 4.8f),
                Mathf.Clamp(target.y, 1.1f, 3.6f));
            for (int index = 0; index < Arms.Length; index++)
            {
                if (Arms[index] == null || Arms[index].IsDead) continue;
                BossField cut = Box(center, new Vector2(9.2f, 0.4f), BossFieldStyle.Rift, windup + index * 0.5f,
                    0.26f, 24, index == 0 ? 35 : 145);
                cut.BindCounterAnchor(Arms[index]);
            }
            if (ActiveFieldCount == 0) PatternName = "裂隙失稳 / 锚点已破，核心暴露可持续反击";
        }

        private Vector2 GetCurrentTarget()
        {
            return game.Player.transform.position;
        }

        private BossFieldStyle TrackingStyle()
        {
            return CurrentAttack switch
            {
                BossAttackKind.CityFaultLine => BossFieldStyle.Fracture,
                BossAttackKind.CityArmourCharge => BossFieldStyle.Charge,
                BossAttackKind.FactoryHydraulicClamp => BossFieldStyle.Hydraulic,
                BossAttackKind.FactoryFuelGeyser => BossFieldStyle.Fuel,
                BossAttackKind.FactoryMoltenHelix => BossFieldStyle.Molten,
                BossAttackKind.IceSpikeHunt => BossFieldStyle.IceSpike,
                BossAttackKind.IceRicochetPrism => BossFieldStyle.Prism,
                BossAttackKind.IcePolarWind => BossFieldStyle.Wind,
                BossAttackKind.CyberForbiddenBoard => BossFieldStyle.Digital,
                BossAttackKind.CyberPolylineScan => BossFieldStyle.Scan,
                BossAttackKind.CyberMirrorExchange => BossFieldStyle.Mirror,
                BossAttackKind.FinalOrbitalStars => BossFieldStyle.Orbit,
                BossAttackKind.FinalGravityCore => BossFieldStyle.Gravity,
                BossAttackKind.FinalRiftCrossSlash => BossFieldStyle.Rift,
                _ => BossFieldStyle.Punch
            };
        }

        private BossField Box(Vector2 position, Vector2 size, BossFieldStyle style, float warning, float duration, float damage, float rotation = 0)
        {
            return Field(position, size, BossFieldShape.Box, style, warning, duration, damage, rotation);
        }

        private BossField Disc(Vector2 position, float radius, BossFieldStyle style, float warning, float duration, float damage)
        {
            return Field(position, Vector2.one * radius * 2, BossFieldShape.Disc, style, warning, duration, damage, 0);
        }

        private BossField Field(Vector2 position, Vector2 size, BossFieldShape shape, BossFieldStyle style,
            float warning, float duration, float damage, float rotation)
        {
            var entity = new GameObject(AttackId + "_" + FieldsCreated);
            entity.transform.SetParent(level.transform, false);
            entity.transform.position = position;
            BossField field = entity.AddComponent<BossField>();
            float area = UltimateAttack ? game.Config.bossUltimateAreaScale : game.Config.bossSkillAreaScale;
            float intensity = UltimateAttack ? game.Config.bossUltimateDamageMultiplier : game.Config.bossSkillDamageMultiplier;
            field.InitializeAttack(game, enemy.Health, AttackId, shape, style, size * area, Mathf.Max(0.6f, warning), duration, damage * intensity, rotation);
            field.HitKind = UltimateAttack ? IncomingHitKind.BossUltimate : IncomingHitKind.BossSkill;
            fields.Add(field);
            FieldsCreated++;
            attackLifetime = Mathf.Max(attackLifetime, Mathf.Max(0.6f, warning) + duration + Time.fixedDeltaTime * 2);
            return field;
        }

        private void CreateRiftAnchors()
        {
            if (anchorsCreated) return;
            anchorsCreated = true;
            Arms = new Health[2];
            for (int index = 0; index < Arms.Length; index++)
            {
                var entity = new GameObject("RiftAnchorWeakPoint_" + index);
                entity.transform.SetParent(transform, false);
                entity.transform.localPosition = new Vector2(index == 0 ? -1.8f : 1.8f, 0.35f);
                Sprite ornament = level.Theme.BossOrnament(3, 0);
                SpriteRenderer visual = game.Art.Renderer("DestructibleRiftConduit", entity.transform, ornament != null ? ornament : game.Art.Diamond,
                    ornament != null ? Color.white : level.Theme.accent,
                    ornament != null ? new Vector2(0.9f / ornament.bounds.size.x, 1.15f / ornament.bounds.size.y) : new Vector2(0.8f, 1.15f), 130);
                BoxCollider2D collider = entity.AddComponent<BoxCollider2D>();
                collider.size = new Vector2(0.72f, 1.05f);
                Health anchor = entity.AddComponent<Health>();
                anchor.Initialize(Team.Enemy, 85 + Mathf.Clamp(game.Difficulty, 0, 2) * 10);
                anchor.Died += () =>
                {
                    if (game != null && game.IsPlaying && OwnerAlive)
                    {
                        game.Effects.Encounter(visual.transform.position, 2, 1.4f, 0.3f);
                        game.Effects.Burst(visual.transform.position, level.Theme.accent, 12);
                    }
                    entity.SetActive(false);
                };
                Arms[index] = anchor;
            }
        }

        public void DeathEffects()
        {
            StopEncounter();
            if (deathEffectsPlayed || game == null || body == null) return;
            deathEffectsPlayed = true;
            Vector2 center = body.position;
            Color color = level != null ? level.Theme.danger : ProceduralArt.Danger;
            game.Effects.BossImpact(center, 6);
            game.Effects.Encounter(center, 3, 7, 0.8f);
            int bursts = 5 + StageIndex;
            for (int index = 0; index < bursts; index++)
            {
                Vector2 offset;
                if (StageIndex == 0) offset = new Vector2((index - bursts * 0.5f) * 0.78f, Mathf.Sin(index * 1.7f) * 0.35f);
                else if (StageIndex == 1) offset = new Vector2(Mathf.Cos(index * 2.4f) * 1.8f, (index % 3 - 1) * 0.72f);
                else if (StageIndex == 2) offset = Quaternion.Euler(0, 0, index * 360f / bursts) * Vector2.right * 1.9f;
                else if (StageIndex == 3) offset = new Vector2((index % 3 - 1) * 1.15f, (index / 3 - 1) * 0.82f);
                else offset = Quaternion.Euler(0, 0, index * 144f) * Vector2.right * (0.55f + index * 0.23f);
                game.Effects.Explosion(center + offset, index == bursts - 1 ? 4.2f : 1.2f + index * 0.13f, color);
            }
            game.AwardStyle(5000);
            game.CameraRig.Shake(0.9f);
        }

        private void OnBossDamaged(float amount)
        {
            if (executionStarted || enemy == null || enemy.Health == null
                || enemy.Health.Current / Mathf.Max(1, enemy.Health.Maximum) > 0.15f) return;
            executionStarted = true;
            ExecutionWindow = true;
            enemy.Health.SetCurrent(enemy.Health.Maximum * 0.15f);
            StartCoroutine(ExecuteFinisher());
        }

        private IEnumerator ExecuteFinisher()
        {
            executionRunning = true;
            attackActive = preparing = false;
            ClearFields();
            enemy.Health.Invulnerable = true;
            float originalScale = Time.timeScale;
            Time.timeScale = 0.24f;
            PlayerController player = game.Player;
            if (player != null && player.Motor != null)
            {
                player.Motor.SetCinematicLocked(true);
                player.Health.Invulnerable = true;
                game.Hud.Announce("處决窗口 // 终结突进", 2.2f);
                Vector2 origin = player.transform.position;
                Vector2 destination = body.position + new Vector2(player.transform.position.x <= body.position.x ? -1.25f : 1.25f, -0.52f);
                float elapsed = 0;
                float nextGhost = 0;
                while (elapsed < 0.72f && player != null)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float t = Mathf.SmoothStep(0, 1, Mathf.Clamp01(elapsed / 0.72f));
                    player.GetComponent<Rigidbody2D>().position = Vector2.Lerp(origin, destination, t);
                    if (Time.unscaledTime >= nextGhost)
                    {
                        nextGhost = Time.unscaledTime + 0.045f;
                        game.Effects.Ghost(player.transform.position);
                        game.Effects.Burst(player.transform.position, game.Character.accent, 4);
                    }
                    yield return null;
                }
            }
            yield return new WaitForSecondsRealtime(0.12f);
            PlayExecutionSignature();
            yield return new WaitForSecondsRealtime(0.22f);
            Time.timeScale = originalScale;
            enemy.Health.Invulnerable = false;
            enemy.Health.TakeDamage(enemy.Health.Maximum, Team.Player, true);
        }

        private void PlayExecutionSignature()
        {
            Vector2 center = body.position;
            Color color = level.Theme.danger;
            game.Effects.Burst(center, Color.white, 44);
            switch (StageIndex)
            {
                case 0:
                    game.Effects.Explosion(center + Vector2.left * 1.6f, 2.1f, color);
                    game.Effects.Explosion(center + Vector2.right * 1.6f, 2.1f, color);
                    break;
                case 1:
                    for (int index = 0; index < 4; index++)
                        game.Effects.Explosion(center + new Vector2((index % 2 == 0 ? -1 : 1) * 1.2f, (index < 2 ? -1 : 1) * 0.7f), 2.3f, color);
                    break;
                case 2:
                    for (int index = 0; index < 6; index++)
                        game.Effects.Explosion(center + (Vector2)(Quaternion.Euler(0, 0, index * 60) * Vector3.right * 2.2f), 1.7f, color);
                    break;
                case 3:
                    for (int index = -2; index <= 2; index++)
                        game.Effects.Explosion(center + new Vector2(index * 0.9f, (index % 2 == 0 ? 1 : -1) * 1.2f), 1.8f, color);
                    break;
                default:
                    for (int index = 0; index < 7; index++)
                        game.Effects.Explosion(center + (Vector2)(Quaternion.Euler(0, 0, index * 360f / 7) * Vector3.right * (0.5f + index * 0.27f)), 1.9f, color);
                    break;
            }
            game.Effects.Explosion(center, 6.5f, Color.white);
            game.Effects.Debris(center, color);
            game.Effects.Landing(center);
            game.CameraRig.Shake(1.1f);
            game.Audio.Play(SoundKind.Blast);
        }

        private void ReleaseTransitionProtection()
        {
            if (!transitionProtected) return;
            transitionProtected = false;
            if (enemy != null && enemy.Health != null) enemy.Health.Invulnerable = false;
        }

        private void ClearFields()
        {
            foreach (BossField field in fields) if (field != null) field.Cancel();
            fields.Clear();
            trackingStrike = null;
        }

        private void StopEncounter()
        {
            ClearFields();
            attackActive = preparing = false;
            transitionRemaining = 0;
            ReleaseTransitionProtection();
            if (body != null) body.linearVelocity = Vector2.zero;
            foreach (Health anchor in Arms)
                if (anchor != null && anchor.gameObject.activeSelf) anchor.gameObject.SetActive(false);
            if (signature != null) signature.SetVisible(false);
        }

        private void OnOwnerDied() { StopEncounter(); }
        private void OnDisable() { StopEncounter(); }
        private void OnDestroy()
        {
            ClearFields();
            if (enemy != null && enemy.Health != null)
            {
                enemy.Health.Died -= OnOwnerDied;
                enemy.Health.Damaged -= OnBossDamaged;
            }
        }
    }
}
