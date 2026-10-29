using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace MoriEchoes
{
    public sealed class HUDController : MonoBehaviour
    {
        private GameManager game;
        private RectTransform canvas;
        private GameObject menu;
        private GameObject hud;
        private GameObject pause;
        private GameObject upgrade;
        private GameObject results;
        private Image health;
        private Image healthTrail;
        private Image hurtVignette;
        private Text hurtText;
        private Text interactionText;
        private float damageTrailUntil, hurtUntil, hurtIntensity;
        private Health displayedHealth;
        public float PlayerHealthFill => health != null ? health.fillAmount : 0;
        public float PlayerDamageTrail => healthTrail != null ? healthTrail.fillAmount : 0;
        public float PlayerHitOpacity => hurtVignette != null ? hurtVignette.color.a : 0;
        public int PlayerHitsPresented { get; private set; }
        private Image experience;
        private Image energy;
        private Image bossHealth;
        private readonly Image[] bossPhaseHealth = new Image[3];
        private GameObject bossPanel;
        private Text healthText;
        private Text weaponText;
        private Text ammoText;
        private Text stateText;
        private Text skillText;
        private Text levelText;
        private Text announce;
        private Text resultTitle;
        private Text resultSummary;
        private readonly Text[] upgradeTitles = new Text[3];
        private readonly Text[] upgradeDescriptions = new Text[3];
        private readonly Text[] upgradeRanks = new Text[3];
        private readonly Button[] upgradeButtons = new Button[3];
        private Button startButton;
        private Button resumeButton;
        private Button restartButton;
        private Button nextStageButton;
        private Text bossTitle;
        private Text armStatus;
        private Text callsign;
        private Image portraitImage;
        private Image viewGun;
        private Image phaseFlash;
        private Text rosterInfo;
        private Text selectedCharacterTitle;
        private readonly Text[] abilityTitles = new Text[4];
        private readonly Text[] abilityDescriptions = new Text[4];
        public string SelectedAbilityDescription(int index) => index >= 0 && index < 4 && abilityDescriptions[index] != null ? abilityDescriptions[index].text : "";
        private Text loadoutInfo;
        private Button[] weaponButtons;
        private Button[] characterButtons;
        private Text[] characterStatuses;
        private Button[] stageButtons;
        private Text difficultyLabel;
        private Text paletteLabel;
        private float flashUntil;
        private float announceUntil;
        private GameObject reticle;
        private Image scope;
        private readonly RectTransform[] reticleArms = new RectTransform[4];
        public bool ReticleVisible => reticle != null && reticle.activeInHierarchy;
        public float ScopeOpacity => scope != null ? scope.color.a : 0;
        private static readonly Color Panel = new Color(0.035f, 0.065f, 0.105f, 0.96f);
        private static readonly Color TextColor = new Color(0.9f, 0.95f, 1f);
        // 缓存字段：避免每帧重新构建并分配字符串（UGUI 在内容不变时本就会跳过重建，
        // 但每帧 new 字符串仍会产生 GC 压力；下面用脏检查 + 节流把分配降到最低）。
        private string lastHealthText, lastLevelText, lastAmmoText, lastStateText, lastSkillText, lastWeaponText, lastBossTitle, lastArmText, lastAnnounce;
        private int lastHpCur = -1, lastHpMax = -1, lastWeaponIndex = -1;
        private int callsignChar = -1;
        private string callsignCache;
        private float hudThrottle, sideThrottle;

        public void Initialize(GameManager owner)
        {
            game = owner;
            var entity = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            entity.transform.SetParent(transform, false);
            var root = entity.GetComponent<Canvas>();
            root.renderMode = RenderMode.ScreenSpaceOverlay;
            root.sortingOrder = 1000;
            var scaler = entity.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            canvas = entity.GetComponent<RectTransform>();
            if (EventSystem.current == null)
            {
                var eventEntity = new GameObject("EventSystem", typeof(EventSystem));
                eventEntity.transform.SetParent(transform, false);
#if ENABLE_INPUT_SYSTEM
                eventEntity.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
#endif
            }
            BuildHud(); BuildMenu(); BuildPause(); BuildUpgrade(); BuildResults();
        }

        private RectTransform Widget(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            var entity = new GameObject(name, typeof(RectTransform));
            var rect = entity.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = anchor; rect.pivot = pivot;
            rect.anchoredPosition = position; rect.sizeDelta = size;
            return rect;
        }

        private RectTransform FullScreen(string name, Color color)
        {
            RectTransform rect = Widget(name, canvas, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            Image image = rect.gameObject.AddComponent<Image>(); image.color = color;
            return rect;
        }

        private Image ImageBox(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size, Color tint)
        {
            RectTransform rect = Widget(name, parent, anchor, pivot, position, size);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = tint; image.raycastTarget = false;
            return image;
        }

        private Text Label(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size,
            string content, int fontSize, Color color, TextAnchor alignment = TextAnchor.MiddleLeft)
        {
            RectTransform rect = Widget(name, parent, anchor, pivot, position, size);
            Text text = rect.gameObject.AddComponent<Text>();
            text.font = game.UiFont; text.fontSize = fontSize; text.text = content; text.color = color;
            text.alignment = alignment; text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }

        private Button Button(string name, Transform parent, Vector2 position, Vector2 size, string title, UnityAction action)
        {
            RectTransform rect = Widget(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, size);
            Image image = rect.gameObject.AddComponent<Image>(); image.color = new Color(0.08f, 0.22f, 0.29f);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white; colors.highlightedColor = new Color(0.5f, 1f, 1f);
            colors.selectedColor = new Color(0.65f, 0.92f, 1f); colors.pressedColor = new Color(0.3f, 0.6f, 0.7f);
            button.colors = colors;
            button.onClick.AddListener(action);
            Label("Label", rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, size - new Vector2(24, 8), title,
                25, TextColor, TextAnchor.MiddleCenter);
            return button;
        }

        private Image Bar(string name, Transform parent, Vector2 position, Vector2 size, Color color)
        {
            Image background = ImageBox(name, parent, Vector2.zero, Vector2.zero, position, size, new Color(0.12f, 0.18f, 0.23f));
            Image fill = ImageBox("Fill", background.transform, Vector2.zero, Vector2.zero, new Vector2(2, 2), size - new Vector2(4, 4), color);
            fill.sprite = game.Art.Square; fill.type = Image.Type.Filled; fill.fillMethod = Image.FillMethod.Horizontal;
            return fill;
        }

        private void BuildHud()
        {
            var root = FullScreen("HUD", Color.clear); hud = root.gameObject;
            root.GetComponent<Image>().raycastTarget = false;
            scope = ImageBox("ScopeVignette", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.one, Color.clear);
            scope.sprite = game.Art.ScopeVignette;
            RectTransform reticleRoot = Widget("CenterReticle", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(80, 80));
            reticle = reticleRoot.gameObject;
            ImageBox("Dot", reticleRoot, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(3, 3), TextColor);
            for (int index = 0; index < reticleArms.Length; index++)
                reticleArms[index] = ImageBox("Arm_" + index, reticleRoot, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero,
                    index < 2 ? new Vector2(10, 2) : new Vector2(2, 10), ProceduralArt.Cyan).rectTransform;
            Image playerPanel = ImageBox("PlayerStatus", root, new Vector2(0, 1), new Vector2(0, 1), new Vector2(28, -28), new Vector2(470, 142), Panel);
            Image portrait = ImageBox("Portrait", playerPanel.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(12, -12), new Vector2(94, 112), Color.white);
            portraitImage = portrait;
            portrait.sprite = game.Config.playerSprite != null ? game.Config.playerSprite : game.Art.Hero; portrait.preserveAspect = true;
            callsign = Label("Callsign", playerPanel.transform, Vector2.zero, Vector2.zero, new Vector2(120, 93), new Vector2(320, 38), "DELTA / 契约执行者", 22, ProceduralArt.Cyan);
            health = Bar("Health", playerPanel.transform, new Vector2(120, 62), new Vector2(320, 18), new Color(0.28f, 0.88f, 0.83f));
            healthTrail = Instantiate(health, health.transform.parent);
            healthTrail.name = "RecentDamageTrail";
            healthTrail.color = new Color(1, 0.3f, 0.32f);
            healthTrail.transform.SetSiblingIndex(0);
            healthText = Label("HealthValue", playerPanel.transform, Vector2.zero, Vector2.zero, new Vector2(120, 30), new Vector2(320, 30), "", 20, TextColor);
            experience = Bar("Experience", playerPanel.transform, new Vector2(120, 17), new Vector2(320, 6), ProceduralArt.Cyan);
            energy = Bar("UltimateEnergy", playerPanel.transform, new Vector2(120, 3), new Vector2(320, 8), new Color(1f, 0.45f, 0.82f));
            Image weaponPanel = ImageBox("WeaponStatus", root, Vector2.one, Vector2.one, new Vector2(-28, -28), new Vector2(360, 142), Panel);
            weaponText = Label("WeaponName", weaponPanel.transform, Vector2.zero, Vector2.zero, new Vector2(18, 96), new Vector2(325, 32), "", 22, ProceduralArt.Cyan);
            ammoText = Label("Ammo", weaponPanel.transform, Vector2.zero, Vector2.zero, new Vector2(18, 43), new Vector2(325, 53), "", 34, TextColor);
            Label("Slots", weaponPanel.transform, Vector2.zero, Vector2.zero, new Vector2(18, 13), new Vector2(325, 27), "1–9 / 滚轮切枪 · R 装填", 18, new Color(0.55f, 0.65f, 0.72f));
            stateText = Label("Wave", root, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -28), new Vector2(620, 60), "", 22, TextColor, TextAnchor.MiddleCenter);
            levelText = Label("RunStats", root, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -88), new Vector2(520, 40), "", 20, ProceduralArt.Cyan, TextAnchor.MiddleCenter);
            Image bossBack = ImageBox("BossStatus", root, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -190), new Vector2(680, 94), Panel);
            bossPanel = bossBack.gameObject;
            bossTitle = Label("BossName", bossBack.transform, Vector2.zero, Vector2.zero, new Vector2(15, 78), new Vector2(650, 26), "歼灭核心 / ANNIHILATION CORE", 18, ProceduralArt.Danger, TextAnchor.MiddleCenter);
            armStatus = Label("Arms", root, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -292), new Vector2(800, 30), "", 18, ProceduralArt.Danger, TextAnchor.MiddleCenter);
            bossHealth = Bar("BossHealth", bossBack.transform, new Vector2(18, 14), new Vector2(644, 10), ProceduralArt.Danger);
            bossPhaseHealth[0] = Bar("BossForm_I", bossBack.transform, new Vector2(18, 54), new Vector2(644, 8), new Color(1f, 0.36f, 0.25f));
            bossPhaseHealth[1] = Bar("BossForm_II", bossBack.transform, new Vector2(18, 38), new Vector2(644, 8), new Color(1f, 0.68f, 0.24f));
            bossPhaseHealth[2] = Bar("BossForm_III", bossBack.transform, new Vector2(18, 22), new Vector2(644, 8), new Color(0.72f, 0.42f, 1f));
            for (int index = 0; index < bossPhaseHealth.Length; index++) bossPhaseHealth[index].gameObject.SetActive(false);
            skillText = Label("Cooldowns", root, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 66), new Vector2(1660, 54), "", 17, TextColor, TextAnchor.MiddleCenter);
            Label("Controls", root, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 22), new Vector2(1450, 32),
                game.Config.sideScroller ? "AD 移动 · SPACE 跳跃 · S 蹲下 · SHIFT 冲刺 · Q/F/C 角色招式 · X 终极技 · 左键射击 · 右键瞄准 · TAB 视角 · 1–9 切枪 · E 互动/密道 · F11 全屏 · ESC 暂停"
                : "WASD 相对移动   ·   鼠标左右转向   ·   左键射击 / 右键开镜   ·   SPACE 闪避   ·   R 装弹   ·   Q 脉冲   ·   ESC 暂停", 18, new Color(0.55f, 0.66f, 0.73f), TextAnchor.MiddleCenter);
            announce = Label("Announcement", root, new Vector2(0.5f, 0.7f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1200, 70), "", 30, TextColor, TextAnchor.MiddleCenter);
            viewGun = ImageBox("IronSightWeapon", root, new Vector2(0.64f, 0), new Vector2(0.5f, 0), new Vector2(0, 100), new Vector2(330, 150), Color.white);
            viewGun.sprite = game.Art.Gun; viewGun.gameObject.SetActive(false);
            viewGun.preserveAspect = true;
            viewGun.transform.SetSiblingIndex(1);
            phaseFlash = ImageBox("PhaseFlash", root, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, Color.clear);
            phaseFlash.rectTransform.anchorMax = Vector2.one; phaseFlash.rectTransform.offsetMax = Vector2.zero;
            hurtVignette = ImageBox("PlayerHitVignette", root, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, Color.clear);
            hurtVignette.sprite = game.Art.ScopeVignette;
            hurtVignette.rectTransform.anchorMax = Vector2.one;
            hurtVignette.rectTransform.offsetMax = Vector2.zero;
            hurtVignette.transform.SetSiblingIndex(0);
            hurtText = Label("PlayerDamage", playerPanel.transform, Vector2.zero, Vector2.zero,
                new Vector2(310, 30), new Vector2(130, 30), "", 23, new Color(1, 0.42f, 0.4f), TextAnchor.MiddleRight);
            interactionText = Label("SecretInteraction", root, new Vector2(0.5f, 0.28f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(1100, 44), "", 25, TextColor, TextAnchor.MiddleCenter);
        }

        private void BuildMenu()
        {
            if (game.Config.sideScroller) { BuildSideMenu(); return; }
            RectTransform root = FullScreen("MainMenu", new Color(0.018f, 0.035f, 0.065f, 0.87f)); menu = root.gameObject;
            Image art = ImageBox("PlaceholderPortrait", root, new Vector2(0.72f, 0.52f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(510, 680), Color.white);
            art.sprite = game.Config.playerSprite != null ? game.Config.playerSprite : game.Art.Hero; art.preserveAspect = true;
            RectTransform content = Widget("Content", root, new Vector2(0.29f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(690, 760));
            Label("Title", content, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -42), new Vector2(650, 116), "末日回响", 76, TextColor, TextAnchor.MiddleCenter);
            Label("Subtitle", content, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -156), new Vector2(650, 50), "E C H O E S   O F   T O M O R R O W", 20, ProceduralArt.Cyan, TextAnchor.MiddleCenter);
            startButton = Button("Start", content, new Vector2(0, 20), new Vector2(450, 70), "进入战区", game.BeginRun);
            Button("Quit", content, new Vector2(0, -72), new Vector2(450, 62), "退出", game.Quit);
            Label("Mission", content, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 90), new Vector2(600, 90),
                "回收回响碎片，强化武装。\n坚持至歼灭核心出现，并摧毁它。", 23, new Color(0.65f, 0.76f, 0.83f), TextAnchor.MiddleCenter);
            Label("Prototype", root, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 25), new Vector2(1200, 38),
                "SOURCE PROTOTYPE  /  程序化占位美术", 18, new Color(0.45f, 0.58f, 0.67f), TextAnchor.MiddleCenter);
        }

        private void BuildSideMenu()
        {
            RectTransform root = FullScreen("MainMenu", new Color(0.018f, 0.035f, 0.065f, 0.99f)); menu = root.gameObject;
            Label("Title", root, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -26), new Vector2(1500, 75), "末日回响  /  ECHOES OF TOMORROW", 48, TextColor, TextAnchor.MiddleCenter);
            Label("Subtitle", root, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -102), new Vector2(1500, 40), "ARSENAL // 选择执行者、专属技能与出战武器", 22, ProceduralArt.Cyan, TextAnchor.MiddleCenter);
            characterButtons = new Button[game.Config.characters.Length];
            characterStatuses = new Text[characterButtons.Length];
            int rosterRows = Mathf.CeilToInt(characterButtons.Length / 2f);
            float cardHeight = (430 - (rosterRows - 1) * 10) / rosterRows;
            for (int index = 0; index < characterButtons.Length; index++)
            {
                int slot = index;
                CharacterDefinition character = game.Config.characters[index];
                Vector2 position = new Vector2(-650 + index % 2 * 460, 370 - cardHeight * 0.5f - index / 2 * (cardHeight + 10));
                Button card = Button("Character" + index, root, position, new Vector2(440, cardHeight), "", () => { game.SelectCharacter(slot); RefreshSideMenu(); });
                characterButtons[index] = card;
                Image image = ImageBox("Portrait", card.transform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(12, 0), new Vector2(100, cardHeight - 20), Color.white);
                image.sprite = game.Art.Side.Portraits[index]; image.preserveAspect = true;
                image.rectTransform.sizeDelta = new Vector2(76, cardHeight - 16);
                Label("Callsign", card.transform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(76, -7), new Vector2(370, 30), character.name, 18, character.accent, TextAnchor.MiddleLeft);
                Label("Specialty", card.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(76, 0), new Vector2(370, 25), character.role, 14, TextColor);
                characterStatuses[index] = Label("Role", card.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(76, 8), new Vector2(370, 34), "", 13, TextColor);
            }
            // A fixed, always-visible panel: no hover requirement, all four ability effects are shown.
            ImageBox("CharacterAbilityPanel", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(550, 155), new Vector2(780, 430), Panel);
            selectedCharacterTitle = Label("SelectedCharacterTitle", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(550, 347), new Vector2(730, 34), "", 23, ProceduralArt.Cyan);
            rosterInfo = Label("RosterInfo", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(550, 303), new Vector2(730, 54), "", 15, TextColor);
            string[] abilityKeys = { "Q", "F", "C", "X" };
            for (int index = 0; index < 4; index++)
            {
                float y = 244 - index * 77;
                Label("AbilityKey_" + index, root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(205, y - 15), new Vector2(42, 42), abilityKeys[index], 25, ProceduralArt.Cyan, TextAnchor.MiddleCenter);
                abilityTitles[index] = Label("AbilityTitle_" + index, root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(578, y), new Vector2(668, 26), "", 18, TextColor);
                abilityDescriptions[index] = Label("AbilityDescription_" + index, root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(578, y - 34), new Vector2(668, 45), "", 16, new Color(0.68f, 0.81f, 0.9f));
            }
            foreach (Button card in characterButtons)
            {
                foreach (Text label in card.GetComponentsInChildren<Text>())
                {
                    if (label.name == "Callsign" || label.name == "Specialty" || label.name == "Role")
                    { label.rectTransform.sizeDelta = new Vector2(326, label.rectTransform.sizeDelta.y); label.rectTransform.anchoredPosition = new Vector2(49, label.rectTransform.anchoredPosition.y); }
                }
            }
            weaponButtons = new Button[game.Config.weapons.Length];
            for (int index = 0; index < weaponButtons.Length; index++)
            {
                int slot = index;
                Vector2 position = new Vector2((index % 3 - 1) * 530, -111 - index / 3 * 62);
                Button button = Button("Loadout_" + index, root, position, new Vector2(500, 56), "",
                    () => { game.SelectStartingWeapon(slot); RefreshSideMenu(); });
                weaponButtons[index] = button;
                Image icon = ImageBox("Weapon", button.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-210, 0), new Vector2(64, 42), Color.white);
                Sprite iconSprite = game.Config.weaponSprites != null && index < game.Config.weaponSprites.Length ? game.Config.weaponSprites[index] : null;
                icon.sprite = iconSprite != null ? iconSprite : game.Art.WeaponSprite(index) != null ? game.Art.WeaponSprite(index) : game.Art.Gun;
                icon.preserveAspect = true;
                Label("Name", button.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(42, 0), new Vector2(400, 48), (index + 1) + "  " + game.Config.weapons[index].displayName, 19, TextColor, TextAnchor.MiddleCenter);
            }
            loadoutInfo = Label("LoadoutInfo", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -277), new Vector2(1600, 28), "", 16, ProceduralArt.Cyan, TextAnchor.MiddleCenter);
            stageButtons = new Button[game.Config.themes.Length];
            for (int index = 0; index < stageButtons.Length; index++)
            {
                int stage = index;
                int column = index % 5;
                int row = index / 5;
                float x = (column - 2) * 345;
                float y = -325 - row * 74;
                stageButtons[index] = Button("Stage_" + index, root, new Vector2(x, y), new Vector2(325, 44), (index + 1).ToString("00") + "  " + game.Config.themes[index].title,
                    () => { game.SelectStage(stage); RefreshSideMenu(); });
                Label("Mechanic_" + index, root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(x, y - 29), new Vector2(325, 26), game.Config.themes[index].mechanic, 13, new Color(0.5f, 0.66f, 0.75f), TextAnchor.MiddleCenter);
            }
            Button difficulty = Button("Difficulty", root, new Vector2(-700, -478), new Vector2(300, 58), "", () => { game.CycleDifficulty(); RefreshSideMenu(); });
            difficultyLabel = difficulty.GetComponentInChildren<Text>();
            Button palette = Button("Palette", root, new Vector2(-360, -478), new Vector2(300, 58), "", () => { game.TogglePalette(); RefreshSideMenu(); });
            paletteLabel = palette.GetComponentInChildren<Text>();
            startButton = Button("Start", root, new Vector2(70, -478), new Vector2(390, 62), "部署 / 开始关卡", game.BeginRun);
            Button("Quit", root, new Vector2(485, -478), new Vector2(230, 58), "退出", game.Quit);
            Label("Prototype", root, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 12), new Vector2(1700, 20), characterButtons.Length + " 名角色 · " + game.Config.weapons.Length + " 种武器 · " + game.Config.themes.Length + " 关战役 · TAB 切换视角 · F11 窗口", 15, new Color(0.45f, 0.6f, 0.7f), TextAnchor.MiddleCenter);
            RefreshSideMenu();
        }

        private void RefreshSideMenu()
        {
            if (!game.Config.sideScroller || rosterInfo == null) return;
            for (int index = 0; index < characterButtons.Length; index++)
            {
                CharacterDefinition candidate = game.Config.characters[index];
                bool available = game.Config.allCharactersAvailable || candidate.unlockAfter == 0 || candidate.unlockAfter < CampaignProgress.Unlocked;
                characterButtons[index].interactable = available;
                characterButtons[index].image.color = index == game.SelectedCharacter ? new Color(0.1f, 0.37f, 0.46f) : new Color(0.045f, 0.11f, 0.18f);
                characterStatuses[index].text = "Q " + candidate.SkillName + " · F " + CharacterTechniqueController.NameFor(candidate.GameplayId, 0)
                    + "\nC " + CharacterTechniqueController.NameFor(candidate.GameplayId, 1) + " · " + (available ? "可选" : "通关 " + candidate.unlockAfter + " 解锁");
            }
            for (int index = 0; index < stageButtons.Length; index++)
            {
                stageButtons[index].interactable = game.Config.allStagesAvailable || index < CampaignProgress.Unlocked;
                stageButtons[index].image.color = index == game.SelectedStage ? new Color(0.1f, 0.37f, 0.46f) : new Color(0.045f, 0.11f, 0.18f);
            }
            CharacterDefinition character = game.Character;
            selectedCharacterTitle.text = character.name;
            rosterInfo.text = character.role + "\n生命 ×" + character.health.ToString("0.00") + " · 移速 ×" + character.speed.ToString("0.00")
                + " · 弹匣 ×" + character.magazine.ToString("0.00") + " · 装填耗时 ×" + character.reload.ToString("0.00");
            int id = game.CharacterId;
            abilityTitles[0].text = character.SkillName + "  /  冷却 " + CharacterSkill.CooldownFor(id).ToString("0") + " 秒";
            abilityDescriptions[0].text = CharacterAbilityCatalog.Primary(id);
            for (int slot = 0; slot < 2; slot++)
            {
                abilityTitles[slot + 1].text = CharacterTechniqueController.NameFor(id, slot) + "  /  冷却 " + CharacterTechniqueController.CooldownFor(slot).ToString("0") + " 秒";
                abilityDescriptions[slot + 1].text = CharacterAbilityCatalog.Technique(id, slot);
            }
            abilityTitles[3].text = CharacterAbilityCatalog.UltimateName(id) + "  /  满 100 能量释放";
            abilityDescriptions[3].text = CharacterAbilityCatalog.Ultimate(id) + " 攻击/受击充能。";
            if (weaponButtons != null)
                for (int index = 0; index < weaponButtons.Length; index++)
                    weaponButtons[index].image.color = index == game.StartingWeapon ? new Color(0.1f, 0.37f, 0.46f) : new Color(0.045f, 0.11f, 0.18f);
            WeaponDefinition weapon = game.Config.weapons[game.StartingWeapon];
            loadoutInfo.text = "出战：" + weapon.displayName + " · " + (CharacterSkill.Mastery(game.CharacterId, game.StartingWeapon) ? "角色专精 +12%" : "通用武器")
                + " · 伤害 " + weapon.damage + " × " + weapon.pellets + " · " + weapon.shotsPerSecond + " 发/秒 · " + weapon.magazine + " 发弹匣 · 战斗中可切换全部武器";
            difficultyLabel.text = "难度：" + new[] { "新兵", "老兵", "契约" }[game.Difficulty];
            paletteLabel.text = CampaignProgress.Read("Palette." + game.CharacterId) > 0 ? "配色：" + (game.AlternatePalette ? "共鸣" : "原型") : "S 评价解锁配色";
        }

        private void BuildPause()
        {
            RectTransform root = FullScreen("Pause", new Color(0.012f, 0.024f, 0.04f, 0.91f)); pause = root.gameObject;
            Label("Title", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 255), new Vector2(700, 75), "战术暂停", 46, TextColor, TextAnchor.MiddleCenter);
            resumeButton = Button("Resume", root, new Vector2(0, 120), new Vector2(430, 64), "继续作战", game.Resume);
            Button("Restart", root, new Vector2(0, 35), new Vector2(430, 64), "重新开始", game.BeginRun);
            Button("Menu", root, new Vector2(0, -50), new Vector2(430, 64), "返回主菜单", game.ReturnToMenu);
            Button("DisplayMode", root, new Vector2(0, -290), new Vector2(430, 60), "F11 窗口 / 无边框全屏", game.Display.ToggleFullscreen);
            Label("VolumeLabel", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -145), new Vector2(430, 40), "主音量", 22, TextColor, TextAnchor.MiddleCenter);
            RectTransform sliderRoot = Widget("Volume", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -195), new Vector2(430, 36));
            var slider = sliderRoot.gameObject.AddComponent<Slider>();
            Image track = ImageBox("Track", sliderRoot, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(430, 8), new Color(0.15f, 0.25f, 0.3f));
            track.raycastTarget = true;
            RectTransform handleArea = Widget("HandleArea", sliderRoot, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
            handleArea.anchorMin = Vector2.zero; handleArea.anchorMax = Vector2.one;
            handleArea.offsetMin = new Vector2(12, 0); handleArea.offsetMax = new Vector2(-12, 0);
            Image handle = ImageBox("Handle", handleArea, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(24, 32), ProceduralArt.Cyan);
            handle.raycastTarget = true;
            slider.handleRect = handle.rectTransform; slider.targetGraphic = handle;
            slider.minValue = 0; slider.maxValue = 1; slider.value = game.Audio.Volume;
            slider.onValueChanged.AddListener(game.Audio.SetVolume);
        }

        private void BuildUpgrade()
        {
            RectTransform root = FullScreen("Upgrade", new Color(0.012f, 0.024f, 0.04f, 0.94f)); upgrade = root.gameObject;
            Label("Title", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 300), new Vector2(1100, 80), "回响共鸣 / 选择强化", 42, TextColor, TextAnchor.MiddleCenter);
            Label("Subtitle", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 230), new Vector2(1100, 45), "时间已暂停 · 每次选择立即改变本局构筑", 21, ProceduralArt.Cyan, TextAnchor.MiddleCenter);
            for (int index = 0; index < 3; index++)
            {
                int slot = index;
                Button button = Button("Option_" + index, root, new Vector2((index - 1) * 365, -20), new Vector2(335, 360), "", () => game.Experience.Choose(slot));
                upgradeButtons[index] = button;
                Label("Index", button.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 120), new Vector2(290, 50), "0" + (index + 1), 30, ProceduralArt.Cyan, TextAnchor.MiddleCenter);
                upgradeTitles[index] = Label("Title", button.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 48), new Vector2(290, 58), "", 30, TextColor, TextAnchor.MiddleCenter);
                upgradeDescriptions[index] = Label("Description", button.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -48), new Vector2(285, 112), "", 22, new Color(0.7f, 0.83f, 0.9f), TextAnchor.MiddleCenter);
                upgradeRanks[index] = Label("Rank", button.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -135), new Vector2(290, 34), "", 18, ProceduralArt.Cyan, TextAnchor.MiddleCenter);
            }
        }

        private void BuildResults()
        {
            RectTransform root = FullScreen("Results", new Color(0.012f, 0.024f, 0.04f, 0.94f)); results = root.gameObject;
            resultTitle = Label("Title", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 255), new Vector2(1000, 90), "", 54, TextColor, TextAnchor.MiddleCenter);
            resultSummary = Label("Summary", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 80), new Vector2(1000, 190), "", 28, new Color(0.7f, 0.83f, 0.9f), TextAnchor.MiddleCenter);
            nextStageButton = Button("NextStage", root, new Vector2(0, -30), new Vector2(450, 64), "下一关", game.NextStage);
            restartButton = Button("Restart", root, new Vector2(0, -120), new Vector2(450, 68), "再次出击", () => { if (game.Config.sideScroller && game.State == RunState.Lost) game.RetryCheckpoint(); else game.BeginRun(); });
            Button("Menu", root, new Vector2(0, -210), new Vector2(450, 62), "返回主菜单", game.ReturnToMenu);
        }

        public void ApplyState()
        {
            lastHpCur = lastHpMax = lastWeaponIndex = -1;
            lastHealthText = lastWeaponText = lastAmmoText = null;
            healthText.SetAllDirty(); weaponText.SetAllDirty(); ammoText.SetAllDirty();
            reticle.SetActive(game.IsPlaying);
            scope.gameObject.SetActive(game.IsPlaying);
            menu.SetActive(game.State == RunState.Menu);
            if (game.State == RunState.Menu) RefreshSideMenu();
            hud.SetActive(game.State == RunState.Playing || game.State == RunState.Paused || game.State == RunState.Upgrade);
            pause.SetActive(game.State == RunState.Paused);
            upgrade.SetActive(game.State == RunState.Upgrade);
            results.SetActive(game.State == RunState.Won || game.State == RunState.Lost);
            GameObject selected = null;
            if (game.State == RunState.Menu) selected = startButton.gameObject;
            if (game.State == RunState.Paused) selected = resumeButton.gameObject;
            if (game.State == RunState.Upgrade) { ShowUpgrade(); return; }
            if (game.State == RunState.Won || game.State == RunState.Lost)
            {
                resultTitle.text = game.State == RunState.Won ? "作战胜利 / VICTORY" : "信号中断 / DEFEAT";
                resultTitle.color = game.State == RunState.Won ? ProceduralArt.Cyan : ProceduralArt.Danger;
                resultSummary.text = "存活 " + FormatTime(game.Elapsed) + "    击破 " + game.Kills + "\n\n等级 " + game.Experience.Track.Level
                    + "    历史最高击破 " + PlayerPrefs.GetInt("MoriEchoes.BestKills", 0);
                nextStageButton.gameObject.SetActive(game.Config.sideScroller && game.State == RunState.Won);
                if (game.Config.sideScroller && game.Level != null)
                {
                    resultSummary.text = game.Level.Theme.title + "    " + FormatTime(game.Elapsed) + "    击破 " + game.Kills + "\n等级 " + game.Experience.Track.Level
                        + "    芯片 " + game.Level.Chips + "/3    重试 " + game.Level.Deaths + "\n"
                        + (game.State == RunState.Won ? "关卡评价  " + game.Level.Rating + "    已保存本地最佳记录" : "返回检查点 " + (game.Level.CheckpointSegment + 1) + " · 未消费经验减半");
                    restartButton.GetComponentInChildren<Text>().text = game.State == RunState.Lost ? "检查点重试" : "重玩本关";
                    nextStageButton.GetComponentInChildren<Text>().text = game.SelectedStage >= game.Config.themes.Length - 1 ? "战役完成 / 返回主菜单" : "进入下一关";
                }
                selected = restartButton.gameObject;
            }
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(selected);
        }

        public void ShowUpgrade()
        {
            UpgradeOption[] options = game.Experience.Options;
            for (int index = 0; index < 3; index++)
            {
                upgradeTitles[index].text = options[index].Title;
                upgradeDescriptions[index].text = options[index].Description;
                upgradeRanks[index].text = "当前阶级 " + game.Stats.Rank(options[index].Kind) + " → " + (game.Stats.Rank(options[index].Kind) + 1);
            }
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(upgradeButtons[0].gameObject);
        }

        public void Announce(string message, float seconds)
        {
            announce.text = message;
            lastAnnounce = message;
            announceUntil = Time.unscaledTime + seconds;
        }
        public void PhaseFlash() { flashUntil = Time.unscaledTime + 0.35f; }

        public void PlayerHit(float damage)
        {
            if (game.Player == null) return;
            float fraction = game.Player.Health.Current / Mathf.Max(1, game.Player.Health.Maximum);
            healthTrail.fillAmount = Mathf.Max(healthTrail.fillAmount, health.fillAmount);
            health.fillAmount = fraction;
            damageTrailUntil = Time.unscaledTime + 0.38f;
            hurtUntil = Time.unscaledTime + 0.7f;
            hurtIntensity = Mathf.Clamp(0.32f + damage / game.Player.Health.Maximum, 0.35f, 0.72f);
            hurtText.text = "−" + Mathf.CeilToInt(damage) + " HP";
            PlayerHitsPresented++;
        }

        private void UpdatePlayerHealth(Health hitPoints)
        {
            float fraction = Mathf.Clamp01(hitPoints.Current / Mathf.Max(1, hitPoints.Maximum));
            if (displayedHealth != hitPoints)
            {
                displayedHealth = hitPoints;
                health.fillAmount = healthTrail.fillAmount = fraction;
                damageTrailUntil = hurtUntil = 0;
                hurtText.text = "";
            }
            health.fillAmount = fraction;
            if (fraction >= healthTrail.fillAmount) healthTrail.fillAmount = fraction;
            else if (Time.unscaledTime > damageTrailUntil)
                healthTrail.fillAmount = Mathf.MoveTowards(healthTrail.fillAmount, fraction, Time.unscaledDeltaTime * 0.6f);
            float remaining = Mathf.Clamp01((hurtUntil - Time.unscaledTime) / 0.7f);
            hurtVignette.color = new Color(1, 0.075f, 0.1f, hurtIntensity * remaining);
            health.color = remaining > 0.65f ? new Color(1, 0.3f, 0.35f)
                : fraction < 0.25f ? new Color(1, 0.4f, 0.3f) : new Color(0.28f, 0.88f, 0.83f);
            hurtText.color = new Color(1, 0.42f, 0.4f, Mathf.Clamp01(remaining * 2));
            interactionText.text = game.Level != null && game.Level.SecretRoutes != null ? game.Level.SecretRoutes.InteractionPrompt : "";
        }

        private static void SetText(Text target, ref string cache, string value)
        {
            if (value == cache && target.text == value) return;
            cache = value;
            target.text = value;
        }

        private void Update()
        {
            if (game == null) return;
            if (game.State == RunState.Lost)
            {
                bool showHit = Time.unscaledTime < damageTrailUntil + 1.7f;
                hud.SetActive(showHit);
                results.SetActive(!showHit);
            }
            if (game.Config.sideScroller && game.Input.HasPointer && !game.Input.UsingGamepad)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas, game.Input.Pointer, null, out Vector2 point);
                reticle.GetComponent<RectTransform>().anchoredPosition = point;
            }
            phaseFlash.color = new Color(0.65f, 0.87f, 1, Mathf.Clamp01((flashUntil - Time.unscaledTime) * 1.4f));
            float ads = game.IsPlaying ? game.CameraRig.AdsAmount : 0;
            scope.color = new Color(1, 1, 1, ads);
            float scopeSize = Mathf.Max(canvas.rect.width, canvas.rect.height);
            scope.rectTransform.sizeDelta = Vector2.one * scopeSize;
            float gap = Mathf.Lerp(15, 8, ads) + game.CameraRig.Recoil * 35;
            reticleArms[0].anchoredPosition = Vector2.left * gap;
            reticleArms[1].anchoredPosition = Vector2.right * gap;
            reticleArms[2].anchoredPosition = Vector2.up * gap;
            reticleArms[3].anchoredPosition = Vector2.down * gap;
            if (Time.unscaledTime >= announceUntil) SetText(announce, ref lastAnnounce, string.Empty);
            if (game.Player == null || game.Experience == null || !game.Config.sideScroller && game.Waves == null) return;
            Health hitPoints = game.Player.Health;
            UpdatePlayerHealth(hitPoints);
            int hpCur = Mathf.CeilToInt(hitPoints.Current);
            int hpMax = Mathf.CeilToInt(hitPoints.Maximum);
            if (hpCur != lastHpCur || hpMax != lastHpMax)
            {
                lastHpCur = hpCur; lastHpMax = hpMax;
                SetText(healthText, ref lastHealthText, "HP " + hpCur + " / " + hpMax);
            }
            experience.fillAmount = (float)game.Experience.Track.Current / game.Experience.Track.Required;
            if (energy != null && game.Player.Energy != null)
            {
                energy.fillAmount = game.Player.Energy.Fraction;
                energy.color = game.Player.Energy.Ready ? Color.white : new Color(1f, 0.45f, 0.82f);
            }
            int weaponIndex = game.Player.Weapon.Selected;
            if (weaponIndex != lastWeaponIndex)
            {
                lastWeaponIndex = weaponIndex;
                SetText(weaponText, ref lastWeaponText, game.Player.Weapon.Definition.displayName);
            }
            if (game.Config.sideScroller)
            {
                WeaponController sideWeapon = game.Player.Weapon;
                SetText(ammoText, ref lastAmmoText, sideWeapon.UsesEnergyAmmo ? "能量装备"
                    : sideWeapon.IsReloading ? "装填 " + Mathf.RoundToInt(sideWeapon.ReloadFraction * 100) + "%"
                    : sideWeapon.Ammo.ToString("00") + " / " + sideWeapon.Capacity);
                UpdateSideHud(); return;
            }
            hudThrottle += Time.unscaledDeltaTime;
            if (hudThrottle < 0.1f) return;
            hudThrottle = 0;
            ExperienceTrack track = game.Experience.Track;
            SetText(levelText, ref lastLevelText, "LV " + track.Level + "   ·   击破 " + game.Kills + "   ·   " + FormatTime(game.Elapsed));
            WeaponController weapon = game.Player.Weapon;
            SetText(ammoText, ref lastAmmoText, weapon.IsReloading ? "装填 " + Mathf.RoundToInt(weapon.ReloadFraction * 100) + "%" : weapon.Ammo.ToString("00") + " / " + weapon.Capacity);
            SetText(stateText, ref lastStateText, game.Waves.BossSpawned ? "BOSS // 最终信号" : game.Waves.Intermission ? "下一波 " + Mathf.CeilToInt(game.Waves.Remaining) + "s"
                : "WAVE " + game.Waves.Wave.ToString("00") + " / " + game.Config.wavesBeforeBoss + "   " + Mathf.CeilToInt(game.Waves.Remaining) + "s");
            SetText(skillText, ref lastSkillText, "SPACE " + (game.Player.DodgeRemaining > 0 ? game.Player.DodgeRemaining.ToString("0.0") + "s" : "就绪")
                + "       Q " + (game.Stats.PulseRank == 0 ? "未解锁 · 升级获取电磁脉冲" : game.Player.PulseRemaining > 0 ? game.Player.PulseRemaining.ToString("0.0") + "s" : "脉冲就绪"));
            bool bossAlive = game.Waves.Boss != null && !game.Waves.Boss.Health.IsDead;
            bossPanel.SetActive(bossAlive);
            if (bossAlive)
            {
                bossHealth.gameObject.SetActive(true);
                bossHealth.fillAmount = game.Waves.Boss.Health.Current / game.Waves.Boss.Health.Maximum;
                foreach (Image bar in bossPhaseHealth) bar.gameObject.SetActive(false);
            }
        }

        private void UpdateSideHud()
        {
            LevelManager level = game.Level;
            if (callsignChar != game.SelectedCharacter)
            {
                callsignChar = game.SelectedCharacter;
                callsignCache = game.Character.name.Split('/')[0].Trim() + " / " + game.Character.role;
            }
            callsign.fontSize = 19;
            callsign.text = callsignCache;
            portraitImage.sprite = game.Art.Side.Portraits[game.SelectedCharacter];
            viewGun.gameObject.SetActive(game.IsPlaying && game.CameraRig.Side.Mode == ViewMode.IronSight);
            if (viewGun.gameObject.activeSelf)
            {
                int selected = game.Player.Weapon.Selected;
                Sprite[] views = game.Config.weaponViewSprites;
                Sprite fallback = game.Config.weaponSprites != null && selected < game.Config.weaponSprites.Length ? game.Config.weaponSprites[selected] : null;
                viewGun.sprite = views != null && selected < views.Length && views[selected] != null ? views[selected]
                    : fallback != null ? fallback : game.Art.WeaponSprite(selected) != null ? game.Art.WeaponSprite(selected) : game.Art.Gun;
                float recoil = game.CameraRig.Recoil;
                float sway = Mathf.Sin(Time.time * 8) * Mathf.Clamp01(Mathf.Abs(game.Input.Move.x)) * 4;
                viewGun.rectTransform.sizeDelta = new Vector2(520, 370);
                viewGun.rectTransform.anchoredPosition = new Vector2(50 + sway, 68 - recoil * 170 - (game.Player.Weapon.IsReloading ? Mathf.Sin(game.Player.Weapon.ReloadFraction * Mathf.PI) * 130 : 0));
                viewGun.rectTransform.localRotation = Quaternion.Euler(0, 0, sway * 0.15f - recoil * 14);
            }
            sideThrottle += Time.unscaledDeltaTime;
            if (sideThrottle < 0.1f) return;
            sideThrottle = 0;
            SetText(stateText, ref lastStateText, level.Theme.title + "  " + (level.SegmentIndex + 1) + "/" + level.Segments.Length + "\n" + level.Objective);
            SetText(levelText, ref lastLevelText, "击破 " + game.Kills
                + (game.Combo > 1 ? "    连击 x" + game.Combo + " (" + Mathf.RoundToInt(game.StyleMultiplier * 100) + "%)" : "")
                + "    风格分 " + game.StyleScore + "    " + FormatTime(game.Elapsed)
                + (level.IsUnderwaterStage && level.Swim != null ? "    氧气 " + level.Swim.Oxygen.ToString("0") + "%"
                    + (level.Swim.NormalizedDepth(game.Player.transform.position.y) > 0.92f ? " · 危险水压" : "") : ""));
            string ultimate = game.Player.Energy.Ready ? "就绪" : game.Player.Energy.Current.ToString("0") + "%";
            SetText(skillText, ref lastSkillText, "SHIFT " + (game.Player.DodgeRemaining > 0 ? game.Player.DodgeRemaining.ToString("0.0") + "s" : "就绪")
                + "   Q " + game.Character.SkillName + " " + (game.Player.Skill.Active ? "生效" : game.Player.Skill.Remaining > 0 ? game.Player.Skill.Remaining.ToString("0.0") + "s" : "就绪")
                + "   F " + game.Player.Techniques.Name(0) + " " + (game.Player.Techniques.Remaining(0) > 0 ? game.Player.Techniques.Remaining(0).ToString("0.0") + "s" : "就绪")
                + "   C " + game.Player.Techniques.Name(1) + " " + (game.Player.Techniques.Remaining(1) > 0 ? game.Player.Techniques.Remaining(1).ToString("0.0") + "s" : "就绪")
                + "   X 终极技 " + ultimate + "   " + game.CameraRig.Side.ModeLabel + "   检查点 " + (level.CheckpointSegment + 1) + "   芯片 " + level.Chips);
            bool alive = level.Boss != null && !level.Boss.Health.IsDead;
            bossPanel.SetActive(alive);
            if (!alive)
            {
                bossHealth.gameObject.SetActive(false);
                foreach (Image bar in bossPhaseHealth) bar.gameObject.SetActive(false);
                SetText(armStatus, ref lastArmText, string.Empty);
                return;
            }
            BossController controller = level.Boss.GetComponent<BossController>();
            SideBossController boss = controller.Side;
            VerticalBossController verticalBoss = controller.Vertical;
            SwimBossController swimBoss = controller.Swim;
            int phase = swimBoss != null ? swimBoss.Phase : verticalBoss != null ? verticalBoss.Phase : boss.Phase;
            bool executionWindow = swimBoss == null && (verticalBoss != null ? verticalBoss.ExecutionWindow : boss.ExecutionWindow);
            string pattern = game.Effects.AttackTelegraphsEnabled
                ? swimBoss != null ? swimBoss.FormName : verticalBoss != null ? verticalBoss.PatternName : boss.PatternName
                : swimBoss != null ? swimBoss.FormName : verticalBoss != null ? verticalBoss.FormName : boss.FormName;
            SetText(bossTitle, ref lastBossTitle, level.Theme.boss + "  ·  PHASE " + phase + "  "
                + (executionWindow ? "处决演出" : pattern));
            bossHealth.gameObject.SetActive(false);
            float ratio = level.Boss.Health.Current / Mathf.Max(1, level.Boss.Health.Maximum);
            float phaseTwo = verticalBoss != null || swimBoss != null ? 0.66f : boss.SecondFormThreshold;
            float phaseThree = verticalBoss != null || swimBoss != null ? 0.3f : boss.ThirdFormThreshold;
            float[] phaseFills =
            {
                phase == 1 ? Mathf.InverseLerp(phaseTwo, 1, ratio) : 0,
                phase == 1 ? 1 : phase == 2 ? Mathf.InverseLerp(phaseThree, phaseTwo, ratio) : 0,
                phase < 3 ? 1 : Mathf.InverseLerp(0, phaseThree, ratio)
            };
            for (int index = 0; index < bossPhaseHealth.Length; index++)
            {
                bossPhaseHealth[index].gameObject.SetActive(true);
                bossPhaseHealth[index].fillAmount = phaseFills[index];
            }
            if (verticalBoss != null)
                SetText(armStatus, ref lastArmText, game.Effects.AttackTelegraphsEnabled
                    ? (verticalBoss.Preparing ? "预警蓄力 · " : "下一招 · ") + verticalBoss.AttackId
                        + (verticalBoss.Transitioning ? " · 形态转换" : "")
                    : verticalBoss.Transitioning ? "形态转换" : string.Empty);
            else if (swimBoss != null)
                SetText(armStatus, ref lastArmText, swimBoss.Preparing ? "红色区域锁定中 · 起手后可闪避" : string.Empty);
            else if (boss != null && boss.Arms != null && boss.Arms.Length > 0)
            {
                string armText = string.Empty;
                foreach (Health arm in boss.Arms) armText += "  弱点 " + Mathf.CeilToInt(arm.Current) + "/" + Mathf.CeilToInt(arm.Maximum);
                SetText(armStatus, ref lastArmText, armText);
            }
            else SetText(armStatus, ref lastArmText, string.Empty);
        }

        private static string FormatTime(float time) => ((int)time / 60).ToString("00") + ":" + ((int)time % 60).ToString("00");
    }
}
