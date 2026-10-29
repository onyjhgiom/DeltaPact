using UnityEngine;

namespace MoriEchoes
{
    public sealed class StageInteractable : MonoBehaviour
    {
        public GameManager Game;
        public GameObject Target;
        public bool Chip;
        public bool Barrel;
        private Health health;
        private bool used;

        public void ConfigureBarrel()
        {
            Barrel = true;
            gameObject.AddComponent<BoxCollider2D>().size = new Vector2(0.7f, 1);
            health = gameObject.AddComponent<Health>();
            health.Initialize(Team.Enemy, 18);
            health.Died += Explode;
        }
        private void Explode()
        {
            if (used) return;
            used = true;
            Game.Pulse(transform.position, 4, 90);
            Game.Effects.Explosion(transform.position, 3, Game.Level.Theme.danger);
            gameObject.SetActive(false);
        }
        private void Update()
        {
            if (used || Game == null || !Game.IsPlaying || Game.Player == null || Barrel) return;
            if (Vector2.Distance(transform.position, Game.Player.transform.position) > (Chip ? 1 : 2)) return;
            if (!Chip && !Game.Input.Interact) return;
            used = true;
            Game.Effects.SkillWave(transform.position, Game.Level.Theme.accent, Chip ? 0.9f : 1.4f);
            if (Chip) { Game.Level.CollectChip(); Game.Audio.Play(SoundKind.Upgrade); }
            if (Target != null)
            {
                SolidWallModule module = Target.GetComponent<SolidWallModule>();
                if (module != null) module.Open(); else Target.SetActive(false);
            }
            gameObject.SetActive(false);
        }
    }
}
