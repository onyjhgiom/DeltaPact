using UnityEngine;

namespace MoriEchoes
{
    public sealed class WaveManager : MonoBehaviour
    {
        public int Wave { get; private set; }
        public float Remaining { get; private set; }
        public bool BossSpawned { get; private set; }
        public bool Intermission { get; private set; }
        public EnemyAI Boss { get; private set; }
        private GameManager game;
        private float spawnAt;
        private int spawnedThisWave;

        public void Initialize(GameManager owner)
        {
            game = owner;
            Intermission = true;
            Remaining = 2;
        }

        private void Update()
        {
            if (!game.IsPlaying || BossSpawned) return;
            Remaining -= Time.deltaTime;
            if (Intermission)
            {
                if (Remaining <= 0) BeginWave();
                return;
            }
            if (Remaining <= 0)
            {
                Intermission = true;
                Remaining = game.Config.intermissionSeconds;
                game.Pickups.Drop(game.Player.transform.position, 5 + Wave);
                game.Player.Health.Restore(8);
                game.Hud.Announce("区域压制完成 // 下一波即将到来", 2.8f);
                return;
            }
            if (Time.time < spawnAt || game.Enemies.Count >= game.Config.maximumEnemies) return;
            spawnAt = Time.time + WaveRules.SpawnInterval(Wave);
            Spawn(WaveRules.SelectEnemy(Wave, spawnedThisWave++));
        }

        private void BeginWave()
        {
            Wave++;
            Intermission = false;
            if (Wave > game.Config.wavesBeforeBoss)
            {
                BossSpawned = true;
                Boss = Spawn(EnemyKind.Boss);
                game.Hud.Announce("警报 // 歼灭核心已进入战区", 3.5f);
                return;
            }
            Remaining = game.Config.waveSeconds;
            spawnedThisWave = 0;
            spawnAt = Time.time + 0.8f;
            game.Hud.Announce("WAVE " + Wave.ToString("00") + " // 清除敌对信号", 2.5f);
        }

        private EnemyAI Spawn(EnemyKind kind)
        {
            Vector2 half = game.Config.arenaHalfSize - Vector2.one * 1.5f;
            Vector2 player = game.Player.transform.position;
            Vector2 point = Vector2.zero;
            float bestDistance = -1;
            Vector2 farthest = Vector2.zero;
            for (int attempt = 0; attempt < 20; attempt++)
            {
                int edge = Random.Range(0, 4);
                point = edge < 2 ? new Vector2(edge == 0 ? -half.x : half.x, Random.Range(-half.y, half.y))
                    : new Vector2(Random.Range(-half.x, half.x), edge == 2 ? -half.y : half.y);
                float distance = (point - player).sqrMagnitude;
                if (distance > bestDistance) { bestDistance = distance; farthest = point; }
                if (distance >= 64) break;
            }
            if ((point - player).sqrMagnitude < 64) point = farthest;
            var entity = new GameObject(kind.ToString());
            entity.transform.SetParent(transform, false);
            entity.transform.position = point;
            var enemy = entity.AddComponent<EnemyAI>();
            enemy.Initialize(game, kind, Wave);
            return enemy;
        }
    }
}
