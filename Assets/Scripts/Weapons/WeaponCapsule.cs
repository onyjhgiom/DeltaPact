using UnityEngine;

namespace MoriEchoes
{
    [DisallowMultipleComponent]
    public sealed class WeaponCapsule : MonoBehaviour
    {
        public CombatPowerupKind Kind { get; private set; }
        public bool IsFalling => gameObject.activeSelf && !grounded && !IsFloating;
        public bool IsFloating => gameObject.activeSelf && game != null && game.Level != null && game.Level.Swim != null;
        public float LifeRemaining { get; private set; }

        private readonly RaycastHit2D[] groundHits = new RaycastHit2D[24];
        private WeaponCapsuleDirector director;
        private GameManager game;
        private Transform visualRoot;
        private Transform parachute;
        private SpriteRenderer shell;
        private SpriteRenderer ring;
        private SpriteRenderer glow;
        private SpriteRenderer icon;
        private TextMesh label;
        private MeshRenderer labelRenderer;
        private SpriteRenderer[] canopyParts;
        private Collider2D support;
        private Vector3 supportPoint;
        private bool grounded;
        private bool supply;
        private float arenaFloor;
        private float fallSpeed;
        private float phase;
        private float floatHeight;

        internal void Initialize(WeaponCapsuleDirector owner, GameManager manager)
        {
            director = owner; game = manager;
            visualRoot = new GameObject("CapsuleVisual").transform;
            visualRoot.SetParent(transform, false);
            glow = game.Art.Renderer("SupplyGlow", visualRoot, game.Art.Glow, Color.white, new Vector2(1.7f, 1.7f), 30);
            shell = game.Art.Renderer("SupplyShell", visualRoot, game.Art.Disc, Color.white, new Vector2(0.98f, 0.75f), 31);
            ring = game.Art.Renderer("SupplyRing", visualRoot, game.Art.Ring, Color.white, new Vector2(1.12f, 0.9f), 32);
            icon = game.Art.Renderer("ExistingWeaponArt", visualRoot, game.Art.Gun, Color.white, new Vector2(0.6f, 0.24f), 33);
            icon.transform.localPosition = new Vector2(0, -0.35f);
            var letter = new GameObject("CapsuleLetter");
            letter.transform.SetParent(visualRoot, false);
            letter.transform.localPosition = new Vector3(0, 0.03f, -0.02f);
            label = letter.AddComponent<TextMesh>();
            label.font = game.UiFont; label.fontSize = 64; label.characterSize = 0.085f;
            label.anchor = TextAnchor.MiddleCenter; label.alignment = TextAlignment.Center; label.color = Color.white;
            labelRenderer = letter.GetComponent<MeshRenderer>();
            if (game.UiFont != null) labelRenderer.sharedMaterial = game.UiFont.material;
            parachute = new GameObject("SupplyParachute").transform;
            parachute.SetParent(visualRoot, false);
            canopyParts = new SpriteRenderer[3];
            canopyParts[0] = game.Art.Renderer("Canopy", parachute, game.Art.Ring, Color.white, new Vector2(1.4f, 0.55f), 32);
            canopyParts[0].transform.localPosition = new Vector2(0, 0.95f);
            for (int index = 1; index < canopyParts.Length; index++)
            {
                float side = index == 1 ? -1 : 1;
                canopyParts[index] = game.Art.Renderer("Tether_" + index, parachute, game.Art.Square,
                    Color.white, new Vector2(0.025f, 0.72f), 32);
                canopyParts[index].transform.localPosition = new Vector2(side * 0.33f, 0.58f);
                canopyParts[index].transform.localRotation = Quaternion.Euler(0, 0, side * -34);
            }
        }

        internal void Activate(Vector2 position, CombatPowerupKind kind, bool fallingSupply, float landingHeight, float lifetime)
        {
            transform.position = position;
            Kind = kind; supply = fallingSupply; arenaFloor = landingHeight;
            grounded = false; support = null; supportPoint = Vector3.zero;
            fallSpeed = supply ? 1.2f : -1.5f;
            LifeRemaining = lifetime; phase = 0;
            floatHeight = position.y;
            CombatPowerupProfile profile = CombatPowerupRules.Profile(kind);
            Color tint = CombatPowerup.ColorFor(kind);
            Sprite pickup = game.Config.pickupSprites != null && (int)kind - 1 >= 0 && (int)kind - 1 < game.Config.pickupSprites.Length
                ? game.Config.pickupSprites[(int)kind - 1] : null;
            if (pickup != null)
            {
                shell.sprite = pickup;
                shell.transform.localScale = Vector3.one * (0.92f / Mathf.Max(pickup.bounds.size.x, pickup.bounds.size.y));
                shell.color = Color.white;
                ring.enabled = false;
            }
            else
            {
                shell.sprite = game.Art.Disc;
                shell.transform.localScale = Vector3.one;
                shell.color = Color.Lerp(tint, ProceduralArt.Ink, 0.65f);
                ring.enabled = true;
            }
            ring.color = tint;
            glow.color = new Color(tint.r, tint.g, tint.b, 0.48f);
            label.text = profile.Code;
            int selected = profile.ResolveWeaponIndex(game.Config.weapons.Length);
            Sprite existing = game.Config.weaponSprites != null && selected >= 0 && selected < game.Config.weaponSprites.Length
                ? game.Config.weaponSprites[selected] : null;
            icon.sprite = existing != null ? existing : game.Art.Gun;
            float width = Mathf.Max(0.01f, icon.sprite.bounds.size.x);
            icon.transform.localScale = Vector3.one * (0.62f / width);
            icon.color = Color.Lerp(Color.white, tint, 0.3f);
            foreach (SpriteRenderer part in canopyParts) part.color = new Color(tint.r, tint.g, tint.b, 0.8f);
            visualRoot.localPosition = Vector3.zero;
            parachute.gameObject.SetActive(supply);
            gameObject.SetActive(true);
            RefreshVisuals();
        }

        private void Update()
        {
            if (director == null || game == null) { Despawn(); return; }
            if (!director.CanSimulate) return;
            float seconds = Time.deltaTime;
            LifeRemaining = Mathf.Max(0, LifeRemaining - seconds);
            float bottom = game.Level != null && game.Level.Vertical != null
                ? (game.Level.Vertical.Ascending ? game.Level.Vertical.StartPoint.y : -game.Level.Vertical.DepthLength) - 12 : -16;
            if (LifeRemaining <= 0 || transform.position.y < bottom
                || Vector2.SqrMagnitude(transform.position - director.Collector.transform.position) > 14400)
            {
                Despawn(); return;
            }
            phase += seconds;
            if (IsFloating)
            {
                SwimStageDirector swim = game.Level.Swim;
                grounded = false; fallSpeed = 0; support = null;
                float y = swim.Underwater ? floatHeight + Mathf.Sin(phase * 1.8f) * 0.22f
                    : swim.SurfaceHeightAt(transform.position.x) + 0.22f;
                transform.position = new Vector2(transform.position.x, Mathf.Clamp(y, swim.MinY + 0.6f, swim.MaxY - 0.6f));
            }
            else StepFall(seconds);
            RefreshVisuals();
            TryCollect(director.Collector);
        }

        private void StepFall(float seconds)
        {
            if (grounded)
            {
                if (!game.Config.sideScroller) return;
                if (support != null && support.enabled && support.gameObject.activeInHierarchy)
                {
                    transform.position = support.transform.TransformPoint(supportPoint);
                    return;
                }
                grounded = false; support = null; fallSpeed = 0;
            }
            fallSpeed = Mathf.Min(supply ? 3.8f : 7f, fallSpeed + seconds * (supply ? 2.6f : 10f));
            float distance = fallSpeed * seconds;
            Vector2 position = transform.position;
            if (game.Config.sideScroller && distance > 0)
            {
                var filter = new ContactFilter2D { useTriggers = false };
                int count = Physics2D.CircleCast(position, 0.24f, Vector2.down, filter, groundHits, distance + 0.025f);
                if (count == groundHits.Length) return;
                float nearest = float.PositiveInfinity;
                Collider2D ground = null;
                for (int index = 0; index < count; index++)
                {
                    RaycastHit2D hit = groundHits[index];
                    if (hit.collider == null || hit.normal.y < 0.5f || hit.collider.GetComponentInParent<Health>() != null) continue;
                    if (hit.distance < nearest) { nearest = hit.distance; ground = hit.collider; }
                }
                if (ground != null)
                {
                    transform.position = position + Vector2.down * Mathf.Max(0, nearest - 0.015f);
                    grounded = true; fallSpeed = 0; support = ground;
                    supportPoint = support.transform.InverseTransformPoint(transform.position);
                    return;
                }
            }
            position.y -= distance;
            if (!game.Config.sideScroller && position.y <= arenaFloor && fallSpeed >= 0)
            {
                position.y = arenaFloor; grounded = true; fallSpeed = 0;
            }
            transform.position = position;
        }

        private void RefreshVisuals()
        {
            visualRoot.rotation = game.CameraRig.transform.rotation;
            visualRoot.localPosition = new Vector3(0, grounded ? Mathf.Sin(phase * 4) * 0.06f : 0, 0);
            parachute.gameObject.SetActive(supply && !grounded && !IsFloating);
            int order = game.CameraRig.SortOrder(transform.position) + 6;
            glow.sortingOrder = order; shell.sortingOrder = order + 1; ring.sortingOrder = order + 2;
            icon.sortingOrder = order + 3; labelRenderer.sortingOrder = order + 4;
            foreach (SpriteRenderer part in canopyParts) part.sortingOrder = order + 2;
            float blink = LifeRemaining < 3 ? 0.45f + 0.55f * Mathf.Abs(Mathf.Sin(phase * 12)) : 1;
            Color tint = CombatPowerup.ColorFor(Kind);
            ring.color = new Color(tint.r, tint.g, tint.b, blink);
            glow.color = new Color(tint.r, tint.g, tint.b, (0.32f + Mathf.Sin(phase * 5) * 0.1f) * blink);
        }

        public bool TryCollect(PlayerController collector)
        {
            if (!gameObject.activeInHierarchy || director == null || !director.CanSimulate || collector != director.Collector) return false;
            Collider2D body = director.CollectorCollider;
            Vector2 position = transform.position;
            Vector2 closest = body != null && body.enabled ? body.ClosestPoint(position) : (Vector2)collector.transform.position;
            float radius = Mathf.Max(0.05f, director.PickupRadius);
            if ((closest - position).sqrMagnitude > radius * radius) return false;
            if (!collector.Weapon.TryApplyPowerup(Kind)) return false;
            CombatPowerupKind collected = Kind;
            Despawn();
            director.NotifyCollected(collected, position);
            return true;
        }

        public void Despawn()
        {
            Kind = CombatPowerupKind.None; LifeRemaining = 0;
            support = null; grounded = false; phase = fallSpeed = 0;
            gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            Kind = CombatPowerupKind.None; LifeRemaining = 0; support = null;
        }
    }
}
