using System;
using UnityEngine;

namespace MoriEchoes
{
    [Serializable]
    public sealed class SegmentDefinition
    {
        public float start;
        public float width = 30;
        public int layout;
        public bool boss;
        public float End => start + width;
    }

    public static class CampaignProgress
    {
        public static bool Volatile;
        private static readonly System.Collections.Generic.Dictionary<string, int> memory = new System.Collections.Generic.Dictionary<string, int>();
        public static int Read(string key, int fallback = 0) => Volatile ? memory.TryGetValue(key, out int value) ? value : fallback : PlayerPrefs.GetInt("MoriEchoes.Side." + key, fallback);
        public static void Write(string key, int value)
        {
            if (Volatile) memory[key] = value;
            else { PlayerPrefs.SetInt("MoriEchoes.Side." + key, value); PlayerPrefs.Save(); }
        }
        public static int Unlocked => Mathf.Clamp(Read("Unlocked", 1), 1, 10);
        public static void Unlock(int stage) => Write("Unlocked", Mathf.Max(Unlocked, Mathf.Clamp(stage, 1, 10)));
        public static string Grade(int deaths, float seconds, int kills)
        {
            if (deaths == 0 && seconds < 180 && kills >= 8) return "S";
            if (deaths <= 1 && seconds < 300) return "A";
            return deaths <= 3 ? "B" : "C";
        }
        public static string Grade(int deaths, float seconds, int kills, int styleScore)
        {
            if (deaths == 0 && seconds < 360 && kills >= 8 && styleScore >= 1800) return "S";
            if (deaths <= 1 && seconds < 480 && styleScore >= 500) return "A";
            return Grade(deaths, seconds, kills);
        }
        public static void ClearVolatile() { memory.Clear(); }
    }
}
