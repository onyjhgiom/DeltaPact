using System.Collections;
using UnityEngine;

namespace MoriEchoes
{
    public sealed class VerticalBossController : MonoBehaviour
    {
        private static readonly string[,] Attacks =
        {
            { "墓碑坠雨", "冥河棺锁", "骨脉喷发" },
            { "沉钟回潮", "潮眼虹吸", "深海横压" },
            { "奇点换极", "星骸棱柱", "地平线坍缩" }
        };
        private static readonly string[,] Forms =
        {
            { "百骸王 · 填墓形态", "百骸王 · 冥火王冠", "百骸王 · 万骨归葬" },
            { "潮汐吞噬者 · 礁甲形态", "潮汐吞噬者 · 海眼形态", "潮汐吞噬者 · 深渊圣兽" },
            { "引力残响 · 星墓外壳", "引力残响 · 双极共振", "引力残响 · 视界吞没" }
        };

        public int Phase { get; private set; } = 1;
        public int FormIndex => Phase - 1;
        public int StageIndex { get; private set; }
        public bool Transitioning => transitionUntil > Time.time;
        public bool ExecutionWindow { get; private set; }
        public bool Preparing => preparing;
        public int AttacksResolved { get; private set; }
        public bool UsesIllustratedParts { get; private set; }
        public int IllustratedPartCount { get; private set; }
        public bool UltimateAttack { get; private set; }
        public float LastProjectileDamage { get; private set; }
        public float LastProjectileRadius { get; private set; }
        public BossField TrackingStrike => trackingStrike;
        public string PatternName { get; private set; } = "竖井回声锁定";
        public string FormName => Forms[StageIndex, FormIndex];
        public string AttackId => Attacks[StageIndex, attackCursor % 3];
        private GameManager game;
        private LevelManager level;
        private EnemyAI enemy;
        private Rigidbody2D body;
        private BossField trackingStrike;
        private Transform formRoot;
        private SpriteRenderer formHalo;
        private SpriteRenderer formCore;
        private float attackAt;
        private float resolveAt;
        private float transitionUntil;
        private int attackCursor;
        private int phaseThreeExecutionStep;
        private Vector2 lockedPlayer;
        private Vector2 patternTarget;
        private bool preparing;
        private bool executionStarted;
        private bool executionRunning;
        private bool deathEffectsPlayed;
        private bool planningAttack;

        private void LateUpdate()
        {
            if (formRoot == null || game == null || UsesIllustratedParts) return;
            float time = Time.time;
            if (StageIndex == 0)
                formRoot.Rotate(0, 0, (8 + Phase * 5) * Time.deltaTime);
            else if (StageIndex == 1)
            {
                formHalo.transform.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(time * 1.1f) * 9);
                float swell = 1 + Mathf.Sin(time * 2.3f) * 0.035f;
                formHalo.transform.localScale = new Vector3(swell, 1 / swell, 1);
            }
            else
            {
                formHalo.transform.Rotate(0, 0, (-24 - Phase * 8) * Time.deltaTime);
                formCore.transform.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(time * 0.7f) * 12);
                float pulse = 1 + Mathf.Sin(time * 1.8f) * 0.055f;
                formHalo.transform.localScale = Vector3.one * pulse;
            }
        }

        public void Initialize(GameManager owner, EnemyAI actor, Rigidbody2D rigidbody)
        {
            game = owner; level = owner.Level; enemy = actor; body = rigidbody;
            StageIndex = Mathf.Clamp(game.SelectedStage - 5, 0, 2);
            body.gravityScale = 0;
            body.constraints = RigidbodyConstraints2D.FreezeAll;
            attackAt = Time.time + 2;
            enemy.Health.Damaged += OnDamaged;
            BuildForm();
            game.Effects.Encounter(body.position, 2, 5.5f, 1.15f, 0, true);
        }

        public void Step()
        {
            if (executionRunning || game == null || level == null || enemy == null || !enemy.IsActive
                || !game.IsPlaying || game.Player == null || game.Player.Health.IsDead) return;
            body.linearVelocity = Vector2.zero;
            int requested = enemy.Health.Current / enemy.Health.Maximum <= 0.3f ? 3
                : enemy.Health.Current / enemy.Health.Maximum <= 0.66f ? 2 : 1;
            if (requested > Phase) EnterForm(requested);
            if (Transitioning) return;
            if (preparing)
            {
                if (trackingStrike != null && trackingStrike.IsWarning)
                {
                    trackingStrike.FollowWarningAt(game.Player.transform.position);
                    lockedPlayer = trackingStrike.transform.position;
                }
                else if (trackingStrike != null) lockedPlayer = trackingStrike.transform.position;
                if (Time.time >= resolveAt) ResolveAttack();
                return;
            }
            if (Time.time < attackAt) return;
            if (GetComponent<BossController>().Techniques.TryBegin()) return;
            preparing = true;
            lockedPlayer = game.Player.transform.position;
            patternTarget = lockedPlayer;
            resolveAt = Time.time + 1.4f;
            PatternName = AttackDescription(attackCursor % 3);
            UltimateAttack = Phase >= 3 && attackCursor % 3 == 2;
            // Preview uses the same locked target and geometry as the actual volley.
            planningAttack = true;
            if (StageIndex == 0) ResolveCrypt(attackCursor % 3);
            else if (StageIndex == 1) ResolveTide(attackCursor % 3);
            else ResolveRift(attackCursor % 3);
            planningAttack = false;
            CreateTrackingStrike();
        }

        private string AttackDescription(int slot)
        {
            if (StageIndex == 0) return new[] { "上方石棺列阵后砸落", "双侧灵柩交错封道", "骸骨脉冲由裂缝向上喷发" }[slot];
            if (StageIndex == 1) return new[] { "巨钟回潮沿竖井横向扫荡", "潮眼向下虹吸后爆发气泡柱", "高压海流与低位回浪交叉" }[slot];
            return new[] { "井内重力反转并交换极性", "星骸棱柱沿墙面折射坠落", "事件视界由外向内逐层闭合" }[slot];
        }

        private void ResolveAttack()
        {
            preparing = false;
            AttacksResolved++;
            int slot = attackCursor % 3;
            UltimateAttack = Phase >= 3 && slot == 2;
            attackCursor++;
            game.Effects.BossSkillImpact(lockedPlayer, TrackingStyle(), StageIndex == 0 ? 3.1f : 2.7f);
            if (UltimateAttack) game.Effects.BossUltimate(lockedPlayer, StageIndex == 2 ? 22 : 20);
            // The primary volley must keep the world-space geometry that its preview showed.
            // The separate target disc follows briefly, then commits to its own marked position.
            lockedPlayer = patternTarget;
            if (StageIndex == 0) ResolveCrypt(slot);
            else if (StageIndex == 1) ResolveTide(slot);
            else ResolveRift(slot);
            game.Audio.Play(SoundKind.Blast);
            attackAt = Time.time + Mathf.Max(1.35f, 3.2f - Phase * 0.38f - game.Difficulty * 0.3f);
        }

        private void CreateTrackingStrike()
        {
            if (trackingStrike != null && trackingStrike.IsWarning) trackingStrike.Cancel();
            var entity = new GameObject(AttackId + "_TrackingStrike");
            entity.transform.SetParent(level.transform, false);
            entity.transform.position = lockedPlayer;
            trackingStrike = entity.AddComponent<BossField>();
            float area = UltimateAttack ? game.Config.bossUltimateAreaScale : game.Config.bossSkillAreaScale;
            float damageScale = UltimateAttack ? game.Config.bossUltimateDamageMultiplier : game.Config.bossSkillDamageMultiplier;
            float radius = UltimateAttack ? 1.35f : 1.05f;
            float damage = UltimateAttack ? 18 : 12;
            trackingStrike.InitializeAttack(game, enemy.Health, AttackId + "_TrackingStrike", BossFieldShape.Disc,
                TrackingStyle(), Vector2.one * radius * 2 * area, Mathf.Max(0.6f, resolveAt - Time.time), 0.42f,
                damage * damageScale);
            trackingStrike.HitKind = UltimateAttack ? IncomingHitKind.BossUltimate : IncomingHitKind.BossSkill;
            trackingStrike.ConfigureTargetLock(0.85f);
        }

        private BossFieldStyle TrackingStyle()
        {
            return StageIndex == 0 ? BossFieldStyle.Fracture : StageIndex == 1 ? BossFieldStyle.Hydraulic : BossFieldStyle.Gravity;
        }

        private void ResolveCrypt(int slot)
        {
            Color tint = new Color(1f, 0.38f, 0.21f);
            if (slot == 0)
            {
                for (int column = -3; column <= 3; column++)
                {
                    if (Mathf.Abs(column - Mathf.RoundToInt(lockedPlayer.x / 2)) == 1) continue;
                    Vector2 spawn = new Vector2(column * 1.85f, lockedPlayer.y + 7.5f);
                    game.Effects.Telegraph(new Vector2(spawn.x, lockedPlayer.y), 0.78f, 0.35f, tint);
                    FireBossProjectile(spawn, Vector2.down, 9.8f, 14, tint);
                }
            }
            else if (slot == 1)
            {
                for (int side = -1; side <= 1; side += 2)
                    for (int lane = 0; lane < 3; lane++)
                    {
                        float x = side * (6.3f - lane * 1.8f);
                        Vector2 origin = new Vector2(x, lockedPlayer.y + 3 + lane * 0.7f);
                        Vector2 heading = new Vector2(-side * 0.42f, -1).normalized;
                        game.Effects.WarningLine(origin, heading, 8, 0.16f);
                        FireBossProjectile(origin, heading, 8.3f, 13, tint);
                    }
            }
            else
            {
                for (int index = -2; index <= 2; index++)
                {
                    Vector2 origin = new Vector2(lockedPlayer.x + index * 1.4f, lockedPlayer.y - 6.2f);
                    Vector2 aim = new Vector2(index * 0.13f, 1).normalized;
                    game.Effects.Telegraph(new Vector2(origin.x, lockedPlayer.y - 1.2f), 0.58f, 0.5f, tint);
                    FireBossProjectile(origin, aim, 10.2f, 12, Color.Lerp(tint, Color.white, 0.25f));
                }
            }
            if (!planningAttack) game.Effects.Explosion(body.position, 2.2f, tint);
        }

        private void ResolveTide(int slot)
        {
            Color tint = new Color(0.18f, 0.8f, 1f);
            if (slot == 0)
            {
                for (int column = -3; column <= 3; column++)
                {
                    Vector2 spawn = new Vector2(column * 1.9f, lockedPlayer.y + 6.5f);
                    Vector2 direction = new Vector2(-Mathf.Sign(column == 0 ? 1 : column) * 0.62f, -0.78f).normalized;
                    FireBossProjectile(spawn, direction, 8.5f, 12, tint);
                    if (!planningAttack) game.Effects.Encounter(spawn, 3, 0.65f, 0.25f, 0, false, true);
                }
            }
            else if (slot == 1)
            {
                game.Effects.Telegraph(new Vector2(0, lockedPlayer.y - 2), 4.4f, 0.38f, tint);
                for (int column = -2; column <= 2; column++)
                {
                    Vector2 spawn = new Vector2(lockedPlayer.x + column * 1.65f, lockedPlayer.y - 7f);
                    FireBossProjectile(spawn, Vector2.up, 10.5f, 13, tint);
                }
            }
            else
            {
                float rowY = lockedPlayer.y + (Phase % 2 == 0 ? 1.2f : -1.5f);
                for (int side = -1; side <= 1; side += 2)
                {
                    Vector2 start = new Vector2(side * 7.2f, rowY);
                    game.Effects.WarningLine(start, Vector2.left * side, 14, 0.23f);
                    for (int index = 0; index < 5; index++)
                    {
                        Vector2 point = start + Vector2.left * side * index * 2.5f;
                        FireBossProjectile(point, Vector2.left * side, 12.2f, 10, tint);
                    }
                }
            }
            if (!planningAttack) game.Effects.SkillWave(lockedPlayer, tint, 3.8f);
        }

        private void ResolveRift(int slot)
        {
            Color tint = new Color(0.7f, 0.34f, 1f);
            if (slot == 0)
            {
                if (!planningAttack)
                {
                    game.Player.Motor.InvertGravity(1.65f);
                    game.Effects.Pulse(lockedPlayer, 4.2f);
                }
                Vector2[] directions = { Vector2.up, Vector2.down, Vector2.left, Vector2.right };
                for (int index = 0; index < directions.Length; index++)
                    FireBossProjectile(body.position + directions[index] * 2.2f, directions[(index + 1) % directions.Length],
                        9.5f, 14, tint);
            }
            else if (slot == 1)
            {
                for (int side = -1; side <= 1; side++)
                {
                    Vector2 spawn = new Vector2(side * 7, lockedPlayer.y + 6);
                    Vector2 bend = new Vector2(-side * 0.42f, -1).normalized;
                    game.Effects.WarningLine(spawn, bend, 10, 0.3f);
                    FireBossProjectile(spawn, bend, 8, 16, tint);
                    FireBossProjectile(spawn, Vector2.down, 10, 10, Color.white);
                }
            }
            else
            {
                for (int ring = 0; ring < 3; ring++)
                {
                    float radius = 2.5f + ring * 2f;
                    game.Effects.Telegraph(lockedPlayer, radius, 0.34f + ring * 0.12f, tint);
                    for (int ray = 0; ray < 8; ray++)
                    {
                        Vector2 direction = Quaternion.Euler(0, 0, ray * 45 + Phase * 15) * Vector2.up;
                        FireBossProjectile(lockedPlayer + direction * radius, direction, 7.8f + ring, 11, tint);
                    }
                }
            }
            if (!planningAttack) game.Effects.Explosion(body.position, 2.6f, tint);
        }

        private void FireBossProjectile(Vector2 origin, Vector2 heading, float speed, float damage, Color tint)
        {
            float multiplier = UltimateAttack ? game.Config.bossUltimateDamageMultiplier : game.Config.bossSkillDamageMultiplier;
            float area = UltimateAttack ? game.Config.bossUltimateAreaScale : game.Config.bossSkillAreaScale;
            LastProjectileDamage = damage * game.DamageScale * multiplier;
            LastProjectileRadius = (UltimateAttack ? 0.9f : 0.55f) * area;
            if (planningAttack)
            {
                game.Effects.BossWarningLane(origin, heading, 14, LastProjectileRadius * 2, resolveAt - Time.time);
                return;
            }
            game.Projectiles.Fire(origin, heading, speed, LastProjectileDamage, Team.Enemy, 1, tint,
                false, 0, LastProjectileRadius, UltimateAttack ? IncomingHitKind.BossUltimate : IncomingHitKind.BossSkill);
        }

        private void EnterForm(int requested)
        {
            if (trackingStrike != null && trackingStrike.IsWarning) trackingStrike.Cancel();
            trackingStrike = null;
            Phase = requested;
            preparing = false;
            attackAt = Time.time + 2;
            transitionUntil = Time.time + 1.7f;
            enemy.Health.Invulnerable = true;
            BuildForm();
            Vector2 center = body.position;
            game.Effects.Burst(center, Color.white, 28);
            game.Effects.Encounter(center, 3, 7 + Phase * 1.5f, 1.2f, 0, true);
            game.Effects.Explosion(center, 3.2f + Phase * 0.5f, level.Theme.accent);
            game.CameraRig.Shake(0.75f);
            game.Hud.Announce(FormName + " // 外壳解体，攻击进入增压阶段", 2.6f);
            StartCoroutine(ReleaseFormProtection());
        }

        private IEnumerator ReleaseFormProtection()
        {
            while (Time.time < transitionUntil) yield return null;
            if (enemy != null && enemy.Health != null && !enemy.Health.IsDead) enemy.Health.Invulnerable = false;
        }

        private void BuildForm()
        {
            if (formRoot != null) { formRoot.gameObject.SetActive(false); Destroy(formRoot.gameObject); }
            formRoot = new GameObject("VerticalBoss_" + StageIndex + "_Form_" + Phase).transform;
            formRoot.SetParent(transform, false);
            UsesIllustratedParts = level.Theme.BossOrnament(Phase, 3) != null;
            IllustratedPartCount = 0;
            if (UsesIllustratedParts)
            {
                Vector2[] positions = { new Vector2(-1.25f, 0), new Vector2(0, 0.25f), new Vector2(1.15f, 0.9f), new Vector2(0, 1.7f) };
                Vector2[] sizes = { new Vector2(1.65f, 1.85f), new Vector2(1.15f, 1.2f), new Vector2(2.3f, 2.55f), new Vector2(1.35f, 1.4f) };
                for (int slot = 0; slot < 4; slot++)
                {
                    Sprite sprite = level.Theme.BossOrnament(Phase, slot);
                    if (sprite == null) continue;
                    Vector2 size = sizes[slot] * (0.9f + FormIndex * 0.1f);
                    Vector2 scale = new Vector2(size.x / sprite.bounds.size.x, size.y / sprite.bounds.size.y);
                    SpriteRenderer part = game.Art.Renderer("IllustratedBossEquipment_" + slot, formRoot, sprite,
                        Color.white, scale, slot == 2 ? 100 : 142);
                    part.transform.localPosition = positions[slot];
                    IllustratedPartCount++;
                }
                return;
            }
            Color tint = level.Theme.accent;
            Sprite coreSprite = StageIndex == 0 ? game.Art.Skull : StageIndex == 1 ? game.Art.Ring : game.Art.Diamond;
            formCore = game.Art.Renderer("BossPhaseSignature", formRoot, coreSprite,
                Color.white, new Vector2(2.2f + Phase * 0.35f, 2.1f + Phase * 0.3f), 142);
            SpriteRenderer crown = formCore;
            crown.transform.localPosition = Vector2.up * (1.25f + Phase * 0.15f);
            formHalo = game.Art.Renderer("BossPhaseHalo", formRoot, game.Art.Ring, tint * (1.1f + Phase * 0.2f),
                new Vector2(4.6f + Phase * 0.8f, 3.9f + Phase * 0.65f), 98);
            formHalo.transform.localPosition = Vector2.up * 0.25f;
            for (int index = 0; index < 3 + Phase; index++)
            {
                float angle = 360f * index / (3 + Phase);
                Vector2 orbit = Quaternion.Euler(0, 0, angle) * Vector2.up * (1.65f + Phase * 0.12f);
                SpriteRenderer shard = game.Art.Renderer("BossFormShard_" + index, formRoot, game.Art.Diamond, tint * 1.4f,
                    new Vector2(0.38f, 0.92f), 143);
                shard.transform.localPosition = orbit;
                shard.transform.localRotation = Quaternion.Euler(0, 0, -angle);
            }
        }

        private void OnDamaged(float amount)
        {
            if (executionStarted || enemy == null || enemy.Health.Current / enemy.Health.Maximum > 0.15f) return;
            executionStarted = true; ExecutionWindow = true;
            enemy.Health.SetCurrent(enemy.Health.Maximum * 0.15f);
            StartCoroutine(ExecuteFinisher());
        }

        private IEnumerator ExecuteFinisher()
        {
            executionRunning = true; preparing = false; enemy.Health.Invulnerable = true;
            PlayerController player = game.Player;
            float originalScale = Time.timeScale;
            Time.timeScale = 0.23f;
            game.Hud.Announce("处决窗口 // 竖井突进终结", 2.3f);
            if (player != null && player.Motor != null)
            {
                player.Motor.SetCinematicLocked(true);
                player.Health.Invulnerable = true;
                Vector2 start = player.transform.position;
                Vector2 end = body.position + new Vector2(player.transform.position.x < body.position.x ? -1.35f : 1.35f, -0.75f);
                float elapsed = 0;
                while (elapsed < 0.72f && player != null)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float blend = Mathf.SmoothStep(0, 1, elapsed / 0.72f);
                    player.GetComponent<Rigidbody2D>().position = Vector2.Lerp(start, end, blend);
                    game.Effects.Ghost(player.transform.position);
                    game.Effects.Burst(player.transform.position, game.Character.accent, 3);
                    yield return null;
                }
            }
            yield return new WaitForSecondsRealtime(0.28f);
            DeathEffects();
            Time.timeScale = originalScale;
            if (player != null)
            {
                player.Health.Invulnerable = false;
                player.Motor.SetCinematicLocked(false);
            }
            enemy.Health.Invulnerable = false;
            enemy.Health.TakeDamage(enemy.Health.Maximum, Team.Player, true);
        }

        public void DeathEffects()
        {
            if (deathEffectsPlayed || game == null || body == null) return;
            if (trackingStrike != null) trackingStrike.Cancel();
            trackingStrike = null;
            deathEffectsPlayed = true;
            Color tint = level.Theme.danger;
            for (int index = 0; index < 8; index++)
            {
                float angle = index * 45;
                Vector2 offset = Quaternion.Euler(0, 0, angle) * Vector2.up * (1.2f + index % 3 * 0.45f);
                game.Effects.Explosion((Vector2)body.position + offset, 1.4f + index % 3 * 0.6f, tint);
            }
            game.Effects.Explosion(body.position, 6.8f, Color.white);
            game.Effects.Debris(body.position, tint);
            game.Effects.Burst(body.position, Color.white, 42);
            game.AwardStyle(6500); game.CameraRig.Shake(1);
        }

        private void OnDestroy() { if (enemy != null && enemy.Health != null) enemy.Health.Damaged -= OnDamaged; }
    }
}
