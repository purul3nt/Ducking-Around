using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DuckingAround
{
    /// <summary>
    /// Use in the intro scene: wire the New Game button to OnNewGameClicked.
    /// Fades out (optional overlay) then loads the main game scene.
    /// </summary>
    public class IntroController : MonoBehaviour
    {
        [Tooltip("Scene name to load (must be in Build Settings).")]
        public string mainGameSceneName = "SampleScene";

        [Header("Fade")]
        [Tooltip("Optional: full-screen image (e.g. black) that fades from transparent to opaque before loading.")]
        public Image fadeOverlay;
        [Tooltip("Fade duration in seconds.")]
        public float fadeDuration = 0.5f;

        public void OnNewGameClicked()
        {
            StartCoroutine(FadeOutAndLoad());
        }

        IEnumerator FadeOutAndLoad()
        {
            if (fadeOverlay != null && fadeDuration > 0f)
            {
                fadeOverlay.gameObject.SetActive(true);
                Color c = fadeOverlay.color;
                c.a = 0f;
                fadeOverlay.color = c;

                float elapsed = 0f;
                while (elapsed < fadeDuration)
                {
                    elapsed += Time.deltaTime;
                    c.a = Mathf.Clamp01(elapsed / fadeDuration);
                    fadeOverlay.color = c;
                    yield return null;
                }
            }

            SceneManager.LoadScene(mainGameSceneName);
        }
    }
}
