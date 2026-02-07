using UnityEngine;

namespace DuckingAround
{
    /// <summary>
    /// Handles background music (looping) and one-shot SFX: breaker pulse hit, session end.
    /// Add to a persistent GameObject (e.g. same as GameManager) and assign clips in the Inspector.
    /// </summary>
    public class MusicManager : MonoBehaviour
    {
        public static MusicManager Instance { get; private set; }

        [Header("Background Music")]
        [Tooltip("Plays on Start and loops. Leave empty to have no BGM.")]
        public AudioClip backgroundMusic;

        [Header("SFX")]
        [Range(0f, 1f)]
        [Tooltip("Volume for all SFX (breaker hit, session end).")]
        public float sfxVolume = 1f;
        [Tooltip("Plays once when the breaker pulse hits at least one duck (only once per pulse even if multiple ducks hit).")]
        public AudioClip breakerHitSfx;
        [Tooltip("Plays once when the session ends (timer reaches zero).")]
        public AudioClip sessionEndSfx;
        [Tooltip("Plays once when the player successfully purchases an upgrade.")]
        public AudioClip upgradePurchaseSfx;
        [Tooltip("Plays once when the player clicks Restart (from summary or upgrades).")]
        public AudioClip restartSfx;

        AudioSource _bgmSource;
        AudioSource _sfxSource;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            var sources = GetComponents<AudioSource>();
            _bgmSource = sources.Length > 0 ? sources[0] : gameObject.AddComponent<AudioSource>();
            _sfxSource = sources.Length > 1 ? sources[1] : gameObject.AddComponent<AudioSource>();
        }

        void Start()
        {
            _bgmSource.loop = true;
            _bgmSource.clip = backgroundMusic;
            if (backgroundMusic != null)
                _bgmSource.Play();
        }

        /// <summary>Call when the breaker pulse hits at least one duck. Plays at most once per pulse.</summary>
        public void PlayBreakerHitSfx()
        {
            if (breakerHitSfx != null && _sfxSource != null)
                _sfxSource.PlayOneShot(breakerHitSfx, sfxVolume);
        }

        /// <summary>Call when the session ends (timer reaches zero).</summary>
        public void PlaySessionEndSfx()
        {
            if (sessionEndSfx != null && _sfxSource != null)
                _sfxSource.PlayOneShot(sessionEndSfx, sfxVolume);
        }

        /// <summary>Call when the player successfully purchases an upgrade.</summary>
        public void PlayUpgradePurchaseSfx()
        {
            if (upgradePurchaseSfx != null && _sfxSource != null)
                _sfxSource.PlayOneShot(upgradePurchaseSfx, sfxVolume);
        }

        /// <summary>Call when the player clicks Restart (from summary or upgrades).</summary>
        public void PlayRestartSfx()
        {
            if (restartSfx != null && _sfxSource != null)
                _sfxSource.PlayOneShot(restartSfx, sfxVolume);
        }
    }
}
