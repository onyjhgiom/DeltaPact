using UnityEngine;

namespace MoriEchoes
{
    public sealed class StageHazard : MonoBehaviour
    {
        public GameManager Game;
        public int Kind;
        public float Phase;
        public Vector2 DamageSize;
        public bool Active { get; private set; }
        private float nextHit;
        private SpriteRenderer visual;
        private Vector3 baseScale;
        private Color color;

        private void Start()
        {
            visual = GetComponentInChildren<SpriteRenderer>();
            baseScale = transform.localScale; color = visual.color;
            if (DamageSize == Vector2.zero) DamageSize = baseScale;
        }
        private void Update()
        {
            if (Game == null) return;
            float cycle = Mathf.Repeat(Time.time + Phase, 3.8f);
            Active = Kind == 0 || cycle > 1.8f && cycle < 2.9f;
            Color tint = color; tint.a = Active ? 0.9f : 0.18f;
            visual.color = tint;
            visual.enabled = Active || Game.Effects.AttackTelegraphsEnabled;
            if (Kind == 1) transform.localScale = new Vector3(baseScale.x, baseScale.y * (Active ? 1 : 0.15f), 1);
            if (!Game.IsPlaying || Game.Player == null || Time.time < nextHit) return;
            Vector2 delta = Game.Player.transform.position - transform.position;
            if (Mathf.Abs(delta.x) < DamageSize.x * 0.5f + 0.3f && Mathf.Abs(delta.y) < DamageSize.y * 0.5f + 0.6f && Active)
            {
                if (Game.Player.Health.TakeDamage(12 * Game.DamageScale, Team.Enemy, false, IncomingHitKind.Hazard)) nextHit = Time.time + 0.8f;
            }
        }
    }
}
