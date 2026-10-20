using System;
using UnityEngine;
using UnityEngine.UI;

public class PlayerStamina : MonoBehaviour
{
    /// <summary>
    /// 슬라이더
    /// </summary>
    private Slider _slider;

    /// <summary>
    /// 플레이어 이동 클래스
    /// </summary>
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