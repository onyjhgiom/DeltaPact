using System;
using System.Collections.Generic;

namespace MoriEchoes
{
    public enum WeaponKind { AssaultRifle, Shotgun, PrecisionRifle }
    public enum EnemyKind { Chaser, Gunner, Runner, Elite, Boss }
    public enum UpgradeKind { FireRate, Pierce, Damage, Dodge, Magazine, Precision, Pulse, Vitality, Focus }
    public enum RunState { Menu, Playing, Upgrade, Paused, Won, Lost }

    public sealed class RunStats
    {
        public float DamageMultiplier { get; private set; } = 1f;
        public float FireRateMultiplier { get; private set; } = 1f;
        public float MovementMultiplier { get; private set; } = 1f;
        public float DodgeCooldownMultiplier { get; private set; } = 1f;
        public float SpreadMultiplier { get; private set; } = 1f;
        public float MagazineMultiplier { get; private set; } = 1f;
        public float CriticalChance { get; private set; } = 0.05f;
        public int ExtraPierce { get; private set; }
        public int PulseRank { get; private set; }
        public int VitalityRank { get; private set; }
        private readonly Dictionary<UpgradeKind, int> ranks = new Dictionary<UpgradeKind, int>();

        public int Rank(UpgradeKind kind)
        {
            int rank;
            return ranks.TryGetValue(kind, out rank) ? rank : 0;
        }

        public void Apply(UpgradeKind kind)
        {
            ranks[kind] = Rank(kind) + 1;
            switch (kind)
            {
                case UpgradeKind.FireRate: FireRateMultiplier += 0.16f; break;
                case UpgradeKind.Pierce: ExtraPierce++; break;
                case UpgradeKind.Damage: DamageMultiplier += 0.2f; break;
                case UpgradeKind.Dodge: DodgeCooldownMultiplier = Math.Max(0.25f, DodgeCooldownMultiplier * 0.85f); break;
                case UpgradeKind.Magazine: MagazineMultiplier += 0.25f; break;
                case UpgradeKind.Precision:
                    SpreadMultiplier = Math.Max(0.15f, SpreadMultiplier * 0.8f);
                    CriticalChance = Math.Min(0.6f, CriticalChance + 0.07f);
                    break;
                case UpgradeKind.Pulse: PulseRank++; break;
                case UpgradeKind.Vitality: VitalityRank++; break;
                case UpgradeKind.Focus: MovementMultiplier += 0.08f; FireRateMultiplier += 0.08f; break;
                default: throw new ArgumentOutOfRangeException(nameof(kind));
            }
        }
    }

    public sealed class UpgradeOption
    {
        public UpgradeKind Kind { get; }
        public string Title { get; }
        public string Description { get; }
        public UpgradeOption(UpgradeKind kind, string title, string description)
        {
            Kind = kind;
            Title = title;
            Description = description;
        }
    }

    public static class UpgradeCatalog
    {
        public static readonly UpgradeOption[] All =
        {
            new UpgradeOption(UpgradeKind.FireRate, "快速供弹", "射速 +16%"),
            new UpgradeOption(UpgradeKind.Pierce, "穿甲弹", "子弹额外穿透 1 个目标"),
            new UpgradeOption(UpgradeKind.Damage, "高能弹头", "武器伤害 +20%"),
            new UpgradeOption(UpgradeKind.Dodge, "战术翻滚", "闪避冷却缩短 15%（最低 25%）"),
            new UpgradeOption(UpgradeKind.Magazine, "扩容弹匣", "弹匣容量 +25%"),
            new UpgradeOption(UpgradeKind.Precision, "精准校准", "散布缩小 20%，暴击率 +7%"),
            new UpgradeOption(UpgradeKind.Pulse, "电磁脉冲", "解锁 Q：范围脉冲；重复选择提高伤害与范围"),
            new UpgradeOption(UpgradeKind.Vitality, "生命补给", "最大生命 +20，并回复 35 生命"),
            new UpgradeOption(UpgradeKind.Focus, "战斗专注", "移动速度 +8%，射速 +8%")
        };

        public static UpgradeOption[] DrawThree(Random random)
        {
            if (random == null) throw new ArgumentNullException(nameof(random));
            var indices = new int[All.Length];
            for (int index = 0; index < indices.Length; index++) indices[index] = index;
            var result = new UpgradeOption[3];
            for (int slot = 0; slot < result.Length; slot++)
            {
                int selected = random.Next(slot, indices.Length);
                int previous = indices[slot];
                indices[slot] = indices[selected];
                indices[selected] = previous;
                result[slot] = All[indices[slot]];
            }
            return result;
        }
    }

    public sealed class ExperienceTrack
    {
        public int Level { get; private set; } = 1;
        public int Current { get; private set; }
        public int Required => 12 + (Level - 1) * 8;
        public int PendingChoices { get; private set; }

        public int Add(int amount)
        {
            if (amount <= 0) return 0;
            Current += amount;
            int gained = 0;
            while (Current >= Required)
            {
                Current -= Required;
                Level++;
                PendingChoices++;
                gained++;
            }
            return gained;
        }

        public bool ConsumeChoice()
        {
            if (PendingChoices <= 0) return false;
            PendingChoices--;
            return true;
        }

        public void LoseUnspentHalf() { Current /= 2; }
    }

    public static class WaveRules
    {
        public static float SpawnInterval(int wave) => Math.Max(0.32f, 1.5f - wave * 0.1f);
        public static float HealthMultiplier(int wave) => 1f + Math.Max(0, wave - 1) * 0.1f;

        public static EnemyKind SelectEnemy(int wave, int spawnIndex)
        {
            if (wave >= 4 && spawnIndex % 13 == 0) return EnemyKind.Elite;
            if (wave >= 3 && spawnIndex % 3 == 0) return EnemyKind.Runner;
            if (wave >= 2 && spawnIndex % 4 == 0) return EnemyKind.Gunner;
            return EnemyKind.Chaser;
        }
    }
}
