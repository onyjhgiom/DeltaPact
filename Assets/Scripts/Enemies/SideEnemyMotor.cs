using UnityEngine;

namespace MoriEchoes
{
    public sealed class SideEnemyMotor : MonoBehaviour
    {
        private enum AttackPattern { Melee, Leap, Dash, LowCannon, Sniper, Laser, Bomb, Dive, PatrolVolley, GroundSlam, LaneVolley }
        private GameManager game;
        private EnemyAI enemy;
        private Rigidbody2D body;
        private float speed;
        private float damage;
        private float clock;
        private float attackAt;
        private float jumpAt;
        private float stunnedUntil;
        private float resolveAt;
        private float aimLockAt;
        private float recoveryUntil;
        private float dashUntil;
        private float burstAt;
        private float repairAt;
        private float laserUntil;
        private int burstRemaining;
        private bool laserDamaged;
        private bool flying;
        private Vector2 lockedAim;
        private Vector2 lockedOrigin;
        private Vector2 dashVelocity;
        private Vector2 home;
        private Vector2 formationAnchor;
        private Vector2 formationOffset;
        private int formationSlot;
        private AttackPattern pattern;
        private SpriteRenderer sight;
        private SpriteRenderer shield;
        private SpriteRenderer[] cannonBarrels;
        private readonly RaycastHit2D[] rayHits = new RaycastHit2D[64];
        private readonly ContactFilter2D solidFilter = new ContactFilter2D { useTriggers = false };

        public bool Attacking => resolveAt > 0 || clock < dashUntil || clock < laserUntil || burstRemaining > 0;
        public bool Preparing => resolveAt > 0;
        public bool AimLocked => Preparing && clock >= aimLockAt;
        public bool ShieldRaised { get; private set; }
        public bool IsStationary => enemy != null && EnemyRoster.Stationary(enemy.Archetype);
        public bool Flying => flying;
        public int VolleysFired { get; private set; }
        public int TelegraphsStarted { get; private set; }
        public int RepairsPerformed { get; private set; }
        public int ChargesResolved { get; private set; }
        public int LaserHits { get; private set; }
        public float TelegraphRemaining => Mathf.Max(0, resolveAt - clock);
        public Vector2 LockedAim => lockedAim;
        public Vector2 LockedOrigin => lockedOrigin;
        public float PatrolLeft { get; private set; }
        public float PatrolRight { get; private set; }
        public string Formation { get; private set; } = "地面阵线";
        public string Action => clock < stunnedUntil ? "眩晕" : Preparing ? AimLocked ? "锁定预警" : "追踪瞄准"
            : clock < laserUntil ? "激光释放" : clock < dashUntil ? "冲击" : clock < recoveryUntil ? "攻击硬直" : ShieldRaised ? "举盾推进" : "移动待机";

        public void Initialize(GameManager owner, EnemyAI controller, Rigidbody2D rigidbody, float movementSpeed, float hitDamage)
        {
            game = owner; enemy = controller; body = rigidbody; speed = movementSpeed; damage = hitDamage;
            flying = enemy.Kind == EnemyKind.Runner;
            body.gravityScale = flying ? 0 : game.Config.gravityScale;
            GetComponent<CircleCollider2D>().enabled = false;
            var capsule = gameObject.AddComponent<CapsuleCollider2D>();
            capsule.size = enemy.Kind == EnemyKind.Boss ? new Vector2(2, 3.1f) : flying ? new Vector2(0.85f, 0.65f) : new Vector2(0.8f, 1.6f);
            home = formationAnchor = body.position;
            PatrolLeft = 0.8f; PatrolRight = game.Level != null ? game.Level.Length - 1 : body.position.x + 20;
            ApplyRole();
            attackAt = 1.3f;
        }

        public void ConfigureEncounter(SegmentDefinition segment, Vector2 anchor, int slot, float delay, string formation)
        {
            home = body.position; formationAnchor = anchor; formationOffset = home - anchor;
            formationSlot = Mathf.Clamp(slot, 0, EncounterDirector.MaximumEncounterEnemies - 1);
            Formation = formation;
            if (segment != null) { PatrolLeft = segment.start + 1.2f; PatrolRight = Mathf.Max(PatrolLeft, segment.End - 1.4f); }
            attackAt = clock + 1 + Mathf.Clamp(delay, 0, 3);
            ApplyRole();
        }

        private void ApplyRole()
        {
            switch (enemy.Archetype)
            {
                case EnemyArchetype.ScrapTurret:
                case EnemyArchetype.FactoryGunner: pattern = AttackPattern.LowCannon; break;
                case EnemyArchetype.SnowGunner:
                case EnemyArchetype.CityGunner: pattern = AttackPattern.Sniper; break;
                case EnemyArchetype.CityLaser: pattern = AttackPattern.Laser; break;
                case EnemyArchetype.FactoryBomber:
                case EnemyArchetype.CitadelRunner: pattern = AttackPattern.Bomb; break;
                case EnemyArchetype.ScrapWing:
                case EnemyArchetype.SnowWing: pattern = AttackPattern.Dive; break;
                case EnemyArchetype.CityRunner: pattern = AttackPattern.PatrolVolley; break;
                case EnemyArchetype.FactoryElite:
                case EnemyArchetype.SnowElite: pattern = AttackPattern.GroundSlam; break;
                case EnemyArchetype.CityChaser:
                case EnemyArchetype.ScrapShield:
                case EnemyArchetype.CityElite:
                case EnemyArchetype.CitadelElite: pattern = AttackPattern.Dash; break;
                case EnemyArchetype.SnowChaser:
                case EnemyArchetype.CitadelChaser: pattern = AttackPattern.Leap; break;
                case EnemyArchetype.CitadelGunner: pattern = AttackPattern.LaneVolley; break;
                default: pattern = AttackPattern.Melee; break;
            }
            if (enemy.Kind != EnemyKind.Boss)
                body.constraints = RigidbodyConstraints2D.FreezeRotation | (IsStationary ? RigidbodyConstraints2D.FreezePositionX : RigidbodyConstraints2D.None);
            if (sight == null && enemy.Kind != EnemyKind.Boss)
            {
                sight = game.Art.Renderer("EnemyAimWarning", transform, game.Art.Square, Color.clear, Vector2.one, 180);
                sight.enabled = false;
            }
            if (EnemyRoster.Shielded(enemy.Archetype) && shield == null)
                shield = game.Art.Renderer("GuardShield", transform, game.Art.Square, new Color(0.4f, 0.85f, 1, 0.75f), new Vector2(0.14f, 1.35f), 140);
            if ((pattern == AttackPattern.LowCannon || pattern == AttackPattern.Laser) && cannonBarrels == null)
            {
                cannonBarrels = new SpriteRenderer[2];
                for (int barrelIndex = 0; barrelIndex < cannonBarrels.Length; barrelIndex++)
                    cannonBarrels[barrelIndex] = game.Art.Renderer("FixedCannonBarrel_" + barrelIndex, transform, game.Art.Square,
                        game.Level.Theme.structure * 2, new Vector2(0.8f, 0.12f), 135);
            }
        }

        public void Stun(float duration)
        {
            stunnedUntil = Mathf.Max(stunnedUntil, clock + Mathf.Max(0, duration));
            resolveAt = dashUntil = laserUntil = 0; burstRemaining = 0;
            attackAt = Mathf.Max(attackAt, stunnedUntil + 0.9f);
            SetShield(false);
            if (sight != null) sight.enabled = false;
        }

        public void Step()
        {
            if (game == null || body == null) return;
            if (!game.IsPlaying || !enemy.IsActive || game.Player == null || game.Player.Health.IsDead)
            {
                body.linearVelocity = Vector2.zero;
                return;
            }
            if (enemy.Kind == EnemyKind.Boss) return;
            clock += Time.fixedDeltaTime;
            if (body.position.y < -8) { enemy.Health.TakeDamage(enemy.Health.Maximum, Team.Player); return; }
            Vector2 delta = (Vector2)game.Player.transform.position - body.position;
            UpdateRoleVisuals(Direction(delta.x));
            if (body.position.x < PatrolLeft || body.position.x > PatrolRight)
                body.position = new Vector2(Mathf.Clamp(body.position.x, PatrolLeft, PatrolRight), body.position.y);
            if (clock < stunnedUntil) { StopHorizontal(); SetShield(false); return; }
            if (clock < dashUntil)
            {
                bool atEdge = body.position.x <= PatrolLeft + 0.05f && dashVelocity.x < 0 || body.position.x >= PatrolRight - 0.05f && dashVelocity.x > 0;
                if (!atEdge)
                {
                    body.linearVelocity = new Vector2(dashVelocity.x, flying ? dashVelocity.y : body.linearVelocity.y);
                    return;
                }
                dashUntil = clock;
            }
            if (dashUntil > 0) { dashUntil = 0; recoveryUntil = clock + 1.05f; }
            if (clock < laserUntil) { StopHorizontal(); ResolveLaser(); return; }
            if (laserUntil > 0) { laserUntil = 0; sight.enabled = false; }
            if (burstRemaining > 0 && clock >= burstAt)
            {
                burstRemaining--; burstAt = clock + 0.36f;
                if (pattern == AttackPattern.LowCannon) FireCannon();
                else FireShot(lockedOrigin, lockedAim, 10, damage);
            }
            if (resolveAt > 0)
            {
                StopHorizontal(); SetShield(false);
                if ((pattern == AttackPattern.Sniper || pattern == AttackPattern.Laser) && clock < aimLockAt) LockPlayerAim();
                DrawWarning();
                if (clock >= resolveAt) ReleaseAttack();
                return;
            }
            if (clock < recoveryUntil || burstRemaining > 0) { StopHorizontal(); SetShield(false); return; }
            Move(delta);
            if (clock >= attackAt && delta.magnitude <= AttackRange()) BeginAttack();
        }

        private void Move(Vector2 delta)
        {
            SetShield(EnemyRoster.Shielded(enemy.Archetype));
            if (IsStationary) { StopHorizontal(); return; }
            if (flying)
            {
                Vector2 destination = formationAnchor + formationOffset;
                destination.x += Mathf.Sin(clock * 0.6f) * 3.5f;
                if (pattern == AttackPattern.Bomb) destination.x = game.Player.transform.position.x + (formationSlot - 1) * 2.6f;
                destination.x = Mathf.Clamp(destination.x, PatrolLeft + 3, Mathf.Max(PatrolLeft + 3, PatrolRight - 2));
                destination.y = home.y + (pattern == AttackPattern.PatrolVolley ? 0 : Mathf.Sin(clock * 1.4f) * 0.25f);
                body.linearVelocity = new Vector2(Mathf.Clamp((destination.x - body.position.x) * 1.6f, -speed, speed),
                    Mathf.Clamp((destination.y - body.position.y) * 2.5f, -4, 4));
                return;
            }
            if (enemy.Archetype == EnemyArchetype.FactoryChaser && Mathf.Abs(delta.x) > 3.3f && TryRepair()) return;
            float horizontal = Mathf.Abs(delta.x);
            float direction = horizontal < 0.95f ? 0 : Direction(delta.x);
            if (pattern == AttackPattern.LaneVolley) direction = horizontal > 9 ? direction : horizontal < 6 ? -direction : 0;
            float movementSpeed = speed * (ShieldRaised ? 0.62f : 1);
            if (body.position.x <= PatrolLeft + 0.1f && direction < 0 || body.position.x >= PatrolRight - 0.1f && direction > 0) direction = 0;
            body.linearVelocity = new Vector2(direction * movementSpeed, body.linearVelocity.y);
            if (direction == 0 || horizontal < 2 || clock < jumpAt || !Grounded()) return;
            int count = Physics2D.Raycast(body.position + Vector2.down * 0.35f, Vector2.right * direction, solidFilter, rayHits, 1.05f);
            bool blocked = false;
            for (int hitIndex = 0; hitIndex < count; hitIndex++)
            {
                Collider2D obstacle = rayHits[hitIndex].collider;
                if (obstacle == null || obstacle.attachedRigidbody == body) continue;
                if (obstacle.GetComponent<PlatformSurface>() != null || obstacle.GetComponentInParent<DestructibleCover>() != null) blocked = true;
            }
            if (blocked || delta.y > 1.25f)
            {
                body.linearVelocity = new Vector2(direction * movementSpeed, 9);
                jumpAt = clock + 1.8f;
            }
        }

        private bool TryRepair()
        {
            EnemyAI target = null;
            float nearest = 9;
            for (int enemyIndex = 0; enemyIndex < game.Enemies.Count; enemyIndex++)
            {
                EnemyAI candidate = game.Enemies[enemyIndex];
                if (candidate == null || candidate == enemy || candidate.Archetype != EnemyArchetype.FactoryGunner
                    || candidate.Health.IsDead || candidate.Health.Current >= candidate.Health.Maximum - 0.01f) continue;
                float distance = Vector2.Distance(candidate.transform.position, body.position);
                if (candidate.transform.position.x < PatrolLeft || candidate.transform.position.x > PatrolRight || distance >= nearest) continue;
                target = candidate; nearest = distance;
            }
            if (target == null) return false;
            float horizontal = target.transform.position.x - body.position.x;
            body.linearVelocity = new Vector2(Mathf.Abs(horizontal) > 1.5f ? Direction(horizontal) * speed : 0, body.linearVelocity.y);
            if (nearest <= 1.8f && clock >= repairAt)
            {
                target.Health.Restore(10); repairAt = clock + 2.8f; RepairsPerformed++;
                game.Effects.SkillWave(target.transform.position, game.Level.Theme.accent, 0.8f);
            }
            return true;
        }

        private void BeginAttack()
        {
            float warning = pattern == AttackPattern.Sniper || pattern == AttackPattern.Laser ? 1.5f
                : pattern == AttackPattern.Bomb || pattern == AttackPattern.GroundSlam ? 1.2f : pattern == AttackPattern.Dash ? 1.1f : 0.9f;
            resolveAt = clock + warning;
            aimLockAt = pattern == AttackPattern.Sniper || pattern == AttackPattern.Laser ? clock + 0.45f : clock;
            LockPlayerAim();
            if (pattern == AttackPattern.LowCannon || pattern == AttackPattern.PatrolVolley || pattern == AttackPattern.LaneVolley)
            {
                lockedAim = Vector2.right * Direction(lockedAim.x);
                lockedOrigin = pattern == AttackPattern.LowCannon ? body.position + Vector2.down * 0.32f
                    : pattern == AttackPattern.LaneVolley ? new Vector2(body.position.x, body.position.y - 0.8f + (VolleysFired % 2 == 0 ? 0.45f : 1.75f)) : body.position;
            }
            else if (pattern == AttackPattern.Bomb) { lockedOrigin = body.position; lockedAim = Vector2.down; }
            else if (pattern == AttackPattern.Dash || pattern == AttackPattern.GroundSlam)
            {
                lockedOrigin = body.position + Vector2.down * 0.45f; lockedAim = Vector2.right * Direction(lockedAim.x);
            }
            SetShield(false); TelegraphsStarted++;
            if (game.Effects.AttackTelegraphsEnabled)
                game.Effects.Encounter(body.position, 1, enemy.Kind == EnemyKind.Elite ? 2 : 1.2f, warning);
            if (pattern == AttackPattern.Bomb) game.Effects.DangerArea(new Vector2(lockedOrigin.x, 0.35f), new Vector2(1.8f, 0.7f), warning);
            if (pattern == AttackPattern.GroundSlam) game.Effects.DangerArea(body.position + Vector2.down * 0.5f, new Vector2(14, 0.8f), warning);
            DrawWarning();
        }

        private void LockPlayerAim()
        {
            lockedOrigin = body.position + Vector2.up * (pattern == AttackPattern.Laser ? 0.5f : 0.18f);
            Vector2 target = (Vector2)game.Player.transform.position - lockedOrigin;
            lockedAim = pattern == AttackPattern.Laser ? Vector2.right * Direction(target.x)
                : target.sqrMagnitude > 0.01f ? target.normalized : Vector2.left;
        }

        private void DrawWarning()
        {
            if (!game.Effects.AttackTelegraphsEnabled) { sight.enabled = false; return; }
            float length = pattern == AttackPattern.Melee ? 2 : pattern == AttackPattern.Dash || pattern == AttackPattern.Leap ? 7
                : pattern == AttackPattern.Dive ? 8 : pattern == AttackPattern.Bomb ? Mathf.Max(1, lockedOrigin.y) : 20;
            SetLine(lockedOrigin, lockedAim, length, pattern == AttackPattern.LowCannon ? 0.45f : AimLocked ? 0.07f : 0.035f,
                AimLocked ? new Color(1, 0.28f, 0.12f, 0.9f) : new Color(1, 0.85f, 0.25f, 0.65f));
        }

        private void ReleaseAttack()
        {
            resolveAt = 0; VolleysFired++;
            sight.enabled = false;
            recoveryUntil = clock + 0.5f;
            attackAt = clock + (pattern == AttackPattern.Sniper || pattern == AttackPattern.Laser ? 2.4f
                : pattern == AttackPattern.GroundSlam ? 2.7f : pattern == AttackPattern.Bomb ? 2.1f : 1.85f);
            switch (pattern)
            {
                case AttackPattern.Melee:
                    Vector2 delta = (Vector2)game.Player.transform.position - body.position;
                    if (Mathf.Abs(delta.x) <= 2.1f && Mathf.Abs(delta.y) <= 1.5f) game.Player.Health.TakeDamage(damage * 1.1f, Team.Enemy);
                    break;
                case AttackPattern.Leap: StartCharge(new Vector2(Direction(lockedAim.x) * 6.5f, 8.5f), 0.65f); break;
                case AttackPattern.Dash: StartCharge(new Vector2(Direction(lockedAim.x) * (enemy.Kind == EnemyKind.Elite ? 8 : 9), 0), 0.65f); break;
                case AttackPattern.Dive: StartCharge(lockedAim * 8, 0.7f); break;
                case AttackPattern.LowCannon: FireCannon(); burstRemaining = 1; burstAt = clock + 0.36f; break;
                case AttackPattern.Sniper: FireShot(lockedOrigin, lockedAim, 22, damage * 1.3f); break;
                case AttackPattern.Laser: laserUntil = clock + 0.6f; laserDamaged = false; ResolveLaser(); break;
                case AttackPattern.Bomb: FireShot(lockedOrigin, Vector2.down, 8, damage * 1.15f, 1.1f); break;
                case AttackPattern.GroundSlam:
                    Vector2 groundOrigin = body.position + Vector2.down * 0.43f;
                    FireShot(groundOrigin, Vector2.left, 8, damage);
                    FireShot(groundOrigin, Vector2.right, 8, damage);
                    game.Effects.Landing(body.position + Vector2.down * 0.8f);
                    recoveryUntil = clock + 1.25f;
                    break;
                case AttackPattern.PatrolVolley:
                    FireShot(lockedOrigin, lockedAim, 8, damage); burstRemaining = 2; burstAt = clock + 0.36f;
                    break;
                case AttackPattern.LaneVolley:
                    FireShot(lockedOrigin, lockedAim, 10, damage); burstRemaining = 1; burstAt = clock + 0.36f;
                    break;
            }
            game.Effects.Encounter(body.position, 2, 1.2f, 0.25f);
        }

        private void StartCharge(Vector2 velocity, float duration)
        {
            dashVelocity = velocity; dashUntil = clock + duration; ChargesResolved++;
            if (pattern == AttackPattern.Leap) body.linearVelocity = velocity;
            else body.linearVelocity = new Vector2(velocity.x, flying ? velocity.y : body.linearVelocity.y);
        }

        private void FireCannon()
        {
            FireShot(lockedOrigin, lockedAim, 9, damage);
            if (enemy.Archetype == EnemyArchetype.FactoryGunner) FireShot(lockedOrigin + Vector2.up * 0.32f, lockedAim, 9, damage);
        }

        private void FireShot(Vector2 origin, Vector2 heading, float velocity, float hitDamage, float blastRadius = 0)
        {
            game.Projectiles.Fire(origin + heading * 0.65f, heading, velocity, hitDamage, Team.Enemy, 1, game.Level.Theme.danger, false, 0, blastRadius);
        }

        private void ResolveLaser()
        {
            float length = 20;
            float playerDistance = float.PositiveInfinity;
            int count = Physics2D.Raycast(lockedOrigin, lockedAim, solidFilter, rayHits, length);
            for (int hitIndex = 0; hitIndex < count; hitIndex++)
            {
                Collider2D collider = rayHits[hitIndex].collider;
                if (collider == null || collider.attachedRigidbody == body) continue;
                if (collider.GetComponentInParent<PlayerController>() != null) { playerDistance = Mathf.Min(playerDistance, rayHits[hitIndex].distance); continue; }
                Health health = collider.GetComponentInParent<Health>();
                if (health != null && health.Team == Team.Enemy) continue;
                length = Mathf.Min(length, rayHits[hitIndex].distance);
            }
            SetLine(lockedOrigin, lockedAim, length, 0.13f, new Color(1.7f, 0.3f, 0.2f, 0.95f));
            if (!laserDamaged && playerDistance <= length && game.Player.Health.TakeDamage(damage * 1.5f, Team.Enemy))
            {
                laserDamaged = true; LaserHits++;
            }
        }

        private float AttackRange()
        {
            switch (pattern)
            {
                case AttackPattern.Melee: return 2.3f;
                case AttackPattern.Leap: return 6.5f;
                case AttackPattern.Dash: return 9;
                case AttackPattern.GroundSlam: return 12;
                default: return 19;
            }
        }

        private bool Grounded()
        {
            if (Mathf.Abs(body.linearVelocity.y) > 0.3f) return false;
            int count = Physics2D.Raycast(body.position, Vector2.down, solidFilter, rayHits, 0.95f);
            for (int hitIndex = 0; hitIndex < count; hitIndex++)
                if (rayHits[hitIndex].collider != null && rayHits[hitIndex].collider.attachedRigidbody != body
                    && rayHits[hitIndex].normal.y > 0.5f && rayHits[hitIndex].collider.GetComponent<PlatformSurface>() != null) return true;
            return false;
        }

        private void SetLine(Vector2 origin, Vector2 heading, float length, float width, Color color)
        {
            sight.enabled = true; sight.color = color;
            sight.transform.position = origin + heading * length * 0.5f;
            sight.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(heading.y, heading.x) * Mathf.Rad2Deg);
            sight.transform.localScale = new Vector3(length, width, 1);
        }

        private void SetShield(bool raised)
        {
            ShieldRaised = raised;
            enemy.Health.DamageReduction = raised ? 0.72f : 0;
            if (shield != null) shield.enabled = raised;
        }

        private void UpdateRoleVisuals(float facing)
        {
            if (shield != null) shield.transform.localPosition = new Vector3(facing * 0.52f, 0, 0);
            if (cannonBarrels == null) return;
            for (int barrelIndex = 0; barrelIndex < cannonBarrels.Length; barrelIndex++)
                cannonBarrels[barrelIndex].transform.localPosition = new Vector3(facing * 0.42f, -0.32f + barrelIndex * 0.32f, 0);
        }

        private void StopHorizontal() { body.linearVelocity = new Vector2(0, flying ? 0 : body.linearVelocity.y); }
        private static float Direction(float horizontal) => horizontal < 0 ? -1 : 1;

        private void OnDisable()
        {
            if (enemy != null && enemy.Health != null) enemy.Health.DamageReduction = 0;
            if (sight != null) sight.enabled = false;
            resolveAt = dashUntil = laserUntil = 0; burstRemaining = 0;
        }
    }
}
