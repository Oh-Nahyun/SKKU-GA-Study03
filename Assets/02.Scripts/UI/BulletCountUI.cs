using System;
using TMPro;
using UnityEngine;

public class BulletCountUI : MonoBehaviour
{
    private TextMeshProUGUI _textMeshProUGUI;
    private PlayerGunFire _playerGunFire;

    private void Awake()
    {
        _textMeshProUGUI = GetComponent<TextMeshProUGUI>();
        _playerGunFire = GameObject.FindWithTag("Player").GetComponent<PlayerGunFire>();
    }

    private void Update()
    {
        _textMeshProUGUI.text = _playerGunFire.BulletCount.ToString();
    }
}