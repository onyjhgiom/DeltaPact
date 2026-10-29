using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace MoriEchoes
{
    public sealed class CombatEffects : MonoBehaviour
    {
        private sealed class Slot
        {
            public SpriteRenderer Sprite;
            public Vector2 Velocity;
            public Vector3 StartScale;
            public Color Color;
            public float Life;
            public float Duration;
            public bool Expand;
            public bool Warning;
            public float Gravity;
            public Sprite[] Animation;
        }

        // 224 effect slots are partitioned into fixed ranges by category so that a
        // slot's free-list is derivable from its index (warning >=192, encounter
        // 128..191, normal 0..127). This keeps behavior identical while letting
        // Acquire and Update run in O(active) instead of scanning the whole array.
        private readonly Slot[] slots = new Slot[224];
        private readonly List<int> active = new List<int>(224);
        private readonly Stack<int> freeNormal = new Stack<int>();
        private readonly Stack<int> freeEncounter = new Stack<int>();
        private readonly Stack<int> freeWarning = new Stack<int>();
        public int EncounterEmissions { get; private set; }
        public int WarningEmissions { get; private set; }
        // Mob/environment debug overlays stay hidden. Boss-only warnings are intentional.
        public bool AttackTelegraphsEnabled => false;
        public bool BossTelegraphsEnabled => true;
        public int BossWarningEmissions { get; private set; }
        public int BossUltimateEmissions { get; private set; }
        public int BossSkillImpactEmissions { get; private set; }
        public int WaterEmissions { get; private set; }
        public int BossTechniqueEmissions { get; private set; }

        public void BossTechnique(Sprite sprite, Vector2 position, float width, float seconds)
        {
            if (sprite == null) return;
            Slot effect = Acquire(sprite, position, Vector2.one * (width / sprite.bounds.size.x),
                new Color(1, 1, 1, 0.88f), seconds, false, true);
            if (effect == null) return;
            effect.Sprite.sortingOrder = 165;
            BossTechniqueEmissions++;
        }
        public int ActiveCount => active.Count;
        public int Capacity => slots.Length;
        private readonly TextMesh[] numbers = new TextMesh[24];
        private readonly float[] numberLife = new float[24];
        private readonly Stack<int> freeNumbers = new Stack<int>();
        private GameManager game;
        private Light2D muzzleLight;
        private float lightUntil;

        private Sprite ProductionEffect(int index, Sprite fallback)
        {
            Sprite[] sprites = game.Config.combatEffectSprites;
            return game.Config.sideScroller && sprites != null && index < sprites.Length && sprites[index] != null ? sprites[index] : fallback;
        }

        public void Initialize(GameManager owner)
        {
            game = owner;
            if (game.Config.sideScroller && game.Config.sideLightPrefab != null)
            {
                var lamp = Instantiate(game.Config.sideLightPrefab, transform); lamp.name = "MuzzleFlashLight";
                muzzleLight = lamp.GetComponent<Light2D>(); muzzleLight.pointLightOuterRadius = 3;
                muzzleLight.intensity = 0;
            }
            for (int index = 0; index < slots.Length; index++)
            {
                SpriteRenderer renderer = game.Art.Renderer("Effect_" + index, transform, game.Art.Glow, Color.white, Vector2.one, 700);
                renderer.gameObject.SetActive(false);
                slots[index] = new Slot { Sprite = renderer };
                if (index >= 192) freeWarning.Push(index);
                else if (index >= 128) freeEncounter.Push(index);
                else freeNormal.Push(index);
            }
            for (int index = 0; index < numbers.Length; index++)
            {
                var entity = new GameObject("DamageNumber_" + index);
                entity.transform.SetParent(transform, false);
                entity.transform.localScale = Vector3.one * 0.045f;
                var mesh = entity.AddComponent<TextMesh>();
                mesh.font = game.UiFont; mesh.fontSize = 48;
                mesh.anchor = TextAnchor.MiddleCenter;
                mesh.alignment = TextAlignment.Center;
                var renderer = entity.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = game.UiFont.material;
                renderer.sortingOrder = 900;
                numbers[index] = mesh;
                freeNumbers.Push(index);
                entity.SetActive(false);
            }
        }

        private Slot Acquire(Sprite sprite, Vector2 position, Vector2 size, Color color, float duration, bool warning = false, bool encounter = false, bool bossWarning = false)
        {
            if (warning && !(bossWarning ? BossTelegraphsEnabled : AttackTelegraphsEnabled)) return null;
            int index = warning ? (freeWarning.Count > 0 ? freeWarning.Pop() : -1)
                : encounter ? (freeEncounter.Count > 0 ? freeEncounter.Pop() : -1)
                : (freeNormal.Count > 0 ? freeNormal.Pop() : -1);
            if (index < 0) return null;
            Slot slot = slots[index];
            slot.Sprite.sprite = sprite;
            slot.Sprite.flipX = false;
            slot.Sprite.flipY = false;
            slot.Sprite.transform.position = position;
            slot.Sprite.transform.rotation = Quaternion.identity;
            slot.Sprite.transform.localScale = new Vector3(size.x, size.y, 1);
            slot.Sprite.color = color;
            slot.Sprite.sortingOrder = warning ? 85 : 700;
            slot.Velocity = Vector2.zero;
            slot.Color = color; slot.Duration = duration; slot.Life = duration; slot.StartScale = new Vector3(size.x, size.y, 1);
            slot.Expand = false; slot.Warning = warning;
            slot.Gravity = 0;
            slot.Animation = null;
            slot.Sprite.gameObject.SetActive(true);
            active.Add(index);
            if (encounter) EncounterEmissions++;
            if (warning) { WarningEmissions++; if (bossWarning) BossWarningEmissions++; }
            return slot;
        }

        public void PlayerHit(Vector2 position, float amount)
        {
            Sprite impact = ProductionEffect(1, game.Art.Glow);
            Acquire(impact, position, Vector2.one * (2.1f / impact.bounds.size.x), new Color(1, 0.48f, 0.48f, 0.95f), 0.32f);
            Burst(position, new Color(1, 0.24f, 0.26f), 12);
            FloatText(position, "−" + Mathf.CeilToInt(amount) + " HP", new Color(1, 0.3f, 0.3f));
            game.Environment?.React(position, 2.6f, new Color(1, 0.3f, 0.3f));
        }

        public void WaterTrail(Vector2 position, Vector2 direction, Color tint)
        {
            Camera camera = game.CameraRig.Camera;
            Vector3 viewport = camera.WorldToViewportPoint(position);
            if (viewport.x < -0.15f || viewport.x > 1.15f || viewport.y < -0.3f || viewport.y > 1.3f) return;
            Sprite wake = game.Level != null && game.Level.IsSwimStage ? EncounterSprite(game.SelectedStage,1) : null;
            Slot bubble = Acquire(wake != null ? wake : game.Art.Ring, position,
                Vector2.one * (wake != null ? 0.32f / wake.bounds.size.x : Random.Range(0.08f, 0.19f)),
                wake != null ? new Color(1,1,1,0.6f) : Color.Lerp(tint, Color.white, 0.65f), 0.55f);
            if (bubble == null) return;
            bubble.Velocity = direction * 0.45f + Vector2.up * 0.75f;
            bubble.Expand = true;
            WaterEmissions++;
        }

        public void WaterMuzzle(Vector2 position, Vector2 direction, Color tint)
        {
            if (game.Level != null && game.Level.IsSwimStage) Encounter(position,0,0.85f,0.2f,Mathf.Atan2(direction.y,direction.x)*Mathf.Rad2Deg);
            else Muzzle(position, direction, tint);
            for (int index = 0; index < 4; index++)
                WaterTrail(position + Random.insideUnitCircle * 0.25f, direction * (1 + index * 0.3f), tint);
        }

        public void BossWarningDisc(Vector2 center, float radius, float seconds)
        {
            Acquire(game.Art.Disc, center, Vector2.one * (radius * 2 / 0.96f),
                new Color(1, 0.035f, 0.07f, 0.24f), seconds, true, false, true);
        }

        public void BossWarningLane(Vector2 origin, Vector2 direction, float length, float width, float seconds)
        {
            direction.Normalize();
            Slot slot = Acquire(game.Art.Square, origin + direction * length * 0.5f, new Vector2(length, width),
                new Color(1, 0.035f, 0.07f, 0.24f), seconds, true, false, true);
            if (slot != null) slot.Sprite.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        }

        public Sprite EncounterSprite(int stage, int index)
        {
            if (!game.Config.sideScroller || stage < 0 || stage >= game.Config.themes.Length) return null;
            Sprite[] sprites = game.Config.themes[stage].encounterEffects;
            return sprites != null && index >= 0 && index < sprites.Length ? sprites[index] : null;
        }

        public void Encounter(Vector2 position, int index, float width, float seconds, float angle = 0, bool expand = false, bool trail = false)
        {
            Sprite sprite = EncounterSprite(game.SelectedStage, index);
            if (game.SelectedStage < 5 && index == 3 && sprite != null && Mathf.Abs(angle) < 0.01f)
                position += Vector2.up * (sprite.bounds.size.y * width / sprite.bounds.size.x * 0.5f);
            Slot slot = Acquire(sprite != null ? sprite : index == 1 ? game.Art.Ring : game.Art.Glow, position,
                Vector2.one * (width / (sprite != null ? sprite.bounds.size.x : 1)),
                sprite != null ? new Color(1, 1, 1, trail ? 0.35f : index == 3 ? 0.65f : 1) : game.Level.Theme.danger, seconds, false, !trail);
            if (slot == null) return;
            slot.Sprite.transform.rotation = Quaternion.Euler(0, 0, angle);
            slot.Expand = expand;
            slot.Sprite.sortingOrder = index == 1 || index == 3 ? 90 : 510;
        }

        public void DangerArea(Vector2 center, Vector2 size, float duration)
        {
            Color color = game.Level.Theme.danger;
            color.a = 0.23f;
            Acquire(game.Art.Square, center, size, color, duration, true);
            color.a = 0.95f;
            Acquire(game.Art.Square, center + Vector2.up * size.y * 0.5f, new Vector2(size.x, 0.045f), color, duration, true);
            Acquire(game.Art.Square, center - Vector2.up * size.y * 0.5f, new Vector2(size.x, 0.045f), color, duration, true);
        }

        public void BossImpact(Vector2 position, float radius)
        {
            Encounter(position, 2, radius * 1.2f, 0.45f);
            Encounter(position, 1, radius * 2, 0.65f, 0, true);
            Burst(position, game.Level.Theme.danger, 14);
            game.Environment?.React(position, radius, game.Level.Theme.danger);
        }

        public void BossSkillImpact(Vector2 position, BossFieldStyle style, float radius)
        {
            BossImpact(position, radius);
            int artworkIndex = style == BossFieldStyle.Gravity || style == BossFieldStyle.Orbit ? 1
                : style == BossFieldStyle.Wind || style == BossFieldStyle.Scan || style == BossFieldStyle.Rift ? 4
                : style == BossFieldStyle.Fracture || style == BossFieldStyle.Prism ? 6
                : style == BossFieldStyle.IceSpike || style == BossFieldStyle.Hydraulic || style == BossFieldStyle.Fuel ? 3 : 2;
            Sprite art = EncounterSprite(game.SelectedStage, artworkIndex);
            if (art != null)
            {
                float scale = radius * 1.7f / Mathf.Max(0.01f, art.bounds.size.x);
                Slot signature = Acquire(art, position, Vector2.one * scale, Color.white, 0.58f, false, true);
                if (signature != null) signature.Expand = true;
            }
            Color accent = game.Level.Theme.accent;
            Slot outer = Acquire(game.Art.Ring, position, Vector2.one * (radius * 3.1f / 0.88f), accent, 0.62f, false, true);
            Slot inner = Acquire(game.Art.Ring, position, Vector2.one * (radius * 1.85f / 0.88f), game.Level.Theme.danger, 0.44f, false, true);
            if (outer != null) { outer.Expand = true; outer.Sprite.transform.rotation = Quaternion.Euler(0, 0, Time.time * 95); }
            if (inner != null) { inner.Expand = true; inner.Sprite.transform.rotation = Quaternion.Euler(0, 0, -Time.time * 130); }
            Burst(position, accent, 20);
            game.CameraRig.Shake(0.25f);
            game.Environment?.React(position, radius * 1.7f, accent);
            BossSkillImpactEmissions++;
        }

        public void BossUltimate(Vector2 position, float width)
        {
            Sprite[] frames = game.Level.Theme.bossUltimateFrames;
            bool animated = frames != null && frames.Length >= 4 && frames[0] != null;
            Sprite primary = animated ? frames[0] : EncounterSprite(game.SelectedStage, 2) ?? game.Art.Glow;
            Slot slot = Acquire(primary, position, Vector2.one * (width / primary.bounds.size.x),
                Color.white, 1.45f, false, true);
            if (slot == null) return;
            slot.Animation = animated ? frames : null;
            slot.Sprite.sortingOrder = 170;
            Sprite secondaryArt = EncounterSprite(game.SelectedStage, 2);
            if (secondaryArt != null)
            {
                float scale = width * 0.72f / Mathf.Max(0.01f, secondaryArt.bounds.size.x);
                Slot crest = Acquire(secondaryArt, position, Vector2.one * scale, Color.white, 0.92f, false, true);
                if (crest != null) { crest.Sprite.sortingOrder = 174; crest.Expand = true; }
            }
            Slot outer = Acquire(game.Art.Ring, position, Vector2.one * (width * 1.55f / 0.88f), game.Level.Theme.danger, 1.15f, false, true);
            Slot inner = Acquire(game.Art.Ring, position, Vector2.one * (width * 0.95f / 0.88f), game.Level.Theme.accent, 0.8f, false, true);
            if (outer != null) { outer.Sprite.sortingOrder = 168; outer.Expand = true; }
            if (inner != null) { inner.Sprite.sortingOrder = 169; inner.Expand = true; }
            BossUltimateEmissions++;
            game.CameraRig.Shake(0.62f);
            game.Environment?.React(position, width * 0.8f, game.Level.Theme.danger);
            Burst(position, game.Level.Theme.danger, 36);
        }

        public void RewardItem(Vector2 position, int index, float width = 1.1f)
        {
            Sprite[] sprites = game.Config.pickupSprites;
            if (sprites == null || index < 0 || index >= sprites.Length || sprites[index] == null) return;
            Sprite sprite = sprites[index];
            Slot slot = Acquire(sprite, position, Vector2.one * (width / sprite.bounds.size.x), Color.white, 0.9f);
            if (slot != null) slot.Velocity = Vector2.up * 0.7f;
        }

        public void CharacterCast(Vector2 position, float width, bool ultimate = false)
        {
            Sprite sprite = ultimate && game.Character.ultimateSprite != null ? game.Character.ultimateSprite : game.Character.skillSprite;
            Slot slot = Acquire(sprite != null ? sprite : game.Art.Ring, position,
                Vector2.one * (width / (sprite != null ? sprite.bounds.size.x : 1)), sprite != null ? Color.white : game.Character.accent, 0.6f, false, true);
            if (slot != null) { slot.Sprite.sortingOrder = 90; slot.Expand = game.SelectedCharacter != 2; }
        }

        public void Burst(Vector2 position, Color color, int count)
        {
            Sprite impact = ProductionEffect(color.b > color.r ? 4 : 3, null);
            if (impact != null) Acquire(impact, position, Vector2.one * (0.65f / impact.bounds.size.x), Color.white, 0.12f);
            for (int index = 0; index < count; index++)
            {
                float size = Random.Range(0.07f, 0.18f);
                Slot slot = Acquire(game.Art.Glow, position, new Vector2(size * 2.6f, size), color, Random.Range(0.16f, 0.4f));
                if (slot == null) break;
                slot.Velocity = Random.insideUnitCircle.normalized * Random.Range(1f, 5f);
                slot.Sprite.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(slot.Velocity.y, slot.Velocity.x) * Mathf.Rad2Deg);
            }
        }

        public void Landing(Vector2 position)
        {
            Sprite dust = ProductionEffect(7, null);
            if (dust != null) Acquire(dust, position, Vector2.one * (0.9f / dust.bounds.size.x), Color.white, 0.2f);
        }

        public void Muzzle(Vector2 position, Vector2 direction, Color color)
        {
            int weapon = game.Player != null ? game.Player.Weapon.Selected : 0;
            Sprite sprite = ProductionEffect(weapon < 3 ? weapon : weapon == 4 ? 2 : weapon == 5 ? 1 : 0, game.Art.Glow);
            bool imported = sprite != game.Art.Glow;
            Slot slot = Acquire(sprite, position, imported ? Vector2.one * (0.8f / sprite.bounds.size.x) : new Vector2(1.1f, 0.4f), imported ? Color.white : color, 0.085f);
            if (slot != null) slot.Sprite.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
            if (game.Config.sideScroller)
            {
                if (muzzleLight != null) { muzzleLight.transform.position = position; muzzleLight.color = color; lightUntil = Time.time + 0.09f; }
                Slot shell = Acquire(game.Art.Square, position - direction * 0.6f, new Vector2(0.07f, 0.035f), new Color(1, 0.73f, 0.28f), 0.6f);
                if (shell != null) { shell.Velocity = Vector2.up * 3 - direction * 1.7f; shell.Gravity = 10; }
            }
        }

        public void Debris(Vector2 position, Color color)
        {
            Sprite impact = ProductionEffect(6, null);
            if (impact != null) Acquire(impact, position, Vector2.one * (1.6f / impact.bounds.size.x), Color.white, 0.22f);
            for (int index = 0; index < 8; index++)
            {
                Slot part = Acquire(game.Art.Side.Armor, position + Random.insideUnitCircle * 0.3f, new Vector2(0.2f, 0.3f), color, 0.7f);
                if (part != null) { part.Velocity = Random.insideUnitCircle * 5 + Vector2.up * 2; part.Gravity = 12; }
            }
        }

        public void SkillWave(Vector2 position, Color color, float radius)
        {
            Slot ring = Acquire(game.Art.Ring, position, Vector2.one * radius * 2, color, 0.5f);
            if (ring != null) ring.Expand = true;
            Burst(position, color, 16);
            game.Environment?.React(position, radius, color);
        }

        public void SurfaceImpact(Vector2 position, Color color)
        {
            Encounter(position, 0, 0.9f, 0.2f);
            Burst(position, color, 4);
            Slot mark = Acquire(game.Art.Disc, position, new Vector2(0.15f, 0.08f), new Color(0.04f, 0.05f, 0.07f, 0.65f), 5);
            if (mark != null) mark.Sprite.sortingOrder = 22;
            game.Environment?.React(position, 1.3f, color);
        }

        public void Explosion(Vector2 position, float radius, Color color)
        {
            if (game.Level != null && game.Level.Theme.encounterEffects != null)
                Encounter(position, 2, Mathf.Min(5, radius * 1.3f), 0.38f);
            Sprite sprite = ProductionEffect(6, game.Art.Glow);
            Slot flash = Acquire(sprite, position, Vector2.one * (radius * 1.5f / sprite.bounds.size.x), Color.white, 0.32f);
            if (flash != null) flash.Expand = true;
            SkillWave(position, color, radius);
            Debris(position, color);
            Landing(position);
            game.Audio.Play(SoundKind.Blast);
            game.CameraRig.Shake(0.16f);
        }

        public void Ghost(Vector2 position)
        {
            SideSpriteAnimation animation = game.Config.sideScroller ? game.Character.animation : null;
            if (animation != null && animation.IsReady)
            {
                SideSpriteAnimator animator = game.Player.GetComponent<SideSpriteAnimator>();
                Slot ghost = Acquire(animation.Frame(animator != null ? animator.CurrentFrame : 0), position + (animator != null ? animator.VisualOffset : Vector2.zero), Vector2.one * game.Character.build,
                    new Color(0.3f, 0.85f, 1, 0.25f), 0.2f);
                if (ghost != null) ghost.Sprite.flipX = (game.Player.Aim.x > 0) != animation.facesRight;
                return;
            }
            Slot slot = Acquire(game.Config.playerBackSprite != null ? game.Config.playerBackSprite
                : game.Config.playerSprite != null ? game.Config.playerSprite : game.Art.HeroBack, position,
                game.Config.playerSprite != null ? Vector2.one : new Vector2(1.1f, 1.4f), new Color(0.25f, 0.85f, 1f, 0.3f), 0.2f);
            if (slot != null) slot.Sprite.transform.rotation = game.CameraRig.transform.rotation;
        }

        public void Telegraph(Vector2 position, float radius, float seconds, Color color)
        {
            color.a = 0.9f;
            Acquire(game.Art.Ring, position, Vector2.one * (radius / 0.44f), color, seconds, true);
            color.a = 0.18f;
            Acquire(game.Art.Disc, position, Vector2.one * (radius / 0.48f), color, seconds, true);
        }

        public void Pulse(Vector2 position, float radius)
        {
            Sprite sprite = ProductionEffect(5, game.Art.Ring);
            Slot slot = Acquire(sprite, position, Vector2.one * (sprite == game.Art.Ring ? radius / 0.44f : radius * 2 / sprite.bounds.size.x), sprite == game.Art.Ring ? ProceduralArt.Cyan : Color.white, 0.5f);
            if (slot != null) slot.Expand = true;
            Burst(position, ProceduralArt.Cyan, 18);
        }

        public void WarningLine(Vector2 position, Vector2 direction, float length, float seconds)
        {
            Slot slot = Acquire(game.Art.Square, position + direction * length * 0.5f, new Vector2(length, 1.8f),
                new Color(1, 0.28f, 0.15f, 0.22f), seconds, true);
            if (slot != null) slot.Sprite.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        }

        public void DamageNumber(Vector2 position, float amount, bool critical)
        {
            if (freeNumbers.Count == 0) return;
            int index = freeNumbers.Pop();
            numbers[index].text = Mathf.CeilToInt(amount).ToString();
            numbers[index].color = critical ? new Color(1, 0.83f, 0.35f) : new Color(0.75f, 0.95f, 1);
            numbers[index].transform.position = (Vector3)position + game.CameraRig.transform.right * Random.Range(-0.2f, 0.2f)
                + game.CameraRig.transform.up * 0.7f;
            numbers[index].gameObject.SetActive(true);
            numberLife[index] = 0.65f;
        }

        // 任意文字飘字（连杀/里程碑提示用），复用 DamageNumber 的 TextMesh 池，不产生 GC。
        public void FloatText(Vector2 position, string text, Color color)
        {
            if (freeNumbers.Count == 0) return;
            int index = freeNumbers.Pop();
            numbers[index].text = text;
            numbers[index].color = color;
            numbers[index].transform.position = (Vector3)position + game.CameraRig.transform.up * 1.15f;
            numbers[index].gameObject.SetActive(true);
            numberLife[index] = 0.9f;
        }

        public void ClearAll()
        {
            active.Clear();
            freeNormal.Clear(); freeEncounter.Clear(); freeWarning.Clear();
            for (int index = 0; index < slots.Length; index++)
            {
                slots[index].Sprite.gameObject.SetActive(false);
                if (index >= 192) freeWarning.Push(index);
                else if (index >= 128) freeEncounter.Push(index);
                else freeNormal.Push(index);
            }
            freeNumbers.Clear();
            for (int index = 0; index < numbers.Length; index++)
            {
                numbers[index].gameObject.SetActive(false);
                freeNumbers.Push(index);
            }
        }

        private void Update()
        {
            if (game == null || !game.IsPlaying && game.State != RunState.Won && game.State != RunState.Lost) return;
            float elapsed = game.IsPlaying ? Time.deltaTime : Time.unscaledDeltaTime;
            if (muzzleLight != null) muzzleLight.intensity = Mathf.Clamp01((lightUntil - Time.time) / 0.09f) * 3;
            for (int iterator = active.Count - 1; iterator >= 0; iterator--)
            {
                int index = active[iterator];
                Slot slot = slots[index];
                slot.Life -= elapsed;
                if (slot.Life <= 0)
                {
                    slot.Sprite.gameObject.SetActive(false);
                    if (index >= 192) freeWarning.Push(index);
                    else if (index >= 128) freeEncounter.Push(index);
                    else freeNormal.Push(index);
                    active.RemoveAt(iterator);
                    continue;
                }
                float progress = 1f - slot.Life / slot.Duration;
                Color color = slot.Color;
                color.a *= slot.Warning ? 0.65f + 0.35f * Mathf.Sin(progress * 16) : 1f - progress;
                if (slot.Animation != null)
                {
                    slot.Sprite.sprite = slot.Animation[Mathf.Min(slot.Animation.Length - 1, (int)(progress * slot.Animation.Length))];
                    color.a = Mathf.Clamp01((1 - progress) * 3.5f);
                    slot.Sprite.transform.localScale = slot.StartScale * Mathf.Lerp(0.9f, 1.12f, progress);
                }
                slot.Sprite.color = color;
                slot.Velocity += Vector2.down * slot.Gravity * elapsed;
                slot.Sprite.transform.position += (Vector3)slot.Velocity * elapsed;
                if (slot.Expand) slot.Sprite.transform.localScale = slot.StartScale * Mathf.Lerp(0.1f, 1, progress);
            }
            for (int index = 0; index < numbers.Length; index++)
            {
                if (!numbers[index].gameObject.activeSelf) continue;
                numberLife[index] -= elapsed;
                if (numberLife[index] <= 0)
                {
                    numbers[index].gameObject.SetActive(false);
                    freeNumbers.Push(index);
                    continue;
                }
                numbers[index].transform.rotation = game.CameraRig.transform.rotation;
                numbers[index].transform.position += game.CameraRig.transform.up * elapsed * 0.65f;
                Color tint = numbers[index].color; tint.a = Mathf.Min(1, numberLife[index] * 3); numbers[index].color = tint;
            }
        }
    }
}
