using System;
using UnityEngine;

namespace MoriEchoes
{
    public sealed class Projectile : MonoBehaviour
    {
        private GameManager game;
        private SpriteRenderer visual;
        private readonly RaycastHit2D[] hits = new RaycastHit2D[128];
        private ContactFilter2D filter;
        private readonly Health[] hitTargets = new Health[64];
        private int hitCount;
        private int targetsRemaining;
        private Vector2 direction;
        private float speed;
        private float damage;
        private float lifetime;
        private Team team;
        private IncomingHitKind incomingHit;
        private Color tint;
        private bool critical;
        private float homing;
        private float explosionRadius;
        private bool encounterVisual;
        private float trailAt;
        private readonly Collider2D[] blastHits = new Collider2D[96];
        private static readonly HitComparer Comparer = new HitComparer();

        private sealed class HitComparer : System.Collections.Generic.IComparer<RaycastHit2D>
        {
            public int Compare(RaycastHit2D first, RaycastHit2D second) => first.distance.CompareTo(second.distance);
        }

        public void Initialize(GameManager owner)
        {
            game = owner;
            filter = new ContactFilter2D { useTriggers = false };
            visual = game.Art.Renderer("Tracer", transform, game.Art.Glow, Color.white, new Vector2(0.5f, 0.12f), 500);
        }

        public void Launch(Vector2 origin, Vector2 heading, float velocity, float hitDamage, Team source,
            int targets, Color color, bool isCritical, float turnRate = 0, float blastRadius = 0,
            IncomingHitKind hitKind = IncomingHitKind.Enemy)
        {
            transform.position = origin;
            direction = heading.normalized;
            transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
            speed = velocity * (source == Team.Player && game.Level != null ? game.Level.SlowProjectiles : 1);
            damage = hitDamage; team = source; tint = color; critical = isCritical;
            incomingHit = hitKind;
            homing = turnRate;
            explosionRadius = blastRadius;
            Array.Clear(hitTargets, 0, hitCount);
            lifetime = 2.8f; hitCount = 0; targetsRemaining = Mathf.Clamp(targets, 1, hitTargets.Length);
            visual.sprite = game.Art.Glow;
            encounterVisual = source == Team.Enemy && game.Config.sideScroller;
            trailAt = Time.time;
            visual.color = color;
            visual.transform.localScale = new Vector3(source == Team.Player ? 0.6f : 0.35f, isCritical ? 0.19f : 0.13f, 1);
            if (explosionRadius > 0) visual.transform.localScale = new Vector3(0.65f, 0.28f, 1);
            Sprite effect = encounterVisual ? game.Effects.EncounterSprite(game.SelectedStage, 0) : null;
            if (effect != null)
            {
                visual.sprite = effect; visual.color = Color.white;
                visual.transform.localScale = Vector3.one * ((homing > 0 ? 1.05f : 0.8f) / effect.bounds.size.x);
            }
            gameObject.SetActive(true);
        }

        private void FixedUpdate()
        {
            if (!game.IsPlaying) return;
            if (encounterVisual && Time.time >= trailAt)
            {
                trailAt = Time.time + 0.08f;
                if (game.Level != null && game.Level.Swim != null)
                    game.Effects.WaterTrail(transform.position, -direction, tint);
                else game.Effects.Encounter(transform.position, 0, 0.65f, 0.18f, transform.eulerAngles.z, false, true);
            }
            if (homing > 0 && game.Player != null)
            {
                Vector2 desired = ((Vector2)game.Player.transform.position - (Vector2)transform.position).normalized;
                direction = Vector3.RotateTowards(direction, desired, homing * Time.fixedDeltaTime, 0);
                transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
            }
            lifetime -= Time.fixedDeltaTime;
            if (lifetime <= 0) { Retire(transform.position); return; }
            Vector2 origin = transform.position;
            float distance = speed * Time.fixedDeltaTime;
            int count = Physics2D.CircleCast(origin, 0.055f, direction, filter, hits, distance);
            Array.Sort(hits, 0, count, Comparer);
            for (int index = 0; index < count; index++)
            {
                Collider2D collider = hits[index].collider;
                if (collider == null || collider.isTrigger) continue;
                Health health = collider.GetComponentInParent<Health>();
                if (health == null)
                {
                    // Match one-way platform physics: shots fired upward pass through from below.
                    PlatformSurface platform = collider.GetComponent<PlatformSurface>();
                    if (platform != null && platform.OneWay && direction.y > 0.05f) continue;
                    game.Effects.SurfaceImpact(hits[index].point, tint);
                    Retire(hits[index].point);
                    return;
                }
                if (health.Team == team || health.IsDead || AlreadyHit(health)) continue;
                if (explosionRadius > 0) { Retire(hits[index].point); return; }
                hitTargets[hitCount++] = health;
                if (health.TakeDamage(damage, team, false, incomingHit))
                {
                    game.Effects.Burst(hits[index].point, tint, critical ? 8 : 4);
                    game.Effects.DamageNumber(health.transform.position, health.LastDamageTaken, critical);
                }
                targetsRemaining--;
                if (targetsRemaining <= 0 || hitCount >= hitTargets.Length)
                {
                    if (encounterVisual) game.Effects.Encounter(hits[index].point, 2, 0.9f, 0.24f);
                    gameObject.SetActive(false);
                    return;
                }
            }
            transform.position = origin + direction * distance;
        }

        private void Retire(Vector2 point)
        {
            gameObject.SetActive(false);
            if (encounterVisual) game.Effects.Encounter(point, 2, 0.8f, 0.2f);
            if (explosionRadius <= 0) return;
            game.Effects.Explosion(point, explosionRadius, tint);
            int count = Physics2D.OverlapCircle(point, explosionRadius, filter, blastHits);
            for (int index = 0; index < count; index++)
            {
                Health target = blastHits[index].GetComponentInParent<Health>();
                if (target == null || target.Team == team || target.IsDead || AlreadyHit(target) || hitCount >= hitTargets.Length) continue;
                hitTargets[hitCount++] = target;
                float distance = Vector2.Distance(point, blastHits[index].ClosestPoint(point));
                float amount = damage * Mathf.Lerp(1, 0.4f, Mathf.Clamp01(distance / explosionRadius));
                if (target.TakeDamage(amount, team, false, incomingHit)) game.Effects.DamageNumber(target.transform.position, target.LastDamageTaken, critical);
            }
        }

        private bool AlreadyHit(Health target)
        {
            for (int index = 0; index < hitCount; index++) if (hitTargets[index] == target) return true;
            return false;
        }
    }
}
