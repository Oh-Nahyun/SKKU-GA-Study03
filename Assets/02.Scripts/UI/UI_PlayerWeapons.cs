using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_PlayerWeapons : MonoBehaviour
{
    [SerializeField] private Image _weaponUI;
    [SerializeField] private List<Sprite> _weaponImages;
    [SerializeField] private TextMeshProUGUI _bulletCount;
    [SerializeField] private TextMeshProUGUI _bombCount;

    private Player _player;
    private PlayerGunFire _playerGunFire;
    private PlayerBombFire _playerBombFire;

    private void Awake()
    {
        _player = GameObject.FindWithTag("Player").GetComponent<Player>();
        _playerGunFire = _player.GetComponent<PlayerGunFire>();
        _playerBombFire = _player.GetComponent<PlayerBombFire>();
    }

    private void Start()
    {
        _player.OnWeaponModeChanged += ChangeWeaponImage;
    }

    private void Update()
    {
        _bulletCount.text = $"총알 {_playerGunFire.BulletCount} / {_playerGunFire.MaxBulletCount}";
        _bombCount.text = $"폭탄 {10 - _playerBombFire.BombCount} / {_playerBombFire.BombMaxCount}";
    }

    private void ChangeWeaponImage()
    {
        if (_player.IsNormalMode)
        {
            _weaponUI.sprite = _weaponImages[0];
        }
        else
        {
            _weaponUI.sprite = _weaponImages[1];
        }
    }
}