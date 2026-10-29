using System;

namespace MoriEchoes
{
    public enum CombatPowerupKind { None, Spread, Rapid, Pierce, Blast }

    public readonly struct CombatPowerupProfile
    {
        public CombatPowerupKind Kind { get; }
        public string Code { get; }
        public string Name { get; }
        public int WeaponIndex { get; }
        public int FallbackWeaponIndex { get; }
        public float Duration { get; }
        public float DamageMultiplier { get; }
        public float FireRateMultiplier { get; }
        public int Pellets { get; }
        public float SpreadDegrees { get; }
        public int ExtraPierce { get; }
        public float SpeedMultiplier { get; }
        public float ExplosionRadius { get; }

        internal CombatPowerupProfile(CombatPowerupKind kind, string code, string name, int weaponIndex,
            int fallbackWeaponIndex, float duration, float damage, float fireRate, int pellets,
            float spread, int extraPierce, float speed, float explosionRadius)
        {
            Kind = kind; Code = code; Name = name; WeaponIndex = weaponIndex;
            FallbackWeaponIndex = fallbackWeaponIndex; Duration = duration;
            DamageMultiplier = damage; FireRateMultiplier = fireRate; Pellets = pellets;
            SpreadDegrees = spread; ExtraPierce = extraPierce;
            SpeedMultiplier = speed; ExplosionRadius = explosionRadius;
        }

        public int ResolveWeaponIndex(int weaponCount)
        {
            if (weaponCount <= 0) return -1;
            return WeaponIndex < weaponCount ? WeaponIndex : Math.Min(FallbackWeaponIndex, weaponCount - 1);
        }
    }

    public static class CombatPowerupRules
    {
        public static bool IsDefined(CombatPowerupKind kind)
            => kind >= CombatPowerupKind.Spread && kind <= CombatPowerupKind.Blast;

        public static CombatPowerupProfile Profile(CombatPowerupKind kind)
        {
            switch (kind)
            {
                case CombatPowerupKind.Spread:
                    return new CombatPowerupProfile(kind, "S", "S / 散弹爆发", 1, 1, 12, 1.08f, 1.04f, 7, 32, 0, 1.1f, 0);
                case CombatPowerupKind.Rapid:
                    return new CombatPowerupProfile(kind, "R", "R / 双路速射", 3, 0, 10, 1.08f, 1.08f, 2, 6, 0, 1.1f, 0);
                case CombatPowerupKind.Pierce:
                    return new CombatPowerupProfile(kind, "L", "L / 贯穿光束", 4, 2, 12, 1.1f, 1.04f, 1, 0, 1, 1.15f, 0);
                case CombatPowerupKind.Blast:
                    return new CombatPowerupProfile(kind, "B", "B / 三联爆破", 5, 0, 10, 1.08f, 1.04f, 3, 14, 0, 1, 2.5f);
                default: return default;
            }
        }

        public static float PelletAngle(int pellet, int count, float spread)
            => count <= 1 ? 0 : (pellet / (float)(count - 1) - 0.5f) * spread;
    }

    public sealed class CombatPowerupState
    {
        public CombatPowerupKind Kind { get; private set; }
        public float Remaining { get; private set; }
        public float Duration { get; private set; }
        public bool IsActive => Kind != CombatPowerupKind.None && Remaining > 0;

        public bool Activate(CombatPowerupKind kind, float duration = 0)
        {
            if (!CombatPowerupRules.IsDefined(kind) || float.IsNaN(duration) || float.IsInfinity(duration) || duration < 0) return false;
            Duration = duration > 0 ? duration : CombatPowerupRules.Profile(kind).Duration;
            Remaining = Duration; Kind = kind;
            return true;
        }

        public bool Tick(float seconds)
        {
            if (!IsActive || seconds <= 0 || float.IsNaN(seconds) || float.IsInfinity(seconds)) return false;
            Remaining = Math.Max(0, Remaining - seconds);
            if (Remaining > 0) return false;
            Clear();
            return true;
        }

        public bool Clear()
        {
            bool changed = Kind != CombatPowerupKind.None;
            Kind = CombatPowerupKind.None; Remaining = Duration = 0;
            return changed;
        }
    }

    public sealed class WeaponCapsuleDropCounter
    {
        public int KillsSinceDrop { get; private set; }

        public bool RegisterKill(float roll, float probability, int guarantee)
        {
            if (KillsSinceDrop < int.MaxValue) KillsSinceDrop++;
            bool guaranteed = guarantee > 0 && KillsSinceDrop >= guarantee;
            bool randomDrop = !float.IsNaN(roll) && roll >= 0 && roll < 1
                && !float.IsNaN(probability) && roll < Math.Max(0, Math.Min(1, probability));
            return guaranteed || randomDrop;
        }

        public void Reset() { KillsSinceDrop = 0; }
    }
}
