using System.Collections.Generic;
using UnityEngine;

namespace MoriEchoes
{
    public sealed class SecretBranchDirector : MonoBehaviour
    {
        private static readonly string[] MiniBossNames =
        {
            "旧城·铜钟守卫", "裂隙·回声织者", "熔炉·铆钉祭司", "锅炉·熔渣猎犬",
            "雪原·镜棱巫", "冻土·霜音猎手", "废都·逆序棋灵", "塔心·镜界窃贼",
            "王庭·断誓先锋", "终焉·星墓看守", "墓窟·殉钟司祭", "圣堂·逆流修女", "核心·失轨观测者"
        };
        private static readonly string[] Stories =
        {
            "旧城档案：钟声停下后，避难所的名单仍在增长。", "裂隙记录：织者把撤离信号改写成了陷阱。",
            "工厂日志：铆钉祭司守着最后一枚燃料芯。", "炉心回声：猎犬曾替工人寻找被封死的出口。",
            "雪原手记：镜棱巫把暴风雪封进了晶体。", "冻土讯号：猎手仍在等待失踪的巡逻队。",
            "废都棋谱：棋灵每走一步，就抹去一段城市记忆。", "塔心残页：窃贼偷走的不是能源，而是时间。",
            "王庭遗言：先锋拒绝向已经坍塌的王座投降。", "星墓坐标：看守者把归航路线藏进了核心。",
            "殉钟密室：司祭把幸存者的名字刻在最后一口钟内。", "逆流侧殿：修女守护着尚未送出的撤离信标。",
            "失轨档案：观测者仍试图把破碎的星图拼回原位。"
        };
        private sealed class Branch
        {
            public int Id, Segment, GuardsSpawned;
            public Vector2 Entry, MiniBossPoint, ReturnPoint;
            public BoxCollider2D Door;
            public bool Opened, Rewarded, Completed, HintShown;
            public SecretMiniBossController MiniBoss;
            public float NextGuard;
        }
        private readonly List<Branch> branches = new List<Branch>();
        private readonly List<EnemyAI> suspended = new List<EnemyAI>();
        private GameManager game;
        private LevelManager level;
        private Branch active;
        private BoxCollider2D exitDoor;
        private float portalReady;
        public bool Inside => active != null;
        public Rect CameraBounds { get; private set; }
        public float ArenaLeft => CameraBounds.xMin + 2;
        public float ArenaRight => CameraBounds.xMax - 2;
        public Vector2 ExitPosition => exitDoor != null ? (Vector2)exitDoor.transform.position : Vector2.zero;
        public int BranchCount => branches.Count;
        public int CompletedCount => branches.FindAll(branch => branch.Completed).Count;
        public int OpenedCount => branches.FindAll(branch => branch.Opened).Count;
        public int GuardWaveBudget => 16 + level.Stage / 2;
        public int GuardsSpawned => branches.Count > 0 ? branches[0].GuardsSpawned : 0;
        public int GuardianId => branches.Count > 0 ? branches[0].Id : -1;
        public string InteractionPrompt { get; private set; } = string.Empty;
        public static string PatternNameFor(int id) => SecretMiniBossController.PatternNameAt(id);
        public Vector2 EntryPosition(int id) => branches.Count > 0 ? branches[0].Entry : Vector2.zero;

        public void Initialize(GameManager owner, LevelManager campaign) { game = owner; level = campaign; }

        public void BuildBranch(int segmentIndex, float start, float upperY)
        {
            if (segmentIndex != 3 || branches.Count != 0) return;
            Vector2 entry = new Vector2(start + 18.2f, upperY + 0.95f);
            Platform("SecretEntryLanding", entry + Vector2.down * 1.1f, new Vector2(4.2f, 0.4f), true);
            BuildEntrance(level.Stage * 2, segmentIndex, entry);
        }

        public void BuildVerticalBranch()
        {
            if (branches.Count != 0) return;
            // A side landing at 50% shaft height, connected to the normal two-unit rungs.
            Vector2 entry = new Vector2(-5.65f, 50.65f);
            Platform("SecretShaftBridge", new Vector2(-3.7f, 49.5f), new Vector2(5.2f, 0.4f), true);
            BuildEntrance(10 + level.Stage - 5, 4, entry);
        }

        private void BuildEntrance(int id, int segment, Vector2 entry)
        {
            var branch = new Branch { Id = id, Segment = segment, Entry = entry };
            branch.Door = Door("IllustratedSecretEntrance", entry);
            branches.Add(branch);
            BuildRoom(branch);
        }

        private BoxCollider2D Door(string name, Vector2 point)
        {
            var root = new GameObject(name);
            root.transform.SetParent(level.transform, false); root.transform.position = point;
            var trigger = root.AddComponent<BoxCollider2D>(); trigger.isTrigger = true; trigger.size = new Vector2(1.45f, 2.25f);
            Sprite image = level.Theme.Architecture(5) ?? level.Theme.Architecture(3) ?? game.Art.Side.Armor;
            game.Art.Renderer("IllustratedDoorBody", root.transform, image, Color.white,
                Vector2.one * (2.8f / image.bounds.size.y), 24);
            return trigger;
        }

        private void BuildRoom(Branch branch)
        {
            float x = level.Length + 60;
            CameraBounds = new Rect(x - 13, 0, 26, 13);
            branch.MiniBossPoint = new Vector2(x + 3, 1.9f);
            Platform("SecretArena_Floor", new Vector2(x, -0.55f), new Vector2(26, 1.1f), false);
            Platform("SecretArena_LeftWall", new Vector2(x - 12.6f, 6), new Vector2(0.8f, 13), false);
            Platform("SecretArena_RightWall", new Vector2(x + 12.6f, 6), new Vector2(0.8f, 13), false);
            Platform("SecretArena_Roof", new Vector2(x, 12.8f), new Vector2(26, 0.8f), false);
            Platform("SecretArena_LedgeL", new Vector2(x - 4.5f, 2.2f), new Vector2(4, 0.4f), true);
            Platform("SecretArena_LedgeR", new Vector2(x + 4.5f, 3.5f), new Vector2(4, 0.4f), true);
            if (level.Theme.background != null)
            {
                Sprite art = level.Theme.background;
                SpriteRenderer backdrop = game.Art.Renderer("IllustratedSecretChamber", level.transform, art,
                    new Color(0.62f, 0.68f, 0.78f, 1), new Vector2(26 / art.bounds.size.x, 13 / art.bounds.size.y), -100);
                backdrop.transform.position = CameraBounds.center;
            }
            for (int side = -1; side <= 1; side += 2)
            {
                Sprite prop = level.Theme.Architecture(4) ?? level.Theme.Architecture(3);
                if (prop == null) continue;
                SpriteRenderer ornament = game.Art.Renderer("IllustratedSecretRelic", level.transform, prop,
                    Color.white, Vector2.one * (3.4f / prop.bounds.size.y), -30);
                ornament.transform.position = new Vector2(x + side * 9.5f, 1.7f);
            }
            exitDoor = Door("IllustratedSecretExit", new Vector2(x - 10, 1.15f));
        }

        private void Platform(string name, Vector2 center, Vector2 size, bool oneWay)
        {
            var root = new GameObject(name); root.transform.SetParent(level.transform, false); root.transform.position = center;
            var collider = root.AddComponent<BoxCollider2D>(); collider.size = size;
            if (oneWay)
            {
                collider.usedByEffector = true;
                var effector = root.AddComponent<PlatformEffector2D>(); effector.useOneWay = true;
                effector.useSideFriction = effector.useSideBounce = effector.useColliderMask = false; effector.surfaceArc = 165;
            }
            root.AddComponent<PlatformSurface>().OneWay = oneWay;
            StageSurfaceArt.Draw(game, level, root.transform, size, oneWay, name == "SecretArena_Roof", 6);
        }

        private void Update()
        {
            InteractionPrompt = string.Empty;
            if (game == null || !game.IsPlaying || game.Player == null) return;
            if (Inside)
            {
                InteractionPrompt = active.Completed ? "密室已清除 / 接触左侧回程门返回主路" : "击败密室守卫领取遗物 / 左侧门可返回主路";
                if (Time.time > portalReady && Touching(exitDoor)) Leave();
                return;
            }
            foreach (Branch branch in branches)
            {
                if (branch.Completed) continue;
                Vector2 distance = (Vector2)game.Player.transform.position - branch.Entry;
                bool nearby = Mathf.Abs(distance.x) < (level.IsVerticalStage ? 12 : 18) && Mathf.Abs(distance.y) < (level.IsVerticalStage ? 8 : 6);
                if (nearby) SpawnGuards(branch);
                if (Mathf.Abs(distance.x) > 4.5f || Mathf.Abs(distance.y) > 3) continue;
                InteractionPrompt = "遗迹密室 / 接触实体门即可进入（每关一处）";
                if (!branch.HintShown) { branch.HintShown = true; game.Hud.Announce("中段密室 / 守门增援来袭 · 接触门进入", 2.6f); }
                if (Time.time > portalReady && Touching(branch.Door)) Enter(branch);
            }
        }

        private bool Touching(BoxCollider2D door)
        {
            if (door == null || !door.enabled) return false;
            foreach (Collider2D player in game.Player.GetComponents<Collider2D>())
                if (player.enabled && !player.isTrigger && door.bounds.Intersects(player.bounds)) return true;
            return false;
        }

        private void SpawnGuards(Branch branch)
        {
            if (Time.time < branch.NextGuard || branch.GuardsSpawned >= GuardWaveBudget || game.Enemies.Count >= game.Config.maximumEnemies - 3) return;
            branch.NextGuard = Time.time + 0.8f;
            for (int count = 0; count < 2 && branch.GuardsSpawned < GuardWaveBudget; count++)
            {
                int slot = branch.GuardsSpawned;
                EnemyAI enemy;
                if (level.IsVerticalStage)
                {
                    EnemyKind kind = slot % 4 == 0 ? EnemyKind.Elite : slot % 4 == 1 ? EnemyKind.Gunner : slot % 4 == 2 ? EnemyKind.Runner : EnemyKind.Chaser;
                    Vector2 emergence = branch.Entry + new Vector2(slot % 2 == 0 ? 7 : 0.5f, 4 + slot % 3);
                    enemy = level.Spawn(kind, emergence);
                    if (enemy != null) enemy.gameObject.AddComponent<VerticalEnemyController>().Initialize(game, enemy, level.Vertical, branch.Segment, slot);
                }
                else
                {
                    EncounterSpawn[] roster = EncounterDirector.Build(game, level, level.Segments[branch.Segment], branch.Segment);
                    if (roster.Length == 0) return;
                    EncounterSpawn pick = roster[slot % roster.Length];
                    Vector2 emergence = new Vector2(branch.Entry.x + (slot % 2 == 0 ? -10 : 10), pick.Position.y);
                    enemy = level.Spawn(pick.Kind, emergence);
                    if (enemy != null) enemy.ConfigureEncounter(pick.Archetype, level.Segments[branch.Segment],
                        pick.FormationAnchor, slot, 0.7f, "密室门区双翼增援");
                }
                if (enemy == null) break;
                branch.GuardsSpawned++;
                game.Effects.Encounter(enemy.transform.position, 2, 1.6f, 0.45f, 0, true);
            }
        }

        private void Enter(Branch branch)
        {
            active = branch;
            branch.ReturnPoint = branch.Entry + new Vector2(level.IsVerticalStage ? 2.7f : -3.2f, 0.15f);
            suspended.Clear();
            foreach (EnemyAI actor in game.Enemies)
            {
                if (actor == null || actor.GetComponent<SecretMiniBossController>() != null) continue;
                actor.SetSecretSuspended(true); suspended.Add(actor);
            }
            if (!branch.Opened)
            {
                branch.Opened = true;
                var entity = new GameObject(MiniBossNames[branch.Id]); entity.transform.SetParent(level.transform, false);
                entity.transform.position = branch.MiniBossPoint;
                var enemy = entity.AddComponent<EnemyAI>(); enemy.Initialize(game, EnemyKind.Elite, level.Stage + 1);
                enemy.SetCustomController(); enemy.GetComponent<Rigidbody2D>().gravityScale = 0;
                enemy.GetComponent<CircleCollider2D>().radius = 0.65f;
                enemy.Health.IncreaseMaximum(160 + level.Stage * 35, 160 + level.Stage * 35);
                branch.MiniBoss = entity.AddComponent<SecretMiniBossController>(); branch.MiniBoss.Initialize(game, enemy, this, branch.Id);
            }
            else if (branch.MiniBoss != null) branch.MiniBoss.GetComponent<EnemyAI>().SetSecretSuspended(false);
            game.Projectiles.ClearAll();
            game.Player.Motor.TeleportTo(new Vector2(CameraBounds.center.x - 7, 1.2f));
            game.CameraRig.ResetFollow(); portalReady = Time.time + 0.8f;
            game.Hud.Announce(Stories[branch.Id] + " // " + MiniBossNames[branch.Id], 4);
        }

        private void Leave()
        {
            Branch branch = active; active = null;
            if (branch.MiniBoss != null) branch.MiniBoss.GetComponent<EnemyAI>().SetSecretSuspended(true);
            foreach (EnemyAI actor in suspended) if (actor != null) actor.SetSecretSuspended(false);
            suspended.Clear(); game.Projectiles.ClearAll();
            game.Player.Motor.TeleportTo(branch.ReturnPoint); game.CameraRig.ResetFollow(); portalReady = Time.time + 1;
            game.Hud.Announce(branch.Completed ? "遗物已取得 / 返回主路" : "返回主路 / 可再次接触同一扇门挑战", 2.2f);
        }

        public void MiniBossDefeated(SecretMiniBossController controller, EnemyAI enemy, int id)
        {
            Branch branch = branches.Find(candidate => candidate.Id == id);
            if (branch == null || branch.Rewarded) return;
            branch.Rewarded = branch.Completed = true; branch.Door.enabled = false;
            Vector2 point = enemy.transform.position;
            game.Effects.RewardItem(point + Vector2.left * 0.6f, 4); game.Effects.RewardItem(point + Vector2.right * 0.6f, 5);
            game.Effects.Explosion(point, 4.4f, level.Theme.accent); game.Effects.Debris(point, level.Theme.accent);
            game.AwardStyle(2500); level.CollectChip();
            level.GetComponent<WeaponCapsuleDirector>()?.Drop(point, (CombatPowerupKind)(1 + id % 4));
            game.Hud.Announce("密室清除 / 芯片 + 武器胶囊 + 风格分2500 · 左侧门返回", 3.1f);
            branch.MiniBoss = null;
        }

        public void ResetForRetry()
        {
            active = null; suspended.Clear(); portalReady = Time.time + 0.8f;
            foreach (Branch branch in branches)
            {
                branch.Opened = branch.Completed; branch.HintShown = false; branch.MiniBoss = null;
                branch.Door.enabled = !branch.Completed;
            }
        }
    }

    public sealed class SecretMiniBossController : MonoBehaviour
    {
        private static readonly string[] PatternNames =
        {
            "三棱锥散射", "逆向回响环", "横向折返弹", "落点符印", "螺旋刻印",
            "双弧交错", "镜面狙击", "坠星雨", "封锁矩阵", "共振拍击", "殉钟三连折扇", "逆流双侧潮刺", "失轨旋星射线"
        };

        private GameManager game;
        private EnemyAI enemy;
        private SecretBranchDirector owner;
        private Rigidbody2D body;
        private int id;
        private int attackCount;
        private float attackAt;
        private float homeX;
        private bool rewarded;
        private bool planning, preparing;
        private Vector2 lockedTarget;
        private SpriteRenderer generatedPortrait;
        private SpriteRenderer generatedAttack;

        public string PatternName => PatternNames[id];
        public static string PatternNameAt(int patternId) => PatternNames[Mathf.Clamp(patternId, 0, PatternNames.Length - 1)];

        public void Initialize(GameManager manager, EnemyAI controller, SecretBranchDirector director, int patternId)
        {
            game = manager;
            enemy = controller;
            owner = director;
            id = Mathf.Clamp(patternId, 0, PatternNames.Length - 1);
            body = GetComponent<Rigidbody2D>();
            homeX = body.position.x;
            attackAt = Time.time + 1.2f;
            BuildSilhouette();
            enemy.Health.Died += OnDeath;
        }

        private void BuildSilhouette()
        {
            Color color = Color.Lerp(game.Level.Theme.accent, Color.white, (id % 3) * 0.16f);
            Sprite[] guardian = game.Level.Theme.guardianSprites;
            int variant = id % 2;
            if (guardian != null && guardian.Length >= 4 && guardian[variant] != null && guardian[2 + variant] != null)
            {
                Sprite portrait = guardian[variant];
                float scale = 2.5f / Mathf.Max(0.01f, portrait.bounds.size.y);
                generatedPortrait = game.Art.Renderer("GeneratedSecretGuardian", transform, portrait, Color.white,
                    Vector2.one * scale, 53);
                generatedAttack = game.Art.Renderer("GeneratedSecretGuardianAttack", transform, guardian[2 + variant], Color.white,
                    Vector2.one * scale, 54);
                generatedAttack.enabled = false;
                SideSpriteAnimator original = GetComponent<SideSpriteAnimator>();
                if (original != null) original.enabled = false;
                foreach (SpriteRenderer renderer in GetComponentsInChildren<SpriteRenderer>())
                    if (renderer.name == "AnimeAnimatedSprite") renderer.enabled = false;
                return;
            }
            SideSpriteAnimation animation = game.Level.Theme.EnemyAnimation(EnemyKind.Elite, game.Config.enemyAnimations);
            Sprite image = animation != null ? animation.Frame(0) : game.Art.Side.Armor;
            Sprite attack = animation != null ? animation.Frame(3) : image;
            float height = 2.8f / image.bounds.size.y;
            generatedPortrait = game.Art.Renderer("GeneratedSecretGuardian", transform, image, Color.white, Vector2.one * height, 53);
            generatedAttack = game.Art.Renderer("GeneratedSecretGuardianAttack", transform, attack, Color.white, Vector2.one * height, 54);
            generatedAttack.enabled = false;
            SideSpriteAnimator fallback = GetComponent<SideSpriteAnimator>();
            if (fallback != null) fallback.enabled = false;
            foreach (SpriteRenderer renderer in GetComponentsInChildren<SpriteRenderer>())
                if (renderer.name == "AnimeAnimatedSprite") renderer.enabled = false;
        }

        private void FixedUpdate()
        {
            if (game == null || body == null || enemy == null || enemy.Health.IsDead) return;
            if (!game.IsPlaying || !enemy.IsActive || game.Player == null) { body.linearVelocity = Vector2.zero; return; }
            if (preparing) { body.linearVelocity = Vector2.zero; return; }
            float desiredX = Mathf.Clamp(game.Player.transform.position.x + (game.Player.transform.position.x > homeX ? -3f : 3f),
                levelLeft(), levelRight());
            body.MovePosition(new Vector2(Mathf.MoveTowards(body.position.x, desiredX, 2.2f * Time.fixedDeltaTime), body.position.y));
            if (generatedPortrait != null)
            {
                generatedPortrait.flipX = game.Player.transform.position.x > body.position.x;
                generatedAttack.flipX = generatedPortrait.flipX;
            }
            if (Time.time >= attackAt) FireSignature();
        }

        private float levelLeft() => owner.ArenaLeft;
        private float levelRight() => owner.ArenaRight;

        private void FireSignature()
        {
            attackAt = Time.time + 3.5f;
            attackCount++;
            StartCoroutine(PrepareSignature());
        }

        private System.Collections.IEnumerator PrepareSignature()
        {
            preparing = true; lockedTarget = game.Player.transform.position;
            planning = true; ResolveSignature(); planning = false;
            yield return new WaitForSeconds(0.85f);
            if (enemy != null && !enemy.Health.IsDead && enemy.IsActive && owner.Inside) ResolveSignature();
            preparing = false;
        }

        private void ResolveSignature()
        {
            if (!planning && generatedPortrait != null) generatedPortrait.enabled = false;
            if (!planning && generatedAttack != null) generatedAttack.enabled = true;
            Vector2 origin = body.position;
            Vector2 target = lockedTarget;
            Vector2 aim = (target - origin).normalized;
            Color color = game.Level.Theme.danger;
            if (game.Effects.AttackTelegraphsEnabled) game.Hud.Announce("密道守卫 // " + PatternName, 1.25f);
            game.Effects.Telegraph(target, 1.2f, 0.72f, color);
            switch (id)
            {
                case 0:
                    for (int row = -1; row <= 1; row++)
                        for (int wing = -1; wing <= 1; wing++)
                        {
                            Vector2 rotatedAim = Quaternion.Euler(0, 0, row * 13) * new Vector3(aim.x, aim.y, 0);
                            Vector2 direction = (rotatedAim + Vector2.up * (wing * 0.32f)).normalized;
                            Fire(origin, direction, 7.2f, 9 * game.DamageScale, Team.Enemy, 1, color);
                        }
                    break;
                case 1:
                    for (int index = 0; index < 18; index++)
                    {
                        float angle = index * 20 + attackCount * 11;
                        Vector2 direction = Quaternion.Euler(0, 0, angle) * Vector2.right;
                        Fire(origin, direction, index % 2 == 0 ? 6.6f : 4.8f, 8 * game.DamageScale, Team.Enemy, 1, color);
                    }
                    break;
                case 2:
                    for (int side = -1; side <= 1; side++)
                    {
                        Vector2 direction = new Vector2(side * 0.94f, (target.y > origin.y ? 0.34f : -0.34f)).normalized;
                        Fire(origin, direction, 9.5f, 12 * game.DamageScale, Team.Enemy, 1, color);
                        Fire(origin, new Vector2(-direction.x, direction.y), 8.3f, 10 * game.DamageScale, Team.Enemy, 1, color);
                    }
                    break;
                case 3:
                    for (int lane = -2; lane <= 2; lane++)
                    {
                        Vector2 spawn = new Vector2(target.x + lane * 1.15f, target.y + 5.5f);
                        game.Effects.Telegraph(new Vector2(spawn.x, target.y), 0.65f, 0.7f, color);
                        Fire(spawn, Vector2.down, 10.5f, 11 * game.DamageScale, Team.Enemy, 1, color);
                    }
                    break;
                case 4:
                    for (int index = 0; index < 7; index++)
                    {
                        Vector2 direction = Quaternion.Euler(0, 0, index * 51 + attackCount * 27) * Vector2.right;
                        Fire(origin, direction, 6.1f, 9 * game.DamageScale, Team.Enemy, 1, color);
                    }
                    break;
                case 5:
                    for (int arc = 0; arc < 2; arc++)
                        for (int index = 0; index < 7; index++)
                        {
                            float angle = -60 + index * 20 + arc * 180 + attackCount * 9;
                            Vector2 direction = Quaternion.Euler(0, 0, angle) * aim;
                            Fire(origin, direction, 7.8f, 8 * game.DamageScale, Team.Enemy, 1, color);
                        }
                    break;
                case 6:
                    Vector2 locked = (target - origin).normalized;
                    game.Effects.WarningLine(origin, locked, 12f, 0.55f);
                    for (int burst = -1; burst <= 1; burst++)
                    {
                        Vector2 direction = Quaternion.Euler(0, 0, burst * 7f) * locked;
                        Fire(origin, direction, 14, 16 * game.DamageScale, Team.Enemy, 1, color);
                    }
                    break;
                case 7:
                    for (int star = -2; star <= 2; star++)
                    {
                        Vector2 spawn = new Vector2(target.x + star * 2.1f, target.y + 7.2f);
                        game.Effects.Telegraph(new Vector2(spawn.x, target.y), 0.85f, 0.65f, color);
                        Fire(spawn, Vector2.down, 12f, 13 * game.DamageScale, Team.Enemy, 1, color);
                    }
                    break;
                case 8:
                    for (int index = 0; index < 8; index++)
                    {
                        float angle = index * 45 + (attackCount % 2) * 22.5f;
                        Vector2 direction = Quaternion.Euler(0, 0, angle) * Vector2.right;
                        Fire(origin, direction, 8.4f, 11 * game.DamageScale, Team.Enemy, 1, color);
                    }
                    break;
                default:
                    for (int index = -2; index <= 2; index++)
                    {
                        Vector2 low = new Vector2(aim.x, -0.42f + index * 0.04f).normalized;
                        Vector2 high = new Vector2(aim.x, 0.44f - index * 0.04f).normalized;
                        Fire(origin, low, 10.2f, 10 * game.DamageScale, Team.Enemy, 1, color);
                        Fire(origin, high, 7.2f, 10 * game.DamageScale, Team.Enemy, 1, color);
                    }
                    break;
                case 10:
                    for (int wing = -2; wing <= 2; wing++)
                    {
                        Vector2 direction = Quaternion.Euler(0, 0, wing * 14 + (attackCount % 3 - 1) * 9) * aim;
                        Fire(origin, direction, 7.5f + Mathf.Abs(wing), 12 * game.DamageScale, Team.Enemy, 1, color);
                    }
                    break;
                case 11:
                    for (int side = -1; side <= 1; side += 2)
                        for (int row = 0; row < 3; row++)
                            Fire(new Vector2(side < 0 ? owner.ArenaLeft : owner.ArenaRight, 1.2f + row * 1.3f),
                                new Vector2(-side, row % 2 == 0 ? 0.14f : -0.1f).normalized, 6.6f, 11 * game.DamageScale, Team.Enemy, 1, color);
                    break;
                case 12:
                    for (int star = 0; star < 3; star++)
                        for (int arm = 0; arm < 3; arm++)
                        {
                            Vector2 direction = Quaternion.Euler(0, 0, arm * 120 + attackCount * 23 + star * 12) * Vector2.right;
                            Fire(origin, direction, 5.5f + star * 1.7f, 13 * game.DamageScale, Team.Enemy, 1, color);
                        }
                    break;
            }
            if (!planning)
            {
                game.Effects.Burst(origin, color, 12);
                if (generatedPortrait != null) StartCoroutine(ShowGuardianIdle());
            }
        }

        private void Fire(Vector2 origin, Vector2 direction, float speed, float damage, Team team, int pierce, Color color)
        {
            if (planning) game.Effects.BossWarningLane(origin, direction, Mathf.Min(14, speed * 1.5f), 0.48f, 0.85f);
            else game.Projectiles.Fire(origin, direction, speed, damage, team, pierce, color, hitKind: IncomingHitKind.BossSkill);
        }

        private System.Collections.IEnumerator ShowGuardianIdle()
        {
            yield return new WaitForSeconds(0.42f);
            if (generatedPortrait != null && generatedAttack != null && enemy != null && !enemy.Health.IsDead)
            {
                generatedAttack.enabled = false;
                generatedPortrait.enabled = true;
            }
        }

        public void OnDefeated(EnemyAI controller)
        {
            if (rewarded) return;
            rewarded = true;
            owner.MiniBossDefeated(this, controller, id);
        }

        private void OnDeath() { body.linearVelocity = Vector2.zero; }

        private void OnDestroy()
        {
            if (enemy != null && enemy.Health != null) enemy.Health.Died -= OnDeath;
        }
    }
}
