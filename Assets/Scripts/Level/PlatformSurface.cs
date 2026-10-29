using UnityEngine;

namespace MoriEchoes
{
    [DefaultExecutionOrder(-30)]
    public sealed class PlatformSurface : MonoBehaviour
    {
        public bool OneWay;
        public bool Ice;
        public float ConveyorSpeed;
        public bool Crumbling;
        public bool Elevator;
        public bool Buoyant;
        public float BuoyancyPhase;
        public Vector2 Origin;
        public Vector2 Velocity { get; private set; }
        private float collapseAt;
        private float restoreAt;
        private float elevatorTime;
        private Collider2D colliderComponent;
        private SpriteRenderer[] visuals;
        private Rigidbody2D body;

        private void Start()
        {
            colliderComponent = GetComponent<Collider2D>();
            visuals = GetComponentsInChildren<SpriteRenderer>();
            Origin = transform.position;
            if (Elevator || Buoyant)
            {
                body = GetComponent<Rigidbody2D>();
                if (body == null) body = gameObject.AddComponent<Rigidbody2D>();
                body.bodyType = RigidbodyType2D.Kinematic;
                body.interpolation = RigidbodyInterpolation2D.Interpolate;
            }
        }

        private void FixedUpdate()
        {
            if (body != null)
            {
                elevatorTime += Time.fixedDeltaTime;
                Vector2 destination = Origin + Vector2.up * (Buoyant
                    ? (Mathf.Sin(elevatorTime * 1.35f + BuoyancyPhase) - Mathf.Sin(BuoyancyPhase)) * 0.12f
                    : (1 - Mathf.Cos(elevatorTime * 0.8f)) * 1.8f);
                Velocity = (destination - body.position) / Time.fixedDeltaTime;
                body.MovePosition(destination);
            }
            if (collapseAt > 0 && Time.time >= collapseAt)
            {
                collapseAt = 0; restoreAt = Time.time + 3;
                colliderComponent.enabled = false;
                foreach (SpriteRenderer visual in visuals) visual.enabled = false;
            }
            if (restoreAt > 0 && Time.time >= restoreAt)
            {
                restoreAt = 0; colliderComponent.enabled = true;
                foreach (SpriteRenderer visual in visuals) visual.enabled = true;
            }
        }

        private void OnDisable() { Velocity = Vector2.zero; }

        private void OnCollisionStay2D(Collision2D collision)
        {
            if (Crumbling && collapseAt == 0 && restoreAt == 0 && collision.gameObject.GetComponent<PlayerController>() != null)
                collapseAt = Time.time + 0.6f;
        }
    }
}
