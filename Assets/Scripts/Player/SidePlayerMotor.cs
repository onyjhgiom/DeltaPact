using UnityEngine;

namespace MoriEchoes
{
    public sealed class SidePlayerMotor : MonoBehaviour
    {
        public bool Grounded { get; private set; }
        public PlatformSurface StandingSurface => Grounded ? surface : null;
        public bool CinematicLocked { get; private set; }
        public bool Crouching { get; private set; }
        public bool IsDodging => dashUntil > Time.time;
        public float DodgeRemaining => Mathf.Max(0, nextDash - Time.time);
        public float PulseRemaining => player != null && player.Skill != null ? player.Skill.Remaining : 0;
        public Vector2 Aim { get; private set; } = Vector2.right;
        public Vector2 MuzzleOrigin => (Vector2)transform.position + Vector2.up * (Crouching ? -0.23f : 0.25f);
        public int JumpCount { get; private set; }
        public bool BufferedJumpUsed { get; private set; }
        public bool CoyoteJumpUsed { get; private set; }
        private GameManager game;
        private PlayerController player;
        private Rigidbody2D body;
        private CapsuleCollider2D capsule;
        private readonly RaycastHit2D[] hits = new RaycastHit2D[12];
        private readonly Collider2D[] ignored = new Collider2D[12];
        private readonly RaycastHit2D[] blinkHits = new RaycastHit2D[64];
        private readonly Collider2D[] blinkOverlaps = new Collider2D[64];
        private int ignoredCount;
        private float restoreCollisionAt;
        private float lastGround = -10;
        private float bufferedAt = -10;
        private float nextDash;
        private float dashUntil;
        private Vector2 dashDirection;
        private float nextGhost;
        private float nextBubble;
        private bool surfaceLeaping;
        private float frozenUntil;
        private bool airJumpUsed;
        private bool jumpCutApplied = true;
        private float baseGravity;
        private float gravityInvertedUntil;
        private bool rookImpact;
        private float horizontal;
        private PlatformSurface surface;
        private PhysicsMaterial2D material;

        public void Initialize(GameManager owner, PlayerController controller, Rigidbody2D rigidbody)
        {
            game = owner; player = controller; body = rigidbody;
            baseGravity = game.Config.gravityScale;
            GetComponent<CircleCollider2D>().enabled = false;
            body.gravityScale = game.Config.gravityScale;
            capsule = gameObject.AddComponent<CapsuleCollider2D>();
            capsule.size = new Vector2(0.65f, 1.6f);
            capsule.direction = CapsuleDirection2D.Vertical;
            material = new PhysicsMaterial2D("SidePlayer_NoWallStick") { friction = 0, bounciness = 0 };
            capsule.sharedMaterial = material;
            var feet = new GameObject("FeetSensor", typeof(BoxCollider2D));
            feet.transform.SetParent(transform, false);
            feet.transform.localPosition = new Vector3(0, -0.8f, 0);
            var sensor = feet.GetComponent<BoxCollider2D>();
            sensor.isTrigger = true; sensor.size = new Vector2(0.5f, 0.08f);
        }

        public void Capture()
        {
            if (CinematicLocked) { horizontal = 0; return; }
            if (game == null || !game.IsPlaying || player.Health.IsDead) { horizontal = 0; return; }
            InputReader input = game.Input;
            Vector2 origin = MuzzleOrigin;
            if (input.UsingGamepad && input.AimStick.sqrMagnitude > 0.04f) Aim = input.AimStick.normalized;
            else if (!input.UsingGamepad && input.HasPointer && game.CameraRig.GroundPoint(input.Pointer, out Vector2 point))
            {
                if ((point - origin).sqrMagnitude > 0.03f) Aim = (point - origin).normalized;
            }
            else if (Mathf.Abs(input.Move.x) > 0.1f) Aim = new Vector2(Mathf.Sign(input.Move.x), 0);
            if (input.UsingGamepad && input.AimStick.sqrMagnitude < 0.04f && Mathf.Abs(input.Move.y) > 0.4f)
                Aim = new Vector2(Aim.x >= 0 ? 1 : -1, Mathf.Sign(input.Move.y)).normalized;
            SetCrouching(input.Crouch && Grounded && !IsDodging);
            if (Crouching) Aim = new Vector2(Aim.x >= 0 ? 1 : -1, 0);
            horizontal = game.ControlsReady ? input.Move.x : 0;
            if (!game.ControlsReady || Time.time < frozenUntil) { horizontal = 0; return; }
            if (input.Jump) bufferedAt = Time.time;
            if (!input.JumpHeld && !jumpCutApplied && !Grounded && !IsDodging && body.linearVelocity.y > 0)
            {
                body.linearVelocity = new Vector2(body.linearVelocity.x, body.linearVelocity.y * game.Config.jumpCutMultiplier);
                jumpCutApplied = true;
            }
            if (input.Dodge && Time.time >= nextDash)
            {
                Vector2 direction = input.Move;
                if (Grounded && direction.y < 0) direction.y = 0;
                dashDirection = direction.sqrMagnitude > 0.04f ? direction.normalized : new Vector2(Aim.x >= 0 ? 1 : -1, 0);
                bool rook = game.CharacterId == 11;
                dashUntil = Time.time + game.Config.dodgeDuration * (rook ? 1.7f : game.CharacterId == 9 && !Grounded ? 1.45f : 1);
                nextDash = Time.time + game.Config.dodgeCooldown * game.Stats.DodgeCooldownMultiplier + game.Character.dodgeExtra;
                rookImpact = rook;
                SetCrouching(false);
                jumpCutApplied = true;
                if (dashDirection.y > 0.1f) { Grounded = false; lastGround = -10; surface = null; }
                player.Health.Invulnerable = true;
                game.Audio.Play(SoundKind.Dodge);
            }
            if (input.Skill) player.Skill.TryActivate();
            if (input.TechniqueOne) player.Techniques.TryActivate(0);
            if (input.TechniqueTwo) player.Techniques.TryActivate(1);
            if (input.Ultimate) player.Energy.TryActivateUltimate();
            if (game.CharacterId == 11 && input.Jump && Grounded) PlantJumpMine();
        }

        public void Step()
        {
            if (CinematicLocked) { body.linearVelocity = Vector2.zero; body.gravityScale = 0; return; }
            if (game == null || !game.IsPlaying || player.Health.IsDead) { body.linearVelocity = Vector2.zero; return; }
            SwimStageDirector swim = game.Level != null ? game.Level.Swim : null;
            if (swim != null) { StepSwim(swim); return; }
            if (Time.time >= restoreCollisionAt) RestoreCollisions();
            body.gravityScale = Time.time < gravityInvertedUntil ? -baseGravity * 0.72f : baseGravity;
            bool wasGrounded = Grounded;
            Grounded = false; surface = null;
            int count = capsule.Cast(Vector2.down, new ContactFilter2D { useTriggers = false }, hits, 0.1f);
            for (int index = 0; index < count; index++)
            {
                PlatformSurface candidate = hits[index].collider.GetComponent<PlatformSurface>();
                if (candidate == null || hits[index].normal.y < 0.5f || body.linearVelocity.y - candidate.Velocity.y > 0.2f
                    || Physics2D.GetIgnoreCollision(capsule, hits[index].collider)) continue;
                Grounded = true; surface = candidate; break;
            }
            if (Grounded)
            {
                lastGround = Time.time; airJumpUsed = false; jumpCutApplied = true;
                if (!wasGrounded) game.Effects.Burst((Vector2)transform.position + Vector2.down * 0.8f, game.Level.Theme.accent, 5);
            }
            SetCrouching(game.Input.Crouch && Grounded && !IsDodging);
            if (Time.time - bufferedAt <= game.Config.jumpBuffer)
            {
                if (Grounded && game.Input.Crouch && surface != null && surface.OneWay) DropThrough();
                else if (Time.time - lastGround <= game.Config.coyoteTime)
                {
                    CoyoteJumpUsed |= !Grounded;
                    BufferedJumpUsed |= Time.time - bufferedAt > 0.025f;
                    Jump();
                }
                else if (!airJumpUsed && !LandingImminent())
                { airJumpUsed = true; Jump(); }
            }
            if (IsDodging)
            {
                Vector2 velocity = dashDirection * game.Config.moveSpeed * game.Config.dodgeSpeedMultiplier;
                if (Grounded && dashDirection.y <= 0) velocity.y = surface.Velocity.y;
                else { Grounded = false; lastGround = -10; surface = null; }
                body.linearVelocity = velocity;
                if (Time.time >= nextGhost) { nextGhost = Time.time + 0.04f; game.Effects.Ghost(transform.position); }
                return;
            }
            if (dashUntil > 0)
            {
                dashUntil = 0; player.Health.Invulnerable = false;
                if (rookImpact && game.IsPlaying) { rookImpact = false; game.Pulse(transform.position, 2.8f, 38); }
                body.linearVelocity = new Vector2(body.linearVelocity.x, Mathf.Min(body.linearVelocity.y, game.Config.jumpVelocity));
            }
            float speed = game.Config.moveSpeed * game.Character.speed * game.Stats.MovementMultiplier
                * (player.Skill != null ? player.Skill.SpeedMultiplier : 1)
                * (game.CameraRig.IsAds ? game.Config.adsMoveMultiplier : 1) * (Crouching ? 0.45f : 1);
            float desired = Time.time < frozenUntil ? 0 : horizontal * speed;
            float acceleration = surface != null && surface.Ice ? 48 * game.Config.iceFrictionFactor : Grounded ? 70 : 38;
            float conveyor = surface != null ? surface.ConveyorSpeed : 0;
            float vertical = Grounded ? surface.Velocity.y : Mathf.Max(body.linearVelocity.y, -22);
            body.linearVelocity = new Vector2(Mathf.MoveTowards(body.linearVelocity.x, desired + conveyor, acceleration * Time.fixedDeltaTime), vertical);
            float deathFloor = game.Level != null && game.Level.Vertical != null
                ? (game.Level.Vertical.Ascending ? game.Level.Vertical.StartPoint.y : -game.Level.Vertical.DepthLength) - 8 : -7;
            if (transform.position.y < deathFloor) player.Health.TakeDamage(10000, Team.Enemy, true);
        }

        // 游泳模式：海面贴水面游泳向前（波浪起伏+跃出水面）；水下上下浮沉 + 前进 + 攻击。
        private void StepSwim(SwimStageDirector swim)
        {
            if (Time.time >= restoreCollisionAt) RestoreCollisions();
            body.gravityScale = 0;
            bool wasOnDeck = Grounded && surface != null && surface.Buoyant;
            Grounded = false; surface = null;
            SetCrouching(false);
            float dt = Time.fixedDeltaTime;
            float speed = game.Config.moveSpeed * game.Character.speed * game.Stats.MovementMultiplier
                * (player.Skill != null ? player.Skill.SpeedMultiplier : 1)
                * (game.CameraRig.IsAds ? game.Config.adsMoveMultiplier : 1) * 0.94f;
            if (IsDodging)
            {
                body.linearVelocity = dashDirection * game.Config.moveSpeed * game.Config.dodgeSpeedMultiplier;
                if (Time.time >= nextGhost) { nextGhost = Time.time + 0.05f; game.Effects.Ghost(transform.position); }
                ClampSwim(swim);
                return;
            }
            if (dashUntil > 0) { dashUntil = 0; player.Health.Invulnerable = false; }
            float verticalInput = game.ControlsReady && Time.time >= frozenUntil ? game.Input.Move.y : 0;
            if (swim.Underwater)
            {
                // 越深阻力越大：有输入时渐进加速，松开输入时快速消除漂移。
                float depth = swim.NormalizedDepth(body.position.y);
                Vector2 input = Vector2.ClampMagnitude(new Vector2(horizontal, verticalInput), 1);
                float thrust = Mathf.SmoothStep(0, 1, input.magnitude);
                Vector2 target = input.normalized * thrust * speed * Mathf.Lerp(1f, 0.78f, depth);
                float response = input.sqrMagnitude > 0.01f ? Mathf.Lerp(4.8f, 2.7f, depth) : Mathf.Lerp(5.5f, 9f, depth);
                body.linearVelocity = Vector2.Lerp(body.linearVelocity, target, 1 - Mathf.Exp(-response * dt));
                if (Time.time >= nextBubble)
                { nextBubble = Time.time + 0.24f; game.Effects.WaterTrail(transform.position, -body.linearVelocity.normalized, game.Level.Theme.accent); }
            }
            else
            {
                int count = capsule.Cast(Vector2.down, new ContactFilter2D { useTriggers = false }, hits, 0.12f);
                for (int index = 0; index < count; index++)
                {
                    PlatformSurface deck = hits[index].collider.GetComponent<PlatformSurface>();
                    if (deck == null || !deck.Buoyant || hits[index].normal.y < 0.5f
                        || body.linearVelocity.y - deck.Velocity.y > 0.3f
                        || Physics2D.GetIgnoreCollision(capsule, hits[index].collider)) continue;
                    Grounded = true; surface = deck; surfaceLeaping = false; lastGround = Time.time;
                    airJumpUsed = false; jumpCutApplied = true;
                    if (!wasOnDeck) game.Effects.WaterMuzzle(body.position + Vector2.down * 0.8f, Vector2.up, Color.white);
                    break;
                }
                if (Grounded)
                {
                    if (Time.time - bufferedAt <= game.Config.jumpBuffer)
                    {
                        if (verticalInput < -0.4f) DropThrough();
                        else { Jump(); surfaceLeaping = true; }
                    }
                    else body.linearVelocity = new Vector2(Mathf.MoveTowards(body.linearVelocity.x, horizontal * speed, 70 * dt), surface.Velocity.y);
                    ClampSwim(swim);
                    return;
                }
                // Walking off a deck enters an airborne arc before returning to wave swimming.
                if (wasOnDeck) surfaceLeaping = true;
                float wave = swim.SurfaceHeightAt(body.position.x);
                bool nearSurface = Mathf.Abs(body.position.y - wave) < 0.85f;
                if (Time.time - bufferedAt <= game.Config.jumpBuffer && nearSurface && !surfaceLeaping)
                {
                    bufferedAt = -10;
                    surfaceLeaping = true; JumpCount++; jumpCutApplied = true;
                    body.linearVelocity = new Vector2(horizontal * speed, game.Config.jumpVelocity * 0.82f);
                    game.Effects.WaterMuzzle(transform.position, Vector2.up, Color.white);
                    game.Audio.Play(SoundKind.Dodge);
                }
                else if (surfaceLeaping || body.position.y > wave + 0.5f)
                {
                    body.linearVelocity = new Vector2(horizontal * speed, body.linearVelocity.y - 26 * dt);
                    if (body.linearVelocity.y < 0 && body.position.y <= wave)
                    { surfaceLeaping = false; game.Effects.WaterMuzzle(body.position, Vector2.up, Color.white); }
                }
                else
                {
                    float targetY = wave + Mathf.Min(0, verticalInput) * 1.85f;
                    body.linearVelocity = new Vector2(Mathf.MoveTowards(body.linearVelocity.x, horizontal * speed, 38 * dt),
                        Mathf.Clamp((targetY - body.position.y) * 5.5f, -6, 6));
                }
            }
            ClampSwim(swim);
        }

        private void ClampSwim(SwimStageDirector swim)
        {
            Vector2 p = body.position;
            float clampedY = Mathf.Clamp(p.y, swim.MinY, swim.MaxY);
            float clampedX = Mathf.Clamp(p.x, 1, swim.CourseLength - 2);
            if (clampedX != p.x) body.position = new Vector2(clampedX, p.y);
            if (clampedY != p.y)
            {
                body.position = new Vector2(clampedX, clampedY);
                body.linearVelocity = new Vector2(body.linearVelocity.x, 0);
            }
        }

        private void Jump()
        {
            float lift = Grounded && surface != null ? Mathf.Max(0, surface.Velocity.y) : 0;
            if (dashUntil > 0) { dashUntil = 0; player.Health.Invulnerable = false; }
            body.linearVelocity = new Vector2(body.linearVelocity.x, game.Config.jumpVelocity + lift);
            bufferedAt = lastGround = -10; Grounded = false; JumpCount++;
            SetCrouching(false); surface = null;
            jumpCutApplied = false;
            game.Effects.Burst((Vector2)transform.position + Vector2.down * 0.8f, game.Level.Theme.accent, 6);
        }

        private bool LandingImminent()
        {
            float remaining = game.Config.jumpBuffer - (Time.time - bufferedAt);
            if (body.linearVelocity.y > 0 || remaining <= 0) return false;
            float distance = -body.linearVelocity.y * remaining + Mathf.Abs(Physics2D.gravity.y) * body.gravityScale * remaining * remaining * 0.5f;
            int count = capsule.Cast(Vector2.down, new ContactFilter2D { useTriggers = false }, hits, distance + 0.05f);
            for (int index = 0; index < count; index++)
                if (hits[index].normal.y >= 0.5f && hits[index].collider.GetComponent<PlatformSurface>() != null
                    && !Physics2D.GetIgnoreCollision(capsule, hits[index].collider)) return true;
            return false;
        }

        private void SetCrouching(bool crouching)
        {
            if (Crouching == crouching) return;
            Crouching = crouching;
            capsule.size = new Vector2(0.65f, crouching ? 1f : 1.6f);
            capsule.offset = new Vector2(0, crouching ? -0.3f : 0);
        }

        private void DropThrough()
        {
            var platform = surface.GetComponent<Collider2D>();
            if (ignoredCount < ignored.Length)
            { ignored[ignoredCount++] = platform; Physics2D.IgnoreCollision(capsule, platform, true); }
            restoreCollisionAt = Time.time + 0.35f;
            bufferedAt = lastGround = -10;
            body.position += Vector2.down * 0.15f;
            body.linearVelocity = new Vector2(body.linearVelocity.x, -3);
            Grounded = false; surface = null; SetCrouching(false);
        }

        private void RestoreCollisions()
        {
            for (int index = 0; index < ignoredCount; index++)
                if (ignored[index] != null) Physics2D.IgnoreCollision(capsule, ignored[index], false);
            ignoredCount = 0;
        }

        public void Freeze(float seconds) { frozenUntil = Time.time + seconds; }
        public void InvertGravity(float seconds) { gravityInvertedUntil = Mathf.Max(gravityInvertedUntil, Time.time + Mathf.Max(0.1f, seconds)); }

        private void PlantJumpMine()
        {
            var entity = new GameObject("ROOK_JumpMine");
            entity.transform.position = (Vector2)transform.position + Vector2.down * 0.78f;
            entity.AddComponent<DelayedBlastMine>().Initialize(game, 0.72f, 2.8f, 48 * game.CombatDamageMultiplier, game.Character.accent);
            game.Effects.Burst(entity.transform.position, game.Character.accent, 5);
        }
        public bool TryBlinkDestination(Vector2 heading, float distance, out Vector2 destination)
        {
            Vector2 origin = body.position;
            heading = heading.normalized;
            var filter = new ContactFilter2D { useTriggers = false };
            Physics2D.SyncTransforms();
            int count = capsule.Cast(heading, filter, blinkHits, distance);
            if (count == blinkHits.Length) distance = 0;
            for (int index = 0; index < count; index++)
            {
                RaycastHit2D hit = blinkHits[index];
                if (hit.collider.GetComponentInParent<Health>() != null) continue;
                if (Vector2.Dot(hit.normal, heading) >= -0.001f) continue;
                distance = Mathf.Min(distance, Mathf.Max(0, hit.distance - 0.08f));
            }
            while (distance >= 0.25f)
            {
                destination = origin + heading * distance;
                int overlaps = Physics2D.OverlapCapsule(destination + capsule.offset, capsule.size * 0.96f,
                    capsule.direction, 0, filter, blinkOverlaps);
                bool blocked = overlaps == blinkOverlaps.Length;
                for (int index = 0; index < overlaps; index++)
                    if (blinkOverlaps[index].GetComponentInParent<Health>() == null) blocked = true;
                if (!blocked) return true;
                distance -= 0.25f;
            }
            destination = origin;
            return false;
        }

        public void TeleportTo(Vector2 point)
        {
            RestoreCollisions();
            body.position = point; body.linearVelocity = Vector2.zero;
            if (dashUntil > 0) player.Health.Invulnerable = false;
            dashUntil = restoreCollisionAt = frozenUntil = 0;
            bufferedAt = lastGround = -10; Grounded = false; surface = null;
            surfaceLeaping = false;
            jumpCutApplied = true; SetCrouching(false);
        }

        public void SetCinematicLocked(bool locked)
        {
            CinematicLocked = locked;
            if (body == null) return;
            if (locked) body.linearVelocity = Vector2.zero;
            body.gravityScale = locked ? 0 : Time.time < gravityInvertedUntil ? -baseGravity * 0.72f : baseGravity;
        }
        public void ResetAt(Vector2 point)
        {
            RestoreCollisions();
            body.position = point; body.linearVelocity = Vector2.zero;
            player.Skill?.ResetForCheckpoint();
            dashUntil = nextDash = restoreCollisionAt = frozenUntil = 0;
            gravityInvertedUntil = 0; body.gravityScale = baseGravity; rookImpact = false;
            bufferedAt = lastGround = -10; Grounded = false; airJumpUsed = false; surface = null;
            surfaceLeaping = false;
            jumpCutApplied = true; SetCrouching(false);
        }
        private void OnDestroy() { RestoreCollisions(); if (material != null) Destroy(material); }
    }
}
