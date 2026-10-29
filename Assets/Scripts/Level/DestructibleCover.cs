using System;
using UnityEngine;

namespace MoriEchoes
{
    [DisallowMultipleComponent]
    public sealed class DestructibleCover : MonoBehaviour
    {
        public Health Health { get; private set; }
        public bool Explosive { get; private set; }
        public bool IsDestroyed { get; private set; }
        public int SegmentIndex { get; private set; }
        public int TimesDestroyed { get; private set; }
        public float ExplosionRadius => Explosive ? 4 : 0;
        public float ExplosionDamage => Explosive ? 90 : 0;
        public Bounds BlockingBounds => new Bounds(transform.position, new Vector3(dimensions.x, dimensions.y, 1));
        public event Action<DestructibleCover> Destroyed;
        private GameManager game;
        private EncounterHazardDirector director;
        private BoxCollider2D solid;
        private BoxCollider2D target;
        private SpriteRenderer visual;
        private Vector2 dimensions;
        private float maximumHealth;
        private Color originalColor;

        public void Initialize(GameManager owner, EncounterHazardDirector encounterDirector, int segmentIndex, Vector2 size,
            float hitPoints = 42, bool explosive = false, Sprite sprite = null)
        {
            if (owner == null) throw new ArgumentNullException(nameof(owner));
            if (Health != null) throw new InvalidOperationException("Cover is already initialized; use ResetForCheckpoint to restore it.");
            game = owner; director = encounterDirector; SegmentIndex = segmentIndex; Explosive = explosive;
            dimensions = new Vector2(Mathf.Clamp(size.x, 0.4f, 3), Mathf.Clamp(size.y, 0.5f, 2.2f));
            maximumHealth = Mathf.Max(1, hitPoints);
            solid = gameObject.AddComponent<BoxCollider2D>(); solid.size = dimensions;
            gameObject.AddComponent<PlatformSurface>();
            var hitShell = new GameObject("DestructibleHitShell_PlayerBullets");
            hitShell.transform.SetParent(transform, false);
            Health = hitShell.AddComponent<Health>(); Health.Initialize(Team.Enemy, maximumHealth);
            target = hitShell.AddComponent<BoxCollider2D>(); target.size = dimensions + Vector2.one * 0.16f;
            hitShell.AddComponent<PlatformSurface>();
            Health.Damaged += OnDamaged; Health.Died += OnDied;
            originalColor = explosive ? game.Level.Theme.danger : Color.Lerp(game.Level.Theme.structure, game.Level.Theme.accent, 0.35f) * 1.7f;
            Sprite artwork = sprite != null ? sprite : game.Art.Square;
            visual = game.Art.Renderer(explosive ? "ExplosiveBarrelBody" : "BreakableCoverBody", transform, artwork,
                sprite != null ? Color.white : originalColor, new Vector2(dimensions.x / artwork.bounds.size.x, dimensions.y / artwork.bounds.size.y), 42);
            originalColor = visual.color;
        }

        public bool ApplyDamage(float amount)
        {
            if (game == null || !game.IsPlaying || IsDestroyed || Health == null) return false;
            return Health.TakeDamage(amount, Team.Player);
        }

        private void OnDamaged(float amount)
        {
            if (visual == null) return;
            float fraction = Health.Current / Health.Maximum;
            visual.color = Color.Lerp(new Color(0.32f, 0.28f, 0.25f), originalColor, fraction);
            if (game != null && game.IsPlaying) game.Effects.SurfaceImpact(transform.position, game.Level.Theme.accent);
        }

        private void OnDied()
        {
            if (IsDestroyed) return;
            IsDestroyed = true; TimesDestroyed++;
            solid.enabled = target.enabled = false;
            if (game != null && game.IsPlaying) game.Effects.Burst(transform.position, game.Level.Theme.accent, 8);
            director?.NotifyDestroyed(this);
            Destroyed?.Invoke(this);
            gameObject.SetActive(false);
        }

        public void ResetForCheckpoint()
        {
            if (Health == null) return;
            IsDestroyed = false;
            Health.Initialize(Team.Enemy, maximumHealth);
            solid.enabled = target.enabled = true;
            visual.color = originalColor;
            gameObject.SetActive(true);
        }

        private void OnDestroy()
        {
            if (Health == null) return;
            Health.Damaged -= OnDamaged; Health.Died -= OnDied;
        }
    }
}
