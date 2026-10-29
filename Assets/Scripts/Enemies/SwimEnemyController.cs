using UnityEngine;

namespace MoriEchoes
{
    public enum SwimDepthRole { Surfaced, Submerged, FullColumn }
    // 水中敌人：不受重力，在水中朝玩家游动（带蛇形浮沉），并向玩家射击。
    // 玩家游过之后（落在身后远处）静默回收，保持"边游边打"节奏。
    public sealed class SwimEnemyController : MonoBehaviour
    {
        private GameManager game;
        private EnemyAI enemy;
        private Rigidbody2D body;
        private bool underwater;
        private float surfaceY;
        private float minY;
        private float maxY;
        private int section;
        private float bornAt;
        private float nextAttack;
        private float weavePhase;
        private float speed;
        private float preferredDepth;
        private float stunnedUntil;
        public void Stun(float seconds) { stunnedUntil = Mathf.Max(stunnedUntil, Time.time + seconds); }
        public SwimDepthRole DepthRole { get; private set; }
        public float PreferredDepth => preferredDepth;
        public int AttacksFired { get; private set; }
        public Vector2 EmergencePoint { get; private set; }

        public void Initialize(GameManager owner, EnemyAI actor, SwimStageDirector director, int room, int index)
        {
            game = owner; enemy = actor; body = GetComponent<Rigidbody2D>(); section = room;
            underwater = director.Underwater;
            surfaceY = director.SurfaceY; minY = director.MinY; maxY = director.MaxY;
            EmergencePoint = body.position;
            body.gravityScale = 0;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            bornAt = Time.time; nextAttack = bornAt + Random.Range(1f, 1.9f);
            weavePhase = Random.Range(0f, Mathf.PI * 2f);
            speed = (actor.Kind == EnemyKind.Runner ? 3.4f : actor.Kind == EnemyKind.Elite ? 1.6f : 2.3f) * 1.3f;
            DepthRole = (SwimDepthRole)((room + index) % 3);
            float range = maxY - minY;
            preferredDepth = DepthRole == SwimDepthRole.Surfaced ? (underwater ? maxY - range * 0.2f : surfaceY + 0.1f)
                : DepthRole == SwimDepthRole.Submerged ? (underwater ? minY + range * 0.2f : surfaceY - 1.5f) : body.position.y;
            actor.SetCustomController();
        }

        private void FixedUpdate()
        {
            if (game == null || enemy == null || body == null || !enemy.IsActive || game.Player == null || !game.IsPlaying)
            { if (body != null) body.linearVelocity = Vector2.zero; return; }
            if (Time.time < stunnedUntil) { body.linearVelocity = Vector2.zero; return; }
            Vector2 player = game.Player.transform.position;
            Vector2 delta = player - body.position;
            float distance = delta.magnitude;
            Vector2 dir = distance > 0.01f ? delta / distance : Vector2.right;
            // 接近到首选距离后环绕（射手保持距离、近战贴身）
            float preferred = enemy.Kind == EnemyKind.Gunner ? 6f : enemy.Kind == EnemyKind.Elite ? 3f : 1.2f;
            float approach = distance > preferred + 0.4f ? 1f : distance < preferred - 0.4f ? -0.4f : 0.15f;
            // 蛇形浮沉（水下幅度大，海面贴着波面小幅）
            float targetY = DepthRole == SwimDepthRole.FullColumn ? player.y : preferredDepth;
            float weave = Mathf.Sin(Time.time * 2.1f + weavePhase) * (underwater ? 0.65f : 0.22f);
            Vector2 velocity = new Vector2(Mathf.Sign(delta.x) * approach * speed,
                Mathf.Clamp((targetY + weave - body.position.y) * 2, -speed, speed));
            body.linearVelocity = Vector2.ClampMagnitude(velocity, speed + 1f);
            // 限制在水体柱内
            float floor = underwater ? minY + 0.6f : surfaceY - 1.6f;
            float ceil = underwater ? maxY - 0.6f : surfaceY + 0.5f;
            Vector2 p = body.position;
            float clampedY = Mathf.Clamp(p.y, floor, ceil);
            if (clampedY != p.y) body.position = new Vector2(p.x, clampedY);
            // 开火
            if (Time.time >= nextAttack && distance < 11f) FireAttack(dir, player);
            // 玩家越过后回收（落在身后远处）
            if (body.position.x < player.x - 18f) enemy.Recycle();
        }

        private void FireAttack(Vector2 dir, Vector2 player)
        {
            Color color = game.Level.Theme.danger;
            Vector2 origin = body.position + dir * 0.7f;
            game.Effects.WaterMuzzle(origin, dir, color);
            int pattern = (int)enemy.Archetype % 3;
            if (pattern == 0)
            {
                game.Projectiles.Fire(origin, dir, 9f, 12 * game.DamageScale, Team.Enemy, 1, color);
            }
            else if (pattern == 1)
            {
                for (int index = -1; index <= 1; index++)
                {
                    Vector2 spread = Quaternion.Euler(0, 0, index * 14) * dir;
                    game.Projectiles.Fire(origin, spread, 7.6f, 10 * game.DamageScale, Team.Enemy, 1, color);
                }
            }
            else
            {
                Vector2 aimed = (player - body.position).normalized;
                game.Projectiles.Fire(origin, aimed, 11.5f, 15 * game.DamageScale, Team.Enemy, 1, color, true);
            }
            AttacksFired++;
            nextAttack = Time.time + Mathf.Max(0.9f, 2.1f - game.SelectedStage * 0.06f - game.Difficulty * 0.2f);
        }
    }
}
