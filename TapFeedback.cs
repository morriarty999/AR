using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using AC;

public class TapFeedback : MonoBehaviour
{
    public Image markerImage;
    public float fadeDuration = 0.25f;
    public float pulseScale = 1.2f;
    public float pulseDuration = 0.15f;

    private Vector3 baseScale;

    void Awake()
    {
        if (FindObjectsOfType<TapFeedback>().Length > 1)
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);

        if (markerImage != null)
        {
            baseScale = markerImage.transform.localScale;
            markerImage.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            // Ignore clics UI
            if (EventSystem.current != null &&
                EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            // Ignore cinématiques / menus Adventure Creator
            if (KickStarter.stateHandler.gameState != GameState.Normal)
            {
                return;
            }

            ShowMarker(Input.mousePosition);
        }
    }

    void ShowMarker(Vector2 screenPos)
    {
        StopAllCoroutines();

        markerImage.transform.position = screenPos;
        markerImage.transform.localScale = baseScale;

        Color c = markerImage.color;
        c.a = 1f;
        markerImage.color = c;

        markerImage.gameObject.SetActive(true);
        StartCoroutine(PulseAndFade());
    }

    IEnumerator PulseAndFade()
    {
        float t = 0f;

        // Pulse
        while (t < pulseDuration)
        {
            t += Time.deltaTime;
            float s = Mathf.Lerp(1f, pulseScale, t / pulseDuration);
            markerImage.transform.localScale = baseScale * s;
            yield return null;
        }

        // Fade out
        t = 0f;
        Color c = markerImage.color;

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            c.a = Mathf.Lerp(1f, 0f, t / fadeDuration);
            markerImage.color = c;
            yield return null;
        }

        markerImage.gameObject.SetActive(false);
    }
}
