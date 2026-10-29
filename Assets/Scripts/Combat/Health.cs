using System;
using UnityEngine;

namespace MoriEchoes
{
    public enum Team { Player, Enemy }
    public enum IncomingHitKind { Enemy, Hazard, BossSkill, BossUltimate }

    public sealed class Health : MonoBehaviour
    {
        public float Current { get; private set; }
        public float Maximum { get; private set; }
        public Team Team { get; private set; }
        public bool IsDead { get; private set; }
        public bool Invulnerable { get; set; }
        public float DamageReduction { get; set; }
        public float MaximumHitFraction { get; set; }
        public float LastDamageTaken { get; private set; }
        public event Action<float> Damaged;
        public event Action Died;
        private float nextPlayerHit;
        private GameConfig incomingConfig;

        public void ConfigurePlayerDamage(GameConfig config)
        {
            incomingConfig = config;
            MaximumHitFraction = config.playerMaximumHitFraction;
        }

        public void Initialize(Team team, float maximum)
        {
            Team = team;
            Maximum = Mathf.Max(1f, maximum);
            Current = Maximum;
            IsDead = false;
            Invulnerable = false;
            DamageReduction = 0;
            MaximumHitFraction = 1;
            LastDamageTaken = 0;
            nextPlayerHit = 0;
            incomingConfig = null;
        }

        public bool TakeDamage(float amount, Team source, bool bypassArmour = false, IncomingHitKind hitKind = IncomingHitKind.Enemy)
        {
            if (IsDead || Invulnerable || source == Team || amount <= 0) return false;
            if (Team == MoriEchoes.Team.Player && Time.time < nextPlayerHit) return false;
            if (Team == MoriEchoes.Team.Player)
                nextPlayerHit = Time.time + (incomingConfig != null ? incomingConfig.playerHitProtectionSeconds : 0.35f);
            if (!bypassArmour)
            {
                if (Team == MoriEchoes.Team.Player && incomingConfig != null)
                {
                    float limit = hitKind switch
                    {
                        IncomingHitKind.Hazard => incomingConfig.hazardHitLimit,
                        IncomingHitKind.BossSkill => incomingConfig.bossSkillHitLimit,
                        IncomingHitKind.BossUltimate => incomingConfig.bossUltimateHitLimit,
                        _ => incomingConfig.enemyHitLimit
                    };
                    // Cap BEFORE character shields so defence still reduces the capped hit.
                    amount = Mathf.Min(amount, incomingConfig.playerHealth * limit);
                }
                amount *= 1 - Mathf.Clamp(DamageReduction, 0, 0.9f);
                amount = Mathf.Min(amount, Maximum * Mathf.Clamp(MaximumHitFraction, 0.001f, 1));
            }
            LastDamageTaken = Mathf.Min(Current, amount);
            Current = Mathf.Max(0f, Current - amount);
            Damaged?.Invoke(amount);
            if (Current <= 0)
            {
                IsDead = true;
                Died?.Invoke();
            }
            return true;
        }

        public void Restore(float amount)
        {
            if (!IsDead) Current = Mathf.Min(Maximum, Current + Mathf.Max(0f, amount));
        }

        public void SetCurrent(float amount)
        {
            if (!IsDead) Current = Mathf.Clamp(amount, 1, Maximum);
        }

        public void IncreaseMaximum(float amount, float healing)
        {
            if (IsDead) return;
            Maximum += Mathf.Max(0f, amount);
            Restore(healing);
        }
    }
}
