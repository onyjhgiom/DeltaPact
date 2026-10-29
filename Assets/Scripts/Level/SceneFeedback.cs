using System.Collections.Generic;
using UnityEngine;

namespace MoriEchoes
{
    public sealed class SceneFeedback : MonoBehaviour
    {
        private sealed class Prop
        {
            public SpriteRenderer Sprite;
            public float Sway;
            public float Impulse;
            public Color BaseColor;
            public Quaternion BaseRotation;
        }
        private readonly List<Prop> props = new List<Prop>();
        private readonly SpriteRenderer[] motes = new SpriteRenderer[48];
        private readonly Vector2[] drift = new Vector2[48];
        private GameManager game;
        private float nextDust;
        public int ReactionCount { get; private set; }
        public int PropCount => props.Count;

        public void Initialize(GameManager owner)
        {
            game = owner;
            var random = new System.Random(7301 + game.SelectedStage);
            for (int index = 0; index < motes.Length; index++)
            {
                float size = 0.018f + (float)random.NextDouble() * 0.035f;
                motes[index] = game.Art.Renderer("AtmosphericMote_" + index, transform, game.Art.Glow,
                    Color.white, new Vector2(size * 2, size), index % 3 == 0 ? 280 : -50);
                motes[index].transform.position = new Vector2((float)random.NextDouble() * 45 - 12, (float)random.NextDouble() * 20 - 4);
                drift[index] = new Vector2(-0.25f - (float)random.NextDouble(), game.SelectedStage == 2 ? -0.6f : 0.1f + (float)random.NextDouble() * 0.3f);
            }
        }

        public void Register(SpriteRenderer sprite, bool flexible)
        {
            props.Add(new Prop { Sprite = sprite, Sway = flexible ? 3.2f : 0,
                BaseColor = sprite.color, BaseRotation = sprite.transform.localRotation });
        }

        public void React(Vector2 position, float radius, Color tint)
        {
            ReactionCount++;
            foreach (Prop prop in props)
            {
                if (prop.Sprite == null || !prop.Sprite.gameObject.activeInHierarchy) continue;
                float distance = Vector2.Distance(position, prop.Sprite.transform.position);
                if (distance < radius + 2) prop.Impulse = Mathf.Max(prop.Impulse, (1 - distance / (radius + 2)) * 5);
            }
        }

        private void Update()
        {
            if (game == null || !game.IsPlaying || game.Player == null) return;
            Vector2 camera = game.CameraRig.transform.position;
            Vector2 player = game.Player.transform.position;
            Vector2 velocity = game.Player.GetComponent<Rigidbody2D>().linearVelocity;
            bool verticalStage = game.Level != null && game.Level.IsVerticalStage;
            foreach (Prop prop in props)
            {
                if (prop.Sprite == null || !prop.Sprite.gameObject.activeInHierarchy) continue;
                Vector2 position = prop.Sprite.transform.position;
                bool visible = Mathf.Abs(camera.x - position.x) < (verticalStage ? 9 : 28)
                    && (!verticalStage || Mathf.Abs(camera.y - position.y) < 14);
                prop.Sprite.enabled = visible;
                if (!visible) continue;
                prop.Impulse = Mathf.MoveTowards(prop.Impulse, 0, Time.deltaTime * 4);
                float impulseVelocity = verticalStage ? velocity.y : velocity.x;
                float wake = Mathf.Max(0, 2.5f - Vector2.Distance(player, position)) * impulseVelocity * 0.18f;
                float angle = Mathf.Sin(Time.time * 1.8f + position.x) * prop.Sway + Mathf.Sin(Time.time * 22) * prop.Impulse + wake;
                prop.Sprite.transform.localRotation = prop.BaseRotation * Quaternion.Euler(0, 0, angle);
                Color highlight = new Color(prop.BaseColor.r * 1.7f, prop.BaseColor.g * 1.9f, prop.BaseColor.b * 2, prop.BaseColor.a);
                prop.Sprite.color = Color.Lerp(prop.BaseColor, highlight, Mathf.Clamp01(prop.Impulse * 0.12f));
            }
            for (int index = 0; index < motes.Length; index++)
            {
                Vector2 position = motes[index].transform.position;
                Vector2 separation = position - player;
                float wake = Mathf.Max(0, 2 - separation.magnitude);
                Vector2 wakeDirection = verticalStage ? Vector2.down * Mathf.Sign(velocity.y == 0 ? -1 : velocity.y) : Vector2.right * Mathf.Sign(velocity.x == 0 ? 1 : velocity.x);
                position += (drift[index] + wakeDirection * wake * velocity.magnitude * 0.4f) * Time.deltaTime;
                if (position.x < camera.x - 25) position.x += 50;
                if (position.x > camera.x + 25) position.x -= 50;
                if (position.y < camera.y - 12) position.y += 24;
                if (position.y > camera.y + 12) position.y -= 24;
                motes[index].transform.position = position;
                Color tint = Color.Lerp(game.Level.Theme.accent, Color.white, 0.6f);
                tint.a = 0.25f + Mathf.Sin(Time.time + index) * 0.12f;
                motes[index].color = tint;
            }
            if (game.Player.Motor.Grounded && Mathf.Abs(velocity.x) > 2 && Time.time >= nextDust)
            {
                nextDust = Time.time + 0.22f;
                game.Effects.Landing(player + Vector2.down * 0.76f);
            }
        }
    }
}
