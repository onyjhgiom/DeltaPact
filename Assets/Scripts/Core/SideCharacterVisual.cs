using System.Collections.Generic;
using UnityEngine;

namespace MoriEchoes
{
    public sealed class SideCharacterVisual : MonoBehaviour
    {
        private GameManager game;
        private Health health;
        private PlayerController player;
        private Rigidbody2D body;
        private Transform rig;
        private Transform head;
        private Transform torso;
        private Transform leftArm;
        private Transform rightArm;
        private Transform leftLeg;
        private Transform rightLeg;
        private Transform scarf;
        private SpriteRenderer healthFill;
        private readonly List<SpriteRenderer> parts = new List<SpriteRenderer>();
        private readonly List<Color> colors = new List<Color>();
        private float phase;
        private float flashUntil;
        private float build = 1;
        private Transform bossOrbit;

        public void Initialize(GameManager owner, Health hitPoints, EnemyKind kind)
        {
            game = owner; health = hitPoints; body = GetComponent<Rigidbody2D>(); player = GetComponent<PlayerController>();
            SideSpriteAnimation animation = player != null ? game.Character.animation
                : kind == EnemyKind.Boss ? game.Level.Theme.bossAnimation
                : game.Level.Theme.EnemyAnimation(kind, game.Config.enemyAnimations);
            if (animation != null && animation.IsReady)
            {
                gameObject.AddComponent<SideSpriteAnimator>().Initialize(game, health, animation, kind);
                enabled = false;
                return;
            }
            bool boss = player == null && kind == EnemyKind.Boss;
            build = player != null ? game.Character.build : boss ? 2.1f : kind == EnemyKind.Elite ? 1.15f : 1;
            rig = new GameObject("ArticulatedSpriteRig").transform; rig.SetParent(transform, false);
            Color accent = player != null ? game.Character.accent : game.Level.Theme.danger;
            if (player != null && game.AlternatePalette) accent = new Color(0.86f, 0.5f, 1);
            Color armor = player != null ? Color.white : Color.Lerp(Color.white, game.Level.Theme.structure, 0.5f);
            Part("Shadow", rig, game.Art.Disc, new Vector2(0, -0.76f), new Vector2(0.95f, 0.14f), new Color(0, 0, 0, 0.4f), 95);
            if (player != null && game.CharacterId == 3)
                Part("AmmoBackpack", rig, game.Art.Side.Armor, new Vector2(-0.26f, 0.09f), new Vector2(0.4f, 0.65f), accent, 99);
            if (player != null && game.CharacterId == 6)
            {
                Part("LumenPack", rig, game.Art.Side.Armor, new Vector2(-0.31f, 0.03f), new Vector2(0.36f, 0.68f), accent, 99);
                Part("LumenEmitter", rig, game.Art.Ring, new Vector2(-0.35f, 0.12f), new Vector2(0.5f, 0.62f), accent * 1.4f, 105);
            }
            if (player != null && game.CharacterId == 7)
            {
                Part("NovaShoulder", rig, game.Art.Diamond, new Vector2(0.35f, 0.26f), new Vector2(0.34f, 0.5f), accent * 1.4f, 109);
                Part("NovaCore", rig, game.Art.Ring, new Vector2(0, 0.16f), new Vector2(0.48f, 0.48f), accent * 1.3f, 114);
            }
            leftLeg = Bone("LeftLeg", new Vector2(-0.14f, -0.12f));
            rightLeg = Bone("RightLeg", new Vector2(0.14f, -0.12f));
            Leg(leftLeg, armor, 102); Leg(rightLeg, armor, 104);
            torso = Bone("Torso", new Vector2(0, 0.13f));
            Part("ChestArmor", torso, game.Art.Side.Armor, Vector2.zero, new Vector2(0.65f, 0.62f), armor, 110);
            Part("ChestReactor", torso, game.Art.Diamond, new Vector2(0.04f, 0.1f), new Vector2(0.08f, 0.17f), accent * 1.7f, 113);
            Part("UtilityBelt", torso, game.Art.Side.Armor, new Vector2(0, -0.27f), new Vector2(0.61f, 0.14f), accent, 115);
            leftArm = Bone("LeftArm", new Vector2(-0.27f, 0.28f));
            rightArm = Bone("RightArm", new Vector2(0.27f, 0.28f));
            Arm(leftArm, armor, accent, 108); Arm(rightArm, armor, accent, 118);
            head = Bone("Head", new Vector2(0, 0.61f));
            Sprite custom = player != null ? game.Character.bodySprite ?? game.Config.playerSprite : null;
            if (custom != null)
            {
                foreach (SpriteRenderer renderer in parts) renderer.enabled = false;
                Part("ImportedCharacter", rig, custom, Vector2.zero, new Vector2(1.1f, 1.7f), Color.white, 120);
            }
            else Part("FaceAndHair", head, player != null ? game.Art.Side.Heads[game.SelectedCharacter] : game.Art.Side.Helmet,
                Vector2.zero, new Vector2(0.53f, 0.53f), Color.white, 121);
            if (player != null && (game.CharacterId == 0 || game.CharacterId == 2 || game.CharacterId == 7))
                scarf = Part("Scarf", rig, game.Art.Side.Scarf, new Vector2(-0.47f, 0.39f), new Vector2(0.85f, 0.3f), accent, 101).transform;
            if (boss)
            {
                for (int index = 0; index < 3; index++)
                    Part("BossShoulderPlate", rig, game.Art.Side.Armor, new Vector2(-0.44f + index * 0.44f, 0.23f), new Vector2(0.5f, 0.45f), armor, 114);
                Part("BossHeart", rig, game.Art.Diamond, new Vector2(0, 0.12f), new Vector2(0.22f, 0.32f), accent * 2.2f, 125);
                if (game.SelectedStage == 0)
                {
                    Part("SiegeShield", rig, game.Art.Side.Armor, new Vector2(0.53f, -0.06f), new Vector2(0.4f, 0.9f), accent, 119);
                    Part("SiegeCannon", rig, game.Art.Gun, new Vector2(-0.55f, 0.12f), new Vector2(0.75f, 0.32f), armor, 120);
                }
                else if (game.SelectedStage == 1)
                {
                    Part("FurnaceHalo", rig, game.Art.Ring, new Vector2(0, 0.24f), Vector2.one * 1.2f, accent * 1.5f, 99);
                    for (int index = -1; index <= 1; index += 2)
                        Part("FurnaceExhaust", rig, game.Art.Side.Limb, new Vector2(index * 0.48f, 0.48f), new Vector2(0.3f, 0.75f), armor, 110);
                }
                else if (game.SelectedStage == 2)
                {
                    for (int index = -1; index <= 1; index += 2)
                        Part("IceBladeWing", rig, game.Art.Diamond, new Vector2(index * 0.56f, 0.2f), new Vector2(0.9f, 0.32f), accent * 1.4f, 111).transform.localRotation = Quaternion.Euler(0, 0, index * 25);
                }
                else if (game.SelectedStage == 5)
                {
                    Part("TombKing_CrownSpikes", rig, game.Art.Diamond, new Vector2(0, 1.55f), new Vector2(1.9f, 1.1f), accent * 1.7f, 128);
                    Part("TombKing_FossilFace", rig, game.Art.Skull, new Vector2(0, 0.45f), new Vector2(1.35f, 1.55f), Color.white, 129);
                    for (int index = -1; index <= 1; index += 2)
                    {
                        Part("TombKing_JawHorn", rig, game.Art.Diamond, new Vector2(index * 1.45f, -0.4f), new Vector2(0.68f, 1.5f), armor, 120).transform.localRotation = Quaternion.Euler(0, 0, index * 27);
                        Part("TombKing_GhostFlame", rig, game.Art.Glow, new Vector2(index * 0.52f, 0.7f), new Vector2(0.42f, 0.58f), accent * 1.9f, 130);
                    }
                }
                else if (game.SelectedStage == 6)
                {
                    bossOrbit = Bone("TidalHalo", new Vector2(0, 0.18f));
                    for (int index = 0; index < 4; index++)
                    {
                        float angle = index * 90;
                        Vector2 offset = Quaternion.Euler(0, 0, angle) * Vector2.right * 1.1f;
                        Part("AbyssalFin", bossOrbit, game.Art.Diamond, offset, new Vector2(0.6f, 1.1f), accent * 1.55f, 132).transform.localRotation = Quaternion.Euler(0, 0, angle + 90);
                    }
                    Part("TideBellMantle", rig, game.Art.Ring, new Vector2(0, 0.85f), new Vector2(3.4f, 1.6f), accent, 103);
                    Part("TideLeviathanJaw", rig, game.Art.Side.Armor, new Vector2(0, -0.5f), new Vector2(1.75f, 0.62f), armor, 121);
                }
                else if (game.SelectedStage >= 7)
                {
                    bossOrbit = Bone("GravityCoffinOrbit", new Vector2(0, 0.2f));
                    for (int index = 0; index < 6; index++)
                    {
                        float angle = index * 60;
                        Vector2 offset = Quaternion.Euler(0, 0, angle) * Vector2.right * 1.55f;
                        Part("VoidCoffinShard", bossOrbit, game.Art.Diamond, offset, new Vector2(0.42f, 1.25f), accent * 1.6f, 133)
                            .transform.localRotation = Quaternion.Euler(0, 0, angle + 90);
                    }
                    Part("VoidSingularityLens", rig, game.Art.Ring, new Vector2(0, 0.22f), new Vector2(1.8f, 2.3f), accent * 1.7f, 134);
                    Part("HorizonCore", rig, game.Art.Glow, new Vector2(0, 0.22f), new Vector2(0.9f, 1.4f), Color.white, 135);
                }
                else
                {
                    bossOrbit = Bone("OrbitalWeapons", new Vector2(0, 0.17f));
                    for (int index = 0; index < 4; index++)
                    {
                        Vector2 offset = Quaternion.Euler(0, 0, index * 90) * Vector2.right * 0.72f;
                        Part("OrbitalDrone", bossOrbit, game.Art.Side.Helmet, offset, Vector2.one * 0.32f, accent, 130);
                    }
                }
            }
            else if (player == null)
            {
                if (game.SelectedStage == 5)
                {
                    Part("CryptBoneMask", rig, game.Art.Skull, new Vector2(0, 0.58f), new Vector2(0.78f, 0.82f), Color.white, 124);
                    for (int index = 0; index < 4; index++)
                        Part("GraveRib", rig, game.Art.Diamond, new Vector2((index % 2 == 0 ? -1 : 1) * (0.28f + index / 2 * 0.1f), 0.08f - index / 2 * 0.2f),
                            new Vector2(0.3f, 0.52f), accent * (1.25f - index * 0.08f), 122).transform.localRotation = Quaternion.Euler(0, 0, (index % 2 == 0 ? -1 : 1) * 22);
                }
                else if (game.SelectedStage == 6)
                {
                    for (int index = -1; index <= 1; index += 2)
                        Part("DrownedFin", rig, game.Art.Diamond, new Vector2(index * 0.42f, 0.13f), new Vector2(0.6f, 1.1f), accent * 1.4f, 120)
                            .transform.localRotation = Quaternion.Euler(0, 0, index * 22);
                    Part("FloodMask", rig, game.Art.Ring, new Vector2(0, 0.53f), new Vector2(0.72f, 0.6f), accent * 1.7f, 123);
                }
                else if (game.SelectedStage >= 7)
                {
                    for (int index = 0; index < 3; index++)
                    {
                        int shard = index;
                        Part("RiftArmorShard", rig, game.Art.Diamond, new Vector2((shard - 1) * 0.48f, 0.1f + shard % 2 * 0.18f),
                            new Vector2(0.34f, 0.88f), accent * 1.65f, 122).transform.localRotation = Quaternion.Euler(0, 0, (shard - 1) * 24);
                    }
                    Part("RiftVisor", rig, game.Art.Ring, new Vector2(0, 0.58f), new Vector2(0.85f, 0.4f), accent * 2, 124);
                }
                healthFill = game.Art.Renderer("EnemyHealth", transform, game.Art.Square, accent, new Vector2(0.65f, 0.045f), 500);
                healthFill.transform.localPosition = Vector2.up * 1.15f;
            }
            foreach (SpriteRenderer renderer in parts) colors.Add(renderer.color);
            health.Damaged += Damaged;
            health.Died += OnDeath;
        }
        private Transform Bone(string name, Vector2 position)
        {
            var node = new GameObject(name).transform; node.SetParent(rig, false); node.localPosition = position; return node;
        }
        private SpriteRenderer Part(string name, Transform parent, Sprite sprite, Vector2 position, Vector2 size, Color color, int order)
        {
            SpriteRenderer renderer = game.Art.Renderer(name, parent, sprite, color, size, order);
            renderer.transform.localPosition = position; parts.Add(renderer); return renderer;
        }
        private void Leg(Transform parent, Color armor, int order)
        {
            Part("UpperLeg", parent, game.Art.Side.Limb, new Vector2(0, -0.14f), new Vector2(0.28f, 0.4f), armor, order);
            Part("KneePlate", parent, game.Art.Side.Armor, new Vector2(0, -0.3f), new Vector2(0.2f, 0.2f), armor, order + 1);
            Part("Boot", parent, game.Art.Side.Boot, new Vector2(0.025f, -0.46f), new Vector2(0.28f, 0.34f), Color.white, order + 2);
        }
        private void Arm(Transform parent, Color armor, Color accent, int order)
        {
            Part("Arm", parent, game.Art.Side.Limb, new Vector2(0, -0.15f), new Vector2(0.23f, 0.45f), armor, order);
            Part("Shoulder", parent, game.Art.Side.Armor, Vector2.zero, new Vector2(0.3f, 0.24f), accent, order + 1);
            Part("Glove", parent, game.Art.Side.Boot, new Vector2(0, -0.31f), new Vector2(0.2f, 0.19f), Color.white, order + 2);
        }
        private void Damaged(float damage) { flashUntil = Time.time + 0.08f; }
        private void OnDeath() { if (game.Effects != null) game.Effects.Debris(transform.position, Color.Lerp(Color.white, game.Level.Theme.accent, 0.3f)); }
        private void LateUpdate()
        {
            if (game == null || health == null || body == null) return;
            float speed = Mathf.Abs(body.linearVelocity.x);
            phase += Time.deltaTime * (speed > 0.2f ? speed * 2.3f : 1.7f);
            bool crouch = player != null && player.Motor.Crouching;
            bool airborne = player != null ? !player.Motor.Grounded : Mathf.Abs(body.linearVelocity.y) > 0.6f;
            float facing = player != null ? Mathf.Sign(player.Aim.x) : game.Player != null ? Mathf.Sign(game.Player.transform.position.x - transform.position.x) : 1;
            rig.localScale = new Vector3((facing == 0 ? 1 : facing) * build, build * (crouch ? 0.65f : 1), 1);
            rig.localPosition = new Vector3(0, crouch ? -0.27f : Mathf.Sin(phase * 2) * (speed > 0.2f ? 0.018f : 0.006f), 0);
            float stride = airborne ? 22 : speed > 0.2f ? Mathf.Sin(phase) * 33 : Mathf.Sin(phase) * 2;
            leftLeg.localRotation = Quaternion.Euler(0, 0, stride); rightLeg.localRotation = Quaternion.Euler(0, 0, -stride);
            float angle = player != null ? Mathf.Asin(Mathf.Clamp(player.Aim.y, -1, 1)) * Mathf.Rad2Deg : 0;
            leftArm.localRotation = Quaternion.Euler(0, 0, 55 + angle * 0.65f); rightArm.localRotation = Quaternion.Euler(0, 0, 65 + angle * 0.7f);
            torso.localRotation = Quaternion.Euler(0, 0, airborne ? -7 : -body.linearVelocity.x * facing * 0.7f);
            head.localRotation = Quaternion.Euler(0, 0, angle * 0.13f);
            if (scarf != null) scarf.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(phase * 1.3f) * 12 - speed * 2);
            if (bossOrbit != null) bossOrbit.localRotation = Quaternion.Euler(0, 0, Time.time * 25);
            bool hidden = player != null && game.CameraRig.Side.Mode == ViewMode.IronSight;
            for (int index = 0; index < parts.Count; index++)
            {
                Color color = Time.time < flashUntil ? Color.white * 2 : colors[index];
                if (hidden) color.a = 0;
                parts[index].color = color;
            }
            if (healthFill != null) healthFill.transform.localScale = new Vector3(0.65f * health.Current / health.Maximum, 0.045f, 1);
        }
        private void OnDestroy() { if (health != null) { health.Damaged -= Damaged; health.Died -= OnDeath; } }
    }
}
