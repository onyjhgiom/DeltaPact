#if MORIECHOES_CAPSULE_RULES_CHECKS
using System;

namespace MoriEchoes
{
    internal static class CombatPowerupRulesChecks
    {
        private static int assertions;

        public static int Main()
        {
            try
            {
                CheckTimer();
                CheckProfilesAndFan();
                CheckDropCounter();
                Console.WriteLine("Capsule pure rules: " + assertions + " assertions passed; no Unity runtime loaded.");
                return 0;
            }
            catch (Exception failure)
            {
                Console.Error.WriteLine(failure.Message);
                return 1;
            }
        }

        private static void CheckTimer()
        {
            var state = new CombatPowerupState();
            Require(!state.IsActive && state.Kind == CombatPowerupKind.None, "New state is inactive.");
            Require(!state.Clear(), "Clearing inactive state is idempotent.");
            Require(!state.Activate(CombatPowerupKind.None), "None cannot activate.");
            Require(!state.Activate((CombatPowerupKind)99), "Unknown kind cannot activate.");
            Require(state.Activate(CombatPowerupKind.Spread), "Spread activates.");
            Near(state.Remaining, 12, "Spread has the default duration.");
            Require(!state.Tick(2), "Partial tick does not expire.");
            Near(state.Remaining, 10, "Partial tick subtracts time.");
            Require(!state.Tick(0), "Pause does not expire.");
            Require(!state.Tick(-2) && !state.Tick(float.NaN) && !state.Tick(float.PositiveInfinity), "Invalid ticks are ignored.");
            Near(state.Remaining, 10, "Pause and invalid ticks preserve time.");
            Require(!state.Activate(CombatPowerupKind.Rapid, float.NaN)
                && !state.Activate(CombatPowerupKind.Rapid, float.PositiveInfinity)
                && !state.Activate(CombatPowerupKind.Rapid, -1), "Invalid durations are rejected.");
            Require(state.Kind == CombatPowerupKind.Spread, "Invalid replacement preserves current form.");
            Near(state.Remaining, 10, "Invalid replacement preserves remaining time.");
            Require(state.Activate(CombatPowerupKind.Spread), "Same form refreshes.");
            Near(state.Remaining, 12, "Refresh resets without adding time.");
            Require(state.Activate(CombatPowerupKind.Rapid), "Different form replaces.");
            Require(state.Kind == CombatPowerupKind.Rapid, "Replacement changes kind.");
            Near(state.Remaining, 10, "Replacement uses its own duration.");
            Require(state.Activate(CombatPowerupKind.Pierce, 1.25f), "Duration override activates.");
            Near(state.Duration, 1.25f, "Duration override is recorded.");
            Require(state.Tick(1.25f), "Exact duration expires.");
            Require(!state.IsActive && state.Kind == CombatPowerupKind.None, "Expiration clears form.");
            Near(state.Remaining, 0, "Expiration clears remaining time.");
            Near(state.Duration, 0, "Expiration clears duration.");
            Require(!state.Tick(100), "Expiration notification happens only once.");
            state.Activate(CombatPowerupKind.Blast);
            Require(state.Tick(100) && !state.IsActive, "Large tick safely expires.");
            state.Activate(CombatPowerupKind.Spread);
            var other = new CombatPowerupState();
            other.Activate(CombatPowerupKind.Pierce, 4);
            other.Clear();
            Require(state.IsActive && !other.IsActive, "Instances do not share form state.");
            Require(state.Clear() && !state.Clear(), "Explicit clear notifies only once.");
        }

        private static void CheckProfilesAndFan()
        {
            foreach (CombatPowerupKind kind in new[] { CombatPowerupKind.Spread, CombatPowerupKind.Rapid, CombatPowerupKind.Pierce, CombatPowerupKind.Blast })
            {
                CombatPowerupProfile profile = CombatPowerupRules.Profile(kind);
                Require(profile.Kind == kind && !string.IsNullOrEmpty(profile.Code), "Every capsule has its own identity.");
                Require(profile.Duration > 0 && profile.DamageMultiplier > 1 && profile.FireRateMultiplier > 1, "Every form has finite offensive bonuses.");
                Require(profile.Pellets >= 1 && profile.SpeedMultiplier >= 1, "Every form can fire.");
                Require(profile.ResolveWeaponIndex(1) == 0 && profile.ResolveWeaponIndex(0) == -1, "Weapon lookup handles small inventories.");
                Require(profile.ResolveWeaponIndex(24) == profile.WeaponIndex, "Weapon lookup has no four-entry cap.");
                float angleSum = 0;
                for (int pellet = 0; pellet < profile.Pellets; pellet++) angleSum += CombatPowerupRules.PelletAngle(pellet, profile.Pellets, profile.SpreadDegrees);
                Near(angleSum, 0, "Every energy fan is symmetric.");
            }
            Require(CombatPowerupRules.Profile(CombatPowerupKind.Spread).Pellets == 7, "S provides a shotgun burst without a runaway DPS spike.");
            Require(CombatPowerupRules.Profile(CombatPowerupKind.Rapid).ResolveWeaponIndex(3) == 0, "R falls back to rifle.");
            Require(CombatPowerupRules.Profile(CombatPowerupKind.Pierce).ResolveWeaponIndex(3) == 2, "L falls back to precision rifle.");
            Require(CombatPowerupRules.Profile(CombatPowerupKind.Blast).ResolveWeaponIndex(3) == 0, "B falls back to rifle.");
            Near(CombatPowerupRules.PelletAngle(0, 7, 32), -16, "S fan left edge.");
            Near(CombatPowerupRules.PelletAngle(6, 7, 32), 16, "S fan right edge.");
            Near(CombatPowerupRules.PelletAngle(3, 7, 32), 0, "S has a forward center projectile.");
            Near(CombatPowerupRules.PelletAngle(0, 1, 40), 0, "Single projectile stays forward.");
        }

        private static void CheckDropCounter()
        {
            var counter = new WeaponCapsuleDropCounter();
            for (int kill = 1; kill < 6; kill++)
                Require(!counter.RegisterKill(0.99f, 0.18f, 6), "Guarantee does not trigger early.");
            Require(counter.RegisterKill(0.99f, 0.18f, 6), "Sixth kill guarantees a drop.");
            Require(counter.KillsSinceDrop == 6, "Decision alone does not consume guarantee.");
            Require(counter.RegisterKill(0.99f, 0.18f, 6), "Failed spawn or cooldown preserves guarantee for next kill.");
            counter.Reset();
            Require(counter.KillsSinceDrop == 0, "Successful drop or run clear resets counter.");
            Require(counter.RegisterKill(0.179f, 0.18f, 6), "Roll below chance succeeds.");
            counter.Reset();
            Require(!counter.RegisterKill(0.18f, 0.18f, 6), "Chance boundary is exclusive.");
            Require(!counter.RegisterKill(0, 0, 0), "Zero chance with disabled guarantee never drops.");
            Require(counter.RegisterKill(0.999f, 1, 0), "Full chance always drops for valid rolls.");
            counter.Reset();
            Require(!counter.RegisterKill(float.NaN, 1, 0) && !counter.RegisterKill(0, float.NaN, 0), "Invalid random inputs cannot cause a chance drop.");
            Require(counter.RegisterKill(float.NaN, 0, 3), "Guarantee is independent of random input.");
        }

        private static void Near(float actual, float expected, string message)
            => Require(Math.Abs(actual - expected) < 0.0001f, message);

        private static void Require(bool condition, string message)
        {
            assertions++;
            if (!condition) throw new InvalidOperationException("Assertion failed: " + message);
        }
    }
}
#endif
