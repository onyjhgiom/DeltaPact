using System;
using System.Collections.Generic;
using MoriEchoes;

internal static class RunModelTests
{
    private static int assertions;

    private static void Check(bool condition, string name)
    {
        assertions++;
        if (!condition) throw new Exception("FAILED: " + name);
    }

    private static void Near(float actual, float expected, string name)
        => Check(Math.Abs(actual - expected) < 0.0001f, name);

    public static int Main()
    {
        try
        {
            var first = new RunStats();
            var second = new RunStats();
            Near(first.DamageMultiplier, 1, "fresh damage");
            Near(first.MovementMultiplier, 1, "fresh movement");
            Check(first.PulseRank == 0 && first.ExtraPierce == 0, "fresh abilities");
            foreach (UpgradeOption option in UpgradeCatalog.All) first.Apply(option.Kind);
            Near(first.DamageMultiplier, 1.2f, "damage upgrade");
            Near(first.FireRateMultiplier, 1.24f, "fire rate and focus");
            Near(first.MovementMultiplier, 1.08f, "focus movement");
            Near(first.DodgeCooldownMultiplier, 0.85f, "dodge upgrade");
            Near(first.MagazineMultiplier, 1.25f, "magazine upgrade");
            Near(first.SpreadMultiplier, 0.8f, "spread upgrade");
            Near(first.CriticalChance, 0.12f, "critical upgrade");
            Check(first.ExtraPierce == 1 && first.PulseRank == 1 && first.VitalityRank == 1, "pierce pulse vitality");
            foreach (UpgradeOption option in UpgradeCatalog.All) Check(first.Rank(option.Kind) == 1, "rank tracked");
            Near(second.DamageMultiplier, 1, "runs isolated");
            for (int index = 0; index < 100; index++)
            {
                first.Apply(UpgradeKind.Dodge); first.Apply(UpgradeKind.Precision);
            }
            Near(first.DodgeCooldownMultiplier, 0.25f, "dodge lower bound");
            Near(first.SpreadMultiplier, 0.15f, "spread lower bound");
            Near(first.CriticalChance, 0.6f, "critical upper bound");
            var random = new Random(4707);
            var seen = new HashSet<UpgradeKind>();
            for (int sample = 0; sample < 5000; sample++)
            {
                UpgradeOption[] options = UpgradeCatalog.DrawThree(random);
                Check(options.Length == 3, "three options");
                Check(options[0].Kind != options[1].Kind && options[0].Kind != options[2].Kind && options[1].Kind != options[2].Kind, "unique draw");
                foreach (UpgradeOption option in options) seen.Add(option.Kind);
            }
            Check(seen.Count == 9, "all upgrades reachable");
            var track = new ExperienceTrack();
            Check(track.Level == 1 && track.Required == 12, "initial level");
            Check(track.Add(11) == 0 && track.Current == 11, "subthreshold xp");
            Check(track.Add(1) == 1 && track.Level == 2 && track.Current == 0 && track.Required == 20, "exact threshold");
            Check(track.Add(48) == 2 && track.Level == 4 && track.Current == 0, "multiple levels from one pickup");
            Check(track.PendingChoices == 3, "pending choices queued");
            Check(track.Add(-10) == 0 && track.Current == 0, "negative xp ignored");
            Check(track.ConsumeChoice() && track.ConsumeChoice() && track.ConsumeChoice(), "consume queued upgrades");
            Check(!track.ConsumeChoice() && track.PendingChoices == 0, "cannot consume extra choice");
            Check(WaveRules.SelectEnemy(1, 0) == EnemyKind.Chaser, "early chaser");
            Check(WaveRules.SelectEnemy(2, 4) == EnemyKind.Gunner, "ranged unlock");
            Check(WaveRules.SelectEnemy(3, 3) == EnemyKind.Runner, "runner unlock");
            Check(WaveRules.SelectEnemy(4, 13) == EnemyKind.Elite, "elite unlock");
            Near(WaveRules.HealthMultiplier(1), 1, "wave one hp");
            Check(WaveRules.HealthMultiplier(8) > WaveRules.HealthMultiplier(2), "health scales");
            Check(WaveRules.SpawnInterval(1000) >= 0.32f, "spawn interval bounded");
            Console.WriteLine("PASS: " + assertions + " assertions; 5000 seeded three-choice draws; pure C# model only.");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception.ToString());
            return 1;
        }
    }
}
