using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using AC;

public class IntroHintController : MonoBehaviour
{
    [Header("Rotation Scène")]
    public TouchRotateExtra.TouchRotate6HotspotsExtra sceneRotator;
    public float autoRotateAmount = 20f;
    public float autoRotateDuration = 1.5f;

    [Header("Icône UI")]
    public Image rotateIcon;
    public float iconSwingAmount = 14f;
    public float iconSwingSpeed = 1.8f;

    [Header("Asset - Pulse")]
    public Renderer[] assetRenderers;
    [Tooltip("Indices des materials (dans l'ordre du Renderer) qui doivent pulser pour chaque objet.")]
    public int[] pulseMaterialIndices = new int[] { 0 };
    [Tooltip("Couleur vers laquelle chaque material pulse.")]
    public Color pulseTargetColor = Color.white;
    [Range(0f, 1f)]
    public float pulseAmount = 0.4f;
    public float pulseFadeIn = 0.4f;
    public float pulseFadeOut = 0.6f;

    [Header("Timing")]
    public float autoDisappearDelay = 3f;

    // ---------------------------------------------
    private bool isPlaying = false;
    private Color[][] originalColors;

    // Optimisation mobile : évite de cloner les materials à chaque exécution
    private MaterialPropertyBlock propBlock;
    // Note : Si ton jeu utilise l'Universal Render Pipeline (URP), change "_Color" en "_BaseColor"
    private int colorPropertyID = Shader.PropertyToID("_Color");

    void Awake()
    {
        propBlock = new MaterialPropertyBlock();
    }

    // --- Point d'entrée appelé par l'ActionList AC ---
    public void PlayIntroHint()
    {
        if (isPlaying) return;
        StartCoroutine(IntroSequence());
    }

    // --- Séquence principale ---
    IEnumerator IntroSequence()
    {
        isPlaying = true;

        // Bloque tout pendant l'intro
        if (sceneRotator != null)
            sceneRotator.DisableRotation();

        KickStarter.playerInteraction.enabled = false;

        // Init visuel
        SetAlpha(rotateIcon, 0f);

        // Mémorisation des couleurs d'origine sans instancier de nouveaux materials
        if (assetRenderers != null && assetRenderers.Length > 0)
        {
            originalColors = new Color[assetRenderers.Length][];

            for (int r = 0; r < assetRenderers.Length; r++)
            {
                if (assetRenderers[r] != null)
                {
                    originalColors[r] = new Color[assetRenderers[r].sharedMaterials.Length];
                    for (int i = 0; i < assetRenderers[r].sharedMaterials.Length; i++)
                    {
                        originalColors[r][i] = assetRenderers[r].sharedMaterials[i].color;
                    }
                }
            }
        }

        // Lancement en parallèle
        StartCoroutine(AutoRotateScene());
        StartCoroutine(FadeIn(rotateIcon, 0.5f));
        StartCoroutine(AnimateIcon());
        StartCoroutine(PulseAssets());

        // Attente durée fixe
        yield return new WaitForSeconds(autoDisappearDelay);

        // Tout disparaît proprement
        isPlaying = false;
        yield return StartCoroutine(FadeOut(rotateIcon, 0.4f));

        RestoreAssetColors();

        // Réactive tout
        KickStarter.playerInteraction.enabled = true;

        if (sceneRotator != null)
            sceneRotator.EnableRotation();

        // SUPPRIMÉ : gameObject.SetActive(false); 
        // L'objet reste actif en arrière-plan (invisible) pour pouvoir être relancé proprement.
    }

    // --- Auto-rotate aller-retour ---
    IEnumerator AutoRotateScene()
    {
        if (sceneRotator == null) yield break;

        float startY = sceneRotator.targetRotationY;
        float targetY = startY + autoRotateAmount;
        float t = 0f;

        while (t < 1f && isPlaying)
        {
            t += Time.deltaTime / autoRotateDuration;
            sceneRotator.targetRotationY = Mathf.Lerp(startY, targetY, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }

        yield return new WaitForSeconds(0.2f);

        t = 0f;
        while (t < 1f && isPlaying)
        {
            t += Time.deltaTime / autoRotateDuration;
            sceneRotator.targetRotationY = Mathf.Lerp(targetY, startY, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }

        sceneRotator.targetRotationY = startY;
    }

    // --- Balancement gauche/droite de l'icône ---
    IEnumerator AnimateIcon()
    {
        if (rotateIcon == null) yield break;

        Vector2 origin = rotateIcon.rectTransform.anchoredPosition;

        while (isPlaying)
        {
            float offset = Mathf.Sin(Time.time * iconSwingSpeed * Mathf.PI) * iconSwingAmount;
            rotateIcon.rectTransform.anchoredPosition = origin + new Vector2(offset, 0f);
            yield return null;
        }

        rotateIcon.rectTransform.anchoredPosition = origin;
    }

    // --- Pulse doux via MaterialPropertyBlock ---
    IEnumerator PulseAssets()
    {
        if (assetRenderers == null || pulseMaterialIndices == null || pulseMaterialIndices.Length == 0)
            yield break;

        for (int i = 0; i < 2; i++)
        {
            if (!isPlaying) break;

            float t = 0f;
            while (t < 1f && isPlaying)
            {
                t += Time.deltaTime / pulseFadeIn;
                ApplyPulseFactor(Mathf.SmoothStep(0f, 1f, t) * pulseAmount);
                yield return null;
            }

            t = 0f;
            while (t < 1f && isPlaying)
            {
                t += Time.deltaTime / pulseFadeOut;
                ApplyPulseFactor(Mathf.SmoothStep(0f, 1f, 1f - t) * pulseAmount);
                yield return null;
            }

            yield return new WaitForSeconds(0.2f);
        }
        RestoreAssetColors();
    }

    void ApplyPulseFactor(float factor)
    {
        if (assetRenderers == null) return;

        for (int r = 0; r < assetRenderers.Length; r++)
        {
            if (assetRenderers[r] == null) continue;

            foreach (int idx in pulseMaterialIndices)
            {
                if (idx < 0 || idx >= originalColors[r].Length) continue;

                Color lerpedColor = Color.Lerp(originalColors[r][idx], pulseTargetColor, factor);

                // On applique la couleur dynamiquement sans créer de nouveau Material
                assetRenderers[r].GetPropertyBlock(propBlock, idx);
                propBlock.SetColor(colorPropertyID, lerpedColor);
                assetRenderers[r].SetPropertyBlock(propBlock, idx);
            }
        }
    }

    void RestoreAssetColors()
    {
        if (assetRenderers == null || originalColors == null) return;

        for (int r = 0; r < assetRenderers.Length; r++)
        {
            if (assetRenderers[r] == null) continue;

            foreach (int idx in pulseMaterialIndices)
            {
                if (idx < 0 || idx >= originalColors[r].Length) continue;

                // En passant null, on nettoie l'override et on redonne le plein contrôle au material d'origine
                assetRenderers[r].SetPropertyBlock(null, idx);
            }
        }
    }

    // --- Helpers fade ---
    IEnumerator FadeIn(Image img, float duration)
    {
        if (img == null) yield break;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            SetAlpha(img, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }
        SetAlpha(img, 1f);
    }

    IEnumerator FadeOut(Image img, float duration)
    {
        if (img == null) yield break;
        float startAlpha = img.color.a;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            SetAlpha(img, Mathf.Lerp(startAlpha, 0f, Mathf.SmoothStep(0f, 1f, t)));
            yield return null;
        }
        SetAlpha(img, 0f);
    }

    void SetAlpha(Image img, float alpha)
    {
        if (img == null) return;
        Color c = img.color;
        c.a = alpha;
        img.color = c;
    }
}