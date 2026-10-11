using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ZiplaKac.Core
{
    /// <summary>
    /// Oyun içi ses efektlerini ve mobil haptik titreşimi yöneten sınıf.
    /// Harici .wav/.mp3 dosyaları atanmamışsa, prosedürel olarak sentetik ses sinyalleri (AudioClip) oluşturarak oynatır!
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

        private AudioSource audioSource;

        // Prosedürel sentezlenmiş önbellek klipleri
        private AudioClip synthJump;
        private AudioClip synthDoubleJump;
        private AudioClip synthPowerUp;
        private AudioClip synthShieldHit;
        private AudioClip synthCombo;
        private AudioClip synthGameOver;
        private AudioClip synthNewRecord;

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

            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;

            GenerateProceduralSounds();
        }

        public void PlayJumpSound()
        {
            PlayClipOrSynth(jumpClip, synthJump, 0.5f);
        }

        public void PlayDoubleJumpSound()
        {
            PlayClipOrSynth(doubleJumpClip, synthDoubleJump, 0.6f);
        }

        public void PlayPowerUpSound()
        {
            PlayClipOrSynth(powerUpClip, synthPowerUp, 0.7f);
        }

        public void PlayShieldHitSound()
        {
            PlayClipOrSynth(shieldHitClip, synthShieldHit, 0.8f);
        }

        public void PlayComboSound()
        {
            PlayClipOrSynth(comboClip, synthCombo, 0.7f);
        }

        public void PlayGameOverSound()
        {
            PlayClipOrSynth(gameOverClip, synthGameOver, 0.8f);
        }

        public void PlayNewRecordSound()
        {
            PlayClipOrSynth(newRecordClip, synthNewRecord, 0.9f);
        }

        public void TriggerHapticFeedback()
        {
#if UNITY_ANDROID || UNITY_IOS
            try
            {
                Handheld.Vibrate();
            }
            catch
            {
                // Editor'da veya desteklenmeyen cihazda sessizce devam et
            }
#endif
        }

        private void PlayClipOrSynth(AudioClip customClip, AudioClip synthClip, float volume)
        {
            AudioClip target = customClip != null ? customClip : synthClip;
            if (target != null && audioSource != null)
            {
                audioSource.PlayOneShot(target, volume);
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
        #endregion
    }
}
