using System.Collections.Generic;
using UnityEngine;

public class CardHandLayout : MonoBehaviour
{
    [Header("Distribution")]
    public float handWidth = 900f;
    public float extraSpacing = 0f;

    [Header("Curve")]
    public float curveHeight = 60f;
    public float rotationMultiplier = 1f;

    RectTransform rt;

    void OnTransformChildrenChanged()
    {
        Rebuild();
    }

    private void Awake()
    {
        rt = (RectTransform)transform;
    }

    private void Rebuild()
    {
        if (rt.childCount == 0) return;

        var children = new List<RectTransform>(rt.childCount);
        for (int i = 0; i < rt.childCount; i++)
        {
            var child = (RectTransform)rt.GetChild(i);
            if (child != null && child.gameObject.activeSelf)
                children.Add(child);
        }
        
        float totalSpan = handWidth + extraSpacing * (children.Count - 1);

        float stepX = 160f + extraSpacing * (children.Count - 1);
        float halfSpan = Mathf.Max(1f, totalSpan * 0.5f);


        for (int i = 0; i < children.Count; i++)
        {
            RectTransform card = children[i];

            float x = children.Count == 1 ? 0f : stepX * i - stepX * (children.Count - 1) * 0.5f;
            float norm = Mathf.Clamp(x / halfSpan, -1f, 1f);
            float y = curveHeight * (1f - norm * norm);

            card.anchoredPosition3D = new Vector3(x, y, 0);

            float dydx = -2f * curveHeight * x / (halfSpan * halfSpan);
            float rotZ = Mathf.Atan(dydx) * Mathf.Rad2Deg * rotationMultiplier;
            card.localRotation = Quaternion.Euler(0f, 0f, rotZ);
            card.GetComponent<CardHover>().SetBaseValues();
        }
    }
}
