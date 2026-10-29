using UnityEngine;

namespace MoriEchoes
{
    public sealed class SideSpriteAnimator : MonoBehaviour
    {
        public int CurrentFrame { get; private set; }
        public Vector2 VisualOffset => visual != null ? (Vector2)visual.transform.localPosition : Vector2.zero;
        private GameManager game;
        private Health health;
        private PlayerController player;
        private CombatPowerup powerup;
        private EnemyAI enemy;
        private Rigidbody2D body;
        private SideSpriteAnimation animationSet;
        private SideSpriteAnimation poweredAnimation;
        private SideSpriteAnimation displayedAnimation;
        private SpriteRenderer visual;
        private SpriteRenderer healthFill;
        private SpriteRenderer shadow;
        private float phase;
        private float flashUntil;
        private float build;
        private float footDepth;
        private bool powered;
        private Color poweredTint = Color.white;
        private readonly RaycastHit2D[] shadowHits = new RaycastHit2D[24];
        private bool wasGrounded = true;

        public void Initialize(GameManager owner, Health hitPoints, SideSpriteAnimation animation, EnemyKind kind)
        {
            game = owner; health = hitPoints; animationSet = animation;
            player = GetComponent<PlayerController>(); enemy = GetComponent<EnemyAI>(); body = GetComponent<Rigidbody2D>();
            powerup = player != null ? player.GetComponent<CombatPowerup>() : null;
            build = player != null ? game.Character.build : kind == EnemyKind.Boss ? 2.1f : kind == EnemyKind.Elite ? 1.15f : 1;
            visual = game.Art.Renderer("AnimeAnimatedSprite", transform, animation.Frame(0), Color.white, Vector2.one * build, 101);
            footDepth = kind == EnemyKind.Boss ? 1.55f : 0.8f;
            displayedAnimation = animation;
            visual.transform.localPosition = Vector2.up * (animation.Frame(0).bounds.size.y * (213f / 512) * build - footDepth);
            shadow = game.Art.Renderer("AnimeShadow", transform, game.Art.Disc, new Color(0, 0, 0, 0.4f), new Vector2(0.95f, 0.14f) * build, 95);
            shadow.transform.localPosition = Vector2.down * (footDepth - 0.04f);
            UpdateShadow();
            if (player == null && kind != EnemyKind.Boss)
            {
                healthFill = game.Art.Renderer("EnemyHealth", transform, game.Art.Square, game.Level.Theme.danger, new Vector2(0.65f, 0.045f), 500);
                healthFill.transform.localPosition = Vector2.up * 1.15f * build;
            }
            health.Damaged += OnDamage; health.Died += OnDeath;
        }

        private void OnDamage(float damage) { flashUntil = Time.time + (player != null ? 0.18f : 0.08f); }
        private void OnDeath() { game.Effects.Debris(transform.position, game.Level.Theme.accent); }

        public void SetPoweredAppearance(bool active, Color tint, SideSpriteAnimation animation = null)
        {
            powered = active; poweredTint = tint; poweredAnimation = animation;
        }

        private void UpdateShadow()
        {
            Vector2 feet = (Vector2)transform.position + Vector2.down * footDepth;
            Vector2 origin = feet + Vector2.up * 0.12f;
            int count = Physics2D.Raycast(origin, Vector2.down, new ContactFilter2D { useTriggers = false }, shadowHits, 32);
            float nearest = float.PositiveInfinity;
            int ground = -1;
            for (int index = 0; index < count; index++)
            {
                RaycastHit2D hit = shadowHits[index];
                if (hit.collider.GetComponent<PlatformSurface>() == null || hit.normal.y < 0.5f
                    || hit.collider.bounds.max.y > origin.y || hit.distance >= nearest) continue;
                nearest = hit.distance; ground = index;
            }
            shadow.enabled = ground >= 0;
            if (ground < 0) return;
            Vector2 point = shadowHits[ground].point;
            float height = Mathf.Clamp01(Mathf.Max(0, feet.y - point.y) / 8);
            shadow.transform.position = new Vector3(feet.x, point.y + 0.04f, transform.position.z);
            shadow.transform.localScale = new Vector3(0.95f, 0.14f, 1) * (build * Mathf.Lerp(1, 0.55f, height));
            shadow.color = new Color(0, 0, 0, Mathf.Lerp(0.4f, 0.08f, height));
        }

        private void LateUpdate()
        {
            if (game == null || body == null || health == null) return;
            if (powerup != null) SetPoweredAppearance(powerup.IsActive, powerup.Tint, powerup.Animation);
            UpdateShadow();
            SideSpriteAnimation activeAnimation = player != null && powered && poweredAnimation != null && poweredAnimation.IsReady ? poweredAnimation : animationSet;
            if (enemy != null && enemy.Kind != EnemyKind.Boss && game.Level.Theme.stationaryAnimation != null
                && game.Level.Theme.stationaryAnimation.IsReady && (enemy.Archetype == EnemyArchetype.ScrapTurret
                || enemy.Archetype == EnemyArchetype.FactoryGunner || enemy.Archetype == EnemyArchetype.CityLaser))
                activeAnimation = game.Level.Theme.stationaryAnimation;
            if (enemy != null && enemy.Kind == EnemyKind.Boss)
            {
                BossController controller = GetComponent<BossController>();
                SideSpriteAnimation form = game.Level.Theme.BossForm(controller != null
                    ? controller.Side != null ? controller.Side.Phase : controller.Vertical != null ? controller.Vertical.Phase : controller.Swim != null ? controller.Swim.Phase : 1 : 1);
                if (form != null && form.IsReady) activeAnimation = form;
            }
            if (activeAnimation != displayedAnimation)
            {
                displayedAnimation = activeAnimation;
                visual.transform.localPosition = Vector2.up * (activeAnimation.Frame(0).bounds.size.y * (213f / 512) * build - footDepth);
            }
            if (game.IsPlaying) phase += Time.deltaTime * activeAnimation.framesPerSecond * Mathf.Clamp(Mathf.Abs(body.linearVelocity.x) / 3, 0.65f, 1.6f);
            float facing = player != null ? player.Aim.x : game.Player != null ? game.Player.transform.position.x - transform.position.x : -1;
            if (Mathf.Abs(facing) > 0.01f) visual.flipX = (facing > 0) != activeAnimation.facesRight;
            CurrentFrame = 0;
            if (player != null)
            {
                if (player.Motor.Grounded && !wasGrounded && game.IsPlaying) game.Effects.Landing((Vector2)transform.position + Vector2.down * 0.75f);
                wasGrounded = player.Motor.Grounded;
                if (player.Motor.Crouching && player.Motor.Grounded) CurrentFrame = 7;
                else if (!player.Motor.Grounded) CurrentFrame = 6;
                else if (Mathf.Abs(body.linearVelocity.x) > 0.2f) CurrentFrame = 1 + (int)phase % 5;
            }
            else if (enemy != null && enemy.Kind == EnemyKind.Boss)
            {
                BossController controller = GetComponent<BossController>();
                SideBossController boss = controller != null ? controller.Side : null;
                VerticalBossController vertical = controller != null ? controller.Vertical : null;
                SwimBossController swim = controller != null ? controller.Swim : null;
                if (vertical != null) CurrentFrame = vertical.Transitioning ? 2 : vertical.Preparing ? 1 : 0;
                else if (swim != null) CurrentFrame = swim.Preparing ? 1 : 0;
                else if (boss != null) CurrentFrame = boss.Phase >= 3 ? 3 : boss.Transitioning ? 2 : boss.Attacking ? 1 : 0;
            }
            else if (enemy != null && enemy.GetComponent<SideEnemyMotor>().Attacking) CurrentFrame = 1;
            else if (Mathf.Abs(body.linearVelocity.x) > 0.2f || enemy != null && enemy.Kind == EnemyKind.Runner) CurrentFrame = (int)phase % 2;
            visual.sprite = activeAnimation.Frame(CurrentFrame);
            Color tint = player != null && powered ? poweredTint : player != null && game.AlternatePalette ? new Color(1.1f, 0.82f, 1.15f) : Color.white;
            visual.color = Time.time < flashUntil ? Color.white * 2 : tint;
            visual.enabled = player == null || game.CameraRig.Side.Mode != ViewMode.IronSight;
            visual.transform.localScale = Vector3.one * build;
            if (healthFill != null) healthFill.transform.localScale = new Vector3(0.65f * Mathf.Clamp01(health.Current / Mathf.Max(1, health.Maximum)), 0.045f, 1);
        }

        private void OnDestroy()
        {
            if (health != null) { health.Damaged -= OnDamage; health.Died -= OnDeath; }
        }
    }
}
