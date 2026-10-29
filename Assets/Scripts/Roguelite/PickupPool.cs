using UnityEngine;

namespace MoriEchoes
{
    public sealed class PickupPool : MonoBehaviour
    {
        private ExperiencePickup[] items;
        private GameManager game;

        public void Initialize(GameManager owner)
        {
            game = owner;
            items = new ExperiencePickup[game.Config.maximumPickups];
            for (int index = 0; index < items.Length; index++)
            {
                var entity = new GameObject("Experience_" + index);
                entity.transform.SetParent(transform, false);
                items[index] = entity.AddComponent<ExperiencePickup>();
                items[index].Initialize(game);
                entity.SetActive(false);
            }
        }

        public void Drop(Vector2 position, int amount)
        {
            ExperiencePickup nearest = null;
            float closest = float.MaxValue;
            foreach (ExperiencePickup item in items)
            {
                if (!item.gameObject.activeSelf) { item.Activate(position, amount); return; }
                float distance = ((Vector2)item.transform.position - position).sqrMagnitude;
                if (distance < closest) { nearest = item; closest = distance; }
            }
            if (nearest != null) nearest.Merge(amount);
        }

        public void ClearAll() { foreach (ExperiencePickup item in items) item.gameObject.SetActive(false); }
    }
}
