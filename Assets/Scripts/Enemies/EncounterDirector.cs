using System;
using System.Collections.Generic;
using UnityEngine;

namespace MoriEchoes
{
    public readonly struct EncounterSpawn
    {
        public EnemyArchetype Archetype { get; }
        public EnemyKind Kind => EnemyRoster.Kind(Archetype);
        public Vector2 Position { get; }
        public Vector2 FormationAnchor { get; }
        public int FormationSlot { get; }
        public float AttackDelay { get; }
        public string Formation { get; }

        public EncounterSpawn(EnemyArchetype archetype, Vector2 position, Vector2 anchor, int slot, float delay, string formation)
        {
            Archetype = archetype; Position = position; FormationAnchor = anchor;
            FormationSlot = slot; AttackDelay = delay; Formation = formation;
        }
    }

    public static class EncounterDirector
    {
        public const int MaximumEncounterEnemies = 10;

        public static EncounterSpawn[] Build(GameManager game, LevelManager level, SegmentDefinition segment, int segmentIndex)
        {
            if (game == null || level == null) return Array.Empty<EncounterSpawn>();
            return Build(level.Stage, segment, segmentIndex, game.Difficulty);
        }

        public static EncounterSpawn[] Build(int stage, SegmentDefinition segment, int segmentIndex, int difficulty = 0)
        {
            if (segment == null || segment.boss || segment.width < 8 || float.IsNaN(segment.start)
                || float.IsInfinity(segment.start) || float.IsNaN(segment.width) || float.IsInfinity(segment.width)) return Array.Empty<EncounterSpawn>();
            stage = Mathf.Clamp(stage, 0, 4);
            int variant = Mathf.Abs(segmentIndex % 4);
            var roster = new List<EncounterSpawn>(MaximumEncounterEnemies);
            switch (stage)
            {
                case 0:
                    if (variant == 0)
                    {
                        Add(roster, segment, EnemyArchetype.ScrapChaser, 12); Add(roster, segment, EnemyArchetype.ScrapChaser, 16);
                        Add(roster, segment, EnemyArchetype.ScrapTurret, 23);
                    }
                    else if (variant == 1)
                    {
                        Wing(roster, segment, EnemyArchetype.ScrapWing, 3, false); Add(roster, segment, EnemyArchetype.ScrapTurret, 24);
                    }
                    else if (variant == 2)
                    {
                        Add(roster, segment, EnemyArchetype.ScrapShield, 19); Add(roster, segment, EnemyArchetype.ScrapTurret, 15);
                        Add(roster, segment, EnemyArchetype.ScrapTurret, 24);
                    }
                    else
                    {
                        Add(roster, segment, EnemyArchetype.ScrapChaser, 12); Add(roster, segment, EnemyArchetype.ScrapChaser, 15);
                        Wing(roster, segment, EnemyArchetype.ScrapWing, 2, false); Add(roster, segment, EnemyArchetype.ScrapShield, 24);
                    }
                    break;
                case 1:
                    if (variant == 0 || variant == 1)
                    {
                        Add(roster, segment, EnemyArchetype.FactoryGunner, variant == 0 ? 21 : 16);
                        Add(roster, segment, EnemyArchetype.FactoryChaser, 18);
                        Wing(roster, segment, EnemyArchetype.FactoryBomber, 1, false);
                        if (variant == 1) Add(roster, segment, EnemyArchetype.FactoryGunner, 24);
                    }
                    else
                    {
                        Wing(roster, segment, EnemyArchetype.FactoryBomber, variant == 2 ? 3 : 2, false);
                        Add(roster, segment, EnemyArchetype.FactoryElite, 19);
                        if (variant == 3)
                        {
                            Add(roster, segment, EnemyArchetype.FactoryGunner, 24); Add(roster, segment, EnemyArchetype.FactoryChaser, 21);
                        }
                    }
                    break;
                case 2:
                    if (variant == 0)
                    {
                        Add(roster, segment, EnemyArchetype.SnowGunner, 24); Add(roster, segment, EnemyArchetype.SnowChaser, 15);
                        Add(roster, segment, EnemyArchetype.SnowChaser, 19);
                    }
                    else if (variant == 1) Wing(roster, segment, EnemyArchetype.SnowWing, 4, false);
                    else if (variant == 2)
                    {
                        Add(roster, segment, EnemyArchetype.SnowElite, 19); Add(roster, segment, EnemyArchetype.SnowGunner, 14);
                        Add(roster, segment, EnemyArchetype.SnowGunner, 24);
                    }
                    else
                    {
                        Wing(roster, segment, EnemyArchetype.SnowWing, 2, false); Add(roster, segment, EnemyArchetype.SnowGunner, 24);
                        Add(roster, segment, EnemyArchetype.SnowChaser, 20); Add(roster, segment, EnemyArchetype.SnowElite, 22);
                    }
                    break;
                case 3:
                    if (variant == 0)
                    {
                        Add(roster, segment, EnemyArchetype.CityGunner, 24); Add(roster, segment, EnemyArchetype.CityChaser, 14);
                        Add(roster, segment, EnemyArchetype.CityChaser, 19);
                    }
                    else if (variant == 2)
                    {
                        Add(roster, segment, EnemyArchetype.CityElite, 20); Add(roster, segment, EnemyArchetype.CityGunner, 24);
                        Add(roster, segment, EnemyArchetype.CityLaser, 14);
                    }
                    else
                    {
                        Wing(roster, segment, EnemyArchetype.CityRunner, variant == 1 ? 2 : 3, true);
                        Add(roster, segment, EnemyArchetype.CityLaser, 24);
                        if (variant == 3) Add(roster, segment, EnemyArchetype.CityChaser, 13);
                    }
                    break;
                default:
                    if (variant == 0)
                    {
                        Add(roster, segment, EnemyArchetype.CitadelElite, 19); Add(roster, segment, EnemyArchetype.CitadelGunner, 24);
                        Add(roster, segment, EnemyArchetype.CitadelChaser, 14);
                    }
                    else if (variant == 1)
                    {
                        Wing(roster, segment, EnemyArchetype.CitadelRunner, 3, false); Add(roster, segment, EnemyArchetype.CitadelGunner, 24);
                    }
                    else
                    {
                        Add(roster, segment, EnemyArchetype.CitadelElite, 21); Add(roster, segment, EnemyArchetype.CitadelGunner, 24);
                        Wing(roster, segment, EnemyArchetype.CitadelRunner, variant == 2 ? 1 : 2, false);
                        if (variant == 2) Add(roster, segment, EnemyArchetype.CitadelElite, 16);
                        else
                        {
                            Add(roster, segment, EnemyArchetype.CitadelChaser, 12); Add(roster, segment, EnemyArchetype.CitadelChaser, 15);
                        }
                    }
                    break;
            }
            EnemyArchetype rear = EnemyRoster.DefaultArchetype(stage, EnemyKind.Chaser);
            Add(roster, segment, rear, 2.7f, 1.05f, "后翼伏击", 0);
            EnemyArchetype aerial = EnemyRoster.DefaultArchetype(stage, EnemyKind.Runner);
            Add(roster, segment, aerial, 28.5f, 5.7f, "高空交叉编队", 0);
            int targetCount = Mathf.Min(10, 8 + difficulty + (segmentIndex >= 4 ? 1 : 0));
            EnemyArchetype[] themeRoster = ThemeRoster(stage);
            int formationSlot = roster.Count;
            while (roster.Count < targetCount)
            {
                int slot = formationSlot++;
                EnemyArchetype support = themeRoster[(slot + variant) % themeRoster.Length];
                float offset = new[] { 10.5f, 15f, 20.5f, 24f }[(slot - 2) % 4];
                bool flies = EnemyRoster.Kind(support) == EnemyKind.Runner;
                Add(roster, segment, support, offset, flies ? 4.9f + (slot % 2) * 0.7f : 1.05f,
                    flies ? "上层交叉火力" : slot % 2 == 0 ? "中央推进" : "侧翼压进", slot);
            }
            if (difficulty >= 2)
            {
                EnemyArchetype reinforcement = EnemyRoster.DefaultArchetype(stage, variant % 2 == 0 ? EnemyKind.Chaser : EnemyKind.Runner);
                Add(roster, segment, reinforcement, 22, EnemyRoster.Kind(reinforcement) == EnemyKind.Runner ? 5.9f : 0.9f, "侧翼增援", 3);
            }
            return roster.ToArray();
        }

        private static EnemyArchetype[] ThemeRoster(int stage)
        {
            switch (stage)
            {
                case 1: return new[] { EnemyArchetype.FactoryChaser, EnemyArchetype.FactoryGunner, EnemyArchetype.FactoryBomber, EnemyArchetype.FactoryElite };
                case 2: return new[] { EnemyArchetype.SnowChaser, EnemyArchetype.SnowGunner, EnemyArchetype.SnowWing, EnemyArchetype.SnowElite };
                case 3: return new[] { EnemyArchetype.CityChaser, EnemyArchetype.CityGunner, EnemyArchetype.CityRunner, EnemyArchetype.CityElite };
                case 4: return new[] { EnemyArchetype.CitadelChaser, EnemyArchetype.CitadelGunner, EnemyArchetype.CitadelRunner, EnemyArchetype.CitadelElite };
                default: return new[] { EnemyArchetype.ScrapChaser, EnemyArchetype.ScrapTurret, EnemyArchetype.ScrapWing, EnemyArchetype.ScrapShield };
            }
        }

        public static int SpawnEncounter(GameManager game, LevelManager level, SegmentDefinition segment, int segmentIndex, List<EnemyAI> encounter)
        {
            if (encounter == null) throw new ArgumentNullException(nameof(encounter));
            if (game == null || level == null || game.Config == null || !game.Config.sideScroller || !game.IsPlaying) return 0;
            encounter.RemoveAll(enemy => enemy == null || enemy.Health == null || enemy.Health.IsDead || !enemy.gameObject.activeSelf);
            if (encounter.Count > 0) return 0;
            EncounterSpawn[] roster = Build(game, level, segment, segmentIndex);
            int spawned = 0;
            foreach (EncounterSpawn entry in roster)
            {
                if (game.Enemies.Count >= game.Config.maximumEnemies || spawned >= MaximumEncounterEnemies) break;
                EnemyAI enemy = level.Spawn(entry.Kind, entry.Position);
                if (enemy == null) continue;
                enemy.ConfigureEncounter(entry.Archetype, segment, entry.FormationAnchor, entry.FormationSlot, entry.AttackDelay, entry.Formation);
                encounter.Add(enemy); spawned++;
            }
            return spawned;
        }

        private static void Wing(List<EncounterSpawn> roster, SegmentDefinition segment, EnemyArchetype archetype, int count, bool lanes)
        {
            for (int slot = 0; slot < count; slot++)
            {
                float height = lanes ? slot % 2 == 0 ? 2.8f : 5.4f : 5 + Mathf.Abs(slot - (count - 1) * 0.5f) * 0.65f;
                Add(roster, segment, archetype, 15 + slot * 2.5f, height, lanes ? "分层巡航" : archetype == EnemyArchetype.FactoryBomber
                    || archetype == EnemyArchetype.CitadelRunner ? "梯次轰炸" : "楔形俯冲", slot);
            }
        }

        private static void Add(List<EncounterSpawn> roster, SegmentDefinition segment, EnemyArchetype archetype, float offset,
            float height = 0.9f, string formation = "地面阵线", int slot = 0)
        {
            if (roster.Count >= MaximumEncounterEnemies) return;
            float horizontal = Mathf.Lerp(segment.start + 2, segment.End - 2, Mathf.InverseLerp(2, 26, offset));
            Vector2 anchor = new Vector2(segment.start + segment.width * 0.65f, 5);
            roster.Add(new EncounterSpawn(archetype, new Vector2(horizontal, height), anchor, slot, 0.3f * roster.Count, formation));
        }
    }
}
