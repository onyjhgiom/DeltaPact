using UnityEngine;

namespace MoriEchoes
{
    public enum BossFieldShape { Box, Disc, Ring }
    public enum BossFieldStyle { Punch, Fracture, Charge, Hydraulic, Fuel, Molten, IceSpike, Prism, Wind, Digital, Scan, Mirror, Orbit, Gravity, Rift }

    public sealed class BossField : MonoBehaviour
    {
        public Vector2 DamageSize { get; private set; }
        public float Remaining { get; private set; }
        public float WarningRemaining => Mathf.Max(0, warningDuration - elapsed);
        public bool IsWarning => elapsed < warningDuration;
        public bool Active => initialized && isActiveAndEnabled && !IsWarning && Remaining > 0 && SourceValid;
        public string AttackId { get; private set; }
        public BossFieldShape Shape { get; private set; }
        public BossFieldStyle Style { get; private set; }
        public float InnerRadius { get; private set; }
        public float Damage { get; private set; }
        public float TargetLockWindow { get; private set; }
        public bool TargetCommitted => TargetLockWindow > 0 && WarningRemaining <= TargetLockWindow;
        public IncomingHitKind HitKind = IncomingHitKind.BossSkill;
        public float FreezeSeconds;
        public Health Owner => owner;
        public Vector2[] Path => path != null ? (Vector2[])path.Clone() : System.Array.Empty<Vector2>();
        public float PullRadius { get; private set; }
        public bool OwnerAlive => owner != null && !owner.IsDead && owner.isActiveAndEnabled && owner.gameObject.activeInHierarchy;
        private bool SourceValid => OwnerAlive && game != null && level != null && game.Level == level && level.gameObject.activeInHierarchy
            && (!hasCounterAnchor || counterAnchor != null && !counterAnchor.IsDead && counterAnchor.gameObject.activeInHierarchy);
        private GameManager game;
        private LevelManager level;
        private Health owner;
        private Health counterAnchor;
        private bool hasCounterAnchor;
        private bool initialized;
        private float elapsed;
        private float warningDuration;
        private float activeDuration;
        private float hitCooldown;
        private float angle;
        private float pathDuration;
        private float pathLength;
        private float pullStrength;
        private Vector2[] path;
        private float[] pathDistances;
        private Color tint;
        private SpriteRenderer fill;
        private SpriteRenderer clockFill;
        private Transform routeRoot;
        private Transform detailsRoot;
        private SpriteRenderer[] edges;
        private SpriteRenderer[] details;
        private PlatformSurface conveyor;
        private BoxCollider2D conveyorCollider;
        private float conveyorSpeed;
        private Texture2D warningMask;
        private Sprite warningSprite;
        private bool customArtwork;

        public void ConfigureArtwork(Sprite artwork)
        {
            if (artwork == null || detailsRoot == null) return;
            foreach (SpriteRenderer detail in details)
            {
                detail.gameObject.SetActive(false);
                Destroy(detail.gameObject);
            }
            Vector2 bounds = artwork.bounds.size;
            float scale = Mathf.Clamp(DamageSize.magnitude, 2.2f, 6.5f) / bounds.x;
            SpriteRenderer painted = Visual("UniquePaintedTechnique", detailsRoot, artwork,
                Vector2.one * scale, Color.white, 160);
            details = new[] { painted };
            customArtwork = true;
        }

        public void Initialize(GameManager manager, Health source, Vector2 size, float duration, bool beam)
        {
            InitializeAttack(manager, source, "LegacyField", BossFieldShape.Box,
                beam ? BossFieldStyle.Scan : BossFieldStyle.Fuel, size, 0, duration, beam ? 18 : 12);
        }

        public void InitializeAttack(GameManager manager, Health source, string attackId, BossFieldShape shape,
            BossFieldStyle style, Vector2 size, float warning, float duration, float damage, float rotation = 0, float innerRadius = 0)
        {
            game = manager;
            level = manager.Level;
            owner = source;
            AttackId = attackId;
            Shape = shape;
            Style = style;
            DamageSize = new Vector2(Mathf.Max(0.05f, size.x), Mathf.Max(0.05f, size.y));
            warningDuration = Mathf.Max(0, warning);
            activeDuration = Mathf.Max(0.08f, duration);
            Remaining = warningDuration + activeDuration;
            Damage = Mathf.Max(0, damage);
            angle = rotation;
            InnerRadius = Mathf.Clamp(innerRadius, 0, DamageSize.x * 0.5f - 0.01f);
            tint = Damage > 0 ? game.Level.Theme.danger : game.Level.Theme.accent;
            transform.rotation = Quaternion.Euler(0, 0, angle);
            initialized = true;
            owner.Died += Cancel;
            BuildVisuals();
            RefreshVisuals();
        }

        public void ConfigurePath(Vector2[] points, float seconds, bool showRoute = true)
        {
            if (points == null || points.Length < 2) return;
            path = (Vector2[])points.Clone();
            pathDuration = Mathf.Clamp(seconds, 0.08f, activeDuration);
            pathDistances = new float[path.Length];
            pathLength = 0;
            for (int index = 1; index < path.Length; index++)
            {
                pathLength += Vector2.Distance(path[index - 1], path[index]);
                pathDistances[index] = pathLength;
            }
            transform.position = path[0];
            if (!showRoute || !game.Effects.BossTelegraphsEnabled || Damage <= 0) return;
            routeRoot = new GameObject("CommittedTrajectory_NoHitbox").transform;
            routeRoot.SetParent(transform, false);
            float width = Shape == BossFieldShape.Box ? DamageSize.y : DamageSize.x;
            for (int index = 1; index < path.Length; index++)
            {
                Vector2 delta = path[index] - path[index - 1];
                if (delta.sqrMagnitude < 0.0001f) continue;
                Vector2 center = (path[index] + path[index - 1]) * 0.5f;
                SpriteRenderer route = Visual("Route_" + index, routeRoot, game.Art.Square,
                        new Vector2(delta.magnitude, width), new Color(1, 0.035f, 0.07f, 0.2f), 85);
                route.transform.position = center;
                route.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
                if (Shape == BossFieldShape.Box)
                {
                    SpriteRenderer cap = Visual("RouteEnd_" + index, routeRoot, game.Art.Square,
                        DamageSize, new Color(1, 0.035f, 0.07f, 0.2f), 85);
                    cap.transform.position = path[index];
                    cap.transform.rotation = transform.rotation;
                }
                else
                {
                    SpriteRenderer cap = Visual("RouteCorner_" + index, routeRoot, game.Art.Disc,
                        Vector2.one * (DamageSize.x / 0.96f), new Color(1, 0.035f, 0.07f, 0.2f), 85);
                    cap.transform.position = path[index];
                }
            }
        }

        public bool FollowWarningAt(Vector2 position)
        {
            if (!initialized || !IsWarning || Damage <= 0 || TargetCommitted) return false;
            Vector2 offset = position - (Vector2)transform.position;
            if (path != null)
                for (int index = 0; index < path.Length; index++) path[index] += offset;
            transform.position = position;
            return true;
        }

        // After commitment both the warning and its damage footprint stay at the last world position.
        public void ConfigureTargetLock(float escapeSeconds)
        {
            TargetLockWindow = Mathf.Clamp(escapeSeconds, 0, warningDuration);
        }

        public void ExtendActiveDuration(float seconds)
        {
            activeDuration = Mathf.Max(activeDuration, seconds);
            Remaining = Mathf.Max(0, warningDuration + activeDuration - elapsed);
        }

        public void ConfigurePull(float radius, float strength)
        {
            PullRadius = Mathf.Max(DamageSize.x * 0.5f, radius);
            pullStrength = Mathf.Clamp(strength, 0, 3.5f);
            if (!game.Effects.AttackTelegraphsEnabled) return;
            Visual("GravityInfluence_NoDamage", transform, game.Art.Ring,
                Vector2.one * (PullRadius * 2 / 0.88f), WithAlpha(game.Level.Theme.accent, 0.35f), 70);
            for (int index = 0; index < 8; index++)
            {
                float radians = index * Mathf.PI * 0.25f;
                SpriteRenderer arrow = Visual("InwardVector_" + index, transform, game.Art.Diamond,
                    new Vector2(0.32f, 0.12f), WithAlpha(game.Level.Theme.accent, 0.65f), 71);
                arrow.transform.localPosition = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * (PullRadius - 0.35f);
                arrow.transform.localRotation = Quaternion.Euler(0, 0, index * 45);
            }
        }

        public void ConfigureConveyor(float speed)
        {
            if (Shape != BossFieldShape.Box || Damage > 0) return;
            conveyorSpeed = Mathf.Clamp(speed, -2.4f, 2.4f);
            conveyorCollider = gameObject.AddComponent<BoxCollider2D>();
            conveyorCollider.size = DamageSize;
            conveyorCollider.enabled = !IsWarning;
            conveyor = gameObject.AddComponent<PlatformSurface>();
            conveyor.ConveyorSpeed = IsWarning ? 0 : conveyorSpeed;
            foreach (SpriteRenderer detail in details) { detail.enabled = false; Destroy(detail.gameObject); }
            var deck = new GameObject("StageSurfaceConveyor");
            deck.transform.SetParent(detailsRoot, false);
            StageSurfaceArt.Draw(game, level, deck.transform, DamageSize, true, false, 79);
            details = deck.GetComponentsInChildren<SpriteRenderer>();
        }

        public void BindCounterAnchor(Health anchor)
        {
            if (counterAnchor != null) counterAnchor.Died -= Cancel;
            counterAnchor = anchor;
            hasCounterAnchor = true;
            if (counterAnchor != null) counterAnchor.Died += Cancel;
        }

        public void Cancel()
        {
            if (!initialized) return;
            initialized = false;
            Remaining = 0;
            gameObject.SetActive(false);
            Destroy(gameObject);
        }

        private void FixedUpdate()
        {
            if (!initialized) return;
            if (!SourceValid)
            {
                Cancel();
                return;
            }
            if (!game.IsPlaying || Time.timeScale <= 0) return;
            float delta = Time.fixedDeltaTime;
            elapsed += delta;
            Remaining = Mathf.Max(0, warningDuration + activeDuration - elapsed);
            if (Remaining <= 0) { Cancel(); return; }
            if (IsWarning) return;
            if (conveyor != null)
            {
                conveyorCollider.enabled = true;
                conveyor.ConveyorSpeed = conveyorSpeed;
            }
            if (path != null) transform.position = PositionOnPath(Mathf.Clamp01((elapsed - warningDuration) / pathDuration));
            if (routeRoot != null) routeRoot.gameObject.SetActive(false);
            if (game.Player == null || game.Player.Health.IsDead) return;
            if (PullRadius > 0 && !game.Player.Motor.IsDodging)
            {
                Vector2 offset = (Vector2)transform.position - (Vector2)game.Player.transform.position;
                if (offset.sqrMagnitude < PullRadius * PullRadius && Mathf.Abs(offset.x) > 0.15f)
                {
                    Rigidbody2D playerBody = game.Player.GetComponent<Rigidbody2D>();
                    if (playerBody != null)
                        playerBody.AddForce(Vector2.right * Mathf.Sign(offset.x) * pullStrength * playerBody.mass
                            * (1 - Mathf.Clamp01(offset.magnitude / PullRadius)), ForceMode2D.Force);
                }
            }
            hitCooldown = Mathf.Max(0, hitCooldown - delta);
            if (Damage <= 0 || hitCooldown > 0) return;
            CapsuleCollider2D capsule = game.Player.GetComponent<CapsuleCollider2D>();
            if (capsule == null || !capsule.enabled) return;
            Vector2 scale = capsule.transform.lossyScale;
            Vector2 size = Vector2.Scale(capsule.size, new Vector2(Mathf.Abs(scale.x), Mathf.Abs(scale.y)));
            Vector2 center = capsule.transform.TransformPoint(capsule.offset);
            if (OverlapsCapsule(center, size) && game.Player.Health.TakeDamage(Damage * game.DamageScale, Team.Enemy, false, HitKind))
            {
                hitCooldown = 0.8f;
                // Boss hits never disable movement; damage feedback is handled by Health/CharacterVisual.
            }
        }

        public bool Contains(Vector2 point)
        {
            Vector2 local = Rotate(point - (Vector2)transform.position, -angle);
            if (Shape == BossFieldShape.Box) return Mathf.Abs(local.x) <= DamageSize.x * 0.5f && Mathf.Abs(local.y) <= DamageSize.y * 0.5f;
            float square = local.sqrMagnitude;
            return square <= DamageSize.x * DamageSize.x * 0.25f && (Shape != BossFieldShape.Ring || square >= InnerRadius * InnerRadius);
        }

        public bool OverlapsCapsule(Vector2 center, Vector2 size)
        {
            return CapsuleIntersects(Shape, transform.position, DamageSize, angle, InnerRadius, center, size);
        }

        public static bool CapsuleIntersects(BossFieldShape shape, Vector2 fieldCenter, Vector2 fieldSize, float rotation,
            float innerRadius, Vector2 center, Vector2 size)
        {
            float radius = Mathf.Min(size.x, size.y) * 0.5f;
            Vector2 axis = size.y >= size.x ? Vector2.up : Vector2.right;
            Vector2 extension = axis * (Mathf.Max(size.x, size.y) * 0.5f - radius);
            Vector2 first = Rotate(center - extension - fieldCenter, -rotation);
            Vector2 second = Rotate(center + extension - fieldCenter, -rotation);
            if (shape != BossFieldShape.Box)
            {
                float outer = fieldSize.x * 0.5f;
                if (PointSegmentDistanceSquared(Vector2.zero, first, second) > (outer + radius) * (outer + radius)) return false;
                float farthest = Mathf.Max(first.magnitude, second.magnitude) + radius;
                return shape != BossFieldShape.Ring || farthest >= innerRadius;
            }
            Vector2 half = fieldSize * 0.5f;
            if (SegmentIntersectsBox(first, second, half)) return true;
            float distance = Mathf.Min(PointBoxDistanceSquared(first, half), PointBoxDistanceSquared(second, half));
            distance = Mathf.Min(distance, PointSegmentDistanceSquared(new Vector2(-half.x, -half.y), first, second));
            distance = Mathf.Min(distance, PointSegmentDistanceSquared(new Vector2(-half.x, half.y), first, second));
            distance = Mathf.Min(distance, PointSegmentDistanceSquared(new Vector2(half.x, -half.y), first, second));
            distance = Mathf.Min(distance, PointSegmentDistanceSquared(new Vector2(half.x, half.y), first, second));
            return distance <= radius * radius;
        }

        private static bool SegmentIntersectsBox(Vector2 first, Vector2 second, Vector2 half)
        {
            Vector2 delta = second - first;
            float enter = 0;
            float exit = 1;
            for (int axis = 0; axis < 2; axis++)
            {
                if (Mathf.Abs(delta[axis]) < 0.00001f)
                {
                    if (Mathf.Abs(first[axis]) > half[axis]) return false;
                    continue;
                }
                float near = (-half[axis] - first[axis]) / delta[axis];
                float far = (half[axis] - first[axis]) / delta[axis];
                enter = Mathf.Max(enter, Mathf.Min(near, far));
                exit = Mathf.Min(exit, Mathf.Max(near, far));
                if (enter > exit) return false;
            }
            return true;
        }

        private static float PointBoxDistanceSquared(Vector2 point, Vector2 half)
        {
            Vector2 outside = new Vector2(Mathf.Max(0, Mathf.Abs(point.x) - half.x), Mathf.Max(0, Mathf.Abs(point.y) - half.y));
            return outside.sqrMagnitude;
        }

        private static float PointSegmentDistanceSquared(Vector2 point, Vector2 first, Vector2 second)
        {
            Vector2 segment = second - first;
            float fraction = segment.sqrMagnitude > 0.00001f ? Mathf.Clamp01(Vector2.Dot(point - first, segment) / segment.sqrMagnitude) : 0;
            return (point - first - segment * fraction).sqrMagnitude;
        }

        private static Vector2 Rotate(Vector2 point, float degrees)
        {
            float radians = degrees * Mathf.Deg2Rad;
            return new Vector2(point.x * Mathf.Cos(radians) - point.y * Mathf.Sin(radians), point.x * Mathf.Sin(radians) + point.y * Mathf.Cos(radians));
        }

        private Vector2 PositionOnPath(float progress)
        {
            float distance = progress * pathLength;
            for (int index = 1; index < path.Length; index++)
            {
                if (distance > pathDistances[index]) continue;
                float length = pathDistances[index] - pathDistances[index - 1];
                return Vector2.Lerp(path[index - 1], path[index], length > 0.00001f ? (distance - pathDistances[index - 1]) / length : 0);
            }
            return path[path.Length - 1];
        }

        private void BuildVisuals()
        {
            Sprite sprite = Shape == BossFieldShape.Box ? game.Art.Square : game.Art.Disc;
            Vector2 visualSize = Shape == BossFieldShape.Box ? DamageSize : Vector2.one * (DamageSize.x / 0.96f);
            fill = Visual("DamageFootprint", transform, sprite, visualSize, WithAlpha(tint, 0.16f), 74);
            if (Shape == BossFieldShape.Ring)
            {
                warningMask = new Texture2D(128, 128, TextureFormat.RGBA32, false);
                warningMask.wrapMode = TextureWrapMode.Clamp;
                Color[] pixels = new Color[128 * 128];
                float inner = InnerRadius / (DamageSize.x * 0.5f);
                for (int y = 0; y < 128; y++)
                    for (int x = 0; x < 128; x++)
                    {
                        float radius = new Vector2((x + 0.5f) / 64 - 1, (y + 0.5f) / 64 - 1).magnitude;
                        pixels[y * 128 + x] = radius <= 1 && radius >= inner ? Color.white : Color.clear;
                    }
                warningMask.SetPixels(pixels); warningMask.Apply();
                warningSprite = Sprite.Create(warningMask, new Rect(0, 0, 128, 128), Vector2.one * 0.5f, 128);
                fill.sprite = warningSprite;
                fill.transform.localScale = Vector2.one * DamageSize.x;
                edges = new SpriteRenderer[2];
                edges[0] = Visual("OuterDamageBoundary", transform, game.Art.Ring, Vector2.one * (DamageSize.x / 0.88f), tint, 76);
                edges[1] = Visual("InnerSafeBoundary", transform, game.Art.Ring, Vector2.one * (InnerRadius * 2 / 0.88f), tint, 76);
            }
            else if (Shape == BossFieldShape.Disc)
            {
                edges = new[] { Visual("DamageBoundary", transform, game.Art.Ring, Vector2.one * (DamageSize.x / 0.88f), tint, 76) };
            }
            else
            {
                edges = new SpriteRenderer[4];
                for (int index = 0; index < 4; index++)
                {
                    bool horizontal = index < 2;
                    edges[index] = Visual("DamageEdge_" + index, transform, game.Art.Square,
                        horizontal ? new Vector2(DamageSize.x, 0.035f) : new Vector2(0.035f, DamageSize.y), tint, 76);
                    edges[index].transform.localPosition = horizontal ? new Vector2(0, (index == 0 ? -1 : 1) * DamageSize.y * 0.5f)
                        : new Vector2((index == 2 ? -1 : 1) * DamageSize.x * 0.5f, 0);
                }
            }
            clockFill = Visual("ActivationCountdown_NoDamage", transform, game.Art.Square,
                new Vector2(DamageSize.x, 0.055f), Color.white, 78);
            clockFill.transform.localPosition = Vector2.up * (DamageSize.y * 0.5f + 0.12f);
            detailsRoot = new GameObject("AttackSilhouette_NoHitbox").transform;
            detailsRoot.SetParent(transform, false);
            BuildDetails();
            details = detailsRoot.GetComponentsInChildren<SpriteRenderer>();
        }

        private void BuildDetails()
        {
            Vector2 size = DamageSize;
            Sprite[] artwork = level.Theme.encounterEffects;
            if (artwork != null && artwork.Length >= 8)
            {
                int slot = Style == BossFieldStyle.IceSpike || Style == BossFieldStyle.Hydraulic || Style == BossFieldStyle.Fuel ? 3
                    : Style == BossFieldStyle.Gravity || Style == BossFieldStyle.Orbit ? 1
                    : Style == BossFieldStyle.Wind || Style == BossFieldStyle.Scan || Style == BossFieldStyle.Rift ? 4
                    : Style == BossFieldStyle.Fracture || Style == BossFieldStyle.Prism ? 6 : 2;
                if (artwork[slot] != null)
                {
                    if (Style == BossFieldStyle.Mirror)
                    {
                        SideSpriteAnimation animation = level.Theme.bossAnimation;
                        if (animation != null && animation.IsReady)
                            Detail("IllustratedMirrorGuardian", animation.Frame(1), size * 0.9f, Vector2.zero, 0).color = Color.white;
                        return;
                    }
                    Detail("IllustratedBossAttack_" + Style, artwork[slot], size * 0.94f, Vector2.zero, 0).color = Color.white;
                    return;
                }
            }
            if (Style == BossFieldStyle.Punch || Style == BossFieldStyle.Charge)
            {
                for (int index = 0; index < 4; index++)
                    Detail("ArmourKnuckle_" + index, game.Art.Square, new Vector2(size.x * 0.15f, size.y * 0.65f),
                        new Vector2((index - 1.5f) * size.x * 0.21f, 0), 0);
            }
            else if (Style == BossFieldStyle.Fracture)
            {
                for (int index = 0; index < 5; index++)
                    Detail("JaggedFault_" + index, game.Art.Square, new Vector2(size.x * 0.25f, 0.1f),
                        new Vector2((index - 2) * size.x * 0.18f, (index % 2 == 0 ? -1 : 1) * size.y * 0.12f), index % 2 == 0 ? 24 : -24);
            }
            else if (Style == BossFieldStyle.Hydraulic)
            {
                for (int index = 0; index < 4; index++)
                    Detail("PressRib_" + index, game.Art.Square, new Vector2(size.x * 0.75f, Mathf.Min(0.16f, size.y * 0.6f)),
                        new Vector2(0, (index - 1.5f) * size.y * 0.2f), 0);
            }
            else if (Style == BossFieldStyle.Fuel)
            {
                for (int index = 0; index < 3; index++)
                    Detail("FuelJet_" + index, game.Art.Diamond, new Vector2(size.x * 0.34f, size.y * (0.8f - index * 0.12f)),
                        new Vector2((index - 1) * size.x * 0.27f, -size.y * index * 0.04f), 0);
            }
            else if (Style == BossFieldStyle.IceSpike || Style == BossFieldStyle.Prism)
            {
                Detail("FacetedCrystal", game.Art.Diamond, size * 0.98f, Vector2.zero, 0);
                Detail("CrystalSeam", game.Art.Square, new Vector2(0.04f, size.y * 0.7f), Vector2.zero, 0);
            }
            else if (Style == BossFieldStyle.Wind)
            {
                for (int index = 0; index < 3; index++)
                    Detail("WindBlade_" + index, game.Art.Diamond, new Vector2(size.x * 0.45f, size.y * 0.35f),
                        new Vector2((index - 1) * size.x * 0.23f, (index - 1) * size.y * 0.22f), 0);
            }
            else if (Style == BossFieldStyle.Digital)
            {
                for (int index = 0; index < 6; index++)
                    Detail("DigitalBit_" + index, game.Art.Square, new Vector2(size.x * 0.1f, size.y * 0.13f),
                        new Vector2((index % 3 - 1) * size.x * 0.25f, (index / 3 == 0 ? -1 : 1) * size.y * 0.2f), 0);
            }
            else if (Style == BossFieldStyle.Mirror)
            {
                SideSpriteAnimation animation = game.Level.Theme.bossAnimation;
                Sprite sprite = animation != null && animation.IsReady ? animation.Frame(1) : game.Art.Diamond;
                Detail("Hologram_NoCollider", sprite, size * 0.85f, Vector2.zero, 0);
                for (int index = 0; index < 3; index++)
                    Detail("MirrorScan_" + index, game.Art.Square, new Vector2(size.x * 0.85f, 0.035f), new Vector2(0, (index - 1) * size.y * 0.3f), 0);
            }
            else if (Style == BossFieldStyle.Orbit)
            {
                Detail("OrbitalStar", game.Art.Diamond, size * 1.02f, Vector2.zero, 0);
                Detail("StarCross", game.Art.Diamond, size * 0.7f, Vector2.zero, 90);
            }
            else if (Style == BossFieldStyle.Gravity)
            {
                Detail("EventHorizon", game.Art.Ring, size / 0.88f, Vector2.zero, 0);
                Detail("DarkCore", game.Art.Disc, size * 0.72f, Vector2.zero, 0).color = new Color(0.025f, 0.01f, 0.045f, 1);
            }
            else if (Style == BossFieldStyle.Rift || Style == BossFieldStyle.Scan)
            {
                Detail("CutAxis", game.Art.Square, new Vector2(size.x, size.y * 0.24f), Vector2.zero, 0);
                if (Style == BossFieldStyle.Rift)
                    for (int index = 0; index < 2; index++)
                        Detail("RiftMouth_" + index, game.Art.Diamond, new Vector2(size.y * 0.9f, size.y * 2),
                            new Vector2((index == 0 ? -1 : 1) * (size.x * 0.5f - size.y * 0.5f), 0), 0);
            }
            else
            {
                Detail("MoltenBead", game.Art.Disc, size * 0.9f, Vector2.zero, 0);
                Detail("SpiralCoil", game.Art.Ring, size * 0.9f / 0.88f, Vector2.zero, 0);
            }
        }

        private SpriteRenderer Detail(string name, Sprite sprite, Vector2 size, Vector2 position, float rotation)
        {
            Vector2 bounds = sprite.bounds.size;
            SpriteRenderer detail = Visual(name, detailsRoot, sprite,
                new Vector2(size.x / Mathf.Max(0.01f, bounds.x), size.y / Mathf.Max(0.01f, bounds.y)), tint, 77);
            detail.transform.localPosition = position;
            detail.transform.localRotation = Quaternion.Euler(0, 0, rotation);
            return detail;
        }

        private SpriteRenderer Visual(string name, Transform parent, Sprite sprite, Vector2 size, Color color, int order)
        {
            return game.Art.Renderer(name, parent, sprite, color, size, order);
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }

        private void LateUpdate()
        {
            if (!initialized) return;
            if (!SourceValid) { Cancel(); return; }
            if (game.IsPlaying) RefreshVisuals();
        }

        private void RefreshVisuals()
        {
            // A translucent red footprint is shown only during windup; active attacks
            // use their illustrated effects, with no coloured debug edges/countdown.
            if (game.Effects.BossTelegraphsEnabled)
            {
                bool preview = IsWarning && Damage > 0;
                fill.enabled = preview;
                fill.sortingOrder = 85;
                fill.color = new Color(1, 0.035f, 0.07f, 0.21f + Mathf.Sin(elapsed * 7) * 0.035f);
                foreach (SpriteRenderer edge in edges)
                {
                    edge.enabled = false;
                    edge.color = new Color(1, 0.035f, 0.07f, 0.5f);
                    edge.sortingOrder = 85;
                }
                clockFill.enabled = false;
                if (routeRoot != null) routeRoot.gameObject.SetActive(preview);
                detailsRoot.gameObject.SetActive(!IsWarning);
                foreach (SpriteRenderer detail in details)
                {
                    Color activeColor = detail.color; activeColor.a = 0.92f; detail.color = activeColor;
                }
                if (!customArtwork && (Style == BossFieldStyle.Orbit || Style == BossFieldStyle.Molten || Style == BossFieldStyle.Gravity))
                    detailsRoot.localRotation = Quaternion.Euler(0, 0, elapsed * (Style == BossFieldStyle.Gravity ? -65 : 120));
                return;
            }
            if (!game.Effects.AttackTelegraphsEnabled)
            {
                fill.enabled = false;
                foreach (SpriteRenderer edge in edges) edge.enabled = false;
                clockFill.enabled = false;
                if (routeRoot != null) routeRoot.gameObject.SetActive(false);
                detailsRoot.gameObject.SetActive(!IsWarning);
                foreach (SpriteRenderer detail in details)
                {
                    Color activeColor = detail.color;
                    activeColor.a = 0.92f;
                    detail.color = activeColor;
                }
                if (!customArtwork && (Style == BossFieldStyle.Orbit || Style == BossFieldStyle.Molten || Style == BossFieldStyle.Gravity))
                    detailsRoot.localRotation = Quaternion.Euler(0, 0, elapsed * (Style == BossFieldStyle.Gravity ? -65 : 120));
                return;
            }
            float progress = warningDuration > 0 ? Mathf.Clamp01(elapsed / warningDuration) : 1;
            float pulse = 0.5f + Mathf.Sin(elapsed * 18) * 0.5f;
            fill.color = WithAlpha(tint, IsWarning ? 0.1f + progress * 0.1f : Damage > 0 ? 0.32f : 0.2f);
            foreach (SpriteRenderer edge in edges) edge.color = WithAlpha(tint, IsWarning ? 0.6f + pulse * 0.3f : 1);
            clockFill.enabled = IsWarning && warningDuration > 0;
            clockFill.transform.localScale = new Vector3(DamageSize.x * (1 - progress), 0.055f, 1);
            foreach (SpriteRenderer detail in details)
            {
                Color color = detail.color;
                color.a = IsWarning ? 0.22f + progress * 0.2f : 0.92f;
                detail.color = color;
            }
            if (Style == BossFieldStyle.Orbit || Style == BossFieldStyle.Molten || Style == BossFieldStyle.Gravity)
                detailsRoot.localRotation = Quaternion.Euler(0, 0, elapsed * (Style == BossFieldStyle.Gravity ? -65 : 120));
        }

        private void OnDisable()
        {
            if (conveyor != null) conveyor.ConveyorSpeed = 0;
            if (conveyorCollider != null) conveyorCollider.enabled = false;
        }

        private void OnDestroy()
        {
            if (warningSprite != null) Destroy(warningSprite);
            if (warningMask != null) Destroy(warningMask);
            if (owner != null) owner.Died -= Cancel;
            if (counterAnchor != null) counterAnchor.Died -= Cancel;
        }
    }
}

