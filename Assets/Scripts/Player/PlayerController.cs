using UnityEngine;

namespace MoriEchoes
{
    [DefaultExecutionOrder(-20)]
    [RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D), typeof(Health))]
    public sealed class PlayerController : MonoBehaviour
    {
        public Vector2 Aim { get; private set; } = Vector2.up;
        public Health Health { get; private set; }
        public WeaponController Weapon { get; private set; }
        public SidePlayerMotor Motor { get; private set; }
        public CharacterSkill Skill { get; private set; }
        public CharacterTechniqueController Techniques { get; private set; }
        public PlayerEnergySystem Energy { get; private set; }
        public float DodgeRemaining => Motor != null ? Motor.DodgeRemaining : Mathf.Max(0f, nextDodge - Time.time);
        public float PulseRemaining => Motor != null ? Motor.PulseRemaining : Mathf.Max(0f, nextPulse - Time.time);
        public bool IsDodging => Motor != null ? Motor.IsDodging : dodgeRemaining > 0;
        private GameManager game;
        private Rigidbody2D body;
        private Vector2 move;
        private Vector2 dodgeDirection;
        private float dodgeRemaining;
        private float nextDodge;
        private float nextPulse;
        private float nextAfterimage;

        public void Initialize(GameManager owner)
        {
            game = owner;
            body = GetComponent<Rigidbody2D>();
            body.gravityScale = 0;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            GetComponent<CircleCollider2D>().radius = 0.29f;
            Health = GetComponent<Health>();
            Health.Initialize(Team.Player, game.Config.playerHealth * (game.Config.sideScroller ? game.Character.health : 1));
            Health.ConfigurePlayerDamage(game.Config);
            Health.Died += game.Lose;
            Health.Damaged += OnDamaged;
            Weapon = gameObject.AddComponent<WeaponController>();
            Weapon.Initialize(game, this);
            if (game.Config.sideScroller)
            {
                Motor = gameObject.AddComponent<SidePlayerMotor>();
                Motor.Initialize(game, this, body);
                Skill = gameObject.AddComponent<CharacterSkill>();
                Skill.Initialize(game, this);
                Techniques = gameObject.AddComponent<CharacterTechniqueController>();
                Techniques.Initialize(game, this);
                Energy = gameObject.AddComponent<PlayerEnergySystem>();
                Energy.Initialize(game, this);
            }
        }

        private void OnDamaged(float damage)
        {
            float actual = Health.LastDamageTaken;
            game.Hud.PlayerHit(actual);
            game.Effects.PlayerHit(transform.position, actual);
            game.CameraRig.Shake(Mathf.Clamp(0.16f + actual / Health.Maximum * 0.4f, 0.18f, 0.35f));
            game.Audio.Play(SoundKind.PlayerHit);
        }

        private void Update()
        {
            if (Motor != null)
            {
                Motor.Capture();
                Aim = Motor.Aim;
                return;
            }
            if (game == null || !game.IsPlaying) { move = Vector2.zero; return; }
            InputReader input = game.Input;
            Aim = game.CameraRig.AimDirection(transform.position);
            Vector2 right = new Vector2(Aim.y, -Aim.x);
            move = Vector2.ClampMagnitude(Aim * input.Move.y + right * input.Move.x, 1f);
            if (!game.ControlsReady) { move = Vector2.zero; return; }
            if ((input.Dodge || input.LegacyDodge) && Time.time >= nextDodge && !IsDodging)
            {
                dodgeDirection = move.sqrMagnitude > 0.01f ? move.normalized : Aim;
                dodgeRemaining = game.Config.dodgeDuration;
                nextDodge = Time.time + game.Config.dodgeCooldown * game.Stats.DodgeCooldownMultiplier;
                Health.Invulnerable = true;
                game.Audio.Play(SoundKind.Dodge);
            }
            if (input.Skill && game.Stats.PulseRank > 0 && Time.time >= nextPulse)
            {
                nextPulse = Time.time + 10f;
                game.Pulse(transform.position, 4f + game.Stats.PulseRank * 0.6f, 35f + game.Stats.PulseRank * 20f);
            }
        }

        private void FixedUpdate()
        {
            if (Motor != null) { Motor.Step(); return; }
            if (game == null || body == null) return;
            if (!game.IsPlaying || Health.IsDead) { body.linearVelocity = Vector2.zero; return; }
            if (IsDodging)
            {
                body.linearVelocity = dodgeDirection * game.Config.moveSpeed * game.Config.dodgeSpeedMultiplier;
                dodgeRemaining = Mathf.Max(0, dodgeRemaining - Time.fixedDeltaTime);
                if (Time.time >= nextAfterimage)
                {
                    nextAfterimage = Time.time + 0.035f;
                    game.Effects.Ghost(transform.position);
                }
                if (!IsDodging) Health.Invulnerable = false;
            }
            else body.linearVelocity = move * game.Config.moveSpeed * game.Stats.MovementMultiplier
                * (game.CameraRig.IsAds ? game.Config.adsMoveMultiplier : 1f);
        }
    }
}
