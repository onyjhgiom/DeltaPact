using UnityEngine;

namespace MoriEchoes
{
    public sealed class BossController : MonoBehaviour
    {
        private GameManager game;
        private EnemyAI enemy;
        private Rigidbody2D body;
        private float speed;
        private float attackAt;
        private float resolveAt;
        private float chargeUntil;
        private Vector2 lockedDirection;
        private Vector2 impactPoint;
        private int attackIndex;
        private bool preparing;
        public SideBossController Side { get; private set; }
        public VerticalBossController Vertical { get; private set; }
        public SwimBossController Swim { get; private set; }
        public BossTechniqueController Techniques { get; private set; }
        public BossPursuitController Pursuit { get; private set; }

        public void Initialize(GameManager owner, EnemyAI controller, Rigidbody2D rigidbody, float movementSpeed)
        {
            game = owner; enemy = controller; body = rigidbody; speed = movementSpeed;
            attackAt = Time.time + 3;
            if (owner.Config.sideScroller)
            {
                Techniques = gameObject.AddComponent<BossTechniqueController>();
                Techniques.Initialize(owner, controller, this, rigidbody);
                if (owner.Level != null && owner.Level.IsSwimStage)
                {
                    Swim = gameObject.AddComponent<SwimBossController>();
                    Swim.Initialize(owner, controller, rigidbody);
                    InitializePursuit();
                    return;
                }
                if (owner.Level != null && owner.Level.IsVerticalStage)
                {
                    Vertical = gameObject.AddComponent<VerticalBossController>();
                    Vertical.Initialize(owner, controller, rigidbody);
                    InitializePursuit();
                    return;
                }
                Side = gameObject.AddComponent<SideBossController>();
                Side.Initialize(owner, controller, rigidbody);
                InitializePursuit();
            }
        }

        private void InitializePursuit()
        {
            Pursuit = gameObject.AddComponent<BossPursuitController>();
            Pursuit.Initialize(game, enemy, this, body);
        }

        private void FixedUpdate()
        {
            if (Pursuit != null && Pursuit.StepApproach()) return;
            if (Techniques != null && Techniques.Step()) return;
            if (Vertical != null) { Vertical.Step(); Pursuit.StepFollow(); return; }
            if (Swim != null) { Swim.Step(); Pursuit.StepFollow(); return; }
            if (Side != null) { Side.Step(); Pursuit.StepFollow(); return; }
            if (game == null || !enemy.IsActive || game.Player == null) return;
            if (Time.time < chargeUntil)
            {
                body.linearVelocity = lockedDirection * 13f;
                return;
            }
            Vector2 toPlayer = game.Player.transform.position - transform.position;
            if (preparing)
            {
                body.linearVelocity = Vector2.zero;
                if (Time.time >= resolveAt) ResolveAttack();
                return;
            }
            body.linearVelocity = toPlayer.sqrMagnitude > 6 ? toPlayer.normalized * speed : Vector2.zero;
            if (Time.time < attackAt) return;
            preparing = true;
            resolveAt = Time.time + (attackIndex % 2 == 0 ? 1f : 1.2f);
            if (attackIndex % 2 == 0)
            {
                impactPoint = game.Player.transform.position;
                game.Effects.Telegraph(impactPoint, 2.7f, 1f, ProceduralArt.Danger);
                game.Hud.Announce("歼灭核心 // 区域轰击", 1.1f);
            }
            else
            {
                lockedDirection = toPlayer.normalized;
                game.Effects.WarningLine(transform.position, lockedDirection, 11f, 1.2f);
                game.Hud.Announce("歼灭核心 // 锁定冲锋", 1.3f);
            }
        }

        private void ResolveAttack()
        {
            preparing = false;
            if (attackIndex % 2 == 0)
            {
                game.Effects.Telegraph(impactPoint, 2.7f, 0.3f, new Color(1f, 0.7f, 0.25f));
                game.Effects.Burst(impactPoint, ProceduralArt.Danger, 22);
                if (Vector2.Distance(game.Player.transform.position, impactPoint) <= 2.7f)
                    game.Player.Health.TakeDamage(24, Team.Enemy);
                for (int index = 0; index < 18; index++)
                {
                    Vector2 direction = Quaternion.Euler(0, 0, index * 20) * Vector2.right;
                    game.Projectiles.Fire((Vector2)transform.position + direction * 1.15f, direction, 5.5f, 10,
                        Team.Enemy, 1, ProceduralArt.Danger);
                }
                game.CameraRig.Shake(0.5f);
            }
            else chargeUntil = Time.time + 0.65f;
            game.Audio.Play(SoundKind.Blast);
            attackIndex++;
            attackAt = Time.time + (enemy.Health.Current < enemy.Health.Maximum * 0.4f ? 2.5f : 4f);
        }
    }
}
