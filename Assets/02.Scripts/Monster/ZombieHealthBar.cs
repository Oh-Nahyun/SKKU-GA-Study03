using System;
using UnityEngine;
using UnityEngine.UI;

public class ZombieHealthBar : MonoBehaviour
{
    [SerializeField] private Slider _slider;

    private Zombie _zombie;

    private void Awake()
    {
        _zombie = GetComponent<Zombie>();
    }

    private void OnEnable()
    {
        // 좀비가 피격될 때만 체력바를 갱신하도록 구독
        _zombie.OnDamaged += RefreshHealth;

        // 프리팹 슬라이더의 초기값이 남지 않도록 한 번 갱신
        RefreshHealth();
    }

    private void OnDisable()
    {
        _zombie.OnDamaged -= RefreshHealth;
    }

    private void LateUpdate()
    {
        // 빌보드 : 체력바가 카메라를 쳐다보게 한다.
        _slider.transform.forward = Camera.main.transform.forward;
    }

    private void RefreshHealth()
    {
        // 0 ~ 100% -> 0 ~ 1
        // _slider.value = 좀비의 현재 체력 / 좀비의 최대 체력
        _slider.value = _zombie.Health / _zombie.MaxHealth;
    }
}
