using System;
using UnityEngine;

namespace MoriEchoes
{
    [Serializable]
    public sealed class BossTechniqueProfile
    {
        public string name;
        public string release;
        public string visual;
        public string dodge;
        public Sprite[] frames;
    }

    [Serializable]
    public sealed class ThemeProfile
    {
        public string title;
        public string boss;
        public string mechanic;
        public Color sky;
        public Color distance;
        public Color structure;
        public Color accent;
        public Color danger;
        public Sprite background;
        public SideSpriteAnimation bossAnimation;
        public SideSpriteAnimation[] bossForms;
        public Sprite[] bossOrnaments;
        public Sprite[] bossUltimateFrames;
        public BossTechniqueProfile[] bossTechniques;
        public Sprite BossOrnament(int phase, int slot)
        {
            int index = Mathf.Clamp(phase - 1, 0, 2) * 4 + Mathf.Clamp(slot, 0, 3);
            return bossOrnaments != null && index < bossOrnaments.Length ? bossOrnaments[index] : null;
        }
        public SideSpriteAnimation[] enemyAnimations;
        public Sprite[] encounterEffects;
        public Sprite[] architecture;
        public Sprite[] guardianSprites;
        public Sprite[] segmentProps;
        public SideSpriteAnimation[] hazardAnimations;
        public SideSpriteAnimation stationaryAnimation;
        public Sprite Architecture(int index) => architecture != null && index >= 0 && index < architecture.Length ? architecture[index] : null;
        public Sprite SegmentProp(int index) => segmentProps != null && index >= 0 && index < segmentProps.Length ? segmentProps[index] : null;

        public SideSpriteAnimation BossForm(int phase)
        {
            int index = Mathf.Clamp(phase - 1, 0, 2);
            if (bossForms != null && index < bossForms.Length && bossForms[index] != null && bossForms[index].IsReady)
                return bossForms[index];
            return bossAnimation;
        }

        public SideSpriteAnimation EnemyAnimation(EnemyKind kind, SideSpriteAnimation[] fallback)
        {
            int index = (int)kind;
            if (enemyAnimations != null && index < enemyAnimations.Length && enemyAnimations[index] != null && enemyAnimations[index].IsReady)
                return enemyAnimations[index];
            return fallback != null && index < fallback.Length ? fallback[index] : null;
        }

        public static ThemeProfile[] Defaults() => new[]
        {
            Make("城市废墟", "破碎者", "崩塌跳台 / 爆燃油桶", "101D35", "2B405C", "283747", "52D7FA", "FF8B47"),
            Make("工厂蜂巢", "熔核", "传送带 / 液压冲柱 / 电磁门", "0B2025", "17444B", "243C42", "8CF3B6", "FF9C39"),
            Make("寒峰隧道", "霜牙", "冰面惯性 / 碎冰平台 / 暴风雪", "111A38", "384A73", "304B65", "9DE8FF", "BA83FF"),
            Make("未来都市", "零点", "升降平台 / 激光栅栏 / 全息墙", "160F31", "332555", "26334C", "F47FD6", "7CE7FF"),
            Make("最终要塞", "终末回响", "混合机关 / 空降增援 / 心核决战", "200F1E", "492337", "302B3E", "FA6A69", "FFD071"),
            Make("废墟墓窟", "百骸王", "垂直下潜 / 地底骷髅兵 / 击破填充骷髅闸", "1C1424", "493044", "343042", "6DE0D0", "FF695D"),
            Make("沉没圣堂", "潮汐吞噬者", "水压喷流 / 浮泡逆流 / 双侧墓道", "081E31", "1C5365", "244453", "62D6E8", "FF9A61"),
            Make("星坠核心井", "引力残响", "反重力脉冲 / 虚空牵引 / 收缩竖井", "100E2A", "302858", "2E304D", "D28BFF", "FF648F"),
            Make("风暴海面", "狂澜利维坦", "海面游泳 / 浪涌推进 / 跃出水面", "0A2740", "14507A", "1B3A52", "57E6D9", "FFB257"),
            Make("深渊热泉", "深渊母巢", "水下浮沉 / 暗流涌动 / 透光光柱", "030B18", "0E2A44", "12303F", "4FD8FF", "FF6E9E")
        };

        private static ThemeProfile Make(string title, string boss, string mechanic, string sky, string far, string structure, string accent, string danger)
        {
            ColorUtility.TryParseHtmlString("#" + sky, out Color skyColor);
            ColorUtility.TryParseHtmlString("#" + far, out Color farColor);
            ColorUtility.TryParseHtmlString("#" + structure, out Color structureColor);
            ColorUtility.TryParseHtmlString("#" + accent, out Color accentColor);
            ColorUtility.TryParseHtmlString("#" + danger, out Color dangerColor);
            return new ThemeProfile { title = title, boss = boss, mechanic = mechanic, sky = skyColor, distance = farColor, structure = structureColor, accent = accentColor, danger = dangerColor };
        }
    }
}
