#if ENABLE_INPUT_SYSTEM
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace MoriEchoes.Diagnostics
{
    public sealed class VerticalCampaignSmokeTest : MonoBehaviour
    {
        [Serializable]
        private sealed class Report
        {
            public string unity;
            public bool passed;
            public List<string> checks = new List<string>();
            public List<string> errors = new List<string>();
        }

        private readonly Report report = new Report();
        private GameManager game;
        private string output;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Launch()
        {
            string[] arguments = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(arguments, "-moriVerticalSmoke");
            if (index < 0) return;
            var runner = new GameObject("VerticalCampaignSmokeTest").AddComponent<VerticalCampaignSmokeTest>();
            runner.output = index + 1 < arguments.Length ? Path.GetFullPath(arguments[index + 1])
                : Path.Combine(Application.persistentDataPath, "VerticalCampaignSmoke");
            Directory.CreateDirectory(runner.output);
            runner.StartCoroutine(runner.Run());
        }

        private IEnumerator Run()
        {
            yield return DiagnosticWindow.Prepare();
            yield return null;
            yield return null;
            report.unity = Application.unityVersion;
            CampaignProgress.Volatile = true;
            CampaignProgress.ClearVolatile();
            game = FindAnyObjectByType<GameManager>();
            Check(game != null && game.Config.Validate(out _), "Boot has a valid eight-stage side-scroller config");
            Check(game.Config.characters.Length == 8 && game.Config.weapons.Length == 9, "Approved eight-character roster and all nine weapons are configured");
            Check(!game.Effects.AttackTelegraphsEnabled, "Mob debug overlays stay disabled independently of boss warnings");

            for (int stage = 5; stage < 8 && report.errors.Count == 0; stage++)
                yield return ProbeStage(stage);

            report.passed = report.errors.Count == 0;
            File.WriteAllText(Path.Combine(output, "vertical-campaign-smoke.json"), JsonUtility.ToJson(report, true));
            Debug.Log("VERTICAL_CAMPAIGN_SMOKE " + (report.passed ? "PASS" : "FAIL") + " checks=" + report.checks.Count
                + " errors=" + report.errors.Count + " report=" + output);
            Application.Quit(report.passed ? 0 : 1);
        }

        private IEnumerator ProbeStage(int stage)
        {
            game.ReturnToMenu();
            game.SelectCharacter(0);
            Check(game.SelectStage(stage), "Stage " + (stage + 1) + " can be selected");
            game.BeginRun();
            yield return new WaitForSecondsRealtime(0.25f);
            LevelManager level = game.Level;
            VerticalStageDirector vertical = level != null ? level.Vertical : null;
            Check(vertical != null && level.IsVerticalStage, "Stage " + (stage + 1) + " builds its vertical director");
            if (vertical == null) yield break;
            Check(level.Segments.Length == 8 && Mathf.Approximately(vertical.DepthLength, 112), "Eight 14-unit rooms form a 112-unit upward route");
            Check(vertical.GateCount == 7 && vertical.LiveHazards == 14, "Each route has seven breakable gates and fourteen timed hazards");
            Check(level.GetComponent<SceneFeedback>() != null && game.Environment != null, "Vertical route initializes reactive atmosphere feedback");
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(output, "stage_" + (stage + 1) + "_shaft.png"));

            VerticalSkullGate[] gates = level.GetComponentsInChildren<VerticalSkullGate>(true);
            Check(gates.Length == 7, "All seven themed gates are instantiated");
            int skullMotifs = 0;
            foreach (SpriteRenderer renderer in gates[0].GetComponentsInChildren<SpriteRenderer>(true))
                if (renderer.sprite == level.Theme.Architecture(1)) skullMotifs++;
            Check(skullMotifs > 0, "Generated stage-specific illustrated gate replaces procedural barrier motifs");
            float gateHealth = gates[0].Health.Maximum;
            gates[0].Health.TakeDamage(gateHealth, Team.Player);
            Check(gates[0].Broken && !gates[0].GetComponent<BoxCollider2D>().enabled, "Gate damage opens the passage and emits break feedback");

            game.Player.Health.Invulnerable = true;
            Vector2 cameraStart = game.CameraRig.Camera.transform.position;
            game.Player.Motor.ResetAt(new Vector2(0, vertical.StartPoint.y + 30));
            game.Player.Motor.SetCinematicLocked(true);   // 冻结以验证向上推进（向下版依赖重力下坠，方向镜像）
            Rigidbody2D playerBody = game.Player.GetComponent<Rigidbody2D>();
            float lockedHeight = playerBody.position.y;
            yield return new WaitForSeconds(2.2f);
            Check(Mathf.Abs(playerBody.position.y - lockedHeight) < 0.05f,
                "Cinematic lock holds player height without accumulating gravity drift (" + lockedHeight + " -> " + playerBody.position.y + ")");
            Check(vertical.ActiveSection >= 1 && vertical.SpawnedEnemies > 0, "Entering a higher shaft room starts its enemy emergence cadence");
            EnemyAI groundEnemy = null;
            foreach (EnemyAI enemy in game.Enemies)
                if (enemy != null && enemy.GetComponent<VerticalEnemyController>() != null) { groundEnemy = enemy; break; }
            Check(groundEnemy != null && groundEnemy.GetComponent<VerticalEnemyController>().EmergencePoint.y > game.Player.transform.position.y,
                "Sky-borne enemy descends from above the player");
            if (groundEnemy != null)
            {
                VerticalEnemyController controller = groundEnemy.GetComponent<VerticalEnemyController>();
                yield return new WaitForSeconds(1.4f);
                Check(controller.AttacksFired > 0, "Emerging enemy executes its downward attack");
            }
            Check(game.CameraRig.Camera.transform.position.y > cameraStart.y + 2,
                "Soft-follow camera tracks upward traversal without a vertical clamp");

            Vector2 arena = new Vector2(0, vertical.DepthLength - 7.2f);
            game.Player.Motor.ResetAt(new Vector2(0, arena.y - 3.4f));
            EnemyAI bossEnemy = level.Spawn(EnemyKind.Boss, arena);
            level.SetBoss(bossEnemy);
            VerticalBossController boss = bossEnemy.GetComponent<BossController>().Vertical;
            game.Win();
            Check(game.State == RunState.Playing, "Stage " + (stage + 1) + " cannot clear while its boss still has health");
            Check(boss != null && boss.StageIndex == stage - 5, "Stage " + (stage + 1) + " creates its dedicated boss controller");
            Check(boss.AttackId.Length > 0 && !string.IsNullOrEmpty(boss.FormName), "Boss exposes a named attack cycle and form");
            Check(boss.UsesIllustratedParts && boss.IllustratedPartCount == 4, "Vertical boss uses physical illustrated form equipment");
            bossEnemy.Health.TakeDamage(100000, Team.Player);
            Check(Mathf.Abs(bossEnemy.Health.LastDamageTaken / bossEnemy.Health.Maximum - 0.008f) < 0.0001f,
                "Vertical boss armour caps an enormous hit at 0.8 percent health");
            bossEnemy.Health.SetCurrent(bossEnemy.Health.Maximum);
            Check(bossEnemy.transform.Find("AnimeAnimatedSprite").GetComponent<SpriteRenderer>().sprite
                == level.Theme.BossForm(1).Frame(0), "Vertical boss actually displays its generated first form");
            yield return new WaitForSeconds(1.2f);
            SpriteRenderer bossBody = bossEnemy.transform.Find("AnimeAnimatedSprite").GetComponent<SpriteRenderer>();
            float top = game.CameraRig.Camera.WorldToViewportPoint(bossBody.bounds.max).y;
            float feet = game.CameraRig.Camera.WorldToViewportPoint(game.Player.transform.position + Vector3.down * 0.8f).y;
            Check(top < 0.91f && feet > 0.08f, "Vertical boss body and player feet stay inside the tactical camera");
            bool illustratedScenery = false, translucentScenery = true;
            foreach (SpriteRenderer renderer in level.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (renderer.name != "Shaft_PhysicalWallCrest") continue;
                illustratedScenery = true;
                translucentScenery &= renderer.sprite != game.Art.Ring && renderer.color.a < 0.5f;
            }
            Check(illustratedScenery && translucentScenery,
                "Physical shaft crests retain their authored transparency during environment feedback");
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(output, "stage_" + (stage + 1) + "_boss_form_1.png"));
            bossEnemy.Health.TakeDamage(bossEnemy.Health.Maximum * 0.36f, Team.Player, true);
            yield return new WaitForSeconds(2.8f);
            Check(boss.Phase == 2, "Boss crosses its first health threshold and changes form");
            Check(Array.IndexOf(level.Theme.BossForm(2).frames,
                bossEnemy.transform.Find("AnimeAnimatedSprite").GetComponent<SpriteRenderer>().sprite) >= 0,
                "Vertical boss displays its generated second-form frame");
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(output, "stage_" + (stage + 1) + "_boss_form_2.png"));
            bossEnemy.Health.TakeDamage(bossEnemy.Health.Maximum * 0.35f, Team.Player, true);
            yield return new WaitForSeconds(2.8f);
            Check(boss.Phase == 3, "Boss crosses its second health threshold and changes form");
            Check(Array.IndexOf(level.Theme.BossForm(3).frames,
                bossEnemy.transform.Find("AnimeAnimatedSprite").GetComponent<SpriteRenderer>().sprite) >= 0,
                "Vertical boss displays its generated third-form frame");
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(output, "stage_" + (stage + 1) + "_boss_form_3.png"));
            int ultimates = game.Effects.BossUltimateEmissions;
            int impactEffects = game.Effects.BossSkillImpactEmissions;
            float ultimateDeadline = Time.time + 16;
            bool trackingStrikeChecked = false;
            while (game.Effects.BossUltimateEmissions == ultimates && Time.time < ultimateDeadline)
            {
                if (!trackingStrikeChecked && boss.Preparing && boss.TrackingStrike != null)
                {
                    trackingStrikeChecked = true;
                    Check(boss.TrackingStrike.IsWarning && boss.TrackingStrike.Contains(game.Player.transform.position),
                        "Stage " + (stage + 1) + " shows a tracked Boss strike on a stationary player");
                    game.Player.Motor.ResetAt((Vector2)game.Player.transform.position + Vector2.up * 0.8f);
                    yield return new WaitForSeconds(0.08f);
                    Check(boss.TrackingStrike != null && boss.TrackingStrike.IsWarning
                        && boss.TrackingStrike.Contains(game.Player.transform.position),
                        "Stage " + (stage + 1) + " retargets the strike after vertical movement");
                }
                yield return null;
            }
            Check(game.Effects.BossUltimateEmissions > ultimates && boss.UltimateAttack,
                "Final form naturally fires its unique large ultimate effect");
            Check(game.Effects.BossSkillImpactEmissions > impactEffects,
                "Vertical boss attack uses layered stage-specific impact effects");
            Check(boss.LastProjectileDamage >= 10 * game.DamageScale * game.Config.bossUltimateDamageMultiplier
                && boss.LastProjectileRadius >= 1.7f, "Ultimate projectile applies increased damage and broad blast coverage");
            yield return new WaitForSeconds(0.18f);
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(output, "stage_" + (stage + 1) + "_ultimate.png"));
            bossEnemy.Health.TakeDamage(bossEnemy.Health.Current - bossEnemy.Health.Maximum * 0.13f, Team.Player, true);
            yield return new WaitForSecondsRealtime(2.6f);
            Check(game.State == RunState.Won, "Boss execution and death presentation complete the stage");
            Check(game.Effects.BossWarningEmissions > 0 && game.Effects.WarningEmissions == game.Effects.BossWarningEmissions, "Stage " + (stage + 1) + " emits only intentional red boss warnings");
        }

        private void Check(bool condition, string description)
        {
            if (condition) report.checks.Add(description);
            else report.errors.Add(description);
        }
    }
}
#endif
