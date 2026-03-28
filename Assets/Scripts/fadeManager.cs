using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class fadeManager : MonoBehaviour
{
    public CanvasGroup fadeCanvasGroup;
    public float fadeDuration = 1.5f;

    public string sceneToLoad;

    public void StartFadeToBlack()
    {
        StartCoroutine(FadeOutAndSwitchScene());
    }

    private IEnumerator FadeOutAndSwitchScene()
    {
        fadeCanvasGroup.gameObject.SetActive(true);
        fadeCanvasGroup.blocksRaycasts = true;

        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            fadeCanvasGroup.alpha = Mathf.Lerp(0f, 1f, timer / fadeDuration);
            yield return null;
        }
        fadeCanvasGroup.alpha = 1f;

        sceneLoader.LoadSceneByName(sceneToLoad);
    }
}
