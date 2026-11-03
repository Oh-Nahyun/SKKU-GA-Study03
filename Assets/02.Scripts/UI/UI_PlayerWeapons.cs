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

    // 값이 바뀔 때만 텍스트를 다시 만들기 위한 이전 값 (-1 : 아직 표시 전)
    private int _shownBulletCount = -1;
    private int _shownMaxBulletCount = -1;
    private int _shownBombCount = -1;
    private int _shownBombMaxCount = -1;

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
        // 매 프레임 문자열을 만들면 GC 할당과 TMP 메시 재생성이 반복되므로 변경 시에만 갱신
        int bulletCount = _playerGunFire.BulletCount;
        int maxBulletCount = _playerGunFire.MaxBulletCount;
        if (bulletCount != _shownBulletCount || maxBulletCount != _shownMaxBulletCount)
        {
            _shownBulletCount = bulletCount;
            _shownMaxBulletCount = maxBulletCount;
            _bulletCount.text = $"총알 {bulletCount} / {maxBulletCount}";
        }

        int bombCount = _playerBombFire.BombCount;
        int bombMaxCount = _playerBombFire.BombMaxCount;
        if (bombCount != _shownBombCount || bombMaxCount != _shownBombMaxCount)
        {
            _shownBombCount = bombCount;
            _shownBombMaxCount = bombMaxCount;
            _bombCount.text = $"폭탄 {10 - bombCount} / {bombMaxCount}";
        }
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