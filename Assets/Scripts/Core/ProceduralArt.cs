using System;
using System.Collections.Generic;
using UnityEngine;

namespace MoriEchoes
{
    public sealed class ProceduralArt : IDisposable
    {
        public static readonly Color Cyan = new Color(0.24f, 0.86f, 1f);
        public static readonly Color Danger = new Color(1f, 0.32f, 0.21f);
        public static readonly Color Ink = new Color(0.035f, 0.065f, 0.11f);
        public Sprite Square { get; }
        public Sprite Disc { get; }
        public Sprite Ring { get; }
        public Sprite Glow { get; }
        public Sprite Diamond { get; }
        public Sprite Hero { get; }
        public Sprite HeroBack { get; }
        public Sprite ScopeVignette { get; }
        public Sprite SideSky { get; }
        public Sprite SideMountain { get; }
        public Sprite Gun { get; }
        public Sprite Skull { get; }
        public Sprite[] GeneratedWeapons { get; } = new Sprite[9];
        public Sprite[] Enemies { get; }
        public SideArt Side { get; private set; }
        private readonly List<UnityEngine.Object> owned = new List<UnityEngine.Object>();
        private readonly Material spriteMaterial;

        public ProceduralArt(GameConfig config)
        {
            spriteMaterial = config.sideScroller && config.sideSpriteMaterial != null ? config.sideSpriteMaterial : config.spriteMaterial;
            if (spriteMaterial == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
                if (shader == null) shader = Shader.Find("Sprites/Default");
                if (shader == null) throw new InvalidOperationException("No supported unlit sprite shader found. Assign Sprite Material in GameConfig.");
                spriteMaterial = new Material(shader) { name = "MoriEchoes_RuntimeSpriteMaterial" };
                owned.Add(spriteMaterial);
            }
            Square = Paint("Square", 4, (horizontal, vertical) => Color.white);
            SideSky = Paint("SideAtmosphere", 128, (horizontal, vertical) => Color.Lerp(new Color(0.32f, 0.43f, 0.7f), Color.white, Mathf.Clamp01(0.8f - vertical)));
            SideMountain = Paint("SideMountain", 128, (horizontal, vertical) => vertical < 0.48f - Mathf.Abs(horizontal + 0.08f) * 1.7f
                ? Color.Lerp(new Color(0.35f, 0.45f, 0.65f), Color.white, Mathf.Clamp01(vertical + 0.65f)) : Color.clear);
            Disc = Paint("Disc", 64, (horizontal, vertical) => new Color(1, 1, 1,
                Mathf.Clamp01((0.48f - Mathf.Sqrt(horizontal * horizontal + vertical * vertical)) * 64)));
            Ring = Paint("Ring", 128, (horizontal, vertical) => new Color(1, 1, 1,
                Mathf.Clamp01((0.018f - Mathf.Abs(Mathf.Sqrt(horizontal * horizontal + vertical * vertical) - 0.44f)) * 128)));
            Glow = Paint("SoftGlow", 64, (horizontal, vertical) => new Color(1, 1, 1,
                Mathf.Pow(Mathf.Clamp01(1f - Mathf.Sqrt(horizontal * horizontal + vertical * vertical) * 2), 1.7f)));
            Diamond = Paint("Shard", 48, (horizontal, vertical) => Mathf.Abs(horizontal) + Mathf.Abs(vertical) < 0.45f
                ? Color.Lerp(new Color(0.24f, 0.7f, 1), Color.white, Mathf.Clamp01(vertical + 0.5f)) : Color.clear);
            Skull = Paint("Generated_FossilGateSkull", 192, PaintSkull, true);
            Hero = Paint("Hero_ProceduralPlaceholder", 128, PaintHero);
            HeroBack = Paint("HeroBack_ProceduralPlaceholder", 128, (horizontal, vertical) =>
                Ellipse(horizontal, vertical - 0.23f, 0.23f, 0.23f)
                    ? Color.Lerp(new Color(0.52f, 0.65f, 0.8f), Color.white, Mathf.Clamp01(vertical + 0.5f))
                    : PaintHero(horizontal, vertical));
            ScopeVignette = Paint("ScopeVignette", 256, (horizontal, vertical) => new Color(0.01f, 0.025f, 0.045f,
                Mathf.SmoothStep(0, 0.94f, Mathf.InverseLerp(0.18f, 0.3f, Mathf.Sqrt(horizontal * horizontal + vertical * vertical)))));
            Gun = Paint("Rifle_ProceduralPlaceholder", 128, (horizontal, vertical) =>
            {
                if (horizontal > -0.32f && horizontal < 0.38f && Mathf.Abs(vertical) < 0.15f)
                    return vertical > 0.03f ? new Color(0.72f, 0.85f, 0.93f) : new Color(0.16f, 0.24f, 0.33f);
                if (horizontal > 0.15f && horizontal < 0.5f && Mathf.Abs(vertical) < 0.07f) return Cyan;
                if (horizontal > -0.05f && horizontal < 0.08f && vertical > -0.42f && vertical < -0.12f)
                    return new Color(0.12f, 0.18f, 0.27f);
                if (horizontal > -0.5f && horizontal < -0.28f && Mathf.Abs(vertical) < 0.22f) return new Color(0.23f, 0.31f, 0.4f);
                return Color.clear;
            });
            GeneratedWeapons[6] = Paint("Weapon_NeonLaserBlade", 192, (horizontal, vertical) =>
            {
                float blade = Mathf.Abs(vertical - (0.1f + horizontal * 0.1f));
                if (horizontal > -0.25f && horizontal < 0.45f && blade < 0.075f) return Color.Lerp(new Color(0.18f, 0.8f, 1), Color.white, 0.72f);
                if (horizontal > -0.42f && horizontal < -0.16f && Mathf.Abs(vertical) < 0.16f) return new Color(0.12f, 0.2f, 0.31f);
                if (horizontal > -0.2f && horizontal < -0.11f && Mathf.Abs(vertical) < 0.28f) return new Color(0.95f, 0.62f, 0.25f);
                if (horizontal > 0.38f && horizontal < 0.48f && blade < 0.14f) return new Color(0.85f, 0.96f, 1);
                return Color.clear;
            }, true);
            GeneratedWeapons[7] = Paint("Weapon_StormArcCaster", 192, (horizontal, vertical) =>
            {
                float core = Mathf.Abs(vertical - Mathf.Sin(horizontal * 19) * 0.16f);
                if (horizontal > -0.35f && horizontal < 0.38f && core < 0.06f) return Color.Lerp(new Color(0.42f, 0.57f, 0.82f), Color.white, 0.6f);
                if (Mathf.Abs(horizontal + 0.1f) < 0.06f && Mathf.Abs(vertical) < 0.34f) return new Color(0.12f, 0.19f, 0.3f);
                if (horizontal >= 0.35f && horizontal < 0.5f && core < 0.2f) return new Color(0.55f, 0.88f, 1);
                return Color.clear;
            }, true);
            GeneratedWeapons[8] = Paint("Weapon_TriplePulseCrossbow", 192, (horizontal, vertical) =>
            {
                if (horizontal > -0.4f && horizontal < 0.32f && Mathf.Abs(vertical) < 0.1f) return new Color(0.26f, 0.3f, 0.42f);
                if (Mathf.Abs(horizontal) < 0.035f && Mathf.Abs(vertical) < 0.37f) return new Color(0.75f, 0.83f, 0.94f);
                if (horizontal > -0.04f && horizontal < 0.42f && Mathf.Abs(vertical - 0.25f) < 0.025f) return new Color(1f, 0.35f, 0.66f);
                if (horizontal > -0.04f && horizontal < 0.42f && Mathf.Abs(vertical + 0.25f) < 0.025f) return new Color(1f, 0.35f, 0.66f);
                if (horizontal > 0.28f && horizontal < 0.49f && Mathf.Abs(vertical) < 0.035f) return new Color(0.95f, 0.9f, 1);
                return Color.clear;
            }, true);
            Enemies = new Sprite[5];
            for (int index = 0; index < Enemies.Length; index++)
            {
                int variant = index;
                Enemies[index] = Paint("Enemy_" + index, 96, (horizontal, vertical) => PaintEnemy(horizontal, vertical, variant));
            }
            if (config.sideScroller) Side = new SideArt(this, config.characters);
        }

        private static Color PaintHero(float horizontal, float vertical)
        {
            if (Ellipse(horizontal, vertical + 0.29f, 0.12f, 0.19f) && Mathf.Abs(horizontal) > 0.035f)
                return vertical < -0.32f ? new Color(0.1f, 0.16f, 0.25f) : new Color(0.65f, 0.71f, 0.78f);
            if (Ellipse(horizontal, vertical + 0.03f, 0.23f, 0.24f))
            {
                if (Mathf.Abs(horizontal) > 0.15f) return new Color(0.8f, 0.85f, 0.91f);
                if (Mathf.Abs(horizontal) < 0.025f) return Cyan;
                return vertical > -0.08f ? new Color(0.19f, 0.29f, 0.42f) : new Color(0.08f, 0.13f, 0.22f);
            }
            if (Ellipse(horizontal, vertical - 0.23f, 0.23f, 0.23f))
            {
                if (vertical > 0.27f || horizontal < -0.12f || horizontal > 0.13f)
                    return Color.Lerp(new Color(0.66f, 0.77f, 0.89f), Color.white, Mathf.Clamp01(vertical + horizontal + 0.5f));
                if (vertical > 0.19f && vertical < 0.23f && Mathf.Abs(horizontal) > 0.04f && Mathf.Abs(horizontal) < 0.105f)
                    return new Color(0.12f, 0.55f, 0.9f);
                return new Color(0.98f, 0.85f, 0.81f);
            }
            return Color.clear;
        }

        private static Color PaintSkull(float horizontal, float vertical)
        {
            float head = horizontal * horizontal / 0.2f + (vertical - 0.13f) * (vertical - 0.13f) / 0.24f;
            if (head > 1 && !(vertical < -0.18f && vertical > -0.4f && Mathf.Abs(horizontal) < 0.29f)) return Color.clear;
            if (vertical > -0.18f)
            {
                if (Mathf.Abs(horizontal) < 0.025f || Mathf.Abs(Mathf.Abs(horizontal) - 0.29f) < 0.028f) return new Color(0.13f, 0.18f, 0.23f);
                if (vertical < 0.11f && Mathf.Abs(horizontal) > 0.12f && Mathf.Abs(horizontal) < 0.255f) return new Color(1f, 0.28f, 0.14f);
                if (vertical < 0.015f && Mathf.Abs(horizontal) < 0.115f) return new Color(0.12f, 0.14f, 0.2f);
                return Color.Lerp(new Color(0.31f, 0.37f, 0.43f), new Color(0.9f, 0.88f, 0.74f), Mathf.Clamp01(vertical + 0.6f));
            }
            float tooth = Mathf.Repeat((horizontal + 0.3f) * 20, 1);
            if (Mathf.Abs(tooth - 0.5f) < 0.04f && vertical < -0.22f) return new Color(0.16f, 0.19f, 0.24f);
            return new Color(0.76f, 0.77f, 0.7f);
        }

        public Sprite WeaponSprite(int index) => index >= 0 && index < GeneratedWeapons.Length ? GeneratedWeapons[index] : null;

        private static Color PaintEnemy(float horizontal, float vertical, int variant)
        {
            Color armor = variant == 1 ? new Color(0.35f, 0.24f, 0.42f) : variant == 2
                ? new Color(0.24f, 0.32f, 0.37f) : new Color(0.3f, 0.34f, 0.4f);
            if (Ellipse(horizontal, vertical, variant >= 3 ? 0.43f : 0.32f, 0.34f))
            {
                float edge = Mathf.Clamp01((horizontal + vertical + 0.6f) * 0.7f);
                if (Mathf.Abs(horizontal) < 0.07f && Mathf.Abs(vertical) < 0.1f) return Danger * 1.5f;
                if (Mathf.Abs(vertical) < 0.025f) return new Color(0.1f, 0.12f, 0.17f);
                return Color.Lerp(armor * 0.5f, armor * 1.7f, edge);
            }
            if (Ellipse(horizontal, vertical - 0.31f, 0.18f, 0.13f))
                return Mathf.Abs(vertical - 0.3f) < 0.02f ? Danger : armor * 1.3f;
            if (vertical < -0.23f && vertical > -0.48f && Mathf.Abs(horizontal) > 0.065f && Mathf.Abs(horizontal) < 0.2f)
                return armor * 0.6f;
            return Color.clear;
        }

        private static bool Ellipse(float horizontal, float vertical, float radiusX, float radiusY)
            => horizontal * horizontal / (radiusX * radiusX) + vertical * vertical / (radiusY * radiusY) < 1f;

        public Sprite Paint(string name, int resolution, Func<float, float, Color> painter, bool normalMapped = false)
        {
            var texture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false)
            { name = name, filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color[resolution * resolution];
            for (int row = 0; row < resolution; row++)
                for (int column = 0; column < resolution; column++)
                    pixels[row * resolution + column] = painter((column + 0.5f) / resolution - 0.5f, (row + 0.5f) / resolution - 0.5f);
            texture.SetPixels(pixels);
            texture.Apply(false, true);
            SecondarySpriteTexture[] secondary = null;
            if (normalMapped)
            {
                var normal = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false, true)
                { name = name + "_NormalMap", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
                var normals = new Color[pixels.Length];
                for (int row = 0; row < resolution; row++)
                    for (int column = 0; column < resolution; column++)
                    {
                        float left = pixels[row * resolution + Mathf.Max(0, column - 1)].a;
                        float right = pixels[row * resolution + Mathf.Min(resolution - 1, column + 1)].a;
                        float down = pixels[Mathf.Max(0, row - 1) * resolution + column].a;
                        float up = pixels[Mathf.Min(resolution - 1, row + 1) * resolution + column].a;
                        var direction = new Vector3((left - right) * 2.5f, (down - up) * 2.5f, 1).normalized;
                        normals[row * resolution + column] = new Color(direction.x * 0.5f + 0.5f, direction.y * 0.5f + 0.5f, direction.z * 0.5f + 0.5f, 1);
                    }
                normal.SetPixels(normals); normal.Apply(false, true); owned.Add(normal);
                secondary = new[] { new SecondarySpriteTexture { name = "_NormalMap", texture = normal } };
            }
            var sprite = Sprite.Create(texture, new Rect(0, 0, resolution, resolution), new Vector2(0.5f, 0.5f), resolution,
                0, SpriteMeshType.FullRect, Vector4.zero, false, secondary);
            sprite.name = name;
            owned.Add(sprite); owned.Add(texture);
            return sprite;
        }

        public SpriteRenderer Renderer(string name, Transform parent, Sprite sprite, Color color, Vector2 size, int order)
        {
            var entity = new GameObject(name);
            entity.transform.SetParent(parent, false);
            entity.transform.localScale = new Vector3(size.x, size.y, 1);
            var renderer = entity.AddComponent<SpriteRenderer>();
            renderer.sharedMaterial = spriteMaterial;
            renderer.sprite = sprite; renderer.color = color; renderer.sortingOrder = order;
            return renderer;
        }

        public void BuildArena(Transform parent, Vector2 halfSize)
        {
            Renderer("ExteriorGround_NoCollision", parent, Square, new Color(0.045f, 0.07f, 0.1f), new Vector2(400, 400), -1010);
            Renderer("Floor", parent, Square, new Color(0.055f, 0.085f, 0.12f), halfSize * 2, -1000);
            var random = new System.Random(4707);
            for (int row = -(int)halfSize.y; row < halfSize.y; row += 2)
            {
                for (int column = -(int)halfSize.x; column < halfSize.x; column += 2)
                {
                    float shade = (float)random.NextDouble() * 0.018f;
                    SpriteRenderer tile = Renderer("ConcreteTile", parent, Square,
                        new Color(0.075f + shade, 0.105f + shade, 0.145f + shade), new Vector2(1.96f, 1.96f), -990);
                    tile.transform.localPosition = new Vector3(column + 1, row + 1, 0);
                    if (random.NextDouble() < 0.22)
                    {
                        SpriteRenderer crack = Renderer("Crack", parent, Square, new Color(0.035f, 0.055f, 0.08f),
                            new Vector2(0.025f, 0.8f), -980);
                        crack.transform.localPosition = tile.transform.localPosition;
                        crack.transform.localRotation = Quaternion.Euler(0, 0, random.Next(-70, 70));
                    }
                }
            }
            for (int index = 0; index < 70; index++)
            {
                Vector2 position = new Vector2(((float)random.NextDouble() * 2 - 1) * halfSize.x,
                    ((float)random.NextDouble() * 2 - 1) * halfSize.y);
                if (position.sqrMagnitude < 9) continue;
                float width = 0.1f + (float)random.NextDouble() * 0.45f;
                SpriteRenderer rubble = Renderer("DecorativeRubble_NoCollision", parent, Square,
                    new Color(0.14f, 0.18f, 0.22f), new Vector2(width, width * 0.65f), -950);
                rubble.transform.localPosition = position;
                rubble.transform.localRotation = Quaternion.Euler(0, 0, random.Next(0, 180));
            }
            Wall(parent, new Vector2(0, halfSize.y + 0.5f), new Vector2(halfSize.x * 2 + 2, 1));
            Wall(parent, new Vector2(0, -halfSize.y - 0.5f), new Vector2(halfSize.x * 2 + 2, 1));
            Wall(parent, new Vector2(halfSize.x + 0.5f, 0), new Vector2(1, halfSize.y * 2));
            Wall(parent, new Vector2(-halfSize.x - 0.5f, 0), new Vector2(1, halfSize.y * 2));
            SpriteRenderer landing = Renderer("LandingRing", parent, Ring, new Color(0.18f, 0.42f, 0.52f, 0.5f), new Vector2(5, 5), -930);
            landing.transform.localPosition = Vector3.zero;
            for (int index = 0; index < 4; index++)
            {
                SpriteRenderer marker = Renderer("LandingMark", parent, Square, new Color(0.2f, 0.65f, 0.75f, 0.5f), new Vector2(0.06f, 0.9f), -920);
                marker.transform.localPosition = Quaternion.Euler(0, 0, index * 90) * Vector3.up * 2.7f;
                marker.transform.localRotation = Quaternion.Euler(0, 0, index * 90);
            }
        }

        private void Wall(Transform parent, Vector2 position, Vector2 size)
        {
            SpriteRenderer wall = Renderer("ArenaWall", parent, Square, new Color(0.12f, 0.2f, 0.27f), size, -800);
            wall.transform.localPosition = position;
            var collider = wall.gameObject.AddComponent<BoxCollider2D>();
            collider.size = Vector2.one;
            SpriteRenderer edge = Renderer("EnergyEdge", wall.transform, Square, Cyan * new Color(1, 1, 1, 0.6f),
                size.x > size.y ? new Vector2(1, 0.06f) : new Vector2(0.06f, 1), -790);
            edge.transform.localPosition = Vector3.zero;
        }

        public void Dispose()
        {
            foreach (UnityEngine.Object asset in owned) if (asset != null) UnityEngine.Object.Destroy(asset);
            owned.Clear();
        }
    }
}
