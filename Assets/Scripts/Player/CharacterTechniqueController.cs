using UnityEngine;

namespace MoriEchoes
{
    public sealed class CharacterTechniqueController : MonoBehaviour
    {
        private static readonly string[,] Names =
        {
            { "棱镜点射", "标记爆破" },
            { "守护壁垒", "反击脉冲" },
            { "影袭折跃", "幽影环爆" },
            { "裂地三连", "重炮轰击" },
            { "霜棱束缚", "极光棱镜" },
            { "余烬喷涌", "熔火陷阱" },
            { "光幕愈合", "圣域回响" },
            { "星火连环", "坍缩弹" },
            { "零下长射", "极地穿心" },
            { "影缝闪袭", "暗面收割" },
            { "晨辉治疗场", "复苏脉冲" },
            { "震地冲阵", "连环跳雷" }
        };

        private readonly float[] readyAt = new float[2];
        private GameManager game;
        private PlayerController player;
        private float wardUntil;
        private float wardReduction;

        public float Remaining(int slot) => Mathf.Max(0, readyAt[Mathf.Clamp(slot, 0, 1)] - Time.time);
        public float DefenseBonus => wardUntil > Time.time ? wardReduction : 0;
        public string Name(int slot) => NameFor(game != null ? game.CharacterId : 0, slot);
        public static string NameFor(int character, int slot) => Names[Mathf.Clamp(character, 0, 11), Mathf.Clamp(slot, 0, 1)];
        public static float CooldownFor(int slot) => slot == 0 ? 11f : 17f;

        public void Initialize(GameManager owner, PlayerController controller)
        {
            game = owner;
            player = controller;
        }

        public bool TryActivate(int slot)
        {
            if (slot < 0 || slot > 1 || game == null || player == null || player.Motor == null
                || game.Player != player || !game.ControlsReady || !game.IsPlaying || player.Health.IsDead
                || Remaining(slot) > 0) return false;

            int character = game.CharacterId;
            Vector2 origin = player.transform.position;
            Vector2 aim = player.Motor.Aim.normalized;
            float blinkRange = character == 9 ? (slot == 0 ? 3.8f : 5.1f) : character == 2 && slot == 0 ? 3.4f : 0;
            if (blinkRange > 0 && !player.Motor.TryBlinkDestination(aim, blinkRange, out _))
            {
                game.Hud.Announce("跃迁路径受阻 / 调整瞄准方向", 1.1f);
                return false;
            }
            readyAt[slot] = Time.time + CooldownFor(slot);
            game.Audio.Play(SoundKind.Upgrade);
            game.Effects.CharacterCast(origin, slot == 0 ? 2.2f : 3f);
            game.Hud.Announce(game.Character.name.Split('/')[0].Trim() + " / " + Name(slot), 1.3f);

            if (slot == 0)
            {
                switch (character)
                {
                    case 0: FireFan(5, 8, 19, 31, 1, 2.8f); break;
                    case 1: Ward(4.5f, 0.72f); game.Effects.SkillWave(origin, game.Character.accent, 2.8f); break;
                    case 2: Blink(aim, 3.4f); Blast(origin, 3.4f, 38, 1.2f); break;
                    case 3: FireFan(3, 13, 15, 36, 1, 3.2f); break;
                    case 4: FreezeRing(origin, 4.5f, 1.8f); break;
                    case 5: FireFan(7, 7, 13, 22, 1, 2.5f); break;
                    case 6: Heal(0.14f); Ward(3.5f, 0.45f); break;
                    case 8: FireFan(1, 0, 46, 112, 4, 1.6f); break;
                    case 9: Blink(aim, 3.8f); FreezeRing(origin, 3.5f, 1.4f); break;
                    case 10: Heal(0.2f); Ward(4, 0.35f); break;
                    case 11: Blast(origin + aim * 3.2f, 3.6f, 86, 2.1f); break;
                    default: FireFan(9, 5, 17, 23, 1, 2.4f); break;
                }
            }
            else
            {
                switch (character)
                {
                    case 0: Blast(origin + aim * 3.7f, 3.2f, 92, 2.2f); break;
                    case 1: Blast(origin, 5.2f, 54, 1.8f); Heal(0.1f); break;
                    case 2: FreezeRing(origin, 5.8f, 2.4f); game.Effects.Ghost(origin); break;
                    case 3: Blast(origin + aim * 4.2f, 4.4f, 145, 3f); break;
                    case 4: FireFan(3, 3, 22, 70, 1, 4f); FreezeRing(origin + aim * 3f, 3.3f, 2.8f); break;
                    case 5: Blast(origin + aim * 3.4f, 4.8f, 118, 2.7f); break;
                    case 6: Heal(0.25f); Ward(5f, 0.55f); Blast(origin, 3.5f, 48, 1.8f); break;
                    case 8: FireFan(3, 1.8f, 48, 92, 4, 1.8f); break;
                    case 9: Blink(aim, 5.1f); FreezeRing(origin, 4.8f, 2.1f); break;
                    case 10: Heal(0.24f); Ward(5.5f, 0.55f); Blast(origin, 4.2f, 62, 2f); break;
                    case 11:
                        Blast(origin + aim * 4.2f, 4.4f, 112, 2.7f);
                        PlantMine(origin + aim * 2.4f, 1.1f, 2.6f, 68);
                        break;
                    default: Blast(origin + aim * 4.5f, 5.1f, 132, 3.2f); break;
                }
            }
            return true;
        }

        private void Update()
        {
            if (wardUntil > 0 && Time.time >= wardUntil) wardUntil = 0;
        }

        private void Ward(float seconds, float reduction)
        {
            wardUntil = Time.time + seconds;
            wardReduction = Mathf.Max(wardReduction, reduction);
            game.Effects.SkillWave(player.transform.position, game.Character.accent, 2.7f);
        }

        private void Heal(float fraction)
        {
            player.Health.Restore(player.Health.Maximum * fraction);
            game.Effects.SkillWave(player.transform.position, game.Character.accent, 2.4f);
        }

        private void Blink(Vector2 aim, float distance)
        {
            Vector2 origin = player.transform.position;
            game.Effects.Ghost(origin);
            if (!player.Motor.TryBlinkDestination(aim, distance, out Vector2 destination)) return;
            player.Motor.TeleportTo(destination);
            game.Effects.CharacterCast(player.transform.position, 2.4f);
        }

        private void FireFan(int count, float spread, float speed, float damage, int targets, float blast)
        {
            Vector2 aim = player.Motor.Aim.normalized;
            Vector2 origin = player.Motor.MuzzleOrigin;
            for (int index = 0; index < count; index++)
            {
                float angle = count == 1 ? 0 : (index - (count - 1) * 0.5f) * spread;
                Vector2 direction = Quaternion.Euler(0, 0, angle) * aim;
                game.Projectiles.Fire(origin + direction * 0.22f, direction, speed,
                    damage * game.Stats.DamageMultiplier * game.CombatDamageMultiplier, Team.Player, targets, game.Character.accent, false, 0, blast);
            }
            game.Effects.Muzzle(origin, aim, game.Character.accent);
        }

        private void Blast(Vector2 center, float radius, float damage, float styleScale)
        {
            game.AwardStyle(Mathf.RoundToInt(100 * styleScale));
            game.Effects.Explosion(center, radius, game.Character.accent);
            foreach (EnemyAI enemy in game.Enemies.ToArray())
            {
                if (enemy == null || enemy.Health == null || enemy.Health.IsDead) continue;
                float distance = Vector2.Distance(center, enemy.transform.position);
                if (distance <= radius)
                    enemy.Health.TakeDamage(damage * Mathf.Lerp(1, 0.45f, distance / radius)
                        * game.Stats.DamageMultiplier * game.CombatDamageMultiplier, Team.Player);
            }
        }

        private void FreezeRing(Vector2 center, float radius, float seconds)
        {
            game.Effects.SkillWave(center, game.Character.accent, radius);
            foreach (EnemyAI enemy in game.Enemies)
                if (enemy != null && enemy.Kind != EnemyKind.Boss && !enemy.Health.IsDead
                    && Vector2.Distance(center, enemy.transform.position) <= radius)
                    enemy.Stun(seconds);
        }

        private void PlantMine(Vector2 position, float fuse, float radius, float damage)
        {
            GameObject mine = new GameObject("ROOK_TechniqueMine");
            mine.transform.position = position;
            mine.AddComponent<DelayedBlastMine>().Initialize(game, fuse, radius, damage * game.CombatDamageMultiplier, game.Character.accent);
        }
    }
}
