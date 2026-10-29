using System;
using System.Collections.Generic;
using UnityEngine;

namespace MoriEchoes
{
    [DisallowMultipleComponent]
    public sealed class VerticalSkullGate : MonoBehaviour
    {
        public int Index { get; private set; }
        public Health Health { get; private set; }
        public bool Broken { get; private set; }
        public event Action<VerticalSkullGate> GateBroken;
        private GameManager game;
        private BoxCollider2D barrier;
        private SpriteRenderer shell;
        private SpriteRenderer core;
        private readonly List<SpriteRenderer> artPanels = new List<SpriteRenderer>();
        private float originalHealth;
        private Color originalTint;

        public void Initialize(GameManager owner, VerticalStageDirector director, int index, float width, float height, float hitPoints)
        {
            game = owner; Index = index; originalHealth = hitPoints;
            barrier = gameObject.AddComponent<BoxCollider2D>(); barrier.size = new Vector2(width, height);
            Health = gameObject.AddComponent<Health>(); Health.Initialize(Team.Enemy, hitPoints);
            Health.Damaged += OnDamaged; Health.Died += OnDied;
            shell = game.Art.Renderer("Gate_ThemedCore", transform, game.Art.Side.Concrete,
                game.Level.Theme.structure * 1.75f, new Vector2(width, height), 56);
            originalTint = shell.color;
            Sprite gateArt = game.Level.Theme.Architecture(1);
            if (gateArt != null)
            {
                int panels = Mathf.Max(1, Mathf.CeilToInt(width / Mathf.Max(0.1f, height * gateArt.bounds.size.x / gateArt.bounds.size.y)));
                float panelWidth = width / panels;
                for (int panel = 0; panel < panels; panel++)
                {
                    SpriteRenderer artwork = game.Art.Renderer("EtchedGateModule_" + panel, transform, gateArt, Color.white,
                        new Vector2(panelWidth / gateArt.bounds.size.x, height / gateArt.bounds.size.y), 57);
                    artwork.transform.localPosition = new Vector2(-width * 0.5f + panelWidth * (panel + 0.5f), 0);
                    artPanels.Add(artwork);
                }
            }
            Sprite physicalCore = game.Level.Theme.BossOrnament(1, 1) ?? gateArt ?? game.Art.Side.Armor;
            core = game.Art.Renderer("GatePhysicalLock", transform, physicalCore, Color.white,
                new Vector2(1.35f / physicalCore.bounds.size.x, 1.1f / physicalCore.bounds.size.y), 60);
            core.transform.localPosition = Vector2.zero;
            int motifCount = gateArt != null ? 0 : Mathf.FloorToInt(width / 3.2f);
            for (int indexSkull = 0; indexSkull < motifCount; indexSkull++)
            {
                float x = (indexSkull - (motifCount - 1) * 0.5f) * width / motifCount;
                bool tomb = game.SelectedStage == 5;
                Sprite motif = tomb ? game.Art.Skull : game.SelectedStage == 6 ? game.Art.Ring : game.Art.Diamond;
                Color motifColor = tomb ? indexSkull % 2 == 0 ? Color.white : new Color(0.78f, 0.88f, 1f)
                    : game.SelectedStage == 6 ? Color.Lerp(game.Level.Theme.accent, Color.white, 0.25f) : game.Level.Theme.accent;
                string name = tomb ? "InlaidFillSkull_" : game.SelectedStage == 6 ? "TidalChoirGlyph_" : "GravityPrismGlyph_";
                SpriteRenderer skull = game.Art.Renderer(name + indexSkull, transform, motif, motifColor,
                    new Vector2(width / motifCount * 0.92f, height * 0.85f), 62);
                skull.transform.localPosition = new Vector2(x, 0.02f);
                SpriteRenderer accent = game.Art.Renderer(tomb ? "EyeFire" : "GateEnergyNode", skull.transform, game.Art.Glow,
                    game.Level.Theme.danger, new Vector2(0.2f, 0.2f), 64);
                accent.transform.localPosition = new Vector3(0, tomb ? 0.06f : 0, 0);
            }
            for (int braceIndex = -1; braceIndex <= 1; braceIndex++)
            {
                SpriteRenderer brace = game.Art.Renderer("Gate_MetalClasp", transform, game.Art.Side.Armor,
                    game.Level.Theme.structure * 2, new Vector2(0.45f, height * 1.15f), 65);
                brace.transform.localPosition = new Vector2(braceIndex * width * 0.38f, 0);
                brace.transform.localRotation = Quaternion.Euler(0, 0, braceIndex * -11);
            }
        }

        private void OnDamaged(float amount)
        {
            if (game == null || !game.IsPlaying) return;
            float fraction = Health.Current / Health.Maximum;
            shell.color = Color.Lerp(new Color(0.28f, 0.3f, 0.38f), originalTint, fraction);
            game.Effects.SurfaceImpact((Vector2)transform.position + UnityEngine.Random.insideUnitCircle * 3f, game.Level.Theme.accent);
            game.Environment?.React(transform.position, 5, game.Level.Theme.accent);
            game.Effects.DamageNumber(transform.position, amount, false);
            game.Player.Energy?.Add(amount * 0.08f);
            game.AwardStyle(Mathf.RoundToInt(amount * game.StyleMultiplier * 0.24f));
        }

        private void OnDied()
        {
            if (Broken) return;
            Broken = true;
            if (barrier != null) barrier.enabled = false;
            if (shell != null) shell.enabled = false;
            foreach (SpriteRenderer panel in artPanels) if (panel != null) panel.enabled = false;
            if (core != null) core.enabled = false;
            foreach (SpriteRenderer renderer in GetComponentsInChildren<SpriteRenderer>()) renderer.enabled = false;
            game.Effects.Explosion(transform.position, 4.4f, game.Level.Theme.accent);
            game.Effects.Debris(transform.position, game.Level.Theme.structure);
            GateBroken?.Invoke(this);
        }

        public void ResetGate()
        {
            if (Health == null) return;
            Broken = false;
            Health.Initialize(Team.Enemy, originalHealth);
            barrier.enabled = shell.enabled = core.enabled = true;
            foreach (SpriteRenderer panel in artPanels) if (panel != null) panel.enabled = true;
            foreach (SpriteRenderer renderer in GetComponentsInChildren<SpriteRenderer>()) renderer.enabled = true;
            shell.color = originalTint;
            gameObject.SetActive(true);
        }

        private void OnDestroy() { if (Health != null) { Health.Damaged -= OnDamaged; Health.Died -= OnDied; } }
    }
}
