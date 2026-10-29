using UnityEngine;

namespace MoriEchoes
{
    public sealed class VerticalEnemyController : MonoBehaviour
    {
        private GameManager game;
        private EnemyAI enemy;
        private Rigidbody2D body;
        private bool ascending;
        private int section;
        private int waveSlot;
        private float bornAt;
        private float nextAttack;
        private float nextTrail;
        private float speed;
        public int AttacksFired { get; private set; }
        public bool Emerging => Time.time - bornAt < 0.75f;
        public Vector2 EmergencePoint { get; private set; }

        public void Initialize(GameManager owner, EnemyAI actor, VerticalStageDirector director, int room, int index)
        {
            game = owner; enemy = actor; body = GetComponent<Rigidbody2D>(); section = room; waveSlot = index;
            ascending = director != null && director.Ascending;
            EmergencePoint = body.position;
            body.gravityScale = 0;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            bornAt = Time.time; nextAttack = bornAt + Random.Range(0.9f, 1.8f);
            speed = (actor.Kind == EnemyKind.Runner ? 3.1f : actor.Kind == EnemyKind.Elite ? 1.3f : 2.1f) * 1.55f;
            actor.SetCustomController();
            Transform rig = actor.transform.Find("ArticulatedSpriteRig");
            if (rig != null) rig.localScale = new Vector3(1, 0.72f, 1);
        }

        private void FixedUpdate()
        {
            if (game == null || enemy == null || body == null || !enemy.IsActive || game.Player == null || !game.IsPlaying)
            { if (body != null) body.linearVelocity = Vector2.zero; return; }
            Vector2 player = game.Player.transform.position;
            Vector2 delta = player - body.position;
            float emergence = Mathf.Clamp01((Time.time - bornAt) / 0.72f);
            float vertical = Emerging ? 3.5f : Mathf.Clamp(delta.y * 1.3f, -2.2f, speed * 0.75f);
            float horizontal = Mathf.Abs(delta.x) < 0.9f ? 0 : Mathf.Sign(delta.x) * speed;
            body.linearVelocity = Vector2.ClampMagnitude(new Vector2(horizontal, vertical), speed + 1.4f);
            if (Time.time >= nextTrail)
            {
                nextTrail = Time.time + 0.19f;
                game.Effects.Encounter(body.position, section % 4, 0.55f, 0.18f, 0, false, true);
            }
            if (!Emerging && Time.time >= nextAttack) FireAttack(delta);
            // 退出回收：向下版从下方升到玩家上方 12 回收；向上版从上方坠到玩家下方 12 回收（方向镜像）。
            if (ascending ? body.position.y < player.y - 12 : body.position.y > player.y + 12) enemy.Recycle();
            if (body.position.x < -7.4f || body.position.x > 7.4f)
                body.position = new Vector2(Mathf.Clamp(body.position.x, -7.35f, 7.35f), body.position.y);
        }

        private void FireAttack(Vector2 delta)
        {
            int archetype = (int)enemy.Archetype;
            Color color = game.Level.Theme.danger;
            Vector2 origin = body.position + Vector2.up * 0.65f;
            game.Effects.Telegraph(origin, 0.85f, 0.45f, color);
            if (archetype % 4 == 0)
            {
                for (int index = -1; index <= 1; index++)
                {
                    Vector2 direction = Quaternion.Euler(0, 0, index * 18) * Vector2.up;
                    game.Projectiles.Fire(origin, direction, 8.2f, 13 * game.DamageScale, Team.Enemy, 1, color);
                }
            }
            else if (archetype % 4 == 1)
            {
                Vector2 target = new Vector2(Mathf.Clamp(delta.x * 0.18f, -0.55f, 0.55f), 1).normalized;
                game.Effects.WarningLine(origin + Vector2.up * 1.6f, target, 7.5f, 0.38f);
                game.Projectiles.Fire(origin, target, 12.5f, 18 * game.DamageScale, Team.Enemy, 1, color, true);
            }
            else if (archetype % 4 == 2)
            {
                for (int index = 0; index < 5; index++)
                {
                    float angle = -68 + index * 34;
                    Vector2 direction = Quaternion.Euler(0, 0, angle) * Vector2.up;
                    game.Projectiles.Fire(origin, direction, 7.5f, 12 * game.DamageScale, Team.Enemy, 1,
                        Color.Lerp(color, game.Level.Theme.accent, 0.45f));
                }
            }
            else
            {
                float lane = game.Player.transform.position.x;
                for (int index = -1; index <= 1; index++)
                {
                    Vector2 point = new Vector2(lane + index * 1.35f, playerFloor());
                    game.Effects.Telegraph(point, 0.6f, 0.55f, color);
                    game.Projectiles.Fire(point, Vector2.up, 9.5f, 16 * game.DamageScale, Team.Enemy, 1, color);
                }
            }
            game.Effects.Muzzle(origin, Vector2.up, color);
            game.CameraRig.Shake(0.035f);
            AttacksFired++;
            nextAttack = Time.time + Mathf.Max(0.9f, 2.2f - game.SelectedStage * 0.08f - game.Difficulty * 0.2f);
        }

        private float playerFloor() => Mathf.Min(body.position.y - 0.6f, game.Player.transform.position.y - 1.3f);
    }
}
