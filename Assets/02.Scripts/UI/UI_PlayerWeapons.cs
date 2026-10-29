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

    private PlayerGunFire _playerGunFire;
    private PlayerBombFire _playerBombFire;

    private void Awake()
    {
        _playerGunFire = GameObject.FindWithTag("Player").GetComponent<PlayerGunFire>();
        _playerBombFire = GameObject.FindWithTag("Player").GetComponent<PlayerBombFire>();
    }

    private void Update()
    {
        _bulletCount.text = $"총알 {_playerGunFire.BulletCount} / {_playerGunFire.MaxBulletCount}";
        _bombCount.text = $"폭탄 {10 - _playerBombFire.BombCount} / {_playerBombFire.BombMaxCount}";
    }
}