using System;
using UnityEngine;
using UnityEngine.UI;

public class UI_Minimap : MonoBehaviour
{
    // TODO : +/- 버튼 클릭하면...
    [SerializeField] private MinimapCamera _minimapCamera;
    [SerializeField] private Button _zoomOutButton;
    [SerializeField] private Button _zoomInButton;

    private void Start()
    {
        _zoomOutButton.onClick.AddListener(_minimapCamera.ZoomOut);
        _zoomInButton.onClick.AddListener(_minimapCamera.ZoomIn);
    }
}