using System.Collections;
using UnityEngine;

namespace MoriEchoes
{
    // 水中 Boss：悬浮游动、攻击全部以玩家当前位置为基准（适配上下浮沉的水下战斗）。
    // 三相形态，随血量增压。区别于地面 Boss（冻结 Y、站位固定），本 Boss 可在水柱中移动。
    public sealed class SwimBossController : MonoBehaviour
    {
        private GameManager game;
        private LevelManager level;
        private EnemyAI enemy;
        private Rigidbody2D body;
        private bool underwater;
        private float surfaceY;
        private float minY;
        private float maxY;
        private float attackAt;
        private float resolveAt;
        private int attackCursor;
        private int phase = 1;
        private Vector2 lockedPlayer;
        private bool preparing;
        private bool deathEffectsPlayed;
        private float weavePhase;
        private Vector2 anchorVelocity;
        private BossField trackingStrike;
        private Transform formParts;
        public int IllustratedPartCount { get; private set; }

        public int Phase => phase;
        public int AttacksResolved { get; private set; }
        public bool Preparing => preparing;
        public BossField TrackingStrike => trackingStrike;
        public int TrackedWarnings { get; private set; }
        public bool DeathEffectsPlayed => deathEffectsPlayed;
        public string FormName => underwater
            ? new[] { "深渊母巢 · 暗流外壳", "深渊母巢 · 墨渊翻涌", "深渊母巢 · 热泉暴走" }[phase - 1]
            : new[] { "狂澜利维坦 · 破浪形态", "狂澜利维坦 · 怒涛形态", "狂澜利维坦 · 灭浪形态" }[phase - 1];

        public void Initialize(GameManager owner, EnemyAI actor, Rigidbody2D rigidbody)
        {
            game = owner; level = owner.Level; enemy = actor; body = rigidbody;
            underwater = level.IsUnderwaterStage;
            SwimStageDirector swim = level.Swim;
            surfaceY = swim != null ? swim.SurfaceY : 2f;
            minY = swim != null ? swim.MinY : -6f;
            maxY = swim != null ? swim.MaxY : 8f;
            body.gravityScale = 0;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            weavePhase = Random.Range(0f, Mathf.PI * 2f);
            attackAt = Time.time + 2.2f;
            game.Effects.Encounter(body.position, 2, 5f, 1.1f, 0, true);
            game.Hud.Announce(FormName, 2.4f);
            BuildFormParts();
        }

        private void BuildFormParts()
        {
            if (formParts != null) { formParts.gameObject.SetActive(false); Destroy(formParts.gameObject); }
            formParts = new GameObject("SwimPhysicalFormParts_" + phase).transform;
            formParts.SetParent(transform, false); IllustratedPartCount = 0;
            Vector2[] offsets = { new Vector2(-1.7f,0.05f), new Vector2(1.7f,0.05f), new Vector2(0,1.5f), new Vector2(0,0.2f) };
            for (int slot = 0; slot < 4; slot++)
            {
                Sprite part = level.Theme.BossOrnament(phase,slot);
                if (part == null) continue;
                float width = slot < 2 ? 1.35f : slot == 2 ? 1.4f : 0.65f;
                SpriteRenderer renderer = game.Art.Renderer("SwimPhysicalPart_"+slot,formParts,part,Color.white,
                    Vector2.one * (width / part.bounds.size.x),slot == 3 ? 103 : 100);
                renderer.transform.localPosition = offsets[slot]; IllustratedPartCount++;
            }
        }

        public void Step()
        {
            if (game == null || level == null || enemy == null || !enemy.IsActive
                || !game.IsPlaying || game.Player == null || game.Player.Health.IsDead)
            { if (body != null) body.linearVelocity = Vector2.zero; return; }

            float ratio = enemy.Health.Current / Mathf.Max(1, enemy.Health.Maximum);
            int requested = ratio <= 0.3f ? 3 : ratio <= 0.66f ? 2 : 1;
            if (requested > phase) EnterForm(requested);

            // 悬浮游动：保持在玩家前方一段距离，水下还跟随玩家高度
            Vector2 player = game.Player.transform.position;
            Camera camera = game.CameraRig.Camera;
            float low = Mathf.Max(minY + 1.7f, camera.transform.position.y - camera.orthographicSize + 2.1f);
            float high = Mathf.Max(low, Mathf.Min(maxY - 1.7f, camera.transform.position.y + camera.orthographicSize - 2.1f));
            float anchorX = Mathf.Clamp(player.x + 6.5f, 5, level.Swim.CourseLength - 3);
            float anchorY = Mathf.Clamp(underwater ? player.y + (phase - 2) * 1.1f : surfaceY + 0.25f * (phase - 1), low, high);
            Vector2 anchor = new Vector2(anchorX, anchorY);
            Vector2 next = Vector2.SmoothDamp(body.position, anchor, ref anchorVelocity, 0.65f, 4.2f, Time.fixedDeltaTime);
            body.linearVelocity = (next - body.position) / Time.fixedDeltaTime;
            // Soft steering plus a final water-column clamp prevents phase transitions leaving the arena.
            if (body.position.y < low - 0.5f || body.position.y > high + 0.5f)
                body.position = new Vector2(body.position.x, Mathf.Clamp(body.position.y, low - 0.5f, high + 0.5f));

            if (preparing)
            {
                if (trackingStrike != null && trackingStrike.IsWarning)
                {
                    if (trackingStrike.FollowWarningAt(player)) TrackedWarnings++;
                    lockedPlayer = trackingStrike.transform.position;
                }
                if (Time.time >= resolveAt) ResolveAttack();
                return;
            }
            if (Time.time < attackAt) return;
            if (GetComponent<BossController>().Techniques.TryBegin()) return;
            preparing = true;
            lockedPlayer = player;
            resolveAt = Time.time + 1.65f;
            trackingStrike = CreateField(lockedPlayer, underwater ? "AbyssPressureLock" : "SeaWaveLock",
                BossFieldShape.Disc, underwater ? BossFieldStyle.Gravity : BossFieldStyle.Hydraulic,
                Vector2.one * (2.8f + phase * 0.3f), resolveAt - Time.time, 0.4f, 15 + phase * 2);
            trackingStrike.ConfigureTargetLock(0.85f);
        }

        private void EnterForm(int requested)
        {
            phase = requested;
            BuildFormParts();
            if (trackingStrike != null && trackingStrike.IsWarning) trackingStrike.Cancel();
            trackingStrike = null; preparing = false; attackAt = Time.time + 1.5f;
            enemy.Health.Invulnerable = true;
            game.Effects.Burst(body.position, Color.white, 26);
            game.Effects.Encounter(body.position, 3, 5 + phase, 1f, 0, true);
            game.Effects.Explosion(body.position, 2.6f + phase * 0.4f, level.Theme.accent);
            game.CameraRig.Shake(0.6f);
            game.Hud.Announce(FormName + " // 洋流失控，弹幕增压", 2.4f);
            StartCoroutine(ReleaseProtection());
        }

        private IEnumerator ReleaseProtection()
        {
            yield return new WaitForSeconds(1.2f);
            if (enemy != null && enemy.Health != null && !enemy.Health.IsDead) enemy.Health.Invulnerable = false;
        }

        private void ResolveAttack()
        {
            preparing = false;
            AttacksResolved++;
            int slot = attackCursor % 3;
            attackCursor++;
            Color tint = level.Theme.danger;
            game.Effects.BossImpact(body.position, 2.2f);
            game.Effects.BossSkillImpact(lockedPlayer, underwater ? BossFieldStyle.Gravity : BossFieldStyle.Hydraulic, 2.1f + phase * 0.3f);
            if (underwater)
            {
                if (slot == 0) AimedVolley(tint);
                else if (slot == 1) SurroundingCurrent(tint);
                else GeyserBurst(tint);
            }
            else
            {
                if (slot == 0) AimedVolley(tint);
                else if (slot == 1)
                {
                    Vector2 origin = lockedPlayer + Vector2.right * 6;
                    BossField wave = CreateField(origin, "SeaBreakingWave", BossFieldShape.Box, BossFieldStyle.Hydraulic, new Vector2(2.8f, 3.1f), 0.65f, 1.8f, 19 + phase * 2);
                    wave.ConfigurePath(new[] { origin, origin + Vector2.left * 13 }, 1.6f);
                }
                else
                {
                    Vector2 origin = lockedPlayer + Vector2.up * 4;
                    BossField crash = CreateField(origin, "SeaSkybreaker", BossFieldShape.Disc, BossFieldStyle.Charge, Vector2.one * (3.4f + phase * 0.45f), 0.7f, 0.9f, 22 + phase * 2);
                    crash.ConfigurePath(new[] { origin, lockedPlayer + Vector2.down }, 0.65f);
                }
            }
            if (phase == 3 && slot == 2)
            {
                game.Effects.BossUltimate(lockedPlayer, underwater ? 13 : 15);
                BossField ultimate = CreateField(lockedPlayer, underwater ? "AbyssVentCataclysm" : "SeaTsunamiCollapse",
                    underwater ? BossFieldShape.Box : BossFieldShape.Ring, underwater ? BossFieldStyle.Molten : BossFieldStyle.Hydraulic,
                    underwater ? new Vector2(7.5f, 12) : Vector2.one * 12, 1.5f, 1.25f, 32);
                ultimate.HitKind = IncomingHitKind.BossUltimate;
            }
            trackingStrike = null;
            game.Audio.Play(SoundKind.Blast);
            attackAt = Time.time + Mathf.Max(1.3f, 3.1f - phase * 0.36f - game.Difficulty * 0.3f);
        }

        // A 瞄准齐射：朝玩家方向的扇形水弹
        private void AimedVolley(Color tint)
        {
            Vector2 origin = body.position;
            Vector2 dir = (lockedPlayer - origin).normalized;
            int count = 3 + phase * 2;
            for (int index = 0; index < count; index++)
            {
                float offset = (index - (count - 1) * 0.5f) * 11f;
                Vector2 spread = Quaternion.Euler(0, 0, offset) * dir;
                game.Projectiles.Fire(origin + spread * 0.9f, spread, 10f + phase, 12 * game.DamageScale, Team.Enemy, 1, tint, hitKind: IncomingHitKind.BossSkill);
            }
            game.Effects.WaterMuzzle(origin, dir, tint);
        }

        // B 环绕暗流：在玩家周围生成一圈弹，向内收拢
        private void SurroundingCurrent(Color tint)
        {
            int rays = 6 + phase * 2;
            float radius = 3.2f + phase * 0.4f;
            for (int ray = 0; ray < rays; ray++)
            {
                Vector2 dir = Quaternion.Euler(0, 0, ray * 360f / rays + phase * 12f) * Vector2.up;
                Vector2 spawn = lockedPlayer + dir * radius;
                game.Projectiles.Fire(spawn, -dir, 6.5f + phase * 0.7f, 11 * game.DamageScale, Team.Enemy, 1,
                    Color.Lerp(tint, level.Theme.accent, 0.4f), hitKind: IncomingHitKind.BossSkill);
            }
            game.Effects.SkillWave(lockedPlayer, tint, radius);
        }

        // C 喷柱/跃浪：在玩家附近多点延迟爆起
        private void GeyserBurst(Color tint)
        {
            int vents = 2 + phase;
            for (int index = 0; index < vents; index++)
            {
                Vector2 point = lockedPlayer + new Vector2((index - (vents - 1) * 0.5f) * 2.1f, 0);
                Vector2 spawn = new Vector2(point.x, minY + 0.5f);
                CreateField(new Vector2(point.x, (minY + maxY) * 0.5f), "AbyssThermalVent_" + index, BossFieldShape.Box,
                    BossFieldStyle.Molten, new Vector2(0.9f, maxY - minY), 1.1f + index * 0.2f, 0.55f, 18 + phase * 2);
                Vector2 up = Vector2.up;
                game.Projectiles.Fire(spawn, up, 12f, 14 * game.DamageScale, Team.Enemy, 1, tint, hitKind: IncomingHitKind.BossSkill);
            }
        }

        private BossField CreateField(Vector2 point, string id, BossFieldShape shape, BossFieldStyle style, Vector2 size, float warning, float duration, float damage)
        {
            var entity = new GameObject(id); entity.transform.SetParent(level.transform, false); entity.transform.position = point;
            BossField field = entity.AddComponent<BossField>();
            field.InitializeAttack(game, enemy.Health, id, shape, style, size, warning, duration, damage, 0, shape == BossFieldShape.Ring ? 2.1f : 0);
            return field;
        }

        public void DeathEffects()
        {
            if (deathEffectsPlayed || game == null || body == null) return;
            deathEffectsPlayed = true;
            Color tint = level != null ? level.Theme.danger : ProceduralArt.Danger;
            for (int index = 0; index < 8; index++)
            {
                float angle = index * 45f;
                Vector2 offset = Quaternion.Euler(0, 0, angle) * Vector2.up * (1.2f + index % 3 * 0.5f);
                game.Effects.Explosion((Vector2)body.position + offset, 1.4f + index % 3 * 0.6f, tint);
            }
            game.Effects.Explosion(body.position, 6.6f, Color.white);
            if (underwater)
            {
                for (int vent = -2; vent <= 2; vent++)
                    game.Effects.BossSkillImpact(body.position + Vector2.right * vent * 1.6f, BossFieldStyle.Molten, 2.8f);
            }
            else
            {
                for (int wave = -3; wave <= 3; wave++)
                    game.Effects.Explosion(new Vector2(body.position.x + wave * 1.5f, surfaceY), 3.2f, level.Theme.accent);
            }
            game.Effects.BossUltimate(body.position, underwater ? 13 : 16);
            game.Effects.Debris(body.position, tint);
            game.Effects.Burst(body.position, Color.white, 40);
            game.AwardStyle(6000);
            game.CameraRig.Shake(1);
        }

        private void OnDestroy()
        {
            if (trackingStrike != null) trackingStrike.Cancel();
        }
    }
}
