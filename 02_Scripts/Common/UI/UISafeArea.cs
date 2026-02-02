using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class UISafeArea : MonoBehaviour
{
    private RectTransform _rectTransform;
    private Rect _lastSafeArea;

    private void Start()
    {
        _rectTransform = GetComponent<RectTransform>();
        ApplySafeArea(true);
    }

    private void Update()
    {
        ApplySafeArea(false);
    }

    private void ApplySafeArea(bool force)
    {
        Rect safeArea = Screen.safeArea;
        
        if (!force && safeArea == _lastSafeArea) return;
        _lastSafeArea = safeArea;
        
        Vector2 safeAreaBottomLeftPos = safeArea.position;
        Vector2 safeAreaTopRightPos = safeAreaBottomLeftPos + safeArea.size;
        
        Vector2 anchorMin = Vector2.zero;
        anchorMin.x = safeAreaBottomLeftPos.x / Screen.width;
        anchorMin.y = safeAreaBottomLeftPos.y / Screen.height;
        
        Vector2 anchorMax = Vector2.one;
        anchorMax.x = safeAreaTopRightPos.x / Screen.width;
        anchorMax.y = safeAreaTopRightPos.y / Screen.height;
        
        _rectTransform.anchorMin = anchorMin;
        _rectTransform.anchorMax = anchorMax;
    }
}
