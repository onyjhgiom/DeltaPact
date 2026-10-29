using UnityEngine;

namespace MoriEchoes
{
    public sealed class DelayedBlastMine : MonoBehaviour
    {
        public static int SpawnedCount { get; private set; }
        private GameManager game;
        private float fuse;
        private float radius;
        private float damage;
        private Color tint;
        private SpriteRenderer ring;
        private bool detonated;

        public void Initialize(GameManager owner, float seconds, float blastRadius, float blastDamage, Color color)
        {
            game = owner; fuse = seconds; radius = blastRadius; damage = blastDamage; tint = color;
            ring = game.Art.Renderer("JumpMine_ArmedHalo", transform, game.Art.Ring, color, Vector2.one * 0.8f, 150);
            game.Art.Renderer("JumpMine_Core", transform, game.Art.Diamond, Color.white, Vector2.one * 0.27f, 151);
            SpawnedCount++;
        }

        private void Update()
        {
            if (game == null || !game.IsPlaying) { Destroy(gameObject); return; }
            fuse -= Time.deltaTime;
            if (ring != null)
            {
                float pulse = 0.75f + 0.25f * Mathf.Sin(Time.time * (fuse < 0.3f ? 32 : 9));
                ring.transform.localScale = Vector3.one * pulse;
            }
            if (fuse > 0) return;
            Detonate();
        }

        private void Detonate()
        {
            if (detonated) return;
            detonated = true;
            Vector2 center = transform.position;
            game.Effects.Explosion(center, radius, tint);
            foreach (EnemyAI enemy in game.Enemies.ToArray())
            {
                if (enemy == null || enemy.Health == null || enemy.Health.IsDead) continue;
                float distance = Vector2.Distance(center, enemy.transform.position);
                if (distance < radius) enemy.Health.TakeDamage(damage * Mathf.Lerp(1, 0.4f, distance / radius), Team.Player);
            }
            Destroy(gameObject);
        }
    }
}
