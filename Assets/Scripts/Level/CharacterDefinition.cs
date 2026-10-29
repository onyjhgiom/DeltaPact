using System;
using UnityEngine;

namespace MoriEchoes
{
    public enum ViewMode { Tactical, Shoulder, IronSight }

    [Serializable]
    public sealed class CharacterDefinition
    {
        public string name;
        public string role;
        public float health = 1;
        public float speed = 1;
        public float build = 1;
        public float dodgeExtra;
        public float magazine = 1;
        public float reload = 1;
        public int unlockAfter;
        public bool doubleJump;
        public bool isMale;
        [TextArea] public string passiveDescription;
        public Color accent;
        public Color hair;
        public Sprite portrait;
        public Sprite bodySprite;
        public SideSpriteAnimation animation;
        public SideSpriteAnimation poweredAnimation;

        public string SkillName => HasCallsign("VANGUARD") ? "复苏壁垒" : HasCallsign("WRAITH") ? "相位跃迁" : HasCallsign("HAVOC") ? "蜂群轰击"
            : HasCallsign("AURORA") ? "零度刻线" : HasCallsign("EMBER") ? "焚火连锁" : HasCallsign("LUMEN") ? "星辉急救"
            : HasCallsign("NOVA") ? "超载共鸣" : HasCallsign("KALT") ? "零下裁决" : HasCallsign("VEIL") ? "无声猎线"
            : HasCallsign("SERA") ? "晨辉救援" : HasCallsign("ROOK") ? "破阵冲锋" : "战术专注";
        public string SkillDescription => HasCallsign("VANGUARD") ? "恢复最大生命 25% · 5 秒减伤 70% · 近身冲击" : HasCallsign("WRAITH") ? "朝瞄准方向传送最多 5.5 米 · 4 秒疾行减伤 · 到达冻结"
            : HasCallsign("HAVOC") ? "三发爆破弹 · 范围伤害 · 不消耗弹药" : HasCallsign("AURORA") ? "24 米精准贯穿 · 120 伤害 · 命中非 Boss 时停 3 秒 · 冷却 15 秒"
            : HasCallsign("EMBER") ? "燃烧弹最多追击弹跳 3 次 · 每次 24 伤害 · 留下 4 秒火区，每秒 16 伤害 · 冷却 18 秒"
            : HasCallsign("LUMEN") ? "立即治疗最大生命 22% · 5 秒持续修复 · 35%减伤 · 冷却 20 秒"
            : HasCallsign("NOVA") ? "脉冲冲击 6 米内敌人 · 5 秒射速 +90% · 冷却 17 秒"
            : HasCallsign("KALT") ? "静止架枪建立冷锋专注 · 短时轨道贯穿射击"
            : HasCallsign("VEIL") ? "短暂隐匿并向准星穿刺 · 脱离锁定后反击"
            : HasCallsign("SERA") ? "群体恢复并展开移动治疗场 · 受击者获得净化护盾"
            : HasCallsign("ROOK") ? "朝瞄准方向撞阵 · 终点重击击退并破坏掩体" : "5 秒射速 +60% · 暴击 +20%";
        public Sprite skillSprite;
        public Sprite ultimateSprite;
        public int GameplayId
        {
            get
            {
                string[] identities = { "DELTA", "VANGUARD", "WRAITH", "HAVOC", "AURORA", "EMBER", "LUMEN", "NOVA", "KALT", "VEIL", "SERA", "ROOK" };
                for (int index = 0; index < identities.Length; index++)
                    if (HasCallsign(identities[index])) return index;
                return 0;
            }
        }

        private bool HasCallsign(string callsign) => name != null && name.StartsWith(callsign, StringComparison.Ordinal);

        public static CharacterDefinition[] Defaults() => new[]
        {
            new CharacterDefinition { name = "DELTA / 契约执行者", role = "均衡 · 精准突击", accent = new Color(0.15f, 0.85f, 1), hair = new Color(0.9f, 0.94f, 1) },
            new CharacterDefinition { name = "VANGUARD / 重装壁垒", role = "生命 +40% · 重甲", health = 1.4f, speed = 0.85f, build = 1.15f, dodgeExtra = 0.5f, accent = new Color(1, 0.4f, 0.18f), hair = new Color(0.6f, 0.27f, 0.2f) },
            new CharacterDefinition { name = "WRAITH / 幽影侦察", role = "二段跳 · 移速 +20%", health = 0.75f, speed = 1.2f, build = 0.92f, doubleJump = true, unlockAfter = 2, accent = new Color(0.65f, 0.42f, 1), hair = new Color(0.72f, 0.8f, 1) },
            new CharacterDefinition { name = "HAVOC / 爆破专家", role = "双重脉冲 · 弹匣 +30%", magazine = 1.3f, reload = 1.2f, build = 1.08f, unlockAfter = 4, accent = new Color(1, 0.8f, 0.16f), hair = new Color(0.92f, 0.7f, 0.4f) },
            new CharacterDefinition { name = "AURORA / 极光狙击手", role = "冰蓝长发 · 装填快 20% · SR / RG 专精", health = 0.85f, speed = 1.05f, build = 0.96f, magazine = 0.85f, reload = 0.8f, accent = new Color(0.32f, 0.84f, 1), hair = new Color(0.64f, 0.9f, 1), animation = new SideSpriteAnimation(), poweredAnimation = new SideSpriteAnimation() },
            new CharacterDefinition { name = "EMBER / 赤焰爆破兵", role = "火红短发 · 生命 +10% · SG / GL 专精", health = 1.1f, speed = 1.08f, build = 1.02f, magazine = 1.1f, reload = 1.1f, accent = new Color(1, 0.3f, 0.08f), hair = new Color(0.96f, 0.18f, 0.08f), animation = new SideSpriteAnimation(), poweredAnimation = new SideSpriteAnimation() },
            new CharacterDefinition { name = "LUMEN / 星辉医疗兵", role = "治疗支援 · 星环护盾 · 稳定恢复", health = 1.12f, speed = 0.96f, build = 1.04f, accent = new Color(0.28f, 1f, 0.78f), hair = new Color(0.84f, 0.9f, 1f) },
            new CharacterDefinition { name = "NOVA / 霓核突击手", role = "高速突击 · 脉冲震荡 · 高射速", health = 0.94f, speed = 1.12f, build = 0.98f, magazine = 1.15f, reload = 0.92f, accent = new Color(1f, 0.42f, 0.82f), hair = new Color(0.55f, 0.68f, 1f) },
            new CharacterDefinition { name = "KALT / 冷锋游骑兵", role = "冷峻男性 · 站定精确架枪 · 贯穿专精", health = 1.05f, speed = 0.94f, build = 1.08f, isMale = true, passiveDescription = "站定瞄准时伤害 +28% · 后坐力降低", accent = new Color(0.42f, 0.86f, 1f), hair = new Color(0.72f, 0.84f, 0.96f) },
            new CharacterDefinition { name = "VEIL / 影缝猎手", role = "隐身射手 · 空中冲刺 · 标记处决", health = 0.82f, speed = 1.16f, build = 0.91f, doubleJump = true, passiveDescription = "空中闪避额外获得短暂无敌与追踪残影", accent = new Color(0.72f, 0.49f, 1f), hair = new Color(0.42f, 0.35f, 0.62f) },
            new CharacterDefinition { name = "SERA / 晨辉医师", role = "战地医疗 · 击杀回疗 · 群体净化", health = 1.08f, speed = 0.92f, build = 1f, passiveDescription = "击杀回复生命 · 治疗场强化能量获取", accent = new Color(0.37f, 1f, 0.78f), hair = new Color(0.94f, 0.82f, 0.63f) },
            new CharacterDefinition { name = "ROOK / 破阵先锋", role = "重甲冲锋 · 跳雷 · 近战破障", health = 1.24f, speed = 1.02f, build = 1.18f, dodgeExtra = 0.25f, passiveDescription = "闪避变为重装撞阵 · 起跳留下延迟爆雷", accent = new Color(1f, 0.55f, 0.22f), hair = new Color(0.44f, 0.3f, 0.25f) }
        };
    }
}
