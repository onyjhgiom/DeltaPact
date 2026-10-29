using System.Collections.Generic;
using UnityEngine;

namespace MoriEchoes
{
    public sealed class CharacterSkill : MonoBehaviour
    {
        public float Remaining => Mathf.Max(0, readyAt - Time.time);
        public float ActiveRemaining => Mathf.Max(0, activeUntil - Time.time);
        public bool Active => ActiveRemaining > 0;
        public float FireRateMultiplier => Active && game.CharacterId == 0 ? 1.6f : Active && game.CharacterId == 7 ? 1.9f : 1;
        public float CriticalBonus => Active && game.CharacterId == 0 ? 0.2f : Active && game.CharacterId == 7 ? 0.08f : 0;
        public float RecoilMultiplier => Active && game.CharacterId == 8 ? 0.35f : 1f;
        public float SpeedMultiplier => Active && game.CharacterId == 2 ? 1.5f : 1;
        public Sprite Icon { get; private set; }
        public int Activations { get; private set; }
        public float LastHealing { get; private set; }
        public float LastTeleportDistance { get; private set; }
        public int LastPiercedTargets { get; private set; }
        public int EmberImpacts { get; private set; }
        private readonly RaycastHit2D[] skillHits = new RaycastHit2D[256];
        private readonly HashSet<Health> piercedTargets = new HashSet<Health>();
        private readonly HashSet<EnemyAI> emberTargets = new HashSet<EnemyAI>();
        private readonly List<FrozenEnemy> frozenEnemies = new List<FrozenEnemy>();
        private readonly BurningZone[] burningZones = new BurningZone[3];
        private GameManager game;
        private PlayerController player;
        private SpriteRenderer aura;
        private SpriteRenderer sniperBeam;
        private SpriteRenderer emberCharge;
        private float readyAt;
        private float activeUntil;
        private float nextHealTick;
        private float nextTrail;
        private float beamUntil;
        private Vector2 beamOrigin;
        private Vector2 beamDirection;
        private float beamLength;
        private int skillHitCount;
        private EnemyAI emberTarget;
        private Vector2 emberOrigin;
        private Vector2 emberDestination;
        private Vector2 emberPosition;
        private float emberFlight;
        private float emberFlightDuration;
        private int emberBounces;
        private bool emberFlying;

        private sealed class BurningZone
        {
            public Vector2 Center;
            public float Until;
            public float NextTick;
            public SpriteRenderer Visual;
        }

        private sealed class FrozenEnemy
        {
            public readonly EnemyAI Enemy;
            public readonly float Until;
            private readonly Rigidbody2D body;
            private readonly RigidbodyConstraints2D constraints;
            private readonly Vector2 velocity;
            private readonly float angularVelocity;
            private readonly bool enemyEnabled;
            private readonly SideSpriteAnimator animator;
            private readonly bool animatorEnabled;
            private readonly SideCharacterVisual visual;
            private readonly bool visualEnabled;
            private readonly Collider2D collider;
            private readonly Collider2D playerCollider;
            private readonly bool ignoredCollision;

            public FrozenEnemy(EnemyAI enemy, Collider2D playerBody, float seconds)
            {
                Enemy = enemy; Until = Time.time + seconds;
                enemyEnabled = enemy.enabled;
                collider = enemy.GetComponent<CapsuleCollider2D>(); playerCollider = playerBody;
                if (collider != null && playerCollider != null)
                {
                    ignoredCollision = Physics2D.GetIgnoreCollision(collider, playerCollider);
                    Physics2D.IgnoreCollision(collider, playerCollider, true);
                }
                body = enemy.GetComponent<Rigidbody2D>();
                if (body != null)
                {
                    constraints = body.constraints; velocity = body.linearVelocity; angularVelocity = body.angularVelocity;
                    body.linearVelocity = Vector2.zero; body.angularVelocity = 0;
                    body.constraints = RigidbodyConstraints2D.FreezeAll;
                }
                animator = enemy.GetComponent<SideSpriteAnimator>();
                if (animator != null) { animatorEnabled = animator.enabled; animator.enabled = false; }
                visual = enemy.GetComponent<SideCharacterVisual>();
                if (visual != null) { visualEnabled = visual.enabled; visual.enabled = false; }
                enemy.enabled = false;
            }

            public void Restore()
            {
                if (collider != null && playerCollider != null)
                    Physics2D.IgnoreCollision(collider, playerCollider, ignoredCollision);
                if (Enemy == null || Enemy.Health == null || Enemy.Health.IsDead) return;
                if (body != null)
                {
                    body.constraints = constraints;
                    body.linearVelocity = velocity; body.angularVelocity = angularVelocity;
                }
                if (animator != null) animator.enabled = animatorEnabled;
                if (visual != null) visual.enabled = visualEnabled;
                Enemy.enabled = enemyEnabled;
            }
        }

        public void Initialize(GameManager owner, PlayerController controller)
        {
            game = owner; player = controller;
            Icon = game.Character.skillSprite != null ? game.Character.skillSprite : BuildSkillIcon();
            aura = game.Art.Renderer("SkillAura", transform, Icon, Color.clear, Vector2.one, 90);
            player.Health.Died += ClearActiveEffects;
        }

        private Sprite BuildSkillIcon()
        {
            string callsign = game.Character.name.Split('/')[0].Trim();
            Color accent = game.Character.accent;
            return game.Art.Paint("SkillGlyph_" + callsign, 128, (x, y) =>
            {
                float radius = Mathf.Sqrt(x * x + y * y);
                float angle = Mathf.Atan2(y, x);
                if (callsign == "LUMEN")
                {
                    bool halo = radius > 0.3f && radius < 0.42f;
                    bool cross = Mathf.Abs(x) < 0.075f && Mathf.Abs(y) < 0.32f
                        || Mathf.Abs(y) < 0.075f && Mathf.Abs(x) < 0.32f;
                    return halo || cross ? Color.Lerp(accent, Color.white, 0.35f) : Color.clear;
                }
                if (callsign == "NOVA")
                {
                    float spike = Mathf.Abs(Mathf.Sin(angle * 4));
                    bool star = radius < 0.13f || radius > 0.2f && radius < 0.3f + spike * 0.16f;
                    return star ? Color.Lerp(accent, Color.white, 0.22f) : Color.clear;
                }
                if (callsign == "AURORA")
                {
                    bool ring = radius > 0.28f && radius < 0.36f;
                    bool sight = Mathf.Abs(x) < 0.04f && Mathf.Abs(y) < 0.43f
                        || Mathf.Abs(y) < 0.04f && Mathf.Abs(x) < 0.43f;
                    return ring || sight ? accent : Color.clear;
                }
                bool flame = Mathf.Abs(x) < 0.32f && y > -0.4f && y < 0.38f
                    && Mathf.Abs(x) < Mathf.Max(0.035f, (0.38f - y) * 0.7f + Mathf.Sin(y * 23) * 0.035f);
                return flame ? Color.Lerp(accent, Color.white, 0.2f) : Color.clear;
            });
        }

        public static float CooldownFor(int character) => character == 1 ? 16 : character == 2 ? 12 : character == 4 ? 15 : character == 5 ? 18 : character == 6 ? 20 : character == 7 ? 17 : 14;

        public static bool Mastery(int character, int weapon)
        {
            switch (character)
            {
                case 0: return weapon == 0 || weapon == 3;
                case 1: return weapon == 1 || weapon == 5;
                case 2: return weapon == 2 || weapon == 4;
                case 3: return weapon == 3 || weapon == 5;
                case 4: return weapon == 2 || weapon == 4;
                case 5: return weapon == 1 || weapon == 5;
                case 6: return weapon == 0 || weapon == 1;
                case 7: return weapon == 3 || weapon == 5;
                case 8: return weapon == 2 || weapon == 4;
                case 9: return weapon == 3 || weapon == 6;
                case 10: return weapon == 1 || weapon == 7;
                case 11: return weapon == 5 || weapon == 6;
                default: return false;
            }
        }

        public bool TryActivate()
        {
            if (!isActiveAndEnabled || game == null || player == null || player.Motor == null || game.Player != player
                || !game.ControlsReady || Time.timeScale <= 0 || player.Health.IsDead || Remaining > 0) return false;
            int character = game.CharacterId;
            LastHealing = LastTeleportDistance = 0;
            LastPiercedTargets = EmberImpacts = 0;
            Vector2 destination = transform.position;
            float blinkRange = character == 2 ? 5.5f : character == 9 ? 4.2f : character == 11 ? 4.8f : 0;
            if (blinkRange > 0 && !player.Motor.TryBlinkDestination(player.Motor.Aim, blinkRange, out destination))
            {
                game.Hud.Announce("跃迁路径受阻 / 调整瞄准方向", 1.1f);
                return false;
            }
            readyAt = Time.time + CooldownFor(character);
            activeUntil = Time.time + (character == 2 ? 4 : character == 3 ? 0.5f : character == 4 ? 3 : character == 5 ? 6 : 5);
            Activations++;
            ApplyDefense();
            Vector2 origin = transform.position;
            if (character == 2)
            {
                game.Effects.CharacterCast(origin, 2.5f);
                game.Effects.Ghost(origin);
                LastTeleportDistance = Vector2.Distance(origin, destination);
                player.Motor.TeleportTo(destination);
                game.Effects.CharacterCast(destination, 2.5f);
            }
            game.Effects.SkillWave(transform.position, game.Character.accent, character == 1 ? 4 : 2);
            game.Hud.Announce(game.Character.SkillName + " / ACTIVE", 1.2f);
            game.Audio.Play(SoundKind.Upgrade);
            if (character == 1)
            {
                float before = player.Health.Current;
                player.Health.Restore(player.Health.Maximum * 0.25f);
                LastHealing = player.Health.Current - before;
                game.Pulse(transform.position, 4.5f, 35);
            }
            if (character != 2) game.Effects.CharacterCast(transform.position, character == 1 ? 3 : 2.4f);
            if (character == 2)
                foreach (EnemyAI enemy in game.Enemies)
                    if (enemy != null && enemy.Kind != EnemyKind.Boss && Vector2.Distance(transform.position, enemy.transform.position) < 5)
                        enemy.Stun(2);
            if (character == 3)
                for (int index = 0; index < 3; index++)
                {
                    Vector2 heading = Quaternion.Euler(0, 0, (index - 1) * 14) * player.Motor.Aim;
                    game.Projectiles.Fire(player.Motor.MuzzleOrigin + heading * 0.2f, heading, 16, 40 * game.Stats.DamageMultiplier,
                        Team.Player, 1, game.Character.accent, false, 0, 2.4f);
                }
            if (character == 4) FireAurora();
            if (character == 5) FireEmber();
            if (character == 6)
            {
                float healthBefore = player.Health.Current;
                player.Health.Restore(player.Health.Maximum * 0.22f);
                LastHealing = player.Health.Current - healthBefore;
                nextHealTick = Time.time + 1;
                game.Effects.SkillWave(origin, game.Character.accent, 5.5f);
            }
            if (character == 7)
            {
                game.Pulse(origin, 6, 46 * game.Stats.DamageMultiplier);
                game.Effects.SkillWave(origin, game.Character.accent, 6);
            }
            if (character == 8)
            {
                activeUntil = Time.time + 4.5f;
                Vector2 heading = player.Motor.Aim.normalized;
                game.Effects.WarningLine(origin + heading * 10, heading, 20, 0.22f);
                for (int index = 0; index < 3; index++)
                    game.Projectiles.Fire(player.Motor.MuzzleOrigin + heading * 0.2f, heading, 50, 52 * game.CombatDamageMultiplier,
                        Team.Player, 5, game.Character.accent, true);
                game.Hud.Announce("KALT // 零下裁决", 1.6f);
            }
            if (character == 9)
            {
                activeUntil = Time.time + 2.4f;
                game.Effects.Ghost(origin);
                Vector2 veilDestination = destination;
                LastTeleportDistance = Vector2.Distance(origin, destination);
                player.Motor.TeleportTo(veilDestination);
                game.Effects.Explosion(veilDestination, 2.4f, game.Character.accent);
                foreach (EnemyAI enemy in game.Enemies)
                    if (enemy != null && enemy.Kind != EnemyKind.Boss && Vector2.Distance(veilDestination, enemy.transform.position) < 3)
                        enemy.Stun(1.4f);
                game.Hud.Announce("VEIL // 无声猎线", 1.6f);
            }
            if (character == 10)
            {
                float before = player.Health.Current;
                player.Health.Restore(player.Health.Maximum * 0.32f);
                LastHealing = player.Health.Current - before;
                nextHealTick = Time.time + 1;
                activeUntil = Time.time + 7;
                game.Effects.SkillWave(origin, game.Character.accent, 6.5f);
                game.Hud.Announce("SERA // 晨辉救援", 1.6f);
            }
            if (character == 11)
            {
                Vector2 rookDestination = destination;
                LastTeleportDistance = Vector2.Distance(origin, destination);
                game.Effects.Ghost(origin);
                player.Motor.TeleportTo(rookDestination);
                game.Pulse(rookDestination, 3.8f, 66 * game.Stats.DamageMultiplier);
                game.Effects.Explosion(rookDestination, 3.4f, game.Character.accent);
                game.Hud.Announce("ROOK // 破阵冲锋", 1.6f);
            }
            if (LastHealing > 0) game.Effects.RewardItem(transform.position + Vector3.up, 6);
            if (game.IsPlaying && game.Stats.PulseRank > 0) game.Pulse(transform.position, 4.6f, 35 + game.Stats.PulseRank * 20);
            return true;
        }

        private float TraceDistance(Vector2 origin, Vector2 heading, float distance, float radius = 0)
        {
            var filter = new ContactFilter2D { useTriggers = false };
            skillHitCount = radius > 0 ? Physics2D.CircleCast(origin, radius, heading, filter, skillHits, distance)
                : Physics2D.Raycast(origin, heading, filter, skillHits, distance);
            if (skillHitCount == skillHits.Length) return 0;
            for (int index = 0; index < skillHitCount; index++)
            {
                RaycastHit2D hit = skillHits[index];
                if (hit.collider != null && hit.collider.GetComponentInParent<Health>() == null)
                    distance = Mathf.Min(distance, hit.distance);
            }
            return distance;
        }

        private bool HasLineOfSight(Vector2 origin, Vector2 destination)
        {
            Vector2 offset = destination - origin;
            float distance = offset.magnitude;
            return distance < 0.05f || TraceDistance(origin, offset / distance, distance) >= distance - 0.05f;
        }

        private void FireAurora()
        {
            Physics2D.SyncTransforms();
            beamOrigin = player.Motor.MuzzleOrigin;
            beamDirection = player.Motor.Aim.normalized;
            beamLength = TraceDistance(beamOrigin, beamDirection, 24, 0.08f);
            beamUntil = Time.time + 0.22f;
            piercedTargets.Clear();
            float damage = 120 * game.Stats.DamageMultiplier * game.CombatDamageMultiplier;
            for (int index = 0; index < skillHitCount && game.IsPlaying; index++)
            {
                RaycastHit2D hit = skillHits[index];
                if (hit.collider == null || hit.distance > beamLength + 0.001f) continue;
                Health target = hit.collider.GetComponentInParent<Health>();
                if (target == null || target.Team != Team.Enemy || target.IsDead || !piercedTargets.Add(target)) continue;
                if (!target.TakeDamage(damage, Team.Player)) continue;
                LastPiercedTargets++;
                game.Effects.DamageNumber(hit.point, target.LastDamageTaken, true);
                game.Effects.Burst(hit.point, game.Character.accent, 5);
                EnemyAI enemy = target.GetComponentInParent<EnemyAI>();
                if (enemy == null || enemy.Kind == EnemyKind.Boss || target.IsDead) continue;
                frozenEnemies.Add(new FrozenEnemy(enemy, player.GetComponent<CapsuleCollider2D>(), 3));
                game.Effects.Telegraph(enemy.transform.position, 1.05f, 3, game.Character.accent);
            }
            if (sniperBeam == null)
                sniperBeam = game.Art.Renderer("AuroraPiercingBeam", transform, game.Art.Square, game.Character.accent, Vector2.one, 130);
            UpdateBeam();
            game.Effects.CharacterCast(beamOrigin + beamDirection * beamLength, 1.6f);
            game.Audio.Play(SoundKind.Sniper);
            game.CameraRig.AddRecoil(0.16f);
        }

        private void UpdateBeam()
        {
            if (sniperBeam == null) return;
            sniperBeam.enabled = game.IsPlaying && Time.timeScale > 0 && Time.time < beamUntil && beamLength > 0;
            if (!sniperBeam.enabled) return;
            sniperBeam.transform.position = beamOrigin + beamDirection * (beamLength * 0.5f);
            sniperBeam.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(beamDirection.y, beamDirection.x) * Mathf.Rad2Deg);
            sniperBeam.transform.localScale = new Vector3(beamLength / sniperBeam.sprite.bounds.size.x, 0.075f / sniperBeam.sprite.bounds.size.y, 1);
            Color tint = game.Character.accent;
            tint.a = Mathf.Clamp01((beamUntil - Time.time) / 0.22f);
            sniperBeam.color = tint;
        }

        private void RestoreFrozenEnemies(bool all)
        {
            for (int index = frozenEnemies.Count - 1; index >= 0; index--)
            {
                FrozenEnemy frozen = frozenEnemies[index];
                if (!all && frozen.Enemy != null && !frozen.Enemy.Health.IsDead && Time.time < frozen.Until) continue;
                frozen.Restore();
                frozenEnemies.RemoveAt(index);
            }
        }

        private EnemyAI FindEmberTarget(Vector2 origin, Vector2 heading, float range, bool forwardOnly)
        {
            EnemyAI nearest = null;
            float bestScore = float.PositiveInfinity;
            foreach (EnemyAI enemy in game.Enemies)
            {
                if (enemy == null || enemy.Health.IsDead || !enemy.gameObject.activeInHierarchy || emberTargets.Contains(enemy)) continue;
                Vector2 offset = (Vector2)enemy.transform.position - origin;
                float distance = offset.magnitude;
                float alignment = distance > 0.01f ? Vector2.Dot(offset / distance, heading) : 1;
                if (distance > range || forwardOnly && alignment < 0.65f) continue;
                float score = distance + (forwardOnly ? (1 - alignment) * range : 0);
                if (score >= bestScore || !HasLineOfSight(origin, enemy.transform.position)) continue;
                bestScore = score; nearest = enemy;
            }
            return nearest;
        }

        private void FireEmber()
        {
            Physics2D.SyncTransforms();
            emberTargets.Clear();
            emberBounces = 3;
            Vector2 origin = player.Motor.MuzzleOrigin;
            Vector2 heading = player.Motor.Aim.normalized;
            EnemyAI target = FindEmberTarget(origin, heading, 10, true);
            Vector2 fallback = origin + heading * Mathf.Max(0, TraceDistance(origin, heading, 8) - 0.12f);
            if (emberCharge == null)
            {
                Sprite sprite = game.Character.skillSprite != null ? game.Character.skillSprite : game.Art.Glow;
                emberCharge = game.Art.Renderer("EmberBouncingCharge", transform, sprite,
                    game.Character.skillSprite != null ? Color.white : game.Character.accent, Vector2.one, 131);
                emberCharge.transform.localScale = Vector3.one * (0.7f / sprite.bounds.size.x);
            }
            BeginEmberFlight(origin, target, fallback);
        }

        private void BeginEmberFlight(Vector2 origin, EnemyAI target, Vector2 fallback)
        {
            emberOrigin = emberPosition = origin;
            emberTarget = target;
            emberDestination = target != null ? (Vector2)target.transform.position : fallback;
            emberFlight = 0;
            emberFlightDuration = Mathf.Clamp(Vector2.Distance(origin, emberDestination) / 18, 0.16f, 0.55f);
            emberFlying = true;
            emberCharge.enabled = true;
            emberCharge.transform.position = origin;
        }

        private void UpdateEmber()
        {
            if (!emberFlying) return;
            if (emberTarget != null && !emberTarget.Health.IsDead && emberTarget.gameObject.activeInHierarchy)
                emberDestination = emberTarget.transform.position;
            emberFlight += Time.deltaTime;
            float progress = Mathf.Clamp01(emberFlight / emberFlightDuration);
            Vector2 next = Vector2.Lerp(emberOrigin, emberDestination, progress)
                + Vector2.up * (Mathf.Sin(progress * Mathf.PI) * Mathf.Min(1.2f, Vector2.Distance(emberOrigin, emberDestination) * 0.2f));
            Vector2 step = next - emberPosition;
            float distance = step.magnitude;
            float clearDistance = distance > 0.001f ? TraceDistance(emberPosition, step / distance, distance, 0.1f) : distance;
            bool blocked = clearDistance < distance - 0.001f;
            emberPosition = blocked ? emberPosition + step.normalized * Mathf.Max(0, clearDistance - 0.1f) : next;
            emberCharge.transform.position = emberPosition;
            emberCharge.transform.Rotate(0, 0, Time.deltaTime * 540);
            if (!blocked && progress < 1) return;
            if (emberTarget != null) emberTargets.Add(emberTarget);
            EmberImpacts++;
            emberBounces--;
            AddBurningZone(emberPosition);
            game.Effects.Explosion(emberPosition, 2.1f, game.Character.accent);
            DamageEmberArea(emberPosition, 24);
            if (!game.IsPlaying) { ClearActiveEffects(); return; }
            EnemyAI nextTarget = emberBounces > 0 ? FindEmberTarget(emberPosition, Vector2.zero, 7, false) : null;
            if (nextTarget != null) BeginEmberFlight(emberPosition, nextTarget, emberPosition);
            else { emberFlying = false; emberTarget = null; emberCharge.enabled = false; }
        }

        private void AddBurningZone(Vector2 center)
        {
            int slot = Mathf.Clamp(EmberImpacts - 1, 0, burningZones.Length - 1);
            BurningZone zone = burningZones[slot];
            if (zone == null)
            {
                Sprite sprite = game.Character.skillSprite != null ? game.Character.skillSprite : game.Art.Disc;
                zone = new BurningZone { Visual = game.Art.Renderer("EmberFireZone_" + slot, transform, sprite, Color.white, Vector2.one, 89) };
                burningZones[slot] = zone;
            }
            zone.Center = center; zone.Until = Time.time + 4; zone.NextTick = Time.time + 0.5f;
            activeUntil = Mathf.Max(activeUntil, zone.Until);
            zone.Visual.enabled = true;
            zone.Visual.transform.position = center;
        }

        private void DamageEmberArea(Vector2 center, float baseDamage)
        {
            float damage = baseDamage * game.Stats.DamageMultiplier * game.CombatDamageMultiplier;
            for (int index = game.Enemies.Count - 1; index >= 0 && game.IsPlaying; index--)
            {
                EnemyAI enemy = game.Enemies[index];
                if (enemy == null || enemy.Health.IsDead || !enemy.gameObject.activeInHierarchy) continue;
                Collider2D collider = enemy.GetComponent<CapsuleCollider2D>();
                Vector2 closest = collider != null ? collider.ClosestPoint(center) : (Vector2)enemy.transform.position;
                if (Vector2.Distance(center, closest) > 2.1f || !HasLineOfSight(center, closest)) continue;
                if (enemy.Health.TakeDamage(damage, Team.Player)) game.Effects.DamageNumber(enemy.transform.position, enemy.Health.LastDamageTaken, false);
            }
        }

        private void UpdateBurningZones()
        {
            foreach (BurningZone zone in burningZones)
            {
                if (zone == null) continue;
                while (zone.NextTick > 0 && zone.NextTick <= zone.Until && Time.time >= zone.NextTick && game.IsPlaying)
                {
                    zone.NextTick += 0.5f;
                    DamageEmberArea(zone.Center, 8);
                }
                zone.Visual.enabled = game.IsPlaying && Time.time < zone.Until;
                if (!zone.Visual.enabled) continue;
                zone.Visual.transform.position = zone.Center;
                float width = 4.2f * (1 + Mathf.Sin(Time.time * 10) * 0.04f);
                zone.Visual.transform.localScale = Vector3.one * (width / zone.Visual.sprite.bounds.size.x);
                Color tint = game.Character.skillSprite != null ? Color.white : game.Character.accent;
                tint.a = 0.25f * Mathf.Clamp01((zone.Until - Time.time) * 2);
                zone.Visual.color = tint;
            }
        }

        private void ApplyDefense()
        {
            float skillReduction = game.CharacterId == 1 ? Mathf.Max(0.12f, Active ? 0.7f : 0)
                : Active && game.CharacterId == 2 ? 0.4f
                : Active && game.CharacterId == 6 ? 0.35f
                : Active && game.CharacterId == 10 ? 0.2f : 0;
            float techniqueReduction = player.Techniques != null ? player.Techniques.DefenseBonus : 0;
            float ultimateReduction = player.Energy != null ? player.Energy.DefenseBonus : 0;
            player.Health.DamageReduction = Mathf.Max(skillReduction, techniqueReduction, ultimateReduction);
        }

        public void ResetForCheckpoint()
        {
            ClearActiveEffects();
            readyAt = 0;
            nextHealTick = 0;
            LastHealing = LastTeleportDistance = 0;
            LastPiercedTargets = EmberImpacts = 0;
        }

        private void ClearActiveEffects()
        {
            RestoreFrozenEnemies(true);
            activeUntil = nextTrail = beamUntil = 0;
            emberFlying = false; emberTarget = null; emberBounces = 0;
            emberTargets.Clear(); piercedTargets.Clear();
            foreach (BurningZone zone in burningZones)
            {
                if (zone == null) continue;
                zone.Until = zone.NextTick = 0;
                if (zone.Visual != null) zone.Visual.enabled = false;
            }
            if (aura != null) aura.enabled = false;
            if (sniperBeam != null) sniperBeam.enabled = false;
            if (emberCharge != null) emberCharge.enabled = false;
            if (player != null && player.Health != null) player.Health.DamageReduction = 0;
        }

        private void Update()
        {
            if (game == null || player == null) return;
            if (player.Health.IsDead || game.Player != player || game.State == RunState.Menu || game.State == RunState.Won || game.State == RunState.Lost)
            { ClearActiveEffects(); return; }
            bool simulate = game.IsPlaying && Time.timeScale > 0;
            UpdateBeam();
            if (emberCharge != null) emberCharge.enabled = simulate && emberFlying;
            foreach (BurningZone zone in burningZones)
                if (zone != null && zone.Visual != null) zone.Visual.enabled = simulate && Time.time < zone.Until;
            aura.enabled = simulate && Active && game.CameraRig.Side.Mode != ViewMode.IronSight;
            if (!simulate) return;
            RestoreFrozenEnemies(false);
            UpdateEmber();
            UpdateBurningZones();
            if ((game.CharacterId == 6 || game.CharacterId == 10) && Active && Time.time >= nextHealTick)
            {
                player.Health.Restore(player.Health.Maximum * (game.CharacterId == 10 ? 0.06f : 0.04f));
                nextHealTick = Time.time + 1;
                game.Effects.SkillWave(transform.position, game.Character.accent, 1.6f);
            }
            ApplyDefense();
            if (!aura.enabled) return;
            Color tint = game.Character.skillSprite != null ? Color.white : game.Character.accent;
            tint.a = game.CharacterId == 1 ? 0.48f : 0.2f;
            aura.color = tint;
            float auraSize = game.CharacterId == 2 ? 2.5f : game.CharacterId == 6 ? 5.5f : 2.3f;
            aura.transform.localScale = Vector3.one * (auraSize / aura.sprite.bounds.size.x);
            if (game.CharacterId == 6)
                aura.color = new Color(game.Character.accent.r, game.Character.accent.g, game.Character.accent.b, 0.3f + Mathf.Sin(Time.time * 5) * 0.12f);
            if (game.CharacterId == 2 && Time.time >= nextTrail)
            { nextTrail = Time.time + 0.09f; game.Effects.Ghost(transform.position); }
        }

        private void OnDisable() { ResetForCheckpoint(); }

        private void OnDestroy()
        {
            ClearActiveEffects();
            if (player != null && player.Health != null) player.Health.Died -= ClearActiveEffects;
        }
    }
}
