using UnityEngine;

namespace MoriEchoes
{
    public sealed class PlayerEnergySystem : MonoBehaviour
    {
        public const float Maximum = 100f;
        public float Current { get; private set; }
        public float Fraction => Current / Maximum;
        public bool Ready => Current >= Maximum;
        public float DefenseBonus => shieldUntil > Time.time ? shieldReduction : 0;
        public float FireRateMultiplier => Time.time < fireBoostUntil ? 1.7f : 1f;

        private GameManager game;
        private PlayerController player;
        private float shieldUntil;
        private float fireBoostUntil;
        private float shieldReduction;

        public void Initialize(GameManager owner, PlayerController controller)
        {
            game = owner;
            player = controller;
            player.Health.Damaged += OnPlayerDamaged;
        }

        public void Add(float amount)
        {
            if (game == null || !game.IsPlaying || amount <= 0) return;
            float medicBonus = game.CharacterId == 10 ? 1.25f : 1f;
            Current = Mathf.Min(Maximum, Current + amount * game.StyleMultiplier * medicBonus);
        }

        private void OnPlayerDamaged(float amount) => Add(amount * 0.75f);

        public bool TryActivateUltimate()
        {
            if (game == null || player == null || game.Player != player || !game.ControlsReady
                || !Ready || player.Health.IsDead || !game.IsPlaying) return false;
            float blinkRange = game.CharacterId == 2 ? 3.6f : game.CharacterId == 9 ? 5.5f : game.CharacterId == 11 ? 6.2f : 0;
            Vector2 destination = player.transform.position;
            if (blinkRange > 0 && !player.Motor.TryBlinkDestination(player.Motor.Aim, blinkRange, out destination))
            {
                game.Hud.Announce("跃迁路径受阻 / 调整瞄准方向", 1.1f);
                return false;
            }
            Current = 0;
            Vector2 origin = player.transform.position;
            Color color = game.Character.accent;
            float screenRadius = game.Level != null ? Mathf.Max(12, game.CameraRig.Camera.orthographicSize * game.CameraRig.Camera.aspect * 1.5f) : 10;
            game.AwardStyle(3500);
            game.Audio.Play(SoundKind.Blast);
            game.Effects.CharacterCast(origin, 7, true);
            switch (game.CharacterId)
            {
                case 0:
                    game.Hud.Announce("ULTIMATE // 聚变脉冲", 2.4f);
                    game.Pulse(origin, screenRadius, 210 * game.Stats.DamageMultiplier);
                    game.Effects.Explosion(origin, screenRadius * 0.68f, color);
                    break;
                case 1:
                    game.Hud.Announce("ULTIMATE // 满血壁垒", 2.4f);
                    player.Health.Restore(player.Health.Maximum);
                    GrantShield(9, 0.9f);
                    game.Pulse(origin, 5.5f, 76);
                    break;
                case 2:
                    game.Hud.Announce("ULTIMATE // 幽影环爆", 2.4f);
                    game.Effects.Ghost(origin);
                    player.Motor.TeleportTo(destination);
                    game.Pulse(origin, 9.5f, 175 * game.Stats.DamageMultiplier);
                    game.Effects.SkillWave(player.transform.position, color, 9.5f);
                    break;
                case 3:
                    game.Hud.Announce("ULTIMATE // 巨型爆破", 2.4f);
                    game.Pulse(origin + player.Motor.Aim.normalized * 4f, 11, 460 * game.Stats.DamageMultiplier);
                    game.Effects.Explosion(origin + player.Motor.Aim.normalized * 4f, 11, color);
                    break;
                case 4:
                    game.Hud.Announce("ULTIMATE // 极光棱镜", 2.4f);
                    game.Pulse(origin, screenRadius, 165 * game.Stats.DamageMultiplier);
                    foreach (EnemyAI enemy in game.Enemies)
                        if (enemy != null && enemy.Kind != EnemyKind.Boss) enemy.Motor?.Stun(3.4f);
                    game.Effects.SkillWave(origin, color, screenRadius);
                    break;
                case 5:
                    game.Hud.Announce("ULTIMATE // 焰陨覆盖", 2.4f);
                    Vector2 aim = player.Motor.Aim.normalized;
                    for (int index = 0; index < 5; index++)
                    {
                        Vector2 impact = origin + aim * (3 + index * 1.8f) + Vector2.up * ((index % 2) * 1.7f);
                        game.Pulse(impact, 3.5f, 145 * game.Stats.DamageMultiplier);
                        game.Effects.Explosion(impact, 3.5f, color);
                    }
                    break;
                case 6:
                    game.Hud.Announce("ULTIMATE // 圣域重启", 2.4f);
                    player.Health.Restore(player.Health.Maximum);
                    GrantShield(10, 0.9f);
                    game.Pulse(origin, 7.5f, 112);
                    game.Effects.CharacterCast(origin, 8);
                    break;
                case 8:
                    game.Hud.Announce("ULTIMATE // 零下裁决", 2.4f);
                    Vector2 kaltAim = player.Motor.Aim.normalized;
                    for (int index = -2; index <= 2; index++)
                    {
                        Vector2 direction = Quaternion.Euler(0, 0, index * 2.2f) * kaltAim;
                        game.Projectiles.Fire(player.Motor.MuzzleOrigin, direction, 55, 175 * game.CombatDamageMultiplier,
                            Team.Player, 5, color, true, 0, 1.4f);
                    }
                    game.Effects.WarningLine(origin + kaltAim * 15, kaltAim, 30, 0.12f);
                    game.Effects.CharacterCast(origin + kaltAim * 18, 4.5f);
                    break;
                case 9:
                    game.Hud.Announce("ULTIMATE // 影缝无声处决", 2.4f);
                    game.Effects.Ghost(origin);
                    player.Motor.TeleportTo(destination);
                    player.Health.Invulnerable = true;
                    game.Pulse(origin, 11, 210);
                    game.Pulse(player.transform.position, 8, 155);
                    game.Effects.SkillWave(player.transform.position, color, 11);
                    StartCoroutine(ReleaseInvulnerability(2.4f));
                    break;
                case 10:
                    game.Hud.Announce("ULTIMATE // 晨辉全域复苏", 2.4f);
                    player.Health.Restore(player.Health.Maximum);
                    GrantShield(9, 0.9f);
                    game.Pulse(origin, 9, 148);
                    game.Effects.CharacterCast(origin, 10);
                    break;
                case 11:
                    game.Hud.Announce("ULTIMATE // 破阵天陨", 2.4f);
                    Vector2 rookTarget = destination;
                    game.Effects.Ghost(origin);
                    player.Motor.TeleportTo(rookTarget);
                    game.Effects.Explosion(rookTarget, 8.5f, color);
                    game.Pulse(rookTarget, 9.5f, 340);
                    game.CameraRig.Shake(1.1f);
                    break;
                default:
                    game.Hud.Announce("ULTIMATE // 新星坍缩", 2.4f);
                    game.Pulse(origin, screenRadius, 240 * game.Stats.DamageMultiplier);
                    game.Effects.Explosion(origin, screenRadius, color);
                    fireBoostUntil = Time.time + 8;
                    break;
            }
            game.CameraRig.Shake(0.85f);
            return true;
        }

        private System.Collections.IEnumerator ReleaseInvulnerability(float duration)
        {
            yield return new WaitForSeconds(duration);
            if (player != null && player.Health != null && !player.Health.IsDead) player.Health.Invulnerable = false;
        }

        private void GrantShield(float seconds, float reduction)
        {
            shieldUntil = Time.time + seconds;
            shieldReduction = Mathf.Max(shieldReduction, reduction);
        }

        private void Update()
        {
            if (player == null || player.Health == null) return;
            if (shieldUntil > 0 && Time.time >= shieldUntil)
            {
                shieldUntil = 0;
            }
        }

        private void OnDestroy()
        {
            if (player != null && player.Health != null) player.Health.Damaged -= OnPlayerDamaged;
        }
    }
}
