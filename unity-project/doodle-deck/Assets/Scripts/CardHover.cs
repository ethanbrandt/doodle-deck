using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;

public class CardHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Hover Settings")]
    [SerializeField] float hoverScale = 1.15f;
    [SerializeField] float hoverLift = 30f;
    [SerializeField] float animTime = 0.12f;

    RectTransform rt;
    Vector2 baseAnchoredPos;
    Vector3 baseScale;
    Outline outline;
    private Canvas localCanvas;

    Coroutine anim;

    void Awake()
    {
        rt = GetComponent<RectTransform>();
        outline = GetComponent<Outline>();

        localCanvas = GetComponent<Canvas>();
        
        if (outline) outline.enabled = false;
    }

    public void SetBaseValues()
    {
        baseAnchoredPos = rt.position;
        baseScale = rt.localScale;
    }

    public void OnPointerEnter(PointerEventData _eventData)
    {
        if (outline) outline.enabled = true;

        localCanvas.sortingOrder = 1000;
        // animate up and scale
        Vector2 targetPos = baseAnchoredPos + new Vector2(0f, hoverLift);
        Vector3 targetScale = Vector3.one * hoverScale;
        StartAnim(targetPos, targetScale);
    }

    public void OnPointerExit(PointerEventData _eventData)
    {
        if (outline) outline.enabled = false;

        localCanvas.sortingOrder = 0;
        StartAnim(baseAnchoredPos, baseScale);
    }

    public void OnPointerClick(PointerEventData _eventData)
    {
        GetComponent<UnitUICard>().CardClicked();
    }
    
    void StartAnim(Vector2 pos, Vector3 scale)
    {
        if (anim != null) StopCoroutine(anim);
        anim = StartCoroutine(AnimateTo(pos, scale));
    }

    IEnumerator AnimateTo(Vector2 targetPos, Vector3 targetScale)
    {
        float t = 0f;
        Vector2 startPos = rt.position;
        Vector3 startScale = rt.localScale;

        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / animTime; // UI feel independent of timescale
            float e = Smooth(t);
            rt.position = Vector2.LerpUnclamped(startPos, targetPos, e);
            rt.localScale = Vector3.LerpUnclamped(startScale, targetScale, e);
            yield return null;
        }

        rt.position = targetPos;
        rt.localScale = targetScale;
        anim = null;
    }

    float Smooth(float x) => 1f - Mathf.Pow(1f - Mathf.Clamp01(x), 3f);
}
