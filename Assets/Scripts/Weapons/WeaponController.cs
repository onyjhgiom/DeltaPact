using UnityEngine;

namespace MoriEchoes
{
    public sealed class WeaponController : MonoBehaviour
    {
        public int Selected { get; private set; }
        public WeaponDefinition Definition => game.Config.weapons[Selected];
        public CombatPowerup Powerup { get; private set; }
        public bool UsesEnergyAmmo => Powerup != null && Powerup.IsActive;
        public int Ammo => ammo[Selected];
        public int Capacity => Mathf.Max(1, Mathf.CeilToInt(Definition.magazine * game.Stats.MagazineMultiplier * (game.Config.sideScroller ? game.Character.magazine : 1)));
        public float ReloadDuration => Definition.reloadSeconds * (game.Config.sideScroller ? game.Character.reload : 1);
        public bool IsReloading => reloadRemaining > 0;
        public float ReloadFraction => IsReloading ? 1f - reloadRemaining / ReloadDuration : 1f;
        private int[] ammo;
        private GameManager game;
        private PlayerController player;
        private Transform pivot;
        private SpriteRenderer gun;
        private float cooldown;
        private float reloadRemaining;
        private bool hasPowerupSnapshot;
        private int savedSelected;
        private float savedCooldown;
        private float savedReloadRemaining;
        private readonly RaycastHit2D[] bladeHits = new RaycastHit2D[64];

        public void Initialize(GameManager owner, PlayerController controller)
        {
            if (Powerup != null) { Powerup.Clear(); Powerup.Changed -= OnPowerupChanged; }
            if (pivot != null) { pivot.gameObject.SetActive(false); Destroy(pivot.gameObject); }
            game = owner; player = controller;
            hasPowerupSnapshot = false;
            cooldown = reloadRemaining = 0;
            ammo = new int[game.Config.weapons.Length];
            Selected = game.Config.sideScroller ? game.StartingWeapon : 0;
            for (int index = 0; index < ammo.Length; index++) ammo[index] = Mathf.CeilToInt(game.Config.weapons[index].magazine * (game.Config.sideScroller ? game.Character.magazine : 1));
            pivot = new GameObject("WeaponPivot").transform;
            pivot.SetParent(transform, false);
            gun = game.Art.Renderer("Gun", pivot, game.Art.Gun, Color.white, new Vector2(0.9f, 0.33f), 111);
            gun.transform.localPosition = new Vector3(0.48f, 0, 0);
            Powerup = GetComponent<CombatPowerup>();
            if (Powerup == null) Powerup = gameObject.AddComponent<CombatPowerup>();
            Powerup.Initialize(game, player);
            Powerup.Changed += OnPowerupChanged;
            RefreshSprite();
        }

        public bool TryApplyPowerup(CombatPowerupKind kind, float duration = 0)
            => isActiveAndEnabled && Powerup != null && Powerup.TryActivate(kind, duration);

        public void ClearPowerup() { if (Powerup != null) Powerup.Clear(); }

        private void OnPowerupChanged(CombatPowerup powerup)
        {
            if (ammo == null || game == null) return;
            if (powerup.IsActive)
            {
                if (!hasPowerupSnapshot)
                {
                    savedSelected = Selected; savedCooldown = cooldown; savedReloadRemaining = reloadRemaining;
                    hasPowerupSnapshot = true;
                }
                Selected = powerup.Profile.ResolveWeaponIndex(ammo.Length);
                cooldown = reloadRemaining = 0;
            }
            else if (hasPowerupSnapshot)
            {
                Selected = savedSelected; cooldown = savedCooldown; reloadRemaining = savedReloadRemaining;
                hasPowerupSnapshot = false;
            }
            if (gun != null) RefreshSprite();
        }

        public void RefreshSprite()
        {
            Sprite custom = game.Config.weaponSprites != null && game.Config.weaponSprites.Length > Selected
                ? game.Config.weaponSprites[Selected] : null;
            if (custom == null && Selected >= 6) custom = game.Art.WeaponSprite(Selected);
            gun.sprite = custom != null ? custom : game.Art.Gun;
            gun.color = custom != null ? Color.white : Color.Lerp(Definition.color, Color.white, 0.3f);
            if (game.Config.sideScroller && CampaignProgress.Read("Skin." + game.SelectedStage) > 0 && custom == null)
                gun.color = new Color(1.5f, 1.1f, 0.45f);
            if (UsesEnergyAmmo) gun.color = Color.Lerp(Color.white, Powerup.Tint, 0.65f);
            gun.transform.localScale = custom != null ? Vector3.one : new Vector3(Selected == 2 ? 1.2f : 0.9f, 0.33f, 1);
        }

        private void Update()
        {
            if (game == null) return;
            if (player == null || player.Health.IsDead || game.State == RunState.Won
                || game.State == RunState.Lost || game.State == RunState.Menu) { ClearPowerup(); return; }
            if (!game.IsPlaying || !game.ControlsReady || Time.timeScale <= 0) return;
            cooldown = Mathf.Max(0, cooldown - Time.deltaTime);
            int requested = game.Input.SwitchWeapon;
            if (requested == -2) requested = (Selected + ammo.Length - 1) % ammo.Length;
            if (requested == -3) requested = (Selected + 1) % ammo.Length;
            if (!UsesEnergyAmmo && requested >= 0 && requested < ammo.Length && requested != Selected)
            {
                Selected = requested;
                reloadRemaining = 0;
                cooldown = Mathf.Max(cooldown, 0.2f);
                RefreshSprite();
            }
            if (IsReloading)
            {
                reloadRemaining = Mathf.Max(0, reloadRemaining - Time.deltaTime);
                if (!IsReloading) ammo[Selected] = Capacity;
                return;
            }
            if (game.Input.Reload) BeginReload();
            if (game.Input.Fire && cooldown <= 0 && !IsReloading)
            {
                if (!UsesEnergyAmmo && Ammo <= 0) BeginReload();
                else Fire();
            }
        }

        private void BeginReload()
        {
            if (UsesEnergyAmmo || Ammo >= Capacity || IsReloading) return;
            reloadRemaining = ReloadDuration;
            game.Audio.Play(SoundKind.Reload);
        }

        private void LateUpdate()
        {
            if (game == null || player == null) return;
            Camera camera = game.CameraRig.Camera;
            Vector3 screenDirection = camera.transform.InverseTransformDirection(player.Aim);
            float angle = Mathf.Atan2(screenDirection.y, screenDirection.x) * Mathf.Rad2Deg;
            pivot.rotation = camera.transform.rotation * Quaternion.Euler(0, 0, angle);
            gun.flipY = screenDirection.x < 0;
            gun.sortingOrder = game.CameraRig.SortOrder(transform.position) + 2;
            if (player.Motor != null)
            {
                pivot.position = player.Motor.MuzzleOrigin;
                gun.enabled = game.CameraRig.Side.Mode != ViewMode.IronSight;
            }
        }

        private void Fire()
        {
            if (Selected == 6 && !UsesEnergyAmmo) { FireLaserBlade(); return; }
            WeaponDefinition definition = Definition;
            bool energy = UsesEnergyAmmo;
            CombatPowerupProfile profile = energy ? Powerup.Profile : default;
            int pellets = energy ? Mathf.Min(profile.Pellets, Mathf.Max(1, game.Config.maximumProjectiles)) : definition.pellets;
            if (game.Projectiles.FreeCount < pellets) return;
            if (!energy) ammo[Selected]--;
            cooldown = 1f / (definition.shotsPerSecond * game.Stats.FireRateMultiplier
                * (player.Skill != null ? player.Skill.FireRateMultiplier : 1)
                * (player.Energy != null ? player.Energy.FireRateMultiplier : 1) * (energy ? profile.FireRateMultiplier : 1));
            Color tint = energy ? Powerup.Tint : definition.color;
            Vector2 origin = player.Motor != null ? player.Motor.MuzzleOrigin : (Vector2)transform.position;
            Vector2 muzzle = origin + player.Aim * 0.95f;
            Vector2 projectileOrigin = origin + player.Aim * 0.15f;
            for (int pellet = 0; pellet < pellets; pellet++)
            {
                float scatter = energy ? CombatPowerupRules.PelletAngle(pellet, pellets, profile.SpreadDegrees) * game.Stats.SpreadMultiplier
                    : Random.Range(-0.5f, 0.5f) * definition.spreadDegrees * game.Stats.SpreadMultiplier;
                Vector2 direction = Quaternion.Euler(0, 0, scatter) * player.Aim;
                bool critical = Random.value < game.Stats.CriticalChance + (player.Skill != null ? player.Skill.CriticalBonus : 0);
                float mastery = (player.Skill != null && CharacterSkill.Mastery(game.CharacterId, Selected) ? 1.12f : 1)
                    * game.CombatDamageMultiplier;
                float pelletBudget = energy ? (float)definition.pellets / Mathf.Max(1, profile.Pellets) : 1;
                game.Projectiles.Fire(projectileOrigin, direction, definition.projectileSpeed * (energy ? profile.SpeedMultiplier : 1),
                    definition.damage * game.Stats.DamageMultiplier * mastery * (critical ? 1.8f : 1f) * (energy ? profile.DamageMultiplier : 1) * pelletBudget,
                    Team.Player, definition.targetsPerBullet + game.Stats.ExtraPierce + (energy ? profile.ExtraPierce : 0),
                    tint, critical, 0, energy ? profile.ExplosionRadius : definition.explosionRadius);
            }
            game.Effects.Muzzle(muzzle, player.Aim, tint);
            game.CameraRig.Shake(Selected == 1 ? 0.12f : 0.045f);
            game.CameraRig.AddRecoil((Selected < 3 ? game.Config.camWeaponRecoil[Selected] : definition.recoil) * player.Skill.RecoilMultiplier);
            game.Audio.Play(Selected == 1 ? SoundKind.Shotgun : Selected == 2 ? SoundKind.Sniper : SoundKind.Shot);
        }

        private void FireLaserBlade()
        {
            WeaponDefinition definition = Definition;
            if (Ammo <= 0) { BeginReload(); return; }
            ammo[Selected]--;
            cooldown = 1f / Mathf.Max(1, definition.shotsPerSecond * (player.Skill != null ? player.Skill.FireRateMultiplier : 1));
            Vector2 origin = player.Motor != null ? player.Motor.MuzzleOrigin : transform.position;
            Vector2 direction = player.Aim.normalized;
            var filter = new ContactFilter2D { useTriggers = false };
            int count = Physics2D.CircleCast(origin, 0.34f, direction, filter, bladeHits, 2.7f);
            game.Effects.WarningLine(origin + direction * 1.35f, direction, 2.7f, 0.12f);
            game.Effects.SkillWave(origin + direction * 1.5f, definition.color, 1.3f);
            for (int index = 0; index < count; index++)
            {
                Collider2D collider = bladeHits[index].collider;
                if (collider == null) continue;
                Health target = collider.GetComponentInParent<Health>();
                if (target == null || target.Team == Team.Player || target.IsDead) continue;
                float mastery = player.Skill != null && CharacterSkill.Mastery(game.CharacterId, Selected) ? 1.12f : 1;
                float damage = definition.damage * mastery * game.Stats.DamageMultiplier * game.CombatDamageMultiplier;
                if (target.TakeDamage(damage, Team.Player))
                {
                    game.Effects.Burst(bladeHits[index].point, definition.color, 9);
                    game.Effects.DamageNumber(target.transform.position, target.LastDamageTaken, true);
                }
            }
            game.AwardStyle(Mathf.RoundToInt(18 * game.StyleMultiplier));
            game.CameraRig.Shake(0.12f); game.Audio.Play(SoundKind.Sniper);
        }

        public int[] CaptureAmmo() => (int[])ammo.Clone();
        public void RestoreAmmo(int[] saved)
        {
            ClearPowerup();
            reloadRemaining = cooldown = 0;
            for (int index = 0; index < ammo.Length; index++)
            {
                int capacity = Mathf.CeilToInt(game.Config.weapons[index].magazine * game.Stats.MagazineMultiplier * game.Character.magazine);
                ammo[index] = saved != null && index < saved.Length ? Mathf.Clamp(saved[index], 0, capacity) : capacity;
            }
        }

        private void OnDisable() { ClearPowerup(); }

        private void OnDestroy()
        {
            ClearPowerup();
            if (Powerup != null) Powerup.Changed -= OnPowerupChanged;
            if (pivot != null) Destroy(pivot.gameObject);
        }
    }
}
