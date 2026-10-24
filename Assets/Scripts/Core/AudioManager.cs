using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ZiplaKac.Core
{
    /// <summary>
    /// Oyun içi ses efektlerini (SFX), arka plan müziğini (BGM) ve haptik titreşimi yöneten merkezi sınıf.
    /// Harici ses dosyası olmasa dahi, saf matematik ve sinyal sentezi (procedural audio) ile
    /// eğlenceli atari melodileri ve ses efektleri üretir.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Harici Ses Klipleri (Opsiyonel)")]
        public AudioClip jumpClip;
        public AudioClip doubleJumpClip;
        public AudioClip powerUpClip;
        public AudioClip shieldHitClip;
        public AudioClip comboClip;
        public AudioClip gameOverClip;
        public AudioClip newRecordClip;
        public AudioClip buttonClickClip;
        public AudioClip coinClip;
        public AudioClip bgmClip;

        private AudioSource sfxSource;
        private AudioSource bgmSource;
        private const string PREF_SOUND_MUTED = "hoppzy_sound_muted";
        public event Action<bool> OnMuteStateChanged;

        public bool IsMuted
        {
            get => PlayerPrefs.GetInt(PREF_SOUND_MUTED, 0) == 1;
            private set
            {
                PlayerPrefs.SetInt(PREF_SOUND_MUTED, value ? 1 : 0);
                PlayerPrefs.Save();
                if (sfxSource != null) sfxSource.mute = value;
                if (bgmSource != null) bgmSource.mute = value;
                OnMuteStateChanged?.Invoke(value);
            }
        }

        public bool ToggleMute()
        {
            IsMuted = !IsMuted;
            return IsMuted;
        }

        // Prosedürel sentezlenmiş ses klipleri
        private AudioClip synthJump;
        private AudioClip synthDoubleJump;
        private AudioClip synthPowerUp;
        private AudioClip synthShieldHit;
        private AudioClip synthCombo;
        private AudioClip synthGameOver;
        private AudioClip synthNewRecord;
        private AudioClip synthButtonClick;
        private AudioClip synthCoin;
        private AudioClip synthBGM;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            sfxSource.mute = IsMuted;

            bgmSource = gameObject.AddComponent<AudioSource>();
            bgmSource.playOnAwake = false;
            bgmSource.loop = true;
            bgmSource.volume = 0.22f;
            bgmSource.mute = IsMuted;

            GenerateProceduralSounds();
            StartBGM();
        }

        public void PlayJumpSound() => PlayClipOrSynth(jumpClip, synthJump, 0.5f);
        public void PlayDoubleJumpSound() => PlayClipOrSynth(doubleJumpClip, synthDoubleJump, 0.6f);
        public void PlayPowerUpSound() => PlayClipOrSynth(powerUpClip, synthPowerUp, 0.7f);
        public void PlayShieldHitSound() => PlayClipOrSynth(shieldHitClip, synthShieldHit, 0.8f);
        public void PlayComboSound() => PlayClipOrSynth(comboClip, synthCombo, 0.7f);
        public void PlayGameOverSound() => PlayClipOrSynth(gameOverClip, synthGameOver, 0.8f);
        public void PlayNewRecordSound() => PlayClipOrSynth(newRecordClip, synthNewRecord, 0.9f);
        public void PlayButtonClickSound() => PlayClipOrSynth(buttonClickClip, synthButtonClick, 0.45f);
        public void PlayCoinSound() => PlayClipOrSynth(coinClip, synthCoin, 0.55f);

        public void TriggerHapticFeedback()
        {
#if UNITY_ANDROID || UNITY_IOS
            try
            {
                Handheld.Vibrate();
            }
            catch
            {
                // Desteklenmeyen ortamlarda sessizce devam et
            }
#endif
        }

        private void StartBGM()
        {
            if (bgmSource == null) return;
            AudioClip clip = bgmClip != null ? bgmClip : synthBGM;
            if (clip != null)
            {
                bgmSource.clip = clip;
                if (!bgmSource.isPlaying)
                {
                    bgmSource.Play();
                }
            }
        }

        private void PlayClipOrSynth(AudioClip customClip, AudioClip synthClip, float volume)
        {
            if (IsMuted) return;
            AudioClip target = customClip != null ? customClip : synthClip;
            if (target != null && sfxSource != null)
            {
                sfxSource.PlayOneShot(target, volume);
            }
        }

        #region Prosedürel Sentezleyici (Procedural Audio Synth)
        private void GenerateProceduralSounds()
        {
            synthJump = CreateToneClip(440f, 660f, 0.12f, EnvelopeType.ExpDecay);
            synthDoubleJump = CreateToneClip(660f, 880f, 0.10f, EnvelopeType.ExpDecay);
            synthPowerUp = CreateArpeggioClip(new float[] { 523.25f, 659.25f, 783.99f, 1046.50f }, 0.20f);
            synthShieldHit = CreateToneClip(300f, 150f, 0.25f, EnvelopeType.LinearDecay);
            synthCombo = CreateArpeggioClip(new float[] { 440f, 554.37f, 659.25f }, 0.15f);
            synthGameOver = CreateToneClip(220f, 80f, 0.45f, EnvelopeType.LinearDecay);
            synthNewRecord = CreateArpeggioClip(new float[] { 523.25f, 659.25f, 783.99f, 987.77f, 1046.50f }, 0.35f);

            // Yumuşak ahşap/baloncuk buton tıklama sesi
            synthButtonClick = CreateToneClip(750f, 1100f, 0.04f, EnvelopeType.ExpDecay);

            // Parlak iki tonlu altın/para sesi
            synthCoin = CreateArpeggioClip(new float[] { 987.77f, 1318.51f }, 0.10f);

            // 8 saniyelik neşeli, tatlı fon müziği döngüsü (C - G - Am - F arpej)
            synthBGM = CreateProceduralBGM();
        }

        private enum EnvelopeType { LinearDecay, ExpDecay }

        private AudioClip CreateToneClip(float startFreq, float endFreq, float duration, EnvelopeType envelope)
        {
            int sampleRate = 44100;
            int sampleCount = (int)(sampleRate * duration);
            float[] samples = new float[sampleCount];

            float phase = 0f;
            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleCount;
                float currentFreq = Mathf.Lerp(startFreq, endFreq, t);
                phase += 2f * Mathf.PI * currentFreq / sampleRate;

                float env = envelope == EnvelopeType.ExpDecay ? Mathf.Exp(-5f * t) : (1f - t);
                samples[i] = Mathf.Sin(phase) * env;
            }

            AudioClip clip = AudioClip.Create("SynthTone", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip CreateArpeggioClip(float[] freqs, float totalDuration)
        {
            int sampleRate = 44100;
            int sampleCount = (int)(sampleRate * totalDuration);
            float[] samples = new float[sampleCount];

            int noteCount = freqs.Length;
            int samplesPerNote = sampleCount / noteCount;

            for (int note = 0; note < noteCount; note++)
            {
                float freq = freqs[note];
                float phase = 0f;

                for (int i = 0; i < samplesPerNote; i++)
                {
                    int globalIndex = note * samplesPerNote + i;
                    if (globalIndex >= sampleCount) break;

                    float noteT = (float)i / samplesPerNote;
                    phase += 2f * Mathf.PI * freq / sampleRate;
                    float env = 1f - noteT;

                    samples[globalIndex] = Mathf.Sin(phase) * env;
                }
            }

            AudioClip clip = AudioClip.Create("SynthArp", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip CreateProceduralBGM()
        {
            int sampleRate = 22050; // Hafif ve akıcı
            float duration = 8.0f;  // 8 saniyelik dikişsiz döngü
            int sampleCount = (int)(sampleRate * duration);
            float[] samples = new float[sampleCount];

            // Akor dizilimi: C (261.63), G (196.00), Am (220.00), F (174.61)
            float[][] chords = new float[][]
            {
                new float[] { 261.63f, 329.63f, 392.00f, 523.25f }, // C
                new float[] { 196.00f, 246.94f, 293.66f, 392.00f }, // G
                new float[] { 220.00f, 261.63f, 329.63f, 440.00f }, // Am
                new float[] { 174.61f, 220.00f, 261.63f, 349.23f }  // F
            };

            float chordDuration = duration / chords.Length; // 2 saniye per chord
            int samplesPerChord = sampleCount / chords.Length;

            for (int c = 0; c < chords.Length; c++)
            {
                float[] currentChord = chords[c];
                int noteSteps = 8; // Her akorda 8 hızlı arpej notası
                int samplesPerNote = samplesPerChord / noteSteps;

                for (int step = 0; step < noteSteps; step++)
                {
                    float noteFreq = currentChord[step % currentChord.Length];
                    float bassFreq = currentChord[0] * 0.5f;

                    int startIdx = c * samplesPerChord + step * samplesPerNote;
                    float phase = 0f;
                    float bassPhase = 0f;

                    for (int i = 0; i < samplesPerNote; i++)
                    {
                        int idx = startIdx + i;
                        if (idx >= sampleCount) break;

                        float tNote = (float)i / samplesPerNote;
                        phase += 2f * Mathf.PI * noteFreq / sampleRate;
                        bassPhase += 2f * Mathf.PI * bassFreq / sampleRate;

                        // Yumuşak çan/arp tınısı
                        float melodyEnv = Mathf.Exp(-4f * tNote);
                        float bassEnv = Mathf.Exp(-2.5f * tNote);

                        float melodySample = Mathf.Sin(phase) * melodyEnv * 0.4f;
                        float bassSample = Mathf.Sin(bassPhase) * bassEnv * 0.3f;

                        samples[idx] = (melodySample + bassSample) * 0.35f;
                    }
                }
            }

            AudioClip bgm = AudioClip.Create("SynthBGM", sampleCount, 1, sampleRate, false);
            bgm.SetData(samples, 0);
            return bgm;
        }
        #endregion
    }
}
