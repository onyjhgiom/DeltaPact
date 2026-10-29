using System;
using UnityEngine;

namespace MoriEchoes
{
    [DisallowMultipleComponent]
    public sealed class WeaponCapsuleDirector : MonoBehaviour
    {
        [Range(1, 32)] public int PoolSize = 8;
        public bool AutomaticSupplies = true;
        public bool EnemyDropsEnabled = true;
        [Range(0, 1)] public float KillDropChance = 0.12f;
        [Min(1)] public int GuaranteedKills = 9;
        [Min(0)] public float EnemyDropCooldown = 1.5f;
        [Min(0)] public float FirstSupplyDelay = 2f;
        [Min(1)] public float SupplyInterval = 26f;
        [Min(1)] public float SupplyHeight = 5.5f;
        [Min(1)] public float CapsuleLifetime = 24f;
        [Min(0.05f)] public float PickupRadius = 0.4f;

        public PlayerController Collector { get; private set; }
        public int KillsSinceDrop => dropCounter.KillsSinceDrop;
        public int ActiveCount
        {
            get
            {
                int count = 0;
                if (capsules != null)
                    foreach (WeaponCapsule capsule in capsules)
                        if (capsule != null && capsule.gameObject.activeSelf) count++;
                return count;
            }
        }
        public event Action<WeaponCapsule> Spawned;
        public event Action<CombatPowerupKind> Collected;

        internal Collider2D CollectorCollider { get; private set; }
        internal bool CanSimulate => isActiveAndEnabled && game != null && game.IsPlaying && game.ControlsReady
            && Time.timeScale > 0 && Collector != null && game.Player == Collector && Collector.isActiveAndEnabled
            && Collector.Health != null && !Collector.Health.IsDead && Collector.Weapon != null
            && Collector.Weapon.isActiveAndEnabled && !ContextChanged();

        private readonly WeaponCapsuleDropCounter dropCounter = new WeaponCapsuleDropCounter();
        private GameManager game;
        private WeaponCapsule[] capsules;
        private Transform poolRoot;
        private LevelManager level;
        private int stage;
        private int deaths;
        private int cursor;
        private int supplySequence;
        private float supplyRemaining;
        private float dropCooldown;
        private bool terminalCleared;

        public void Initialize(GameManager owner)
        {
            if (owner == null) throw new ArgumentNullException(nameof(owner));
            if (owner.Config == null || owner.Art == null || owner.Player == null || owner.Player.Weapon == null)
                throw new InvalidOperationException("Initialize capsules after Player.Initialize and before entering gameplay.");
            ClearAll();
            if (poolRoot != null) { poolRoot.gameObject.SetActive(false); Destroy(poolRoot.gameObject); }
            game = owner;
            BindContext();
            poolRoot = new GameObject("WeaponCapsules_Pool").transform;
            poolRoot.SetParent(transform, false);
            capsules = new WeaponCapsule[Mathf.Clamp(PoolSize, 1, 32)];
            for (int index = 0; index < capsules.Length; index++)
            {
                var entity = new GameObject("WeaponCapsule_" + index);
                entity.transform.SetParent(poolRoot, false);
                entity.SetActive(false);
                capsules[index] = entity.AddComponent<WeaponCapsule>();
                capsules[index].Initialize(this, game);
            }
            ResetSchedule();
            terminalCleared = false;
        }

        private bool ContextChanged()
            => game.Player != Collector || game.Level != level || game.SelectedStage != stage
                || (game.Level != null ? game.Level.Deaths : 0) != deaths;

        private void BindContext()
        {
            Collector = game.Player; level = game.Level; stage = game.SelectedStage;
            deaths = level != null ? level.Deaths : 0;
            CollectorCollider = Collector != null ? (Collider2D)Collector.GetComponent<CapsuleCollider2D>() : null;
            if (CollectorCollider == null && Collector != null) CollectorCollider = Collector.GetComponent<CircleCollider2D>();
        }

        private void SyncContext()
        {
            if (game == null || !ContextChanged()) return;
            ClearAll();
            BindContext();
            terminalCleared = false;
        }

        private void Update()
        {
            if (game == null || capsules == null) return;
            SyncContext();
            bool ended = Collector == null || Collector.Health == null || Collector.Health.IsDead
                || game.State == RunState.Menu || game.State == RunState.Lost || game.State == RunState.Won;
            if (ended)
            {
                if (!terminalCleared) { ClearAll(); terminalCleared = true; }
                return;
            }
            terminalCleared = false;
            if (!CanSimulate) return;
            dropCooldown = Mathf.Max(0, dropCooldown - Time.deltaTime);
            if (!AutomaticSupplies) return;
            supplyRemaining = Mathf.Max(0, supplyRemaining - Time.deltaTime);
            if (supplyRemaining > 0) return;
            Vector2 landing = Collector.transform.position;
            float facing = Mathf.Abs(Collector.Aim.x) > 0.05f ? Mathf.Sign(Collector.Aim.x) : 1;
            landing.x += facing * 5.5f;
            Camera camera = game.CameraRig.Camera;
            float extent = camera.orthographicSize * camera.aspect;
            landing.x = Mathf.Clamp(landing.x, camera.transform.position.x - extent + 1, camera.transform.position.x + extent - 1);
            if (game.Config.sideScroller && level != null) landing.x = level.Vertical != null ? Mathf.Clamp(landing.x, -6.5f, 6.5f) : Mathf.Clamp(landing.x, 1, Mathf.Max(1, level.Length - 1));
            else if (!game.Config.sideScroller)
            {
                landing.x = Mathf.Clamp(landing.x, -game.Config.arenaHalfSize.x + 1, game.Config.arenaHalfSize.x - 1);
                landing.y = Mathf.Clamp(landing.y, -game.Config.arenaHalfSize.y + 1, game.Config.arenaHalfSize.y - 1);
            }
            CombatPowerupKind kind = (CombatPowerupKind)(1 + supplySequence % 4);
            bool spawned = false;
            if (level != null && level.Swim != null)
            {
                // 水中补给：直接出现在玩家所在水层（而非半空），便于游泳拾取
                Vector2 inWater = new Vector2(landing.x, Collector.transform.position.y);
                spawned = TrySpawn(inWater, kind, true, inWater.y);
            }
            else if (SpawnSupply(landing, kind)) spawned = true;
            if (spawned)
            {
                supplySequence = (supplySequence + 1) % 4;
                supplyRemaining = Mathf.Max(1, SupplyInterval);
            }
            else supplyRemaining = 1;
        }

        public bool OnEnemyKilled(Vector2 position, EnemyKind kind = EnemyKind.Chaser)
        {
            if (game == null || capsules == null) return false;
            SyncContext();
            if (!CanSimulate || !EnemyDropsEnabled || kind == EnemyKind.Boss) return false;
            bool shouldDrop = dropCounter.RegisterKill(UnityEngine.Random.value, KillDropChance, GuaranteedKills);
            if (!shouldDrop || dropCooldown > 0) return false;
            CombatPowerupKind powerup = (CombatPowerupKind)UnityEngine.Random.Range(1, 5);
            if (!Drop(position, powerup)) return false;
            dropCounter.Reset();
            dropCooldown = Mathf.Max(0, EnemyDropCooldown);
            return true;
        }

        public bool OnEnemyKilled(EnemyAI enemy)
            => enemy != null && enemy.Health != null && enemy.Health.IsDead && OnEnemyKilled(enemy.transform.position, enemy.Kind);

        public bool SpawnSupply(Vector2 landingPosition, CombatPowerupKind kind = CombatPowerupKind.Spread)
        {
            SyncContext();
            return TrySpawn(landingPosition + Vector2.up * Mathf.Max(1, SupplyHeight), kind, true, landingPosition.y);
        }

        public bool Drop(Vector2 position, CombatPowerupKind kind = CombatPowerupKind.Spread)
        {
            SyncContext();
            return TrySpawn(position + Vector2.up * 0.3f, kind, false, position.y);
        }

        private bool TrySpawn(Vector2 position, CombatPowerupKind kind, bool supply, float arenaFloor)
        {
            if (!CanSimulate || capsules == null || !CombatPowerupRules.IsDefined(kind)
                || float.IsNaN(position.x) || float.IsNaN(position.y) || float.IsInfinity(position.x) || float.IsInfinity(position.y)) return false;
            for (int offset = 0; offset < capsules.Length; offset++)
            {
                int index = (cursor + offset) % capsules.Length;
                WeaponCapsule capsule = capsules[index];
                if (capsule == null || capsule.gameObject.activeSelf) continue;
                cursor = (index + 1) % capsules.Length;
                capsule.Activate(position, kind, supply, arenaFloor, Mathf.Max(1, CapsuleLifetime));
                Spawned?.Invoke(capsule);
                return true;
            }
            return false;
        }

        internal void NotifyCollected(CombatPowerupKind kind, Vector2 position)
        {
            if (game.Effects != null) game.Effects.Burst(position, CombatPowerup.ColorFor(kind), 12);
            if (game.Audio != null) game.Audio.Play(SoundKind.Upgrade);
            Collected?.Invoke(kind);
        }

        public void ClearAll(bool clearPowerup = true)
        {
            if (capsules != null)
                foreach (WeaponCapsule capsule in capsules)
                    if (capsule != null) capsule.Despawn();
            if (clearPowerup && Collector != null && Collector.Weapon != null) Collector.Weapon.ClearPowerup();
            ResetSchedule();
        }

        private void ResetSchedule()
        {
            dropCounter.Reset(); cursor = supplySequence = 0; dropCooldown = 0;
            supplyRemaining = Mathf.Max(0, FirstSupplyDelay);
        }

        private void OnDisable() { ClearAll(); }

        private void OnDestroy()
        {
            ClearAll();
            Spawned = null; Collected = null;
            if (poolRoot != null) Destroy(poolRoot.gameObject);
        }
    }
}
