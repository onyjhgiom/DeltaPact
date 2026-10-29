using UnityEngine;

namespace MoriEchoes
{
    [RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D), typeof(Health))]
    public sealed class EnemyAI : MonoBehaviour
    {
        public EnemyKind Kind { get; private set; }
        public EnemyArchetype Archetype { get; private set; }
        public string DisplayName => EnemyRoster.Name(Archetype);
        public string BehaviorName => EnemyRoster.Behavior(Archetype);
        public SideEnemyMotor Motor => sideMotor;
        public void Stun(float seconds)
        {
            if (Kind == EnemyKind.Boss) return;
            sideMotor?.Stun(seconds);
            GetComponent<SwimEnemyController>()?.Stun(seconds);
        }
        public Health Health { get; private set; }
        private GameManager game;
        private Rigidbody2D body;
        private float speed;
        private float damage;
        private float attackAt;
        private float activateAt;
        private float contactAt;
        private int experience;
        private BossController boss;
        private bool registered;
        private bool deathHandled;
        private bool customController;
        private bool secretSuspended;
        private SideEnemyMotor sideMotor;
        public bool IsActive => !secretSuspended && game != null && game.IsPlaying && Health != null && Time.time >= activateAt && !Health.IsDead;

        public void SetSecretSuspended(bool suspended)
        {
            secretSuspended = suspended;
            if (body == null) return;
            body.linearVelocity = Vector2.zero;
            body.simulated = !suspended;
        }

        public void Initialize(GameManager owner, EnemyKind kind, int wave)
        {
            game = owner; Kind = kind; Archetype = EnemyRoster.DefaultArchetype(game.SelectedStage, kind);
            float maximum;
            switch (kind)
            {
                case EnemyKind.Gunner: maximum = 25; speed = 1.5f; damage = 8; experience = 4; break;
                case EnemyKind.Runner: maximum = 18; speed = 3.4f; damage = 8; experience = 3; break;
                case EnemyKind.Elite: maximum = 240; speed = 1.8f; damage = 16; experience = 16; break;
                case EnemyKind.Boss: maximum = 900; speed = 1.35f; damage = 20; experience = 60; break;
                default: maximum = 35; speed = 2.3f; damage = 10; experience = 3; break;
            }
            if (kind != EnemyKind.Boss) maximum *= WaveRules.HealthMultiplier(wave);
            if (game.Config.sideScroller && kind != EnemyKind.Boss) speed *= 1.5f + game.SelectedStage * 0.035f;
            body = GetComponent<Rigidbody2D>();
            body.gravityScale = 0;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.mass = kind == EnemyKind.Boss ? 8 : kind == EnemyKind.Elite ? 3 : 1;
            GetComponent<CircleCollider2D>().radius = kind == EnemyKind.Boss ? 0.9f : kind == EnemyKind.Elite ? 0.45f : 0.28f;
            Health = GetComponent<Health>();
            if (game.Config.sideScroller) maximum *= kind == EnemyKind.Boss ? 1 + game.SelectedStage * 0.2f : 0.85f;
            if (kind == EnemyKind.Boss) maximum *= game.Config.bossHealthMultiplier;
            Health.Initialize(Team.Enemy, maximum);
            if (kind == EnemyKind.Boss)
            {
                Health.DamageReduction = game.Config.bossDamageReduction;
                Health.MaximumHitFraction = game.Config.bossMaximumHitFraction;
                damage *= game.Config.bossSkillDamageMultiplier;
            }
            Health.Died += OnDied;
            Health.Damaged += OnDamaged;
            activateAt = Time.time + 0.85f;
            attackAt = activateAt + Random.Range(0.2f, 1.2f);
            game.Enemies.Add(this); registered = true;
            if (game.Config.sideScroller)
            {
                damage *= game.DamageScale;
                sideMotor = gameObject.AddComponent<SideEnemyMotor>();
                sideMotor.Initialize(game, this, body, speed, damage);
            }
            gameObject.AddComponent<CharacterVisual>().Initialize(game, Health, kind);
            if (kind == EnemyKind.Boss)
            {
                boss = gameObject.AddComponent<BossController>();
                boss.Initialize(game, this, body, speed);
            }
            game.Effects.Telegraph(transform.position, kind == EnemyKind.Boss ? 2.3f : 0.7f, 0.85f, ProceduralArt.Danger);
        }

        public void ConfigureEncounter(EnemyArchetype archetype, SegmentDefinition segment, Vector2 formationAnchor,
            int formationSlot = 0, float attackDelay = 0, string formation = "地面阵线")
        {
            if (EnemyRoster.Kind(archetype) != Kind) throw new System.ArgumentException("Archetype must preserve the enemy's visual kind.", nameof(archetype));
            Archetype = archetype;
            gameObject.name = EnemyRoster.Name(archetype);
            sideMotor?.ConfigureEncounter(segment, formationAnchor, formationSlot, attackDelay, formation);
        }

        public void SetCustomController()
        {
            customController = true;
            if (sideMotor != null) sideMotor.enabled = false;
        }

        // 软推进回收：玩家越过段落后，落在相机左界外的旧敌静默销毁，
        // 不计入击杀、不掉经验、不触发特效 —— 保持"边冲边打"节奏，避免敌人堆积。
        public void Recycle()
        {
            if (registered) { game.Enemies.Remove(this); registered = false; }
            gameObject.SetActive(false);
            Destroy(gameObject);
        }

        private void OnDamaged(float amount)
        {
            if (game != null && game.Player != null && game.Player.Energy != null)
                game.Player.Energy.Add(amount * 0.1f);
        }

        private void FixedUpdate()
        {
            if (game == null || body == null) return;
            if (customController) return;
            if (sideMotor != null) { sideMotor.Step(); return; }
            if (!IsActive || game.Player == null || game.Player.Health.IsDead)
            {
                body.linearVelocity = Vector2.zero;
                return;
            }
            if (boss != null) return;
            Vector2 toPlayer = game.Player.transform.position - transform.position;
            float distance = toPlayer.magnitude;
            Vector2 direction = distance > 0.01f ? toPlayer / distance : Vector2.zero;
            float desired = Kind == EnemyKind.Gunner ? distance > 6 ? 1 : distance < 3.8f ? -0.65f : 0 : 1;
            Vector2 separation = Vector2.zero;
            foreach (EnemyAI other in game.Enemies)
            {
                if (other == this || other == null || other.Health.IsDead) continue;
                Vector2 apart = transform.position - other.transform.position;
                float square = apart.sqrMagnitude;
                if (square > 0.001f && square < 0.7f) separation += apart / square * 0.22f;
            }
            body.linearVelocity = Vector2.ClampMagnitude(direction * desired + separation, 1) * speed;
            if ((Kind == EnemyKind.Gunner || Kind == EnemyKind.Elite) && distance < 9 && Time.time >= attackAt)
            {
                attackAt = Time.time + (Kind == EnemyKind.Elite ? 2.4f : 1.8f);
                int count = Kind == EnemyKind.Elite ? 3 : 1;
                for (int index = 0; index < count; index++)
                {
                    Vector2 heading = Quaternion.Euler(0, 0, (index - (count - 1) * 0.5f) * 13f) * direction;
                    game.Projectiles.Fire((Vector2)transform.position + heading * 0.7f, heading, 7f, damage,
                        Team.Enemy, 1, ProceduralArt.Danger);
                }
                game.Effects.Muzzle(transform.position, direction, ProceduralArt.Danger);
            }
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            if (game == null || !IsActive || Time.time < contactAt) return;
            PlayerController target = collision.collider.GetComponentInParent<PlayerController>();
            if (target == null) return;
            if (target.Health.TakeDamage(damage, Team.Enemy, false,
                Kind == EnemyKind.Boss ? IncomingHitKind.BossSkill : IncomingHitKind.Enemy)) contactAt = Time.time + 1f;
        }

        private void OnDied()
        {
            if (deathHandled || game == null) return;
            deathHandled = true;
            if (registered) { game.Enemies.Remove(this); registered = false; }
            foreach (Collider2D collider in GetComponentsInChildren<Collider2D>()) collider.enabled = false;
            body.linearVelocity = Vector2.zero;
            game.RecordKill();
            SecretMiniBossController secret = GetComponent<SecretMiniBossController>();
            if (secret != null) secret.OnDefeated(this);
            game.Pickups.Drop(transform.position, experience);
            game.Level?.GetComponent<WeaponCapsuleDirector>()?.OnEnemyKilled(this);
            game.Effects.Burst(transform.position, ProceduralArt.Danger, Kind == EnemyKind.Elite || Kind == EnemyKind.Boss ? 18 : 9);
            game.Audio.Play(SoundKind.EnemyDown);
            if (Kind == EnemyKind.Boss)
            {
                if (boss != null && boss.Side != null) boss.Side.DeathEffects();
                if (boss != null && boss.Vertical != null) boss.Vertical.DeathEffects();
                if (boss != null && boss.Swim != null) boss.Swim.DeathEffects();
                game.Win();
            }
            gameObject.SetActive(false);
            Destroy(gameObject);
        }

        private void OnDisable()
        {
            if (registered && game != null) { game.Enemies.Remove(this); registered = false; }
        }

        private void OnDestroy()
        {
            if (Health != null) Health.Died -= OnDied;
            if (Health != null) Health.Damaged -= OnDamaged;
            if (registered && game != null) game.Enemies.Remove(this);
        }
    }
}
