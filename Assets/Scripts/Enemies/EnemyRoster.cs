namespace MoriEchoes
{
    public enum EnemyArchetype
    {
        ScrapChaser, ScrapTurret, ScrapWing, ScrapShield,
        FactoryChaser, FactoryGunner, FactoryBomber, FactoryElite,
        SnowChaser, SnowGunner, SnowWing, SnowElite,
        CityChaser, CityGunner, CityLaser, CityRunner, CityElite,
        CitadelChaser, CitadelGunner, CitadelRunner, CitadelElite,
        TombGravebound, TombSpitter, TombWyrm, TombBehemoth,
        TideDrowned, TideSiren, TideEel, TideLeviathan,
        RiftHusk, RiftLancer, RiftDrifter, RiftColossus,
        SurfReaver, SurfSpitter, SurfSkimmer, SurfLeviathan,
        AbyssLurker, AbyssSiren, AbyssDart, AbyssMaw,
        Boss
    }

    public static class EnemyRoster
    {
        private static readonly string[,] Names =
        {
            { "锯刃拾荒者", "防毒面具枪手", "三爪回收蜂", "破障重装兵" },
            { "液压维修机", "铆钉炮台兵", "涡轮巡检蜂", "熔炉重工机" },
            { "霜爪潜猎者", "晶矛射手", "冰翼蝠", "冰川巨像" },
            { "刃影武士", "光轨狙击手", "棱镜侦察蜂", "量子武士" },
            { "赤刃近卫", "王庭枪骑士", "血瞳巡游者", "处刑重骑士" },
            { "墓土骸兵", "哭墙墓射手", "骨翅巡猎者", "填墓巨骸" },
            { "溺亡守卫", "圣堂潮咏者", "深渊鳗行者", "沉钟海兽" },
            { "虚空残壳", "棱井长矛兵", "逆相漂移体", "星墓重构者" },
            { "浪刃突击手", "飞沫射手", "掠浪飞鱼", "怒涛巨兽" },
            { "深潜猎手", "气泡射手", "暗流游鱼", "深渊巨口" }
        };
        public static string Name(int stage, EnemyKind kind) => Names[UnityEngine.Mathf.Clamp(stage, 0, 9), UnityEngine.Mathf.Clamp((int)kind, 0, 3)];

        public static EnemyKind Kind(EnemyArchetype archetype)
        {
            switch (archetype)
            {
                case EnemyArchetype.ScrapTurret:
                case EnemyArchetype.FactoryGunner:
                case EnemyArchetype.SnowGunner:
                case EnemyArchetype.CityGunner:
                case EnemyArchetype.CityLaser:
                case EnemyArchetype.CitadelGunner: return EnemyKind.Gunner;
                case EnemyArchetype.ScrapWing:
                case EnemyArchetype.FactoryBomber:
                case EnemyArchetype.SnowWing:
                case EnemyArchetype.CityRunner:
                case EnemyArchetype.CitadelRunner: return EnemyKind.Runner;
                case EnemyArchetype.ScrapShield:
                case EnemyArchetype.FactoryElite:
                case EnemyArchetype.SnowElite:
                case EnemyArchetype.CityElite:
                case EnemyArchetype.CitadelElite: return EnemyKind.Elite;
                case EnemyArchetype.TombSpitter:
                case EnemyArchetype.TideSiren:
                case EnemyArchetype.RiftLancer: return EnemyKind.Gunner;
                case EnemyArchetype.TombWyrm:
                case EnemyArchetype.TideEel:
                case EnemyArchetype.RiftDrifter: return EnemyKind.Runner;
                case EnemyArchetype.TombBehemoth:
                case EnemyArchetype.TideLeviathan:
                case EnemyArchetype.RiftColossus:
                case EnemyArchetype.SurfLeviathan:
                case EnemyArchetype.AbyssMaw: return EnemyKind.Elite;
                case EnemyArchetype.SurfSpitter:
                case EnemyArchetype.AbyssSiren: return EnemyKind.Gunner;
                case EnemyArchetype.SurfSkimmer:
                case EnemyArchetype.AbyssDart: return EnemyKind.Runner;
                case EnemyArchetype.Boss: return EnemyKind.Boss;
                default: return EnemyKind.Chaser;
            }
        }

        public static EnemyArchetype DefaultArchetype(int stage, EnemyKind kind)
        {
            if (kind == EnemyKind.Boss) return EnemyArchetype.Boss;
            int column = UnityEngine.Mathf.Clamp((int)kind, 0, 3);
            switch (UnityEngine.Mathf.Clamp(stage, 0, 9))
            {
                case 8: return new[] { EnemyArchetype.SurfReaver, EnemyArchetype.SurfSpitter, EnemyArchetype.SurfSkimmer, EnemyArchetype.SurfLeviathan }[column];
                case 9: return new[] { EnemyArchetype.AbyssLurker, EnemyArchetype.AbyssSiren, EnemyArchetype.AbyssDart, EnemyArchetype.AbyssMaw }[column];
                case 1: return new[] { EnemyArchetype.FactoryChaser, EnemyArchetype.FactoryGunner, EnemyArchetype.FactoryBomber, EnemyArchetype.FactoryElite }[column];
                case 2: return new[] { EnemyArchetype.SnowChaser, EnemyArchetype.SnowGunner, EnemyArchetype.SnowWing, EnemyArchetype.SnowElite }[column];
                case 3: return new[] { EnemyArchetype.CityChaser, EnemyArchetype.CityGunner, EnemyArchetype.CityRunner, EnemyArchetype.CityElite }[column];
                case 4: return new[] { EnemyArchetype.CitadelChaser, EnemyArchetype.CitadelGunner, EnemyArchetype.CitadelRunner, EnemyArchetype.CitadelElite }[column];
                case 5: return new[] { EnemyArchetype.TombGravebound, EnemyArchetype.TombSpitter, EnemyArchetype.TombWyrm, EnemyArchetype.TombBehemoth }[column];
                case 6: return new[] { EnemyArchetype.TideDrowned, EnemyArchetype.TideSiren, EnemyArchetype.TideEel, EnemyArchetype.TideLeviathan }[column];
                case 7: return new[] { EnemyArchetype.RiftHusk, EnemyArchetype.RiftLancer, EnemyArchetype.RiftDrifter, EnemyArchetype.RiftColossus }[column];
                default: return new[] { EnemyArchetype.ScrapChaser, EnemyArchetype.ScrapTurret, EnemyArchetype.ScrapWing, EnemyArchetype.ScrapShield }[column];
            }
        }

        public static bool Stationary(EnemyArchetype archetype)
            => archetype == EnemyArchetype.ScrapTurret || archetype == EnemyArchetype.FactoryGunner
                || archetype == EnemyArchetype.SnowGunner || archetype == EnemyArchetype.CityGunner || archetype == EnemyArchetype.CityLaser;

        public static bool Shielded(EnemyArchetype archetype)
            => archetype == EnemyArchetype.ScrapShield || archetype == EnemyArchetype.CityElite || archetype == EnemyArchetype.CitadelElite;

        public static string Name(EnemyArchetype archetype)
        {
            switch (archetype)
            {
                case EnemyArchetype.ScrapChaser: return "锯刃突击拾荒者";
                case EnemyArchetype.ScrapTurret: return "废土低轨炮塔";
                case EnemyArchetype.ScrapWing: return "三爪楔形回收蜂";
                case EnemyArchetype.ScrapShield: return "破障盾卫";
                case EnemyArchetype.FactoryChaser: return "液压维修机";
                case EnemyArchetype.FactoryGunner: return "固定双管铆钉炮";
                case EnemyArchetype.FactoryBomber: return "涡轮轰炸巡检蜂";
                case EnemyArchetype.FactoryElite: return "熔炉震地重工机";
                case EnemyArchetype.SnowChaser: return "霜爪跃击潜猎者";
                case EnemyArchetype.SnowGunner: return "晶矛蓄力狙击手";
                case EnemyArchetype.SnowWing: return "冰翼俯冲编队";
                case EnemyArchetype.SnowElite: return "冰川震波巨像";
                case EnemyArchetype.CityChaser: return "刃影地冲武士";
                case EnemyArchetype.CityGunner: return "光轨预警狙击手";
                case EnemyArchetype.CityLaser: return "棱镜锁线激光炮";
                case EnemyArchetype.CityRunner: return "棱镜分层巡航蜂";
                case EnemyArchetype.CityElite: return "量子盾卫";
                case EnemyArchetype.CitadelChaser: return "赤刃跃击近卫";
                case EnemyArchetype.CitadelGunner: return "王庭交替轨道枪骑士";
                case EnemyArchetype.CitadelRunner: return "血瞳轰炸编队";
                case EnemyArchetype.CitadelElite: return "处刑冲阵盾骑士";
                case EnemyArchetype.TombGravebound: return "墓土骸兵";
                case EnemyArchetype.TombSpitter: return "哭墙墓射手";
                case EnemyArchetype.TombWyrm: return "骨翅巡猎者";
                case EnemyArchetype.TombBehemoth: return "填墓巨骸";
                case EnemyArchetype.TideDrowned: return "溺亡守卫";
                case EnemyArchetype.TideSiren: return "圣堂潮咏者";
                case EnemyArchetype.TideEel: return "深渊鳗行者";
                case EnemyArchetype.TideLeviathan: return "沉钟海兽";
                case EnemyArchetype.RiftHusk: return "虚空残壳";
                case EnemyArchetype.RiftLancer: return "棱井长矛兵";
                case EnemyArchetype.RiftDrifter: return "逆相漂移体";
                case EnemyArchetype.RiftColossus: return "星墓重构者";
                case EnemyArchetype.SurfReaver: return "浪刃突击手";
                case EnemyArchetype.SurfSpitter: return "飞沫射手";
                case EnemyArchetype.SurfSkimmer: return "掠浪飞鱼";
                case EnemyArchetype.SurfLeviathan: return "怒涛巨兽";
                case EnemyArchetype.AbyssLurker: return "深潜猎手";
                case EnemyArchetype.AbyssSiren: return "气泡射手";
                case EnemyArchetype.AbyssDart: return "暗流游鱼";
                case EnemyArchetype.AbyssMaw: return "深渊巨口";
                default: return "核心守卫";
            }
        }

        public static string Behavior(EnemyArchetype archetype)
        {
            if (Stationary(archetype))
            {
                if (archetype == EnemyArchetype.SnowGunner || archetype == EnemyArchetype.CityGunner) return "蓄力锁线 / 离开射线或利用掩体";
                if (archetype == EnemyArchetype.CityLaser) return "激光锁线 / 蹲伏、跳跃或掩体";
                return "固定低轨炮 / 跳过弹道";
            }
            if (Shielded(archetype)) return "举盾推进 / 跳过冲阵 / 硬直时攻击";
            if (archetype == EnemyArchetype.FactoryChaser) return "维修炮台 / 近身扳手重击";
            if (archetype == EnemyArchetype.FactoryBomber || archetype == EnemyArchetype.CitadelRunner) return "飞行编队轰炸 / 离开落点";
            if (archetype == EnemyArchetype.CityRunner) return "固定高度分层巡航 / 横向扫射";
            if (archetype == EnemyArchetype.ScrapWing || archetype == EnemyArchetype.SnowWing) return "楔形飞行编队 / 锁定俯冲";
            if (archetype == EnemyArchetype.FactoryElite || archetype == EnemyArchetype.SnowElite) return "蓄力震地 / 跳过地面波";
            return archetype == EnemyArchetype.CityChaser ? "预警地冲 / 跳跃或闪避" : "近身蓄力 / 跃击追猎";
        }
    }
}
