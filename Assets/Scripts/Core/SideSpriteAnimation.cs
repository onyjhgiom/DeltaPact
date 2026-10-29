using System;
using UnityEngine;

namespace MoriEchoes
{
    [Serializable]
    public sealed class SideSpriteAnimation
    {
        public Sprite[] frames = Array.Empty<Sprite>();
        public bool facesRight = true;
        [Range(4, 20)] public float framesPerSecond = 10;
        public bool IsReady => frames != null && frames.Length > 0 && frames[0] != null;

        public Sprite Frame(int index) => IsReady && index >= 0 && index < frames.Length && frames[index] != null ? frames[index] : IsReady ? frames[0] : null;
    }
}
