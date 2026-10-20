using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class InteractableItemHighlight : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image targetImage;
    [SerializeField] private Image highlightOverlay;
    [SerializeField] private SchoolPrologueController controller;
    [SerializeField] [Range(0.2f, 1f)] private float highlightIntensity = 0.55f;

    private Coroutine fadeRoutine;
    private bool isHovered = false;
    private static Material sharedHighlightMat;

    public static Material GetHighlightMaterial()
    {
        if (sharedHighlightMat == null)
        {
            sharedHighlightMat = Resources.Load<Material>("ItemWhiteHighlightMat");
            if (sharedHighlightMat == null)
            {
                Shader s = Shader.Find("UI/WhiteHighlight");
                if (s != null)
                {
                    sharedHighlightMat = new Material(s);
                }
            }
        }
        return sharedHighlightMat;
    }

    private void Awake()
    {
        if (targetImage == null)
        {
            targetImage = GetComponent<Image>();
        }
        if (controller == null)
        {
            controller = GetComponentInParent<SchoolPrologueController>();
            if (controller == null)
            {
                controller = Object.FindAnyObjectByType<SchoolPrologueController>();
            }
        }
        EnsureOverlay();
    }

    public void EnsureOverlay()
    {
        if (targetImage == null) targetImage = GetComponent<Image>();

        Transform glowTf = transform.Find("HoverWhiteGlow");
        if (glowTf != null)
        {
            highlightOverlay = glowTf.GetComponent<Image>();
        }

        if (highlightOverlay == null)
        {
            GameObject glowObj = new GameObject("HoverWhiteGlow", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            glowObj.transform.SetParent(transform, false);

            RectTransform glowRect = glowObj.GetComponent<RectTransform>();
            glowRect.anchorMin = Vector2.zero;
            glowRect.anchorMax = Vector2.one;
            glowRect.offsetMin = Vector2.zero;
            glowRect.offsetMax = Vector2.zero;
            glowRect.pivot = new Vector2(0.5f, 0.5f);

            highlightOverlay = glowObj.GetComponent<Image>();
            highlightOverlay.raycastTarget = false;
        }

        if (highlightOverlay != null && targetImage != null)
        {
            highlightOverlay.sprite = targetImage.sprite;
            highlightOverlay.preserveAspect = targetImage.preserveAspect;
            highlightOverlay.type = targetImage.type;
            highlightOverlay.material = GetHighlightMaterial();
            highlightOverlay.color = new Color(1f, 1f, 1f, 0f);
            highlightOverlay.raycastTarget = false;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;

        // Do not highlight if dialogue is playing, inspection is active, or a popup is open
        if (controller != null && !controller.CanHighlightItems())
        {
            return;
        }

        if (controller != null)
        {
            controller.PlayHoverSound();
        }

        SetHighlight(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        SetHighlight(false);
    }

    private void Update()
    {
        if (isHovered)
        {
            if (controller != null && !controller.CanHighlightItems())
            {
                if (highlightOverlay != null && highlightOverlay.color.a > 0.01f)
                {
                    SetHighlight(false);
                }
            }
            else if (controller != null && controller.CanHighlightItems())
            {
                if (highlightOverlay != null && highlightOverlay.color.a < 0.05f && fadeRoutine == null)
                {
                    SetHighlight(true);
                }
            }
        }
    }

    private void OnDisable()
    {
        isHovered = false;
        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
            fadeRoutine = null;
        }
        if (highlightOverlay != null)
        {
            highlightOverlay.color = new Color(1f, 1f, 1f, 0f);
        }
    }

    public void SetHighlight(bool active)
    {
        if (highlightOverlay == null) EnsureOverlay();
        if (highlightOverlay == null) return;

        if (targetImage != null && highlightOverlay.sprite != targetImage.sprite)
        {
            highlightOverlay.sprite = targetImage.sprite;
            highlightOverlay.preserveAspect = targetImage.preserveAspect;
            highlightOverlay.type = targetImage.type;
        }

        if (highlightOverlay.material == null || highlightOverlay.material.shader.name != "UI/WhiteHighlight")
        {
            highlightOverlay.material = GetHighlightMaterial();
        }

        float targetAlpha = active ? highlightIntensity : 0f;
        if (fadeRoutine != null) StopCoroutine(fadeRoutine);

        if (gameObject.activeInHierarchy)
        {
            fadeRoutine = StartCoroutine(FadeGlowRoutine(targetAlpha, 0.12f));
        }
        else
        {
            highlightOverlay.color = new Color(1f, 1f, 1f, targetAlpha);
        }
    }

    private IEnumerator FadeGlowRoutine(float targetAlpha, float duration)
    {
        if (highlightOverlay == null) yield break;
        float startAlpha = highlightOverlay.color.a;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float a = Mathf.Lerp(startAlpha, targetAlpha, t);
            highlightOverlay.color = new Color(1f, 1f, 1f, a);
            yield return null;
        }
        highlightOverlay.color = new Color(1f, 1f, 1f, targetAlpha);
        fadeRoutine = null;
    }
}
