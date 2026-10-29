using System.Collections.Generic;
using UnityEngine;

namespace MoriEchoes
{
    public enum SoundKind { Shot, Shotgun, Sniper, Reload, EnemyDown, PlayerHit, Dodge, Upgrade, Blast }

    public sealed class GameAudio : MonoBehaviour
    {
        private readonly Dictionary<SoundKind, AudioClip> clips = new Dictionary<SoundKind, AudioClip>();
        private AudioSource[] voices;
        private AudioSource ambience;
        private float volume;

        public void Initialize(float initialVolume)
        {
            volume = Mathf.Clamp01(PlayerPrefs.GetFloat("MoriEchoes.Volume", initialVolume));
            voices = new AudioSource[12];
            for (int index = 0; index < voices.Length; index++)
            {
                voices[index] = gameObject.AddComponent<AudioSource>();
                voices[index].playOnAwake = false;
                voices[index].spatialBlend = 0;
            }
            foreach (SoundKind kind in System.Enum.GetValues(typeof(SoundKind))) clips.Add(kind, MakeClip(kind));
            ambience = gameObject.AddComponent<AudioSource>();
            ambience.loop = true; ambience.playOnAwake = false; ambience.spatialBlend = 0;
            ambience.clip = MakeAmbience();
            ambience.volume = volume * 0.1f;
            ambience.Play();
        }

        public float Volume => volume;
        public void SetIntensity(int phase)
        {
            if (ambience == null) return;
            ambience.pitch = 1 + (Mathf.Clamp(phase, 1, 3) - 1) * 0.08f;
            ambience.volume = volume * (0.1f + phase * 0.025f);
        }

        public void SetVolume(float value)
        {
            volume = Mathf.Clamp01(value);
            if (ambience != null) ambience.volume = volume * 0.1f;
            foreach (AudioSource voice in voices) voice.volume = volume * 0.23f;
            PlayerPrefs.SetFloat("MoriEchoes.Volume", volume);
        }

        public void Play(SoundKind kind)
        {
            foreach (AudioSource voice in voices)
            {
                if (voice.isPlaying) continue;
                voice.clip = clips[kind];
                voice.pitch = Random.Range(0.94f, 1.06f);
                voice.volume = volume * (kind == SoundKind.PlayerHit ? 0.35f : 0.23f);
                voice.Play();
                return;
            }
        }

        public void SetPaused(bool paused)
        {
            foreach (AudioSource voice in voices) { if (paused) voice.Pause(); else voice.UnPause(); }
        }

        private static AudioClip MakeClip(SoundKind kind)
        {
            float length = kind == SoundKind.Upgrade ? 0.55f : kind == SoundKind.Blast ? 0.5f : 0.18f;
            const int sampleRate = 22050;
            var samples = new float[Mathf.CeilToInt(sampleRate * length)];
            var random = new System.Random(700 + (int)kind);
            float frequency = kind == SoundKind.Sniper ? 90 : kind == SoundKind.Reload ? 900
                : kind == SoundKind.Upgrade ? 660 : kind == SoundKind.Dodge ? 420 : 140;
            for (int index = 0; index < samples.Length; index++)
            {
                float time = (float)index / sampleRate;
                float progress = time / length;
                float envelope = Mathf.Min(1, time * 700) * Mathf.Pow(1 - progress, 2.5f);
                float noise = (float)random.NextDouble() * 2 - 1;
                float tone = Mathf.Sin(2 * Mathf.PI * frequency * time * (1 - progress * 0.35f));
                if (kind == SoundKind.Upgrade)
                    tone = Mathf.Sin(2 * Mathf.PI * (progress < 0.33f ? 660 : progress < 0.66f ? 880 : 1100) * time);
                float noiseMix = kind == SoundKind.Upgrade ? 0.03f : kind == SoundKind.Dodge ? 0.8f : 0.6f;
                samples[index] = Mathf.Lerp(tone, noise, noiseMix) * envelope * 0.6f;
            }
            var clip = AudioClip.Create("Procedural_" + kind, samples.Length, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip MakeAmbience()
        {
            const int sampleRate = 22050;
            var samples = new float[sampleRate * 8];
            for (int index = 0; index < samples.Length; index++)
            {
                float time = (float)index / sampleRate;
                samples[index] = (Mathf.Sin(2 * Mathf.PI * 55 * time) * 0.3f + Mathf.Sin(2 * Mathf.PI * 82.5f * time) * 0.15f
                    + Mathf.Sin(2 * Mathf.PI * 110 * time) * 0.1f) * (0.65f + 0.1f * Mathf.Sin(time * Mathf.PI / 4));
            }
            var clip = AudioClip.Create("Procedural_Ambience", samples.Length, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private void OnDestroy()
        {
            foreach (AudioClip clip in clips.Values) if (clip != null) Destroy(clip);
            if (ambience != null && ambience.clip != null) Destroy(ambience.clip);
        }
    }
}
