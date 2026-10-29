using UnityEngine;

namespace MoriEchoes
{
    public sealed class ExperienceManager : MonoBehaviour
    {
        public ExperienceTrack Track { get; private set; } = new ExperienceTrack();
        public UpgradeOption[] Options { get; private set; }
        private GameManager game;
        private System.Random random;

        public void Initialize(GameManager owner)
        {
            game = owner;
            random = new System.Random(System.Environment.TickCount);
        }

        public void Gain(int amount)
        {
            if (!game.IsPlaying) return;
            int gained = Track.Add(amount);
            if (gained > 0)
            {
                Options = UpgradeCatalog.DrawThree(random);
                game.OpenUpgrade();
                game.Audio.Play(SoundKind.Upgrade);
            }
        }

        public void Choose(int index)
        {
            if (game.State != RunState.Upgrade || Options == null || index < 0 || index >= Options.Length) return;
            UpgradeKind kind = Options[index].Kind;
            if (!Track.ConsumeChoice()) return;
            game.Stats.Apply(kind);
            if (kind == UpgradeKind.Vitality) game.Player.Health.IncreaseMaximum(20, 35);
            if (Track.PendingChoices > 0)
            {
                Options = UpgradeCatalog.DrawThree(random);
                game.Hud.ShowUpgrade();
            }
            else
            {
                Options = null;
                game.ResumeFromUpgrade();
            }
        }
    }
}
