using UnityEngine;

namespace MoriEchoes
{
    public sealed class ProjectilePool : MonoBehaviour
    {
        private Projectile[] items;
        private int cursor;
        public int Capacity => items != null ? items.Length : 0;
        public int RejectedShots { get; private set; }
        public int FreeCount
        {
            get
            {
                int count = 0;
                foreach (Projectile item in items) if (!item.gameObject.activeSelf) count++;
                return count;
            }
        }

        public void Initialize(GameManager game)
        {
            items = new Projectile[game.Config.maximumProjectiles];
            for (int index = 0; index < items.Length; index++)
            {
                var entity = new GameObject("Projectile_" + index);
                entity.transform.SetParent(transform, false);
                var projectile = entity.AddComponent<Projectile>();
                projectile.Initialize(game);
                items[index] = projectile;
                entity.SetActive(false);
            }
        }

        public bool Fire(Vector2 position, Vector2 direction, float speed, float damage, Team team,
            int targets, Color tint, bool critical = false, float homing = 0, float blastRadius = 0,
            IncomingHitKind hitKind = IncomingHitKind.Enemy)
        {
            for (int offset = 0; offset < items.Length; offset++)
            {
                int index = (cursor + offset) % items.Length;
                if (items[index].gameObject.activeSelf) continue;
                cursor = (index + 1) % items.Length;
                items[index].Launch(position, direction, speed, damage, team, targets, tint, critical, homing, blastRadius, hitKind);
                return true;
            }
            RejectedShots++;
            return false;
        }

        public void ClearAll()
        {
            foreach (Projectile item in items) item.gameObject.SetActive(false);
        }
    }
}
