using UnityEngine;

namespace MoriEchoes
{
    public sealed class ExperiencePickup : MonoBehaviour
    {
        public int Amount { get; private set; }
        private GameManager game;
        private SpriteRenderer visual;
        private float phase;

        public void Initialize(GameManager owner)
        {
            game = owner;
            Sprite artwork = game.Config.pickupSprites != null && game.Config.pickupSprites.Length > 7
                ? game.Config.pickupSprites[7] : game.Art.Diamond;
            visual = game.Art.Renderer("XPShard", transform, artwork, Color.white,
                new Vector2(0.3f / artwork.bounds.size.x, 0.36f / artwork.bounds.size.y), 15);
            game.Art.Renderer("Glow", transform, game.Art.Glow, new Color(0.1f, 0.65f, 1f, 0.2f), new Vector2(0.65f, 0.65f), 14);
        }

        public void Activate(Vector2 position, int amount)
        {
            transform.position = position;
            Amount = amount; phase = Random.value * 6;
            gameObject.SetActive(true);
        }

        public void Merge(int amount) { Amount += amount; }

        private void Update()
        {
            if (!game.IsPlaying || game.Player == null) return;
            phase += Time.deltaTime * 4;
            visual.transform.localPosition = new Vector3(0, Mathf.Sin(phase) * 0.06f, 0);
            Vector2 target = game.Player.transform.position;
            float distance = Vector2.Distance(transform.position, target);
            if (distance < 6f)
                transform.position = Vector2.MoveTowards(transform.position, target, Time.deltaTime * Mathf.Lerp(12, 5, distance / 6));
            if (distance < 0.55f)
            {
                int value = Amount;
                gameObject.SetActive(false);
                Amount = 0;
                game.Experience.Gain(value);
            }
        }
    }
}
