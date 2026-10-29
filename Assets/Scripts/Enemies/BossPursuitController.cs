using System.Collections.Generic;
using UnityEngine;

namespace MoriEchoes
{
    public sealed class BossPursuitController : MonoBehaviour
    {
        private readonly List<Collider2D> terrain = new List<Collider2D>();
        private GameManager game;
        private LevelManager level;
        private EnemyAI enemy;
        private BossController boss;
        private Rigidbody2D body;
        private Collider2D hull;
        private Collider2D playerCollider;
        private Vector2 burstStart;
        private Vector2 burstEnd;
        private float startedAt;
        private float burstAt;
        private float scanAt;
        private float trailAt;
        private float anchorY;
        private int burstPhase;
        public bool Closing => burstPhase > 0;
        public bool Flying => burstPhase == 2;
        public int BurstCount { get; private set; }
        public int IgnoredTerrainCount { get; private set; }
        public Vector2 Destination => burstEnd;
        public float ClosingSpeed => 5.8f + Phase * 0.7f;
        private int Phase => boss.Side != null ? boss.Side.Phase : boss.Vertical != null ? boss.Vertical.Phase : boss.Swim != null ? boss.Swim.Phase : 1;
        private bool Valid => game != null && level != null && game.Level == level && game.IsPlaying
            && enemy != null && enemy.IsActive && game.Player != null && !game.Player.Health.IsDead;
        private bool Reserved => enemy.Health.Invulnerable || boss.Side != null && (boss.Side.Transitioning || boss.Side.ExecutionWindow)
            || boss.Vertical != null && (boss.Vertical.Transitioning || boss.Vertical.ExecutionWindow);
        private bool Idle => !Reserved && !boss.Techniques.Active
            && (boss.Side == null || !boss.Side.Preparing && !boss.Side.Attacking)
            && (boss.Vertical == null || !boss.Vertical.Preparing)
            && (boss.Swim == null || !boss.Swim.Preparing);

        public void Initialize(GameManager owner, EnemyAI actor, BossController controller, Rigidbody2D rigidbody)
        {
            game = owner; level = owner.Level; enemy = actor; boss = controller; body = rigidbody;
            hull = GetComponent<CapsuleCollider2D>();
            anchorY = body.position.y;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            body.gravityScale = 0;
            burstAt = Time.time + 2.2f;
            IgnoreTerrain();
        }

        private void IgnoreTerrain()
        {
            terrain.Clear(); level.GetComponentsInChildren(false, terrain);
            foreach (Collider2D collider in terrain)
            {
                if (collider == null || collider == hull || collider.isTrigger || collider.transform.IsChildOf(transform)
                    || collider.GetComponentInParent<PlayerController>() != null) continue;
                if (Physics2D.GetIgnoreCollision(hull, collider)) continue;
                Physics2D.IgnoreCollision(hull, collider, true); IgnoredTerrainCount++;
            }
            scanAt = Time.time + 0.5f;
        }

        public bool StepApproach()
        {
            if (!Valid)
            {
                if (Closing && (game == null || game.State != RunState.Paused)) Cancel();
                return false;
            }
            if (Time.time >= scanAt) IgnoreTerrain();
            if (Reserved) { Cancel(); return false; }
            if (Closing)
            {
                float elapsed = Time.time - startedAt;
                body.linearVelocity = Vector2.zero;
                if (elapsed < 0.45f) return true;
                burstPhase = 2;
                if (playerCollider == null)
                {
                    playerCollider = game.Player.GetComponent<CapsuleCollider2D>();
                    if (playerCollider != null) Physics2D.IgnoreCollision(hull, playerCollider, true);
                }
                float progress = Mathf.Clamp01((elapsed - 0.45f) / 0.34f);
                float eased = progress * progress * (3 - 2 * progress);
                body.MovePosition(Vector2.Lerp(burstStart, burstEnd, eased));
                if (Time.time >= trailAt)
                {
                    trailAt = Time.time + 0.055f;
                    game.Effects.Encounter(body.position, level.IsSwimStage ? 1 : 0, 1.4f, 0.22f, 0, false, true);
                }
                if (progress < 1) return true;
                body.position = burstEnd;
                Cancel();
                burstAt = Time.time + 8.5f - Phase * 0.65f;
                game.Effects.Encounter(burstEnd, 3, 2.1f, 0.32f);
                return true;
            }
            if (!Idle || Time.time < burstAt) return false;
            Vector2 destination = ChasePoint();
            if (Vector2.Distance(body.position, destination) < 7.5f) return false;
            burstStart = body.position; burstEnd = destination; startedAt = Time.time;
            burstPhase = 1; BurstCount++;
            body.linearVelocity = Vector2.zero;
            game.Effects.BossWarningDisc(burstEnd, 1.15f, 0.45f);
            game.Effects.Encounter(body.position, 1, 2.4f, 0.45f);
            game.Hud.Announce(level.Theme.boss + " // 高速追击", 0.8f);
            return true;
        }

        public void StepFollow()
        {
            if (!Valid || Closing || !Idle) return;
            Vector2 destination = ChasePoint();
            Vector2 delta = destination - body.position;
            if (delta.magnitude < 0.18f) { body.linearVelocity = Vector2.zero; return; }
            Vector2 desired = Vector2.ClampMagnitude(delta * 2.8f, ClosingSpeed);
            body.linearVelocity = desired;
        }

        private Vector2 ChasePoint()
        {
            Vector2 player = game.Player.transform.position;
            float side = body.position.x >= player.x ? 1 : -1;
            float offset = level.IsVerticalStage ? 3.25f : 3.8f;
            float minimumX = level.IsVerticalStage ? -5.7f : level.Segments[level.Segments.Length - 1].start + 2.6f;
            float maximumX = level.IsVerticalStage ? 5.7f : level.Length - 2.6f;
            float x = Mathf.Clamp(player.x + side * offset, minimumX, maximumX);
            if (Mathf.Abs(x - player.x) < 2.5f) x = Mathf.Clamp(player.x - side * offset, minimumX, maximumX);
            float y;
            if (level.Swim != null)
                y = Mathf.Clamp(player.y + (level.IsUnderwaterStage ? 0.45f : 0.15f), level.Swim.MinY + 1.5f, level.Swim.MaxY - 1.5f);
            else if (level.IsVerticalStage)
                y = Mathf.Clamp(player.y + 1.5f, level.Vertical.DepthLength - 10, level.Vertical.DepthLength - 4.5f);
            else
                y = Mathf.Clamp(player.y + 0.65f, anchorY, 8);
            return new Vector2(x, y);
        }

        private void Cancel()
        {
            burstPhase = 0;
            if (playerCollider != null && hull != null) Physics2D.IgnoreCollision(hull, playerCollider, false);
            playerCollider = null;
            if (body != null) body.linearVelocity = Vector2.zero;
        }

        private void OnDisable() => Cancel();
    }
}
