using System;
using System.Collections.Generic;
using UnityEngine;

namespace MoriEchoes
{
    public enum EncounterHazardKind { FallingRocks, GroundSpikes }

    [DisallowMultipleComponent]
    [DefaultExecutionOrder(30)]
    public sealed class EncounterHazardDirector : MonoBehaviour
    {
        private sealed class TriggerPoint
        {
            public EncounterHazardKind Kind;
            public Vector2 Position;
            public int Segment;
            public bool Used;
        }

        private sealed class Hazard
        {
            public EncounterHazardKind Kind;
            public Vector2 Point;
            public GameObject Root;
            public SpriteRenderer Marker;
            public SpriteRenderer Body;
            public float Warning;
            public float Remaining;
            public float NextHit;
            public bool Active;
        }

        private readonly struct Explosion
        {
            public Vector2 Position { get; }
            public float Radius { get; }
            public float Damage { get; }
            public Explosion(DestructibleCover source)
            { Position = source.transform.position; Radius = source.ExplosionRadius; Damage = source.ExplosionDamage; }
        }

        public const int MaximumActiveHazards = 6;
        public const int MaximumProps = 24;
        public const int MaximumQueuedExplosions = 24;
        public const float RockWarningSeconds = 1.3f;
        public const float SpikeWarningSeconds = 1.15f;
        private GameManager game;
        private LevelManager level;
        private Transform propRoot;
        private Transform hazardRoot;
        private int stage;
        private int deaths;
        private int segmentIndex;
        private float clock;
        private readonly List<DestructibleCover> covers = new List<DestructibleCover>(MaximumProps);
        private readonly List<GameObject> adoptedOriginals = new List<GameObject>();
        private readonly List<TriggerPoint> triggers = new List<TriggerPoint>();
        private readonly List<Hazard> hazards = new List<Hazard>(MaximumActiveHazards);
        private readonly Queue<Explosion> explosions = new Queue<Explosion>(MaximumQueuedExplosions);

        public int ActiveHazards => hazards.Count;
        public int PendingExplosions => explosions.Count;
        public int TriggerCount => triggers.Count;
        public int WarningsIssued { get; private set; }
        public int HazardsResolved { get; private set; }
        public int HazardHits { get; private set; }
        public int ExplosionsResolved { get; private set; }
        public int PropsDestroyed { get; private set; }
        public IReadOnlyList<DestructibleCover> Covers => covers;
        public int IntactCoverCount
        {
            get
            {
                int count = 0;
                foreach (DestructibleCover cover in covers) if (cover != null && !cover.Explosive && !cover.IsDestroyed) count++;
                return count;
            }
        }
        public int IntactBarrelCount
        {
            get
            {
                int count = 0;
                foreach (DestructibleCover cover in covers) if (cover != null && cover.Explosive && !cover.IsDestroyed) count++;
                return count;
            }
        }
        public bool CanSimulate => isActiveAndEnabled && game != null && level != null && game.Level == level
            && (level.SecretRoutes == null || !level.SecretRoutes.Inside)
            && game.SelectedStage == stage && level.Deaths == deaths && game.IsPlaying && Time.timeScale > 0
            && game.Player != null && game.Player.Health != null && !game.Player.Health.IsDead;

        public void Initialize(GameManager owner, LevelManager campaign)
        {
            if (owner == null) throw new ArgumentNullException(nameof(owner));
            if (campaign == null || campaign.Segments == null) throw new ArgumentException("Initialize after the level geometry is built.", nameof(campaign));
            ClearAll();
            game = owner; level = campaign; stage = level.Stage; deaths = level.Deaths; segmentIndex = level.SegmentIndex;
            propRoot = new GameObject("EncounterProps").transform; propRoot.SetParent(level.transform, false);
            hazardRoot = new GameObject("EncounterHazards").transform; hazardRoot.SetParent(level.transform, false);
            AdoptExistingBarrels();
            int count = Mathf.Min(level.Segments.Length, 8);
            for (int index = 0; index < count; index++)
            {
                SegmentDefinition segment = level.Segments[index];
                if (segment == null || segment.boss) continue;
                float height = stage == 2 ? 1.45f : 1.25f;
                AddProp(index, new Vector2(Position(segment, 11.8f), height * 0.5f), new Vector2(1.15f, height), false, game.Level.Theme.SegmentProp(1));
                if (stage == 2 || stage == 3) AddProp(index, new Vector2(Position(segment, 21), 0.65f), new Vector2(1.1f, 1.3f), false);
                if (stage == 0 || stage == 4)
                    AddProp(index, new Vector2(Position(segment, 21.7f), 0.6f), new Vector2(0.75f, 1.1f), true, game.Level.Theme.SegmentProp(0));
                if (stage == 1 && index % 2 == 1)
                    AddProp(index, new Vector2(Position(segment, 22.5f), 0.6f), new Vector2(0.75f, 1.1f), true, game.Level.Theme.SegmentProp(0));
                float offset = (segment.layout - 1) * 0.4f;
                bool snow = stage == 2;
                AddTrigger(index, stage == 1 || stage == 3 ? EncounterHazardKind.GroundSpikes : EncounterHazardKind.FallingRocks,
                    new Vector2(Position(segment, (snow ? 13.8f : 15) + offset), 0.1f));
                AddTrigger(index, snow && index % 2 == 0 ? EncounterHazardKind.FallingRocks : EncounterHazardKind.GroundSpikes,
                    new Vector2(Position(segment, (stage == 1 || stage == 3 ? 23 : 22) - offset), 0.1f));
            }
            ResetCounters();
        }

        private void AdoptExistingBarrels()
        {
            StageInteractable[] interactables = level.GetComponentsInChildren<StageInteractable>(true);
            foreach (StageInteractable barrel in interactables)
            {
                if (barrel == null || !barrel.Barrel || !barrel.gameObject.activeSelf || covers.Count >= MaximumProps) continue;
                int index = SegmentAt(barrel.transform.position.x);
                if (index < 0 || level.Segments[index].boss) continue;
                SpriteRenderer renderer = barrel.GetComponent<SpriteRenderer>();
                AddProp(index, barrel.transform.position, new Vector2(0.75f, 1.1f), true, renderer != null ? renderer.sprite : null);
                adoptedOriginals.Add(barrel.gameObject); barrel.gameObject.SetActive(false);
            }
            foreach (SpriteRenderer renderer in level.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (renderer.name != "BarrelEnergyStripe" || !renderer.gameObject.activeSelf || renderer.GetComponentInParent<DestructibleCover>() != null) continue;
                int originalCount = adoptedOriginals.Count;
                for (int originalIndex = 0; originalIndex < originalCount; originalIndex++)
                {
                    GameObject original = adoptedOriginals[originalIndex];
                    if (Vector2.Distance(original.transform.position, renderer.transform.position) > 0.8f) continue;
                    adoptedOriginals.Add(renderer.gameObject); renderer.gameObject.SetActive(false); break;
                }
            }
        }

        private void AddProp(int index, Vector2 point, Vector2 size, bool explosive, Sprite sprite = null)
        {
            if (covers.Count >= MaximumProps) return;
            var entity = new GameObject((explosive ? "EncounterBarrel_" : "DestructibleCover_") + index + "_" + covers.Count);
            entity.transform.SetParent(propRoot, false); entity.transform.position = point;
            DestructibleCover cover = entity.AddComponent<DestructibleCover>();
            sprite = level.Theme.SegmentProp(explosive ? 0 : 1) ?? sprite;
            if (sprite == null && game.Config.scenerySprites != null && game.Config.scenerySprites.Length > 0)
                sprite = game.Config.scenerySprites[explosive ? 0 : Mathf.Min(4, game.Config.scenerySprites.Length - 1)];
            cover.Initialize(game, this, index, size, explosive ? 18 : 42, explosive, sprite); covers.Add(cover);
        }

        private void AddTrigger(int index, EncounterHazardKind kind, Vector2 point)
        {
            if (triggers.Count < 16) triggers.Add(new TriggerPoint { Segment = index, Kind = kind, Position = point });
        }

        public bool TriggerAt(EncounterHazardKind kind, Vector2 point, int targetSegment = -1)
        {
            if (!CanSimulate || hazards.Count >= MaximumActiveHazards || !Finite(point.x) || !Finite(point.y)
                || kind != EncounterHazardKind.FallingRocks && kind != EncounterHazardKind.GroundSpikes) return false;
            int index = targetSegment < 0 ? level.SegmentIndex : targetSegment;
            if (index != level.SegmentIndex || index < 0 || index >= level.Segments.Length || level.Segments[index].boss) return false;
            SegmentDefinition segment = level.Segments[index];
            point.x = Mathf.Clamp(point.x, segment.start + 2, segment.End - 2);
            point.y = Mathf.Clamp(point.y, 0.05f, 4.5f);
            var entity = new GameObject(kind + "_FixedPoint"); entity.transform.SetParent(hazardRoot, false);
            entity.transform.position = point;
            float width = kind == EncounterHazardKind.FallingRocks ? 2.05f : 3.2f;
            Color danger = level.Theme.danger;
            var hazard = new Hazard { Kind = kind, Point = point, Root = entity,
                Warning = kind == EncounterHazardKind.FallingRocks ? RockWarningSeconds : SpikeWarningSeconds };
            hazard.Marker = game.Art.Renderer("GroundWarning", entity.transform, game.Art.Square,
                new Color(danger.r, danger.g, danger.b, 0.85f), new Vector2(width, 0.1f), 65);
            hazard.Marker.enabled = game.Effects.AttackTelegraphsEnabled;
            int artSlot = kind == EncounterHazardKind.FallingRocks ? 0 : 1;
            Sprite hazardArt = level.Theme.hazardAnimations != null && level.Theme.hazardAnimations.Length > artSlot
                ? level.Theme.hazardAnimations[artSlot].Frame(1) : game.Art.Diamond;
            hazard.Body = game.Art.Renderer(kind == EncounterHazardKind.FallingRocks ? "FallingRock" : "GroundSpike",
                entity.transform, hazardArt, Color.white, new Vector2((kind == EncounterHazardKind.FallingRocks ? 1.8f : width) / hazardArt.bounds.size.x,
                    (kind == EncounterHazardKind.FallingRocks ? 1.8f : 1.2f) / hazardArt.bounds.size.y), 66);
            hazard.Body.enabled = false;
            hazards.Add(hazard); WarningsIssued++;
            game.Effects.DangerArea(point + Vector2.up * 0.35f, new Vector2(width, 0.7f), hazard.Warning);
            if (kind == EncounterHazardKind.FallingRocks)
                game.Effects.DangerArea(point + Vector2.up * 3.5f, new Vector2(width, 7), hazard.Warning);
            return true;
        }

        private void Update()
        {
            if (game == null || level == null) return;
            if (game.Level != level || game.SelectedStage != stage) { ClearTransient(); return; }
            if (level.Deaths != deaths) ResetForCheckpoint();
            if (level.SegmentIndex != segmentIndex) { ClearTransient(); segmentIndex = level.SegmentIndex; }
            if (game.State == RunState.Menu || game.State == RunState.Lost || game.State == RunState.Won) { ClearTransient(); return; }
            if (!CanSimulate) return;
            clock += Time.deltaTime;
            ResolveExplosions();
            if (!CanSimulate) return;
            if (level.EncounterStarted)
            {
                foreach (TriggerPoint trigger in triggers)
                {
                    if (trigger.Used || trigger.Segment != level.SegmentIndex || game.Player.transform.position.x < trigger.Position.x - 8) continue;
                    if (game.Player.transform.position.x > trigger.Position.x + 2) { trigger.Used = true; continue; }
                    trigger.Used = TriggerAt(trigger.Kind, trigger.Position, trigger.Segment);
                }
            }
            for (int index = hazards.Count - 1; index >= 0 && CanSimulate; index--)
                if (StepHazard(hazards[index], Time.deltaTime)) { Retire(hazards[index]); hazards.RemoveAt(index); HazardsResolved++; }
        }

        private bool StepHazard(Hazard hazard, float deltaTime)
        {
            if (!hazard.Active)
            {
                hazard.Warning -= deltaTime;
                Color tint = hazard.Marker.color; tint.a = 0.4f + Mathf.Sin(clock * 14) * 0.25f; hazard.Marker.color = tint;
                if (hazard.Warning > 0) return false;
                hazard.Active = true; hazard.Body.enabled = true; hazard.Remaining = hazard.Kind == EncounterHazardKind.FallingRocks ? 1 : 1.25f;
                hazard.Body.transform.position = hazard.Point + Vector2.up * (hazard.Kind == EncounterHazardKind.FallingRocks ? 6.5f : 0.475f);
                return false;
            }
            hazard.Remaining -= deltaTime;
            Bounds dangerBounds;
            if (hazard.Kind == EncounterHazardKind.FallingRocks)
            {
                Vector2 previous = hazard.Body.transform.position;
                Vector2 next = previous + Vector2.down * (10 * deltaTime);
                hazard.Body.transform.position = next;
                dangerBounds = new Bounds((previous + next) * 0.5f, new Vector3(1.65f, 1.65f + previous.y - next.y, 2));
                foreach (DestructibleCover cover in covers)
                {
                    if (cover == null || cover.IsDestroyed || !dangerBounds.Intersects(cover.BlockingBounds)) continue;
                    cover.ApplyDamage(30); game.Effects.SurfaceImpact(next, level.Theme.danger); return true;
                }
                if (next.y < hazard.Point.y - 0.8f) return true;
            }
            else dangerBounds = new Bounds(hazard.Point + Vector2.up * 0.475f, new Vector3(3.0f, 1.05f, 2));
            Collider2D playerCollider = game.Player.GetComponent<CapsuleCollider2D>();
            if (playerCollider != null && playerCollider.enabled && dangerBounds.Intersects(playerCollider.bounds) && clock >= hazard.NextHit)
            {
                if (game.Player.Health.TakeDamage((hazard.Kind == EncounterHazardKind.FallingRocks ? 24 : 19) * game.DamageScale, Team.Enemy, false, IncomingHitKind.Hazard))
                { HazardHits++; hazard.NextHit = clock + 0.8f; }
                if (hazard.Kind == EncounterHazardKind.FallingRocks) return true;
            }
            return hazard.Remaining <= 0;
        }

        internal void NotifyDestroyed(DestructibleCover cover)
        {
            PropsDestroyed++;
            if (cover.Explosive && CanSimulate && explosions.Count < MaximumQueuedExplosions) explosions.Enqueue(new Explosion(cover));
        }

        private void ResolveExplosions()
        {
            int budget = MaximumQueuedExplosions;
            while (explosions.Count > 0 && budget-- > 0 && CanSimulate)
            {
                Explosion explosion = explosions.Dequeue(); ExplosionsResolved++;
                game.Effects.Explosion(explosion.Position, explosion.Radius, level.Theme.danger);
                EnemyAI[] snapshot = game.Enemies.ToArray();
                foreach (EnemyAI enemy in snapshot)
                {
                    if (!CanSimulate) break;
                    if (enemy == null || enemy.Health == null || enemy.Health.IsDead || !enemy.gameObject.activeInHierarchy
                        || Vector2.Distance(explosion.Position, enemy.transform.position) > explosion.Radius) continue;
                    enemy.Health.TakeDamage(explosion.Damage, Team.Player);
                }
                if (!CanSimulate) break;
                foreach (DestructibleCover cover in covers)
                {
                    if (cover == null || cover.IsDestroyed || Vector2.Distance(explosion.Position, cover.transform.position) > explosion.Radius) continue;
                    cover.ApplyDamage(explosion.Damage);
                }
                game.Audio.Play(SoundKind.Blast); game.CameraRig.Shake(0.18f);
            }
        }

        public void ResetForCheckpoint()
        {
            ClearTransient();
            if (level == null) return;
            deaths = level.Deaths; segmentIndex = level.SegmentIndex; clock = 0;
            foreach (TriggerPoint trigger in triggers) if (trigger.Segment >= level.CheckpointSegment) trigger.Used = false;
            foreach (DestructibleCover cover in covers) if (cover != null && cover.SegmentIndex >= level.CheckpointSegment) cover.ResetForCheckpoint();
            ResetCounters();
        }

        public void ClearTransient()
        {
            foreach (Hazard hazard in hazards) Retire(hazard);
            hazards.Clear(); explosions.Clear();
        }

        public void ClearAll()
        {
            ClearTransient();
            foreach (GameObject original in adoptedOriginals) if (original != null) original.SetActive(true);
            adoptedOriginals.Clear(); covers.Clear(); triggers.Clear();
            if (propRoot != null) { propRoot.gameObject.SetActive(false); Destroy(propRoot.gameObject); }
            if (hazardRoot != null) { hazardRoot.gameObject.SetActive(false); Destroy(hazardRoot.gameObject); }
            propRoot = hazardRoot = null;
            ResetCounters();
        }

        private void ResetCounters() { WarningsIssued = HazardsResolved = HazardHits = ExplosionsResolved = PropsDestroyed = 0; }
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        private static float Position(SegmentDefinition segment, float offset)
            => Mathf.Lerp(segment.start + 2, segment.End - 2, Mathf.InverseLerp(2, 26, offset));
        private int SegmentAt(float horizontal)
        {
            for (int index = 0; index < level.Segments.Length; index++)
                if (horizontal >= level.Segments[index].start && horizontal < level.Segments[index].End) return index;
            return -1;
        }
        private void Retire(Hazard hazard)
        {
            if (hazard.Root == null) return;
            hazard.Root.SetActive(false); Destroy(hazard.Root);
        }
        private void OnDisable() { ClearTransient(); }
        private void OnDestroy() { ClearAll(); }
    }
}
