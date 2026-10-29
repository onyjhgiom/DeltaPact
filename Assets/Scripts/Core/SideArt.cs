using UnityEngine;

namespace MoriEchoes
{
    public sealed class SideArt
    {
        public Sprite[] Heads { get; }
        public Sprite[] Portraits { get; }
        public Sprite Armor { get; }
        public Sprite Limb { get; }
        public Sprite Boot { get; }
        public Sprite Helmet { get; }
        public Sprite Scarf { get; }
        public Sprite Architecture { get; }
        public Sprite Concrete { get; }
        public Sprite Cloud { get; }
        private static readonly Color Outline = new Color(0.025f, 0.035f, 0.065f);

        public SideArt(ProceduralArt art, CharacterDefinition[] characters)
        {
            Heads = new Sprite[characters.Length];
            Portraits = new Sprite[characters.Length];
            Architecture = art.Paint("RuinFacade_Detailed", 256, (horizontal, vertical) =>
            {
                float roof = 0.39f + Mathf.Floor((horizontal + 0.5f) * 9) % 3 * 0.035f;
                if (vertical > roof || Mathf.Abs(horizontal) > 0.48f) return Color.clear;
                float column = Mathf.Repeat((horizontal + 0.5f) * 10, 1);
                float row = Mathf.Repeat((vertical + 0.5f) * 18, 1);
                float cell = Mathf.Sin(Mathf.Floor((horizontal + 0.5f) * 10) * 17.1f + Mathf.Floor((vertical + 0.5f) * 18) * 27.3f);
                if (column > 0.25f && column < 0.72f && row > 0.18f && row < 0.65f)
                    return cell > 0.25f ? new Color(0.7f, 0.88f, 1, 0.9f) : new Color(0.11f, 0.17f, 0.24f);
                if (column < 0.065f || row < 0.025f) return new Color(0.14f, 0.2f, 0.28f);
                float damage = Mathf.Sin(horizontal * 155 + vertical * 91) * Mathf.Sin(vertical * 121);
                return Color.Lerp(new Color(0.28f, 0.36f, 0.47f), new Color(0.5f, 0.61f, 0.74f), Mathf.Clamp01(vertical + 0.7f + damage * 0.13f));
            });
            Concrete = art.Paint("MachinedSteel_Bevel", 128, (horizontal, vertical) =>
            {
                float panel = Mathf.Repeat((horizontal + 0.5f) * 4, 1);
                float scratch = Mathf.Sin(horizontal * 487 + vertical * 331) * 0.045f;
                if (Mathf.Abs(vertical) > 0.43f) return new Color(0.7f, 0.8f, 0.9f);
                if (panel < 0.04f) return new Color(0.12f, 0.18f, 0.25f);
                if (Mathf.Abs(vertical) > 0.3f && (panel < 0.13f || panel > 0.87f)) return new Color(0.72f, 0.76f, 0.8f);
                return Color.Lerp(new Color(0.28f, 0.36f, 0.45f), new Color(0.6f, 0.68f, 0.8f), vertical + 0.5f + scratch);
            }, true);
            Cloud = art.Paint("AtmosphericCloud", 128, (horizontal, vertical) =>
            {
                float radius = horizontal * horizontal * 4 + vertical * vertical * 11;
                float noise = 0.75f + 0.25f * Mathf.Sin(horizontal * 21 + Mathf.Sin(vertical * 31) * 2);
                return new Color(1, 1, 1, Mathf.Clamp01(1 - radius) * noise * 0.6f);
            });
            for (int index = 0; index < characters.Length; index++)
            {
                int variant = index;
                CharacterDefinition character = characters[index];
                Heads[index] = art.Paint("AnimeHead_" + character.name, 256,
                    (horizontal, vertical) => Head(horizontal, vertical, character, variant), true);
                Portraits[index] = character.portrait != null ? character.portrait : art.Paint("Portrait_" + character.name, 256,
                    (horizontal, vertical) => Portrait(horizontal, vertical, character, variant));
            }
            Armor = art.Paint("Layered_Armor", 128, ArmorPixel, true);
            Limb = art.Paint("Articulated_Limb", 128, (horizontal, vertical) =>
            {
                float width = Mathf.Lerp(0.22f, 0.34f, vertical + 0.5f);
                if (Mathf.Abs(horizontal) > width || Mathf.Abs(vertical) > 0.47f) return Color.clear;
                if (Mathf.Abs(horizontal) > width - 0.04f) return Outline;
                if (Mathf.Abs(vertical + 0.06f) < 0.06f) return new Color(0.16f, 0.2f, 0.26f);
                return Color.Lerp(new Color(0.35f, 0.44f, 0.57f), new Color(0.8f, 0.87f, 0.95f), Mathf.Clamp01(0.55f - horizontal));
            }, true);
            Boot = art.Paint("Tactical_Boot", 96, (horizontal, vertical) =>
            {
                bool inside = horizontal > -0.3f && horizontal < 0.28f + (vertical < -0.2f ? 0.18f : 0) && Mathf.Abs(vertical) < 0.48f;
                if (!inside) return Color.clear;
                if (vertical < -0.38f || horizontal < -0.25f) return Outline;
                if (Mathf.Abs(vertical + 0.03f) < 0.025f || Mathf.Abs(vertical - 0.18f) < 0.025f) return new Color(0.6f, 0.7f, 0.8f);
                return new Color(0.13f, 0.19f, 0.28f);
            }, true);
            Helmet = art.Paint("Armored_Helmet", 128, (horizontal, vertical) =>
            {
                if (horizontal * horizontal / 0.15f + vertical * vertical / 0.2f > 1) return Color.clear;
                if (Mathf.Abs(vertical - 0.03f) < 0.055f) return new Color(1, 0.25f, 0.12f);
                if (Mathf.Abs(horizontal) < 0.035f) return Outline;
                return Color.Lerp(new Color(0.13f, 0.17f, 0.24f), new Color(0.52f, 0.6f, 0.7f), vertical + 0.5f);
            }, true);
            Scarf = art.Paint("Scarf", 128, (horizontal, vertical) => Mathf.Abs(vertical - Mathf.Sin(horizontal * 9) * 0.09f) < 0.09f + (horizontal + 0.5f) * 0.09f
                ? Color.Lerp(new Color(0.35f, 0.45f, 0.65f), Color.white, vertical + 0.5f) : Color.clear, true);
        }

        private static Color ArmorPixel(float horizontal, float vertical)
        {
            float width = vertical > 0.1f ? 0.4f : vertical > -0.22f ? 0.31f : 0.36f;
            if (Mathf.Abs(horizontal) > width || Mathf.Abs(vertical) > 0.47f) return Color.clear;
            if (Mathf.Abs(horizontal) > width - 0.035f || Mathf.Abs(vertical) > 0.43f) return Outline;
            if (Mathf.Abs(vertical + 0.25f) < 0.035f) return new Color(0.07f, 0.1f, 0.16f);
            if (Mathf.Abs(horizontal) < 0.025f) return new Color(0.85f, 0.95f, 1);
            if (vertical > 0.1f && Mathf.Abs(horizontal) > 0.14f) return new Color(0.75f, 0.84f, 0.93f);
            return Color.Lerp(new Color(0.09f, 0.14f, 0.23f), new Color(0.34f, 0.43f, 0.58f), Mathf.Clamp01(0.65f + vertical * 0.5f - horizontal * 0.6f));
        }

        private static Color Head(float horizontal, float vertical, CharacterDefinition character, int variant)
        {
            bool male = character.isMale;
            float face = horizontal * horizontal / (male ? 0.092f : 0.105f) + (vertical + 0.015f) * (vertical + 0.015f) / (male ? 0.135f : 0.15f);
            float hairWidth = male ? 0.34f : (variant == 5 ? 0.35f : 0.38f) - Mathf.Max(0, -vertical - 0.1f) * (variant == 4 ? 0.03f : 0.15f);
            float hairBottom = male ? -0.19f : variant == 4 ? -0.49f : variant == 5 ? -0.15f : -0.43f;
            bool hair = horizontal * horizontal / (hairWidth * hairWidth) + (vertical - 0.04f) * (vertical - 0.04f) / 0.185f < 1 && (variant != 5 || vertical > -0.15f)
                || vertical < -0.12f && vertical > hairBottom + Mathf.Sin(horizontal * 35) * 0.055f && Mathf.Abs(horizontal) > 0.24f && Mathf.Abs(horizontal) < hairWidth;
            if (!hair && face > 1) return Color.clear;
            Color hairColor = Color.Lerp(character.hair * new Color(0.55f, 0.63f, 0.8f), character.hair,
                Mathf.Clamp01(vertical + 0.68f + Mathf.Sin(horizontal * 50 + vertical * 6) * 0.13f));
            if (face > 0.95f || vertical > 0.1f + Mathf.Sin(horizontal * 24 + variant) * 0.09f || Mathf.Abs(horizontal) > 0.265f)
                return hair ? hairColor : Outline;
            Color skin = Color.Lerp(new Color(0.86f, 0.65f, 0.63f), new Color(1, 0.9f, 0.85f), Mathf.Clamp01(0.55f + vertical - horizontal));
            float eyeCenter = Mathf.Abs(horizontal) - 0.125f;
            if (Mathf.Abs(eyeCenter) < 0.072f && Mathf.Abs(vertical + 0.015f) < 0.055f)
            {
                if (vertical > 0.017f) return Outline;
                if (Mathf.Abs(eyeCenter) < 0.034f)
                {
                    if (vertical > -0.005f && eyeCenter < -0.006f) return Color.white;
                    if (Mathf.Abs(eyeCenter) < 0.012f) return new Color(0.025f, 0.12f, 0.23f);
                    return Color.Lerp(new Color(0.1f, 0.35f, 0.7f), character.accent, Mathf.Clamp01(-vertical * 10));
                }
                return new Color(0.94f, 0.97f, 1);
            }
            if (Mathf.Abs(horizontal) < 0.045f && Mathf.Abs(vertical + 0.22f) < 0.009f) return new Color(0.7f, 0.37f, 0.4f);
            if (horizontal > 0.015f && horizontal < 0.025f && vertical < -0.09f && vertical > -0.14f) return new Color(0.78f, 0.54f, 0.5f);
            return skin;
        }

        private static Color Portrait(float horizontal, float vertical, CharacterDefinition character, int variant)
        {
            if (vertical > -0.04f) return Head(horizontal * 1.35f, (vertical - 0.22f) * 1.7f, character, variant);
            Color armor = ArmorPixel(horizontal * 0.95f, (vertical + 0.3f) * 2);
            return armor.a > 0 ? Color.Lerp(armor, armor * character.accent * 1.3f, 0.25f) : Color.clear;
        }
    }
}
