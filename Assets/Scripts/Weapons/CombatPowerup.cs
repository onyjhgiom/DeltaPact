using System;
using UnityEngine;

namespace MoriEchoes
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-10)]
    public sealed class CombatPowerup : MonoBehaviour
    {
        public bool IsActive => state.IsActive;
        public CombatPowerupKind Kind => state.Kind;
        public CombatPowerupProfile Profile => CombatPowerupRules.Profile(Kind);
        public Color Tint => IsActive ? ColorFor(Kind) : Color.white;
        public float Remaining => state.Remaining;
        public float Duration => state.Duration;
        public float Fraction => Duration > 0 ? Mathf.Clamp01(Remaining / Duration) : 0;
        public bool UsesEnergyAmmo => IsActive;
        public SideSpriteAnimation Animation => game != null && game.Config.sideScroller ? game.Character.poweredAnimation : null;
        public event Action<CombatPowerup> Changed;

        private readonly CombatPowerupState state = new CombatPowerupState();
        private GameManager game;
        private PlayerController player;
        private Health health;
        private LevelManager level;
        private int stage;
        private int deaths;
        private Transform visualRoot;
        private SpriteRenderer halo;
        private SpriteRenderer orbit;
        private SpriteRenderer glow;
        private SpriteRenderer silhouette;
        private SpriteRenderer sourceVisual;
        private SpriteRenderer[] plates;
        private float phase;

        public void Initialize(GameManager owner, PlayerController controller)
        {
            if (owner == null || controller == null) throw new ArgumentNullException(owner == null ? nameof(owner) : nameof(controller));
            Clear();
            if (health != null) health.Died -= Clear;
            game = owner; player = controller; health = controller.Health;
            if (health != null) health.Died += Clear;
            RememberContext();
            if (visualRoot == null) BuildVisuals();
            sourceVisual = null;
        }

        public bool TryActivate(CombatPowerupKind kind, float duration = 0)
        {
            if (!isActiveAndEnabled || game == null || player == null || health == null || health.IsDead
                || !game.ControlsReady || Time.timeScale <= 0 || game.Player != player
                || player.Weapon == null || !player.Weapon.isActiveAndEnabled) return false;
            if (ContextChanged()) { Clear(); RememberContext(); }
            if (!state.Activate(kind, duration)) return false;
            phase = 0;
            Changed?.Invoke(this);
            return true;
        }

        public void Clear()
        {
            bool changed = state.Clear();
            if (visualRoot != null) visualRoot.gameObject.SetActive(false);
            if (changed) Changed?.Invoke(this);
        }

        private bool ContextChanged()
            => game.Level != level || game.SelectedStage != stage || (game.Level != null ? game.Level.Deaths : 0) != deaths;

        private void RememberContext()
        {
            level = game.Level; stage = game.SelectedStage;
            deaths = level != null ? level.Deaths : 0;
        }

        private void Update()
        {
            if (game == null || player == null || health == null) { Clear(); return; }
            if (ContextChanged()) { Clear(); RememberContext(); }
            if (health.IsDead || game.Player != player || game.State == RunState.Menu
                || game.State == RunState.Lost || game.State == RunState.Won) { Clear(); return; }
            if (!game.IsPlaying || Time.timeScale <= 0) return;
            if (state.Tick(Time.deltaTime))
            {
                if (visualRoot != null) visualRoot.gameObject.SetActive(false);
                Changed?.Invoke(this);
            }
            if (IsActive) phase += Time.deltaTime;
        }

        public static Color ColorFor(CombatPowerupKind kind)
        {
            switch (kind)
            {
                case CombatPowerupKind.Spread: return new Color(1f, 0.5f, 0.12f);
                case CombatPowerupKind.Rapid: return new Color(0.18f, 0.95f, 1f);
                case CombatPowerupKind.Pierce: return new Color(0.72f, 0.48f, 1f);
                case CombatPowerupKind.Blast: return new Color(1f, 0.25f, 0.4f);
                default: return Color.white;
            }
        }

        private void BuildVisuals()
        {
            visualRoot = new GameObject("CombatPowerup_EnergyArmor").transform;
            visualRoot.SetParent(transform, false);
            halo = game.Art.Renderer("EnergyHalo", visualRoot, game.Art.Ring, Color.white, new Vector2(1.65f, 2.1f), 99);
            orbit = game.Art.Renderer("EnergyOrbit", visualRoot, game.Art.Ring, Color.white, new Vector2(1.9f, 0.6f), 104);
            glow = game.Art.Renderer("EnergyGlow", visualRoot, game.Art.Glow, Color.white, new Vector2(2.1f, 2.5f), 98);
            silhouette = game.Art.Renderer("EnergySilhouette", visualRoot, game.Art.Hero, Color.white, new Vector2(1.15f, 1.6f), 99);
            plates = new SpriteRenderer[6];
            for (int index = 0; index < plates.Length; index++)
            {
                plates[index] = game.Art.Renderer("EnergyPlate_" + index, visualRoot, game.Art.Diamond,
                    Color.white, index < 2 ? new Vector2(0.35f, 0.22f) : new Vector2(0.18f, 0.42f), 104);
                float horizontal = index % 2 == 0 ? -1 : 1;
                plates[index].transform.localPosition = new Vector2(horizontal * (index < 2 ? 0.34f : 0.22f),
                    index < 2 ? 0.28f : index < 4 ? -0.08f : -0.48f);
                plates[index].transform.localRotation = Quaternion.Euler(0, 0, horizontal * 18);
            }
            visualRoot.gameObject.SetActive(false);
        }

        private void LateUpdate()
        {
            if (visualRoot == null || game == null || player == null) return;
            bool visible = IsActive && !health.IsDead && game.State != RunState.Menu
                && (!game.Config.sideScroller || game.CameraRig.Side.Mode != ViewMode.IronSight);
            visualRoot.gameObject.SetActive(visible);
            if (!visible) return;
            if (sourceVisual == null)
            {
                Transform source = transform.Find("AnimeAnimatedSprite");
                if (source == null) source = transform.Find("Body");
                if (source != null) sourceVisual = source.GetComponent<SpriteRenderer>();
            }
            float pulse = 0.5f + 0.5f * Mathf.Sin(phase * 8);
            float flicker = Remaining < 2 ? 0.55f + 0.45f * Mathf.Sin(phase * 28) : 1;
            Color tint = Tint;
            float build = game.Config.sideScroller ? game.Character.build : 1;
            visualRoot.rotation = game.CameraRig.transform.rotation;
            int order = sourceVisual != null ? sourceVisual.sortingOrder : game.CameraRig.SortOrder(transform.position);
            glow.color = new Color(tint.r, tint.g, tint.b, (0.16f + pulse * 0.08f) * flicker);
            halo.color = new Color(tint.r, tint.g, tint.b, 0.55f * flicker);
            orbit.color = new Color(tint.r, tint.g, tint.b, 0.7f * flicker);
            halo.transform.localScale = new Vector3(1.65f, 2.1f, 1) * (build * (1 + pulse * 0.04f));
            orbit.transform.localScale = new Vector3(1.9f, 0.6f, 1) * build;
            orbit.transform.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(phase * 2) * 25);
            glow.sortingOrder = order - 3; halo.sortingOrder = order - 2; orbit.sortingOrder = order + 3;
            silhouette.sortingOrder = order - 1;
            silhouette.color = new Color(tint.r, tint.g, tint.b, 0.55f * flicker);
            if (sourceVisual != null)
            {
                silhouette.sprite = sourceVisual.sprite;
                silhouette.flipX = sourceVisual.flipX; silhouette.flipY = sourceVisual.flipY;
                silhouette.transform.SetPositionAndRotation(sourceVisual.transform.position, sourceVisual.transform.rotation);
                Vector3 parentScale = visualRoot.lossyScale;
                Vector3 sourceScale = sourceVisual.transform.lossyScale;
                silhouette.transform.localScale = new Vector3(sourceScale.x / Mathf.Max(0.001f, Mathf.Abs(parentScale.x)),
                    sourceScale.y / Mathf.Max(0.001f, Mathf.Abs(parentScale.y)), 1) * 1.08f;
            }
            foreach (SpriteRenderer plate in plates)
            {
                plate.color = new Color(tint.r, tint.g, tint.b, (0.5f + pulse * 0.3f) * flicker);
                plate.sortingOrder = order + 2;
            }
        }

        private void OnDisable() { Clear(); }

        private void OnDestroy()
        {
            if (health != null) health.Died -= Clear;
            Clear();
            Changed = null;
            if (visualRoot != null) Destroy(visualRoot.gameObject);
        }
    }
}
