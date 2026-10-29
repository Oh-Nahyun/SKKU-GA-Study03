using System;
using UnityEngine;

public class Player : MonoBehaviour, IDamageable
{
    /////[SerializeField] private UI_HitEffect _uiHitEffect;

    /// <summary>
    /// 체력
    /// </summary>
    private float _health = 0f;

    public float Health => _health;

    private WeaponMode _weaponMode = WeaponMode.Normal;
    public WeaponMode WeaponMode => _weaponMode;

    /// <summary>
    /// 최대 체력
    /// </summary>
    public const float MaxHealth = 100f;

    // Action 자료형은 함수(들)를 저장할 수 있다.
    // event 키워드 : 외부에서 해당 액션을 null로 바꾸거나 Invoke 할 수 없게 됨
    public event Action OnDamaged = null;

    private Animator _animator;

    private int _injuredLayerIndex;

    private void Awake()
    {
        _animator = GetComponentInChildren<Animator>();
        _injuredLayerIndex = _animator.GetLayerIndex("Injured Layer");
    }

    private void Start()
    {
        _health = MaxHealth;
    }

    private void Update()
    {
        // 숫자 키 1은 일반 모드, 2는 저격 모드
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            _weaponMode = WeaponMode.Normal;
        }
        else if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            _weaponMode = WeaponMode.Sniper;
        }
    }

    public void TakeDamage(DamageInfo damageInfo)
    {
        if (_health <= 0f)
        {
            _animator.SetFloat("MoveMotion", 0f);
            return;
        }

        _health -= damageInfo.Value;

        // 체력에 따라 Injured Layer의 가중치를 바꿔준다.
        float healthPercent = _health / MaxHealth; // 0 ~ 1 사이의 값
        float injured = Mathf.InverseLerp(0.5f, 0f, healthPercent);
        _animator.SetLayerWeight(_injuredLayerIndex, injured);

        // if (OnDamaged != null)
        // {
        //     OnDamaged();
        // }
        OnDamaged?.Invoke(); // 위 주석 속 코드와 같은 코드

        /////_uiHitEffect.Fade(0.4f);

        Debug.Log($"플레이어가 맞았다. 데미지 {damageInfo.Value}, 남은 체력 {_health}");
        if (_health <= 0f)
        {
            Debug.Log("플레이어가 쓰러졌다!");
            GameManager.Instance.GameOver();
        }
    }
}