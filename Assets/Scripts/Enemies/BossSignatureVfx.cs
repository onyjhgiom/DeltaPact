using System.Collections.Generic;
using UnityEngine;

namespace MoriEchoes
{
    public sealed class BossSignatureVfx : MonoBehaviour
    {
        private enum PartMotion { Fixed, Rotor, Orbit, Hinge, Pulse, Ember, Scan }

        private sealed class SignaturePart
        {
            public SpriteRenderer Renderer;
            public Vector2 Position;
            public Vector2 Scale;
            public Vector2 OrbitRadius;
            public Color Color;
            public float Angle;
            public float Rate;
            public float Offset;
            public int AnchorIndex = -1;
            public PartMotion Motion;
        }

        public int ThemeIndex { get; private set; }
        public int FormIndex { get; private set; }
        public string SignatureName { get; private set; }
        public int PartCount => parts.Count;
        public bool UsesIllustratedParts { get; private set; }
        public string FormName => boss != null ? boss.FormName : string.Empty;
        private readonly List<SignaturePart> parts = new List<SignaturePart>();
        private GameManager game;
        private SideBossController boss;
        private Transform root;
        private float clock;
        private float attackPulse;
        private bool visible = true;
        private Color metal;
        private Color accent;
        private Color energy;

        public void Initialize(GameManager manager, SideBossController controller)
        {
            game = manager;
            boss = controller;
            ThemeIndex = Mathf.Clamp(game.SelectedStage, 0, 4);
            metal = Color.Lerp(game.Level.Theme.structure, Color.white, 0.5f);
            accent = game.Level.Theme.accent;
            energy = game.Level.Theme.danger;
            visible = true;
            clock = 0;
            SetForm(boss.Phase);
        }

        public void SetForm(int phase)
        {
            if (game == null || boss == null) return;
            if (root != null) { root.gameObject.SetActive(false); Destroy(root.gameObject); }
            parts.Clear();
            FormIndex = Mathf.Clamp(phase - 1, 0, 2);
            SignatureName = boss.FormName;
            root = new GameObject("BossForm_" + ThemeIndex + "_" + (FormIndex + 1) + "_" + SignatureName).transform;
            root.SetParent(transform, false);
            root.gameObject.SetActive(visible);
            attackPulse = 1;
            UsesIllustratedParts = game.Level.Theme.BossOrnament(phase, 3) != null;
            if (UsesIllustratedParts) { BuildIllustratedForm(phase); return; }
            switch (ThemeIndex)
            {
                case 0: BuildCity(); break;
                case 1: BuildFactory(); break;
                case 2: BuildIce(); break;
                case 3: BuildCyber(); break;
                case 4: BuildFinal(); break;
            }
        }

        public void PlayAttack() { attackPulse = 1; }

        private void BuildIllustratedForm(int phase)
        {
            ThemeProfile theme = game.Level.Theme;
            float growth = 0.9f + FormIndex * 0.1f;
            Part("IllustratedArmourClaw", theme.BossOrnament(phase, 0), new Vector2(-1.15f, 0),
                new Vector2(1.35f, 1.65f) * growth, Color.white, -5, PartMotion.Hinge, 2.5f);
            Part("IllustratedReactorCuirass", theme.BossOrnament(phase, 1), new Vector2(0, 0.25f),
                new Vector2(0.95f, 1.05f) * growth, Color.white);
            Part("IllustratedBackArmament", theme.BossOrnament(phase, 2), new Vector2(1.03f, 0.85f),
                new Vector2(2.05f, 2.25f) * growth, Color.white, 5, PartMotion.Hinge, 1.5f, 100);
            Part("IllustratedCrown", theme.BossOrnament(phase, 3), new Vector2(0, 1.6f),
                new Vector2(1.1f, 1.15f) * growth, Color.white);
        }

        public void SetVisible(bool value)
        {
            visible = value;
            if (root != null) root.gameObject.SetActive(value);
        }

        private void BuildCity()
        {
            for (int sideIndex = 0; sideIndex < 2; sideIndex++)
            {
                float side = sideIndex == 0 ? -1 : 1;
                float shoulder = FormIndex == 0 ? 1.2f : 1.45f;
                Part("ShoulderPlate_" + sideIndex, game.Art.Square, new Vector2(side * shoulder, 0.8f),
                    new Vector2(FormIndex == 0 ? 0.75f : 0.95f, 0.55f), metal, side * -12);
                Part("ForearmServo_" + sideIndex, game.Art.Square, new Vector2(side * 1.35f, 0.15f),
                    new Vector2(0.24f, 1), metal, side * 12);
                Part("HeavyGauntlet_" + sideIndex, game.Art.Square, new Vector2(side * 1.5f, -0.35f),
                    new Vector2(0.68f, 0.8f), metal, side * 8, PartMotion.Hinge, side * 6);
                for (int knuckle = 0; knuckle < 3; knuckle++)
                    Part("Knuckle_" + sideIndex + "_" + knuckle, game.Art.Square,
                        new Vector2(side * 1.63f, -0.56f + knuckle * 0.2f), new Vector2(0.18f, 0.12f), accent);
                if (FormIndex >= 1)
                {
                    for (int fin = 0; fin < 3; fin++)
                        Part("OpenArmourVent_" + sideIndex + "_" + fin, game.Art.Square,
                            new Vector2(side * (1.55f + fin * 0.18f), 0.65f + fin * 0.22f),
                            new Vector2(0.38f, 0.12f), metal, side * (20 + fin * 12));
                    Part("ExposedShoulderCoil_" + sideIndex, game.Art.Diamond, new Vector2(side * 1.3f, 0.8f),
                        new Vector2(0.25f, 0.45f), energy, side * 20, PartMotion.Pulse, 4);
                }
                if (FormIndex == 2)
                {
                    Part("SiegeRamHorn_" + sideIndex, game.Art.Diamond, new Vector2(side * 1.82f, 1.2f),
                        new Vector2(0.55f, 1.55f), metal, side * -35);
                    Part("FrontalRamShield_" + sideIndex, game.Art.Square, new Vector2(side * 1.88f, -0.05f),
                        new Vector2(0.7f, 1.3f), metal, side * 18);
                    Part("RamEdge_" + sideIndex, game.Art.Square, new Vector2(side * 2.19f, 0),
                        new Vector2(0.08f, 1.15f), energy, side * 18, PartMotion.Pulse, 5);
                }
            }
            Part("ChestPlate", game.Art.Square, new Vector2(0, 0.2f), new Vector2(0.95f, 0.5f), metal);
            if (FormIndex >= 1)
            {
                Part("CrackedArmourCore", game.Art.Diamond, new Vector2(0, 0.25f), new Vector2(0.4f, 0.68f),
                    energy, 0, PartMotion.Pulse, 5);
                Part("ArmourFault", game.Art.Square, new Vector2(0.15f, 0.22f), new Vector2(0.04f, 0.6f), Color.white, 28);
            }
            if (FormIndex == 2)
                for (int index = 0; index < 3; index++)
                    Part("SiegeCrown_" + index, game.Art.Diamond, new Vector2((index - 1) * 0.42f, 1.7f),
                        new Vector2(0.28f, index == 1 ? 0.9f : 0.55f), metal, (index - 1) * -15);
        }

        private void BuildFactory()
        {
            if (FormIndex == 0)
            {
                Part("HydraulicFlywheel", game.Art.Ring, new Vector2(0, 0.2f), new Vector2(2, 2), metal, 0, PartMotion.Rotor, 24, 92);
                for (int sideIndex = 0; sideIndex < 2; sideIndex++)
                {
                    float side = sideIndex == 0 ? -1 : 1;
                    Part("PistonCylinder_" + sideIndex, game.Art.Square, new Vector2(side * 1.25f, 0.5f),
                        new Vector2(0.48f, 1.15f), metal);
                    Part("PistonRod_" + sideIndex, game.Art.Square, new Vector2(side * 1.25f, -0.2f),
                        new Vector2(0.18f, 1.05f), accent, 0, PartMotion.Pulse, 3);
                    Part("ClampJaw_" + sideIndex, game.Art.Square, new Vector2(side * 1.05f, -0.75f),
                        new Vector2(0.95f, 0.24f), metal, side * -10);
                    Part("ClampTooth_" + sideIndex, game.Art.Square, new Vector2(side * 0.63f, -0.57f),
                        new Vector2(0.17f, 0.5f), energy);
                }
            }
            else if (FormIndex == 1)
            {
                Frame("OpenFurnace", Vector2.up * 0.2f, new Vector2(1.5f, 1.5f), 0.2f, metal);
                Part("ExposedFurnace", game.Art.Disc, Vector2.up * 0.2f, new Vector2(1.15f, 1.15f),
                    energy, 0, PartMotion.Pulse, 5);
                for (int index = 0; index < 5; index++)
                {
                    Part("FurnaceUpperTooth_" + index, game.Art.Diamond, new Vector2((index - 2) * 0.28f, 0.9f),
                        new Vector2(0.17f, 0.42f), metal, 180);
                    Part("FurnaceLowerTooth_" + index, game.Art.Diamond, new Vector2((index - 2) * 0.28f, -0.45f),
                        new Vector2(0.17f, 0.42f), metal);
                }
                for (int index = 0; index < 3; index++)
                {
                    float side = index - 1;
                    Part("FuelStack_" + index, game.Art.Square, new Vector2(side * 1.3f, 1.25f),
                        new Vector2(0.35f, 1.25f - Mathf.Abs(side) * 0.2f), metal, side * -15);
                    Part("StackMouth_" + index, game.Art.Square, new Vector2(side * 1.43f, 1.78f),
                        new Vector2(0.55f, 0.2f), energy, side * -15);
                }
                Embers("FuelExhaust", 8, new Vector2(0, 1.65f), 1.5f, energy);
            }
            else
            {
                Part("HelixRotorHousing", game.Art.Ring, Vector2.up * 0.15f, new Vector2(2.75f, 2.75f),
                    metal, 0, PartMotion.Rotor, -35, 92);
                Part("MoltenCannonCore", game.Art.Disc, Vector2.up * 0.15f, new Vector2(1.15f, 1.15f),
                    energy, 0, PartMotion.Pulse, 7);
                Part("CannonInnerCoil", game.Art.Ring, Vector2.up * 0.15f, new Vector2(1.55f, 1.55f),
                    accent, 0, PartMotion.Rotor, 95);
                for (int index = 0; index < 8; index++)
                    OrbitPart("HelixRotorTooth_" + index, Vector2.up * 0.15f, new Vector2(1.22f, 1.22f),
                        new Vector2(0.48f, 0.2f), metal, index * 45, -35);
                for (int sideIndex = 0; sideIndex < 2; sideIndex++)
                {
                    float side = sideIndex == 0 ? -1 : 1;
                    Part("HelixBarrel_" + sideIndex, game.Art.Square, new Vector2(side * 1.35f, 0.35f),
                        new Vector2(0.8f, 0.65f), metal);
                    for (int coil = 0; coil < 3; coil++)
                        Part("BarrelCoil_" + sideIndex + "_" + coil, game.Art.Square,
                            new Vector2(side * (1.05f + coil * 0.26f), 0.35f), new Vector2(0.09f, 0.76f), energy);
                    Part("CoolingFin_" + sideIndex, game.Art.Diamond, new Vector2(side * 1.25f, 1.3f),
                        new Vector2(0.55f, 1.75f), metal, side * -35);
                }
                Embers("CoreSparks", 6, Vector2.up * 0.4f, 1, energy);
            }
        }

        private void BuildIce()
        {
            if (FormIndex == 0)
            {
                for (int sideIndex = 0; sideIndex < 2; sideIndex++)
                {
                    float side = sideIndex == 0 ? -1 : 1;
                    Part("FrozenAntlerBranch_" + sideIndex, game.Art.Square, new Vector2(side * 0.85f, 1.25f),
                        new Vector2(0.12f, 1.5f), metal, side * -35);
                    for (int index = 0; index < 3; index++)
                        Part("AntlerSpike_" + sideIndex + "_" + index, game.Art.Diamond,
                            new Vector2(side * (0.75f + index * 0.32f), 1.5f - index * 0.17f),
                            new Vector2(0.28f, 0.85f), accent, side * (-20 - index * 15));
                    Part("ShoulderIcicle_" + sideIndex, game.Art.Diamond, new Vector2(side * 1.1f, 0.25f),
                        new Vector2(0.45f, 1.15f), accent, side * 25);
                }
            }
            else if (FormIndex == 1)
            {
                for (int sideIndex = 0; sideIndex < 2; sideIndex++)
                {
                    float side = sideIndex == 0 ? -1 : 1;
                    for (int index = 0; index < 3; index++)
                        Part("ReflectorWing_" + sideIndex + "_" + index, game.Art.Diamond,
                            new Vector2(side * (1.35f + index * 0.38f), 0.75f - index * 0.5f),
                            new Vector2(0.65f, 1.9f), accent, side * (-30 - index * 16), PartMotion.Hinge, side * 4);
                }
                for (int index = 0; index < 3; index++)
                    OrbitPart("OrbitingMirrorPrism_" + index, new Vector2(0, 0.25f), new Vector2(1.7f, 1.6f),
                        new Vector2(0.35f, 0.65f), Color.white, index * 120, 26);
                Part("MirrorCrest", game.Art.Diamond, new Vector2(0, 1.65f), new Vector2(0.75f, 1.15f), accent);
            }
            else
            {
                for (int sideIndex = 0; sideIndex < 2; sideIndex++)
                {
                    float side = sideIndex == 0 ? -1 : 1;
                    for (int index = 0; index < 4; index++)
                        Part("PolarWindWing_" + sideIndex + "_" + index, game.Art.Diamond,
                            new Vector2(side * (1.35f + index * 0.32f), 0.65f - index * 0.35f),
                            new Vector2(1.9f, 0.4f), accent, side * (15 + index * 18), PartMotion.Hinge, side * 9);
                }
                for (int index = 0; index < 3; index++)
                    Part("SnowflakeCoreAxis_" + index, game.Art.Square, Vector2.up * 0.25f, new Vector2(0.065f, 1.2f),
                        Color.white, index * 60, PartMotion.Rotor, 18);
                for (int index = 0; index < 5; index++)
                    Part("PolarCrown_" + index, game.Art.Diamond, new Vector2((index - 2) * 0.32f, 1.65f),
                        new Vector2(0.2f, 1.2f - Mathf.Abs(index - 2) * 0.2f), accent, (index - 2) * -12);
                Embers("SnowMotes", 8, new Vector2(0, -0.5f), 2, Color.white);
            }
        }

        private void BuildCyber()
        {
            if (FormIndex == 0)
            {
                Frame("DigitalBodyBracket", new Vector2(0, 0.2f), new Vector2(2.65f, 2.5f), 0.075f, accent);
                for (int index = 0; index < 6; index++)
                    Part("StatusBit_" + index, game.Art.Square, new Vector2((index % 3 - 1) * 0.35f, 0.7f - index / 3 * 0.55f),
                        new Vector2(0.12f, 0.24f), energy, 0, PartMotion.Pulse, 3 + index);
                Part("DigitalAntenna", game.Art.Square, new Vector2(0.75f, 1.7f), new Vector2(0.07f, 0.8f), accent);
                Part("AntennaPixel", game.Art.Square, new Vector2(0.75f, 2.05f), new Vector2(0.25f, 0.25f),
                    Color.white, 0, PartMotion.Pulse, 5);
            }
            else if (FormIndex == 1)
            {
                for (int sideIndex = 0; sideIndex < 2; sideIndex++)
                {
                    float side = sideIndex == 0 ? -1 : 1;
                    Part("ScannerUpperArm_" + sideIndex, game.Art.Square, new Vector2(side * 1.45f, 0.9f),
                        new Vector2(1.35f, 0.13f), metal, side * -25);
                    Part("ScannerElbow_" + sideIndex, game.Art.Square, new Vector2(side * 1.95f, 0.35f),
                        new Vector2(0.13f, 1.2f), accent, side * -15);
                    Part("ScannerLowerArm_" + sideIndex, game.Art.Square, new Vector2(side * 1.6f, -0.28f),
                        new Vector2(0.8f, 0.13f), metal);
                    Part("ScannerEmitter_" + sideIndex, game.Art.Diamond, new Vector2(side * 1.25f, -0.3f),
                        new Vector2(0.35f, 0.5f), energy, 0, PartMotion.Pulse, 6);
                }
                Part("FoldedScannerSpine", game.Art.Square, Vector2.up * 0.4f, new Vector2(0.1f, 2.7f), accent, -35);
                Part("ScanningReticle", game.Art.Square, Vector2.zero, new Vector2(2.3f, 0.06f),
                    energy, 0, PartMotion.Scan, 1);
            }
            else
            {
                for (int sideIndex = 0; sideIndex < 2; sideIndex++)
                {
                    float side = sideIndex == 0 ? -1 : 1;
                    Vector2 center = new Vector2(side * 2, 0.25f);
                    Frame("MirrorPortal_" + sideIndex, center, new Vector2(0.95f, 2.75f), 0.1f, accent);
                    Part("PortalInterior_" + sideIndex, game.Art.Diamond, center, new Vector2(0.55f, 2.1f),
                        energy, 0, PartMotion.Pulse, 4);
                    for (int index = 0; index < 3; index++)
                        Part("MirrorFragment_" + sideIndex + "_" + index, game.Art.Square,
                            new Vector2(side * (1.25f + index * 0.12f), 1.05f - index * 0.9f),
                            new Vector2(0.24f, 0.24f), Color.white, 45, PartMotion.Rotor, side * 25);
                }
                for (int index = 0; index < 3; index++)
                    Part("SplitDigitalCore_" + index, game.Art.Square, new Vector2((index - 1) * 0.32f, 0.3f),
                        new Vector2(0.15f, 1.1f - Mathf.Abs(index - 1) * 0.3f), energy, 0, PartMotion.Pulse, 5 + index);
                Part("MirrorCrown", game.Art.Square, new Vector2(0, 1.75f), new Vector2(1.35f, 0.1f), accent);
            }
        }

        private void BuildFinal()
        {
            if (FormIndex == 0)
            {
                Part("AscendingOrbitalRing", game.Art.Ring, Vector2.up * 0.25f, new Vector2(4.7f, 2.6f),
                    accent, 22, PartMotion.Fixed, 0, 91);
                Part("DescendingOrbitalRing", game.Art.Ring, Vector2.up * 0.25f, new Vector2(4.7f, 2.6f),
                    energy, -22, PartMotion.Fixed, 0, 92);
                for (int index = 0; index < 3; index++)
                    OrbitPart("CrownSatellite_" + index, Vector2.up * 0.25f, new Vector2(2.25f, 1.35f),
                        new Vector2(0.32f, 0.48f), Color.white, index * 120, 32);
                Part("StellarHeart", game.Art.Diamond, new Vector2(0, 0.3f), new Vector2(0.65f, 0.95f),
                    energy, 0, PartMotion.Pulse, 3);
            }
            else if (FormIndex == 1)
            {
                Part("EclipseOuterHorizon", game.Art.Ring, Vector2.up * 0.25f, new Vector2(2.6f, 2.6f),
                    energy, 0, PartMotion.Rotor, -20, 112);
                Part("EclipseBlackHeart", game.Art.Disc, Vector2.up * 0.25f, new Vector2(1.2f, 1.2f),
                    new Color(0.035f, 0.005f, 0.05f), 0, PartMotion.Pulse, 2, 114);
                Part("EclipseAccretionRing", game.Art.Ring, Vector2.up * 0.25f, new Vector2(3.4f, 0.75f),
                    accent, -18, PartMotion.Fixed, 0, 115);
                for (int index = 0; index < 4; index++)
                {
                    float radians = (45 + index * 90) * Mathf.Deg2Rad;
                    Vector2 point = Vector2.up * 0.25f + new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * 1.65f;
                    Part("BrokenGravityShackle_" + index, game.Art.Diamond, point, new Vector2(1.35f, 0.5f),
                        metal, index * 90 + 20, PartMotion.Hinge, 5);
                    OrbitPart("AccretingFragment_" + index, Vector2.up * 0.25f, new Vector2(1.8f, 1.8f),
                        new Vector2(0.17f, 0.33f), energy, index * 90, -48);
                }
            }
            else
            {
                for (int sideIndex = 0; sideIndex < 2; sideIndex++)
                {
                    float side = sideIndex == 0 ? -1 : 1;
                    for (int index = 0; index < 3; index++)
                    {
                        SignaturePart wing = Part("RiftWing_" + sideIndex + "_" + index, game.Art.Diamond,
                            new Vector2(side * (1.6f + index * 0.38f), 0.7f - index * 0.5f),
                            new Vector2(2.4f, 0.65f), accent, side * (40 - index * 30), PartMotion.Hinge, side * 6, 96);
                        wing.AnchorIndex = sideIndex;
                        SignaturePart edge = Part("RiftWingFault_" + sideIndex + "_" + index, game.Art.Square,
                            wing.Position, new Vector2(1.8f, 0.05f), energy, wing.Angle, PartMotion.Hinge, side * 6, 97);
                        edge.AnchorIndex = sideIndex;
                    }
                    Part("SplitRiftHeart_" + sideIndex, game.Art.Diamond, new Vector2(side * 0.3f, 0.3f),
                        new Vector2(0.42f, 1.05f), energy, side * -15, PartMotion.Pulse, 7);
                    SignaturePart conduit = Part("RiftAnchorConduit_" + sideIndex, game.Art.Square,
                        new Vector2(side * 1.15f, 0.35f), new Vector2(1.05f, 0.08f), energy, side * 5);
                    conduit.AnchorIndex = sideIndex;
                }
                for (int index = 0; index < 5; index++)
                    Part("RiftCrown_" + index, game.Art.Diamond, new Vector2((index - 2) * 0.4f, 1.65f),
                        new Vector2(0.22f, 1.1f - Mathf.Abs(index - 2) * 0.15f), accent, (index - 2) * -15);
                Embers("RiftCoreMotes", 8, Vector2.up * 0.25f, 0.85f, energy);
            }
        }

        private SignaturePart Part(string name, Sprite sprite, Vector2 position, Vector2 size, Color color,
            float rotation = 0, PartMotion motion = PartMotion.Fixed, float rate = 0, int order = 113)
        {
            Vector2 bounds = sprite.bounds.size;
            Vector2 scale = new Vector2(size.x / Mathf.Max(0.01f, bounds.x), size.y / Mathf.Max(0.01f, bounds.y));
            SpriteRenderer renderer = game.Art.Renderer(name, root, sprite, color, scale, order);
            renderer.transform.localPosition = position;
            renderer.transform.localRotation = Quaternion.Euler(0, 0, rotation);
            var part = new SignaturePart
            {
                Renderer = renderer, Position = position, Scale = scale, Color = color,
                Angle = rotation, Motion = motion, Rate = rate, Offset = parts.Count * 0.73f
            };
            parts.Add(part);
            return part;
        }

        private void Frame(string name, Vector2 center, Vector2 size, float thickness, Color color)
        {
            for (int index = 0; index < 4; index++)
            {
                bool horizontal = index < 2;
                Vector2 point = center + (horizontal ? new Vector2(0, (index == 0 ? -1 : 1) * size.y * 0.5f)
                    : new Vector2((index == 2 ? -1 : 1) * size.x * 0.5f, 0));
                Part(name + "_" + index, game.Art.Square, point, horizontal ? new Vector2(size.x, thickness) : new Vector2(thickness, size.y), color);
            }
        }

        private void OrbitPart(string name, Vector2 center, Vector2 radius, Vector2 size, Color color, float phase, float speed)
        {
            SignaturePart part = Part(name, game.Art.Diamond, center, size, color, phase, PartMotion.Orbit, speed);
            part.OrbitRadius = radius;
        }

        private void Embers(string name, int count, Vector2 origin, float spread, Color color)
        {
            for (int index = 0; index < count; index++)
            {
                SignaturePart part = Part(name + "_" + index, game.Art.Diamond,
                    origin + Vector2.right * ((index / (float)(count - 1) - 0.5f) * spread * 2),
                    new Vector2(0.08f + index % 3 * 0.03f, 0.17f), color, 0, PartMotion.Ember, 0.6f);
                part.Offset = index / (float)count;
            }
        }

        private void LateUpdate()
        {
            if (game == null || boss == null || root == null) return;
            if (!boss.OwnerAlive || !boss.isActiveAndEnabled) { SetVisible(false); return; }
            if (!visible || !game.IsPlaying || Time.timeScale <= 0) return;
            if (FormIndex != boss.FormIndex) SetForm(boss.Phase);
            clock += Time.deltaTime;
            attackPulse = Mathf.Max(0, attackPulse - Time.deltaTime * 2);
            foreach (SignaturePart part in parts)
            {
                if (part.Renderer == null) continue;
                Vector2 position = part.Position;
                Vector2 scale = part.Scale;
                float rotation = part.Angle;
                Color color = part.Color;
                switch (part.Motion)
                {
                    case PartMotion.Rotor: rotation += clock * part.Rate; break;
                    case PartMotion.Orbit:
                        float radians = (part.Angle + clock * part.Rate) * Mathf.Deg2Rad;
                        position += new Vector2(Mathf.Cos(radians) * part.OrbitRadius.x, Mathf.Sin(radians) * part.OrbitRadius.y);
                        rotation += clock * part.Rate + 90;
                        break;
                    case PartMotion.Hinge: rotation += Mathf.Sin(clock * 2 + part.Offset) * part.Rate + attackPulse * part.Rate; break;
                    case PartMotion.Pulse:
                        scale *= 1 + Mathf.Sin(clock * part.Rate + part.Offset) * 0.06f + attackPulse * 0.12f;
                        break;
                    case PartMotion.Ember:
                        float age = Mathf.Repeat(clock * part.Rate + part.Offset, 1);
                        position += new Vector2(Mathf.Sin(clock * 2 + part.Offset * 9) * 0.15f, age * 1.15f);
                        color.a *= Mathf.Sin(age * Mathf.PI);
                        rotation = age * 120;
                        break;
                    case PartMotion.Scan:
                        position += Vector2.up * (Mathf.Repeat(clock * part.Rate, 2.2f) - 1.1f);
                        break;
                }
                if (part.AnchorIndex >= 0 && part.AnchorIndex < boss.Arms.Length)
                {
                    Health anchor = boss.Arms[part.AnchorIndex];
                    if (anchor == null || anchor.IsDead || !anchor.gameObject.activeSelf)
                    {
                        scale *= 0.3f;
                        color.a *= 0.16f;
                    }
                }
                if (boss.Transitioning || game.Effects.AttackTelegraphsEnabled && boss.Preparing)
                    color = Color.Lerp(color, new Color(1, 1, 1, color.a), 0.22f);
                part.Renderer.color = color;
                part.Renderer.transform.localPosition = position;
                part.Renderer.transform.localRotation = Quaternion.Euler(0, 0, rotation);
                part.Renderer.transform.localScale = new Vector3(scale.x, scale.y, 1);
            }
        }

        private void OnDisable() { SetVisible(false); }
        private void OnDestroy()
        {
            parts.Clear();
            if (root != null) { root.gameObject.SetActive(false); Destroy(root.gameObject); }
        }
    }
}
