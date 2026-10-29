using System.Collections.Generic;
using UnityEngine;

namespace MoriEchoes
{
    [DefaultExecutionOrder(-100)]
    public sealed class GameManager : MonoBehaviour
    {
        public GameConfig Config { get; private set; }
        public RunState State { get; private set; }
        public bool IsPlaying => State == RunState.Playing;
        public bool ControlsReady => IsPlaying && Time.unscaledTime >= controlsAt;
        public InputReader Input { get; private set; }
        public RunStats Stats { get; private set; }
        public PlayerController Player { get; private set; }
        public ProjectilePool Projectiles { get; private set; }
        public PickupPool Pickups { get; private set; }
        public ExperienceManager Experience { get; private set; }
        public WaveManager Waves { get; private set; }
        public LevelManager Level { get; private set; }
        public int SelectedStage { get; private set; }
        public int SelectedCharacter { get; private set; }
        public int StartingWeapon { get; private set; }
        public SceneFeedback Environment { get; set; }
        public DisplaySettings Display { get; private set; }
        public int Difficulty { get; private set; }
        public bool AlternatePalette { get; private set; }
        public CharacterDefinition Character => Config.characters[SelectedCharacter];
        public int CharacterId => Character.GameplayId;
        public float DamageScale => (Difficulty == 2 ? 1.4f : Difficulty == 1 ? 1.2f : 1)
            * (Config.incomingDamageMultiplier + SelectedStage * Config.stageDamageStep);
        public CameraRig CameraRig { get; private set; }
        public CombatEffects Effects { get; private set; }
        public GameAudio Audio { get; private set; }
        public HUDController Hud { get; private set; }
        public ProceduralArt Art { get; private set; }
        public Font UiFont { get; private set; }
        public List<EnemyAI> Enemies { get; } = new List<EnemyAI>();
        public int Kills { get; private set; }
        public int StyleScore { get; private set; }
        public int Combo { get; private set; }
        public float StyleMultiplier => 1f + Mathf.Min(Combo, 30) * 0.05f;
        public float CombatDamageMultiplier => StyleMultiplier * (CharacterId == 8 && Player != null
            && Player.GetComponent<Rigidbody2D>().linearVelocity.magnitude < 0.25f ? 1.28f : 1f);
        public float Elapsed { get; private set; }
        private float comboUntil;
        private Transform runRoot;
        private float controlsAt;
        private bool ownFont;
        private bool initialized;

        public void Initialize(GameConfig config, bool startInMenu)
        {
            Config = config;
            Input = new InputReader();
            Display = gameObject.AddComponent<DisplaySettings>();
            Stats = new RunStats();
            Art = new ProceduralArt(Config);
            UiFont = Config.uiFont;
            if (UiFont == null)
            {
                UiFont = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "SimHei", "Noto Sans CJK SC", "Arial" }, 24);
                ownFont = UiFont != null;
                if (UiFont == null) UiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }
            if (!Config.sideScroller)
            {
                var arena = new GameObject("Arena_ProceduralPlaceholder");
                arena.transform.SetParent(transform, false);
                Art.BuildArena(arena.transform, Config.arenaHalfSize);
            }
            var cameraEntity = new GameObject("MoriEchoesCamera", typeof(Camera), typeof(AudioListener));
            cameraEntity.tag = "MainCamera";
            cameraEntity.transform.SetParent(transform, false);
            CameraRig = cameraEntity.AddComponent<CameraRig>();
            CameraRig.Initialize(this);
            Audio = Child("Audio").gameObject.AddComponent<GameAudio>();
            Audio.Initialize(Config.masterVolume);
            Effects = Child("Effects").gameObject.AddComponent<CombatEffects>();
            Effects.Initialize(this);
            Hud = Child("UI").gameObject.AddComponent<HUDController>();
            Hud.Initialize(this);
            initialized = true;
            if (startInMenu) ReturnToMenu(); else BeginRun();
        }

        private Transform Child(string name)
        {
            var entity = new GameObject(name);
            entity.transform.SetParent(transform, false);
            return entity.transform;
        }

        public void BeginRun()
        {
            ClearRun();
            Stats = new RunStats();
            Kills = 0; StyleScore = 0; Combo = 0; comboUntil = 0; Elapsed = 0;
            Time.timeScale = 1;
            runRoot = Child("Run");
            var projectileRoot = new GameObject("Projectiles");
            projectileRoot.transform.SetParent(runRoot, false);
            Projectiles = projectileRoot.AddComponent<ProjectilePool>();
            Projectiles.Initialize(this);
            var pickupRoot = new GameObject("Pickups");
            pickupRoot.transform.SetParent(runRoot, false);
            Pickups = pickupRoot.AddComponent<PickupPool>();
            Pickups.Initialize(this);
            Experience = runRoot.gameObject.AddComponent<ExperienceManager>();
            Experience.Initialize(this);
            if (Config.sideScroller)
            {
                Level = runRoot.gameObject.AddComponent<LevelManager>();
                Level.Initialize(this);
            }
            var playerEntity = new GameObject("Player");
            playerEntity.transform.SetParent(runRoot, false);
            Player = playerEntity.AddComponent<PlayerController>();
            Player.Initialize(this);
            if (Config.sideScroller)
            {
                Player.Motor.ResetAt(Level.Checkpoint); Level.SaveInitialCheckpoint();
                Level.gameObject.AddComponent<WeaponCapsuleDirector>().Initialize(this);
                if (!Level.IsSwimStage)
                    Level.gameObject.AddComponent<EncounterHazardDirector>().Initialize(this, Level);
            }
            playerEntity.AddComponent<CharacterVisual>().Initialize(this, Player.Health);
            if (!Config.sideScroller)
            {
                Waves = runRoot.gameObject.AddComponent<WaveManager>();
                Waves.Initialize(this);
            }
            CameraRig.ResetFollow();
            controlsAt = Time.unscaledTime + 0.25f;
            SetState(RunState.Playing);
            Hud.Announce(Config.sideScroller ? Level.Theme.title + " // SPACE 二段跳 · 方向 + SHIFT 闪避 · 拾取胶囊变身" : "作战开始 // 保持移动，回收回响碎片", 3);
        }

        private void ClearRun()
        {
            if (runRoot != null)
            {
                runRoot.gameObject.SetActive(false);
                Destroy(runRoot.gameObject);
            }
            runRoot = null; Player = null; Projectiles = null; Pickups = null; Experience = null; Waves = null; Level = null;
            Enemies.Clear();
            Environment = null;
            if (Effects != null) Effects.ClearAll();
        }

        public void ReturnToMenu()
        {
            ClearRun();
            CameraRig.ResetFollow();
            SetState(RunState.Menu);
        }

        private void Update()
        {
            if (!initialized) return;
            Input.Capture();
            if (Input.Pause)
            {
                if (State == RunState.Playing) SetState(RunState.Paused);
                else if (State == RunState.Paused) Resume();
            }
            if (IsPlaying) Elapsed += Time.deltaTime;
            if (IsPlaying && Combo > 0 && Time.time > comboUntil) Combo = 0;
        }

        public void Pause()
        {
            if (IsPlaying) SetState(RunState.Paused);
        }

        public void Resume()
        {
            if (State != RunState.Paused) return;
            controlsAt = Time.unscaledTime + 0.15f;
            SetState(RunState.Playing);
        }

        public void OpenUpgrade()
        {
            if (!IsPlaying) return;
            SetState(RunState.Upgrade);
        }

        public void ResumeFromUpgrade()
        {
            if (State != RunState.Upgrade) return;
            controlsAt = Time.unscaledTime + 0.18f;
            SetState(RunState.Playing);
        }

        private void SetState(RunState state)
        {
            State = state;
            Time.timeScale = state == RunState.Playing ? 1 : 0;
            Audio.SetPaused(state != RunState.Playing);
            Cursor.lockState = state == RunState.Playing && !Config.sideScroller ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = Config.sideScroller || state != RunState.Playing;
            if (state != RunState.Playing) CameraRig.ClearTransient();
            Hud.ApplyState();
        }

        private void OnApplicationFocus(bool focused)
        {
            if (initialized && !focused && IsPlaying) Pause();
        }

        // 击杀连段：2.5s 窗口内连续击杀累加 Combo，风格分随倍率提升，满 3 的倍数弹"连杀"飘字。
        public void RecordKill()
        {
            Kills++;
            Combo = Time.time <= comboUntil ? Combo + 1 : 1;
            comboUntil = Time.time + 2.5f;
            AwardStyle(10 + Mathf.Min(Combo, 30) * 2);
            if (CharacterId == 10 && Player != null && Player.Health != null)
                Player.Health.Restore(Player.Health.Maximum * 0.025f);
            if (Combo >= 3 && Combo % 3 == 0 && Player != null)
                Effects.FloatText(Player.transform.position, "x" + Combo + " 连杀", new Color(1, 0.83f, 0.35f));
        }
        public void AwardStyle(int points) { StyleScore += Mathf.Max(0, points); }

        public void Pulse(Vector2 origin, float radius, float damage)
        {
            Effects.Pulse(origin, radius);
            for (int index = Enemies.Count - 1; index >= 0; index--)
            {
                EnemyAI enemy = Enemies[index];
                if (enemy == null || Vector2.Distance(origin, enemy.transform.position) > radius) continue;
                enemy.Health.TakeDamage(damage * CombatDamageMultiplier, Team.Player);
            }
            Audio.Play(SoundKind.Blast);
            CameraRig.Shake(0.25f);
        }

        public void Win()
        {
            if (State != RunState.Playing) return;
            EnemyAI requiredBoss = Config.sideScroller ? Level?.Boss : Waves?.Boss;
            if (requiredBoss == null || requiredBoss.Health == null || !requiredBoss.Health.IsDead || requiredBoss.Health.Current > 0) return;
            Level?.RecordClear();
            EndRun(RunState.Won);
        }

        public bool SelectCharacter(int index)
        {
            if (State != RunState.Menu || index < 0 || index >= Config.characters.Length ||
                !Config.allCharactersAvailable && Config.characters[index].unlockAfter >= CampaignProgress.Unlocked && Config.characters[index].unlockAfter > 0) return false;
            SelectedCharacter = index; return true;
        }
        public bool SelectStartingWeapon(int index)
        {
            if (State != RunState.Menu || index < 0 || index >= Config.weapons.Length) return false;
            StartingWeapon = index;
            return true;
        }
        public bool SelectStage(int index)
        {
            if (State != RunState.Menu || index < 0 || index >= Config.themes.Length
                || !Config.allStagesAvailable && index >= CampaignProgress.Unlocked) return false;
            SelectedStage = index; return true;
        }
        public void CycleDifficulty() { if (State == RunState.Menu) Difficulty = (Difficulty + 1) % 3; }
        public void TogglePalette()
        {
            if (State == RunState.Menu && CampaignProgress.Read("Palette." + CharacterId) > 0) AlternatePalette = !AlternatePalette;
        }
        public void RetryCheckpoint()
        {
            if (Level == null || State != RunState.Lost) { BeginRun(); return; }
            Level.Retry(); controlsAt = Time.unscaledTime + 0.25f;
            SetState(RunState.Playing);
            Hud.Announce("检查点重试 // 等级保留 · 未消费经验减半", 2);
        }
        public void NextStage()
        {
            if (State != RunState.Won) return;
            if (SelectedStage >= Config.themes.Length - 1) { ReturnToMenu(); return; }
            SelectedStage++; BeginRun();
        }

        public void Lose()
        {
            if (State == RunState.Playing) EndRun(RunState.Lost);
        }

        private void EndRun(RunState outcome)
        {
            if (!Config.sideScroller || !CampaignProgress.Volatile)
            {
                int best = Mathf.Max(Kills, PlayerPrefs.GetInt("MoriEchoes.BestKills", 0));
                PlayerPrefs.SetInt("MoriEchoes.BestKills", best);
                PlayerPrefs.Save();
            }
            SetState(outcome);
        }

        public void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void OnDestroy()
        {
            if (!initialized) return;
            Time.timeScale = 1;
            Art?.Dispose();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            if (ownFont && UiFont != null) Destroy(UiFont);
        }
    }
}
