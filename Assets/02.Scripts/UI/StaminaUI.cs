using System;
using UnityEngine;
using UnityEngine.UI;

public class StaminaUI : MonoBehaviour
{
    private Slider _slider;
    private PlayerMove _playerMove;

    private void Awake()
    {
        _slider = GetComponent<Slider>();
        _playerMove = GameObject.FindWithTag("Player").GetComponent<PlayerMove>();
    }

    private void Update()
    {
        _slider.value = _playerMove.Stamina;
    }
}