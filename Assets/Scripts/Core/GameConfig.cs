using System;
using UnityEngine;

namespace MoriEchoes
{
    [Serializable]
    public sealed class WeaponDefinition
    {
        public string displayName;
        public float damage;
        public float shotsPerSecond;
        public int magazine;
        public float reloadSeconds;
        public int pellets;
        public float spreadDegrees;
        public int targetsPerBullet;
        public float projectileSpeed;
        public Color color;
        [Range(0, 5)] public float explosionRadius;
        [Range(0, 0.4f)] public float recoil = 0.06f;

        public WeaponDefinition(string name, float hitDamage, float rate, int capacity, float reload,
            int pelletCount, float spread, int targets, float speed, Color tint)
        {
            displayName = name; damage = hitDamage; shotsPerSecond = rate; magazine = capacity;
            reloadSeconds = reload; pellets = pelletCount; spreadDegrees = spread;
            targetsPerBullet = targets; projectileSpeed = speed; color = tint;
        }
    }

    [CreateAssetMenu(menuName = "Mori Echoes/Game Config", fileName = "MoriEchoesConfig")]
    public sealed class GameConfig : ScriptableObject
    {
        [Header("Player")]
        public bool sideScroller = true;
        [Header("Platformer movement")]
        [Range(0.5f, 8f)] public float gravityScale = 3.2f;
        [Range(3f, 24f)] public float jumpVelocity = 13f;
        public bool allowDoubleJump;
        [Range(0f, 0.3f)] public float coyoteTime = 0.09f;
        [Range(0f, 0.3f)] public float jumpBuffer = 0.12f;
        [Range(0.1f, 1f)] public float jumpCutMultiplier = 0.45f;
        [Range(0.1f, 1f)] public float iceFrictionFactor = 0.3f;
        [Header("Side-scroller views / content")]
        public ViewMode defaultView = ViewMode.Tactical;
        [Range(2f, 12f)] public float viewTacticalSize = 6.2f;
        [Range(1.5f, 8f)] public float viewShoulderSize = 4.3f;
        [Range(1f, 5f)] public float viewIronSightSize = 2.8f;
        public CharacterDefinition[] characters = CharacterDefinition.Defaults();
        public bool allCharactersAvailable = true;
        [Tooltip("Allow direct deployment to configured stages without changing saved progress.")]
        public bool allStagesAvailable = true;
        public ThemeProfile[] themes = ThemeProfile.Defaults();
        [Min(1)] public float playerHealth = 100f;
        [Min(0.1f)] public float moveSpeed = 5f;
        [Min(0.05f)] public float dodgeDuration = 0.18f;
        [Min(0.2f)] public float dodgeCooldown = 2.5f;
        [Min(1f)] public float dodgeSpeedMultiplier = 3.2f;
        [Header("Arena and run")]
        public Vector2 arenaHalfSize = new Vector2(22f, 16f);
        [Min(1)] public int wavesBeforeBoss = 10;
        [Min(5f)] public float waveSeconds = 55f;
        [Min(0f)] public float intermissionSeconds = 4f;
        [Range(8, 80)] public int maximumEnemies = 36;
        [Range(32, 512)] public int maximumProjectiles = 192;
        [Range(32, 256)] public int maximumPickups = 128;
        [Header("Audio / presentation")]
        [Range(0f, 1f)] public float masterVolume = 0.6f;
        [Range(0f, 1f)] public float screenShake = 0.35f;
        [Header("Camera / orthographic shoulder view")]
        [Range(2f, 14f)] public float camBaseSize = 7.7f;
        [Range(1f, 14f)] public float camAdsSize = 4.2f;
        [Range(0f, 6f)] public float camLead = 3.6f;
        [Range(0f, 8f)] public float camBack = 0.9f;
        [Range(-2f, 2f)] public float camShoulderOffset = 0.65f;
        [Range(0f, 55f)] public float camPitchDeg = 28f;
        [Range(0f, 12f)] public float camHeight = 0f;
        [Range(0.5f, 30f)] public float camFollow = 9f;
        [Range(0.5f, 30f)] public float camZoomSpeed = 12f;
        [Range(0f, 1f)] public float camRecoilMax = 0.25f;
        [Range(0.1f, 10f)] public float camRecoilDecay = 1.6f;
        [Range(0f, 2f)] public float camRecoilPitch = 0.4f;
        public Vector3 camWeaponRecoil = new Vector3(0.045f, 0.12f, 0.18f);
        [Range(0.02f, 1f)] public float mouseLookSensitivity = 0.15f;
        [Range(0.1f, 1f)] public float adsMoveMultiplier = 0.6f;
        public Sprite playerBackSprite;
        public Font uiFont;
        public Material spriteMaterial;
        public Material sideSpriteMaterial;
        public GameObject sideLightPrefab;
        public Sprite playerSprite;
        public Sprite[] enemySprites = new Sprite[5];
        public Sprite[] weaponSprites = new Sprite[9];
        public Sprite[] weaponViewSprites = new Sprite[9];
        public Sprite[] scenerySprites = new Sprite[8];
        public Sprite[] platformSprites = new Sprite[6];
        public Sprite[] solidWallSprites = new Sprite[5];
        [Header("Optional production side-scroller art")]
        public SideSpriteAnimation[] enemyAnimations = new SideSpriteAnimation[4];
        public Sprite[] combatEffectSprites = new Sprite[8];
        [Header("Weapons: AR / SG / SR / SMG / Rail / GL / Sword / Arc / Burst")]
        public WeaponDefinition[] weapons =
        {
            new WeaponDefinition("AR-07 / 突击步枪", 12, 7, 28, 1.2f, 1, 2, 1, 24, new Color(0.28f, 0.88f, 1f)),
            new WeaponDefinition("SG-12 / 霰弹枪", 5, 1.1f, 6, 1.5f, 6, 22, 1, 20, new Color(1f, 0.65f, 0.28f)),
            new WeaponDefinition("SR-09 / 精准步枪", 45, 1.2f, 8, 1.6f, 1, 0.4f, 2, 38, new Color(0.68f, 0.66f, 1f)),
            new WeaponDefinition("SMG-21 / 疾风", 7, 13, 42, 1.05f, 1, 5, 1, 27, new Color(0.2f, 0.8f, 1)) { recoil = 0.035f },
            new WeaponDefinition("RG-08 / 电磁轨道", 82, 0.7f, 4, 1.9f, 1, 0, 4, 52, new Color(0.8f, 0.35f, 1)) { recoil = 0.2f },
            new WeaponDefinition("GL-40 / 烈焰榴弹", 42, 1.2f, 5, 1.8f, 1, 3, 1, 15, new Color(1, 0.64f, 0.1f)) { explosionRadius = 2.5f, recoil = 0.16f },
            new WeaponDefinition("LS-01 / 霓光激光刃", 54, 2.1f, 18, 0.9f, 1, 0, 3, 22, new Color(0.22f, 0.95f, 1)) { recoil = 0.04f },
            new WeaponDefinition("AC-13 / 电弧投射器", 24, 2.4f, 16, 1.35f, 1, 0, 3, 19, new Color(0.48f, 0.68f, 1)) { recoil = 0.08f },
            new WeaponDefinition("BN-07 / 星脉连发弩", 16, 5.5f, 24, 1.25f, 3, 8, 2, 32, new Color(1, 0.44f, 0.7f)) { recoil = 0.055f }
        };

        [Header("Incoming hits / challenging without full-health one-shots")]
        [Range(1, 2)] public float incomingDamageMultiplier = 1.35f;
        [Range(0, 0.1f)] public float stageDamageStep = 0.04f;
        [Range(0.1f, 0.5f)] public float enemyHitLimit = 0.24f;
        [Range(0.1f, 0.5f)] public float hazardHitLimit = 0.28f;
        [Range(0.15f, 0.6f)] public float bossSkillHitLimit = 0.36f;
        [Range(0.2f, 0.7f)] public float bossUltimateHitLimit = 0.45f;
        [Range(0.3f, 0.7f)] public float playerMaximumHitFraction = 0.55f;
        [Range(0.4f, 1)] public float playerHitProtectionSeconds = 0.6f;
        [Header("Boss ascension / high-intensity attacks")]
        [Range(1, 5)] public float bossSkillDamageMultiplier = 1.5f;
        [Range(1, 6)] public float bossUltimateDamageMultiplier = 1.9f;
        [Range(1, 2.5f)] public float bossSkillAreaScale = 1.65f;
        [Range(1, 3)] public float bossUltimateAreaScale = 1.9f;
        [Header("Boss armour / prolonged encounter")]
        [Range(0, 0.85f)] public float bossDamageReduction = 0.62f;
        [Range(0.001f, 0.05f)] public float bossMaximumHitFraction = 0.008f;
        [Range(1, 3)] public float bossHealthMultiplier = 1.5f;
        public Sprite[] pickupSprites;

        public bool Validate(out string reason)
        {
            if (!InRange(enemyHitLimit, 0.1f, 0.5f) || !InRange(hazardHitLimit, 0.1f, 0.5f)
                || !InRange(bossSkillHitLimit, 0.15f, 0.6f) || !InRange(bossUltimateHitLimit, 0.2f, 0.7f)
                || enemyHitLimit > hazardHitLimit || hazardHitLimit > bossSkillHitLimit || bossSkillHitLimit > bossUltimateHitLimit
                || !InRange(playerMaximumHitFraction, 0.3f, 0.7f) || !InRange(playerHitProtectionSeconds, 0.4f, 1))
            { reason = "Invalid player incoming-hit limits or protection time."; return false; }
            if (!InRange(bossDamageReduction, 0, 0.85f) || !InRange(bossMaximumHitFraction, 0.001f, 0.05f)
                || !InRange(bossHealthMultiplier, 1, 3))
            { reason = "Invalid boss armour tuning."; return false; }
            if (!InRange(incomingDamageMultiplier, 1, 2) || !InRange(stageDamageStep, 0, 0.1f))
            { reason = "Invalid incoming damage tuning."; return false; }
            if (!InRange(bossSkillDamageMultiplier, 1, 5) || !InRange(bossUltimateDamageMultiplier, 1, 6)
                || !InRange(bossSkillAreaScale, 1, 2.5f) || !InRange(bossUltimateAreaScale, 1, 3))
            { reason = "Invalid boss ascension tuning."; return false; }
            if (sideScroller && (!InRange(gravityScale, 0.5f, 8) || !InRange(jumpVelocity, 3, 24) ||
                !InRange(coyoteTime, 0, 0.3f) || !InRange(jumpBuffer, 0, 0.3f) || !InRange(jumpCutMultiplier, 0.1f, 1) ||
                !InRange(iceFrictionFactor, 0.1f, 1) || !InRange(viewTacticalSize, 2, 12) ||
                !InRange(viewShoulderSize, 1.5f, viewTacticalSize) || !InRange(viewIronSightSize, 1, viewShoulderSize) ||
                (int)defaultView < 0 || (int)defaultView > 2 || characters == null || characters.Length < 4 || characters.Length > 12 || themes == null || (themes.Length != 8 && themes.Length != 10)))
            {
                reason = "Invalid platformer movement, view modes, four-to-twelve-character roster or eight/ten-stage campaign.";
                return false;
            }
            if (sideScroller)
            {
                foreach (CharacterDefinition character in characters)
                    if (character == null || string.IsNullOrWhiteSpace(character.name) || !InRange(character.health, 0.1f, 3) ||
                        !InRange(character.speed, 0.1f, 3) || !InRange(character.build, 0.5f, 2) || !InRange(character.magazine, 0.5f, 3) ||
                        !InRange(character.reload, 0.5f, 3) || !InRange(character.dodgeExtra, 0, 3) || character.unlockAfter < 0 || character.unlockAfter > 4)
                    { reason = "Invalid character definition."; return false; }
                foreach (ThemeProfile theme in themes)
                    if (theme == null || string.IsNullOrWhiteSpace(theme.title) || string.IsNullOrWhiteSpace(theme.boss))
                    { reason = "A stage theme is missing."; return false; }
            }
            if (!InRange(camBaseSize, 2, 14) || !InRange(camAdsSize, 1, camBaseSize) ||
                !InRange(camLead, 0, 6) || !InRange(camBack, 0, 8) || camLead - camBack < 1f ||
                !InRange(camShoulderOffset, -2, 2) || !InRange(camPitchDeg, 0, 55) || !InRange(camHeight, 0, 12) ||
                !InRange(camFollow, 0.5f, 30) || !InRange(camZoomSpeed, 0.5f, 30) ||
                !InRange(camRecoilMax, 0, 1) || !InRange(camRecoilDecay, 0.1f, 10) ||
                !InRange(camRecoilPitch, 0, 2) || !InRange(camWeaponRecoil.x, 0, 1) ||
                !InRange(camWeaponRecoil.y, 0, 1) || !InRange(camWeaponRecoil.z, 0, 1) ||
                !InRange(mouseLookSensitivity, 0.02f, 1) || !InRange(adsMoveMultiplier, 0.1f, 1))
            {
                reason = "Invalid camera settings: ADS size must not exceed base size; lead minus back must be >= 1; all camera values must be finite and within Inspector ranges.";
                return false;
            }
            if (playerHealth <= 0 || moveSpeed <= 0 || dodgeDuration <= 0 || dodgeCooldown <= 0 ||
                arenaHalfSize.x < 10 || arenaHalfSize.y < 8 || waveSeconds < 5 || wavesBeforeBoss < 1 ||
                maximumEnemies < 1 || maximumProjectiles < 8 || maximumPickups < 8)
            {
                reason = "Player, arena, wave, or object-cap values are invalid.";
                return false;
            }
            if (weapons == null || weapons.Length < 3 || weapons.Length > 9)
            {
                reason = "Three to nine weapons are supported, starting with AR / SG / SR.";
                return false;
            }
            foreach (WeaponDefinition weapon in weapons)
            {
                if (weapon == null || weapon.damage <= 0 || weapon.shotsPerSecond <= 0 || weapon.magazine < 1 ||
                    weapon.reloadSeconds <= 0 || weapon.pellets < 1 || weapon.pellets > 32 ||
                    weapon.targetsPerBullet < 1 || weapon.projectileSpeed <= 0 || weapon.spreadDegrees < 0 ||
                    !InRange(weapon.explosionRadius, 0, 5) || !InRange(weapon.recoil, 0, 0.4f))
                {
                    reason = "A weapon contains invalid damage, rate, ammo, reload, pellet or projectile values.";
                    return false;
                }
            }
            reason = string.Empty;
            return true;
        }

        private static bool InRange(float value, float minimum, float maximum)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value) && value >= minimum && value <= maximum;
        }
    }
}
