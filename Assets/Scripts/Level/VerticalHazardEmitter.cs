using UnityEngine;

namespace MoriEchoes
{
    public sealed class VerticalHazardEmitter : MonoBehaviour
    {
        public bool Active { get; private set; }
        private GameManager game;
        private LevelManager level;
        private int section;
        private int lane;
        private float phase;
        private float clock;
        private float nextHit;
        private SpriteRenderer beam;
        private SpriteRenderer warning;
        private float beamHalfWidth;

        public void Initialize(GameManager owner, LevelManager campaign, int room, int side)
        {
            game = owner; level = campaign; section = room; lane = side;
            phase = (room * 1.43f + side * 1.71f) % 4.5f;
            bool tide = game.SelectedStage == 6;
            beamHalfWidth = tide ? 0.92f : game.SelectedStage == 7 ? 0.66f : 0.48f;
            Sprite warningGlyph = game.SelectedStage == 5 ? game.Art.Skull : tide ? game.Art.Ring : game.Art.Diamond;
            beam = game.Art.Renderer(tide ? "RisingTidalGeyser" : game.SelectedStage == 7 ? "InversionRiftColumn" : "RisingBoneSpikes",
                transform, game.Art.Square, tide ? level.Theme.accent * 1.7f : level.Theme.danger,
                new Vector2(beamHalfWidth * 2, 8.5f), 72);
            beam.enabled = false;
            warning = game.Art.Renderer("VerticalHazard_WarningGlyph", transform, warningGlyph,
                level.Theme.accent, new Vector2(1.05f, 0.85f), 73);
            warning.enabled = game.Effects.AttackTelegraphsEnabled;
            Sprite emitterArt = level.Theme.Architecture(2);
            if (emitterArt != null)
            {
                SpriteRenderer housing = game.Art.Renderer("ThemedTrapEmitter", transform, emitterArt, Color.white,
                    new Vector2(1.45f / emitterArt.bounds.size.x, 1.55f / emitterArt.bounds.size.y), 70);
                housing.transform.localPosition = Vector2.down * 0.5f;
            }
            if (level.Theme.encounterEffects != null && level.Theme.encounterEffects.Length > 3 && level.Theme.encounterEffects[3] != null)
            {
                beam.sprite = level.Theme.encounterEffects[3];
                beam.color = Color.white;
                beam.transform.localScale = new Vector3(beamHalfWidth * 2 / beam.sprite.bounds.size.x, 8.5f / beam.sprite.bounds.size.y, 1);
            }
        }

        private void Update()
        {
            if (game == null || !game.IsPlaying || game.Player == null) return;
            clock += Time.deltaTime;
            float cycle = Mathf.Repeat(clock + phase, 5.1f);
            Active = cycle >= 1.4f && cycle <= 2.7f;
            bool nearby = Mathf.Abs(game.Player.transform.position.y - transform.position.y) < 8.5f;
            beam.enabled = Active && nearby;
            warning.enabled = game.Effects.AttackTelegraphsEnabled && nearby && cycle < 1.4f;
            if (warning.enabled)
            {
                float pulse = 0.8f + Mathf.Sin(clock * 18) * 0.14f;
                warning.transform.localScale = new Vector3(pulse, pulse * 0.7f, 1);
            }
            if (!beam.enabled || Mathf.Abs(game.Player.transform.position.x - transform.position.x) > beamHalfWidth) return;
            Rigidbody2D playerBody = game.Player.GetComponent<Rigidbody2D>();
            if (game.SelectedStage == 6 && playerBody != null)
                playerBody.linearVelocity = new Vector2(playerBody.linearVelocity.x, Mathf.Max(playerBody.linearVelocity.y, 5.5f));
            else if (game.SelectedStage == 7) game.Player.Motor.InvertGravity(0.22f);
            if (Time.time < nextHit) return;
            if (game.Player.Health.TakeDamage(22 * game.DamageScale, Team.Enemy, false, IncomingHitKind.Hazard))
            {
                nextHit = Time.time + 0.8f;
                game.Effects.SurfaceImpact(game.Player.transform.position, level.Theme.danger);
                game.Environment?.React(game.Player.transform.position, 3.2f, level.Theme.danger);
            }
        }

        public void ResetEmitter() { clock = 0; nextHit = 0; Active = false; }
    }
}
