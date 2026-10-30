using System;
using System.Numerics;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;

public enum MeleeHitMode
{
    Collider,
    Fan
}

public class PlayerMeleeAttack : MonoBehaviour
{
    private Player _player;
    private Animator _animator;
    private int _meleeLayerIndex;
    [SerializeField] private Collider _collider;
    [SerializeField] private MeleeHitMode _meleeHitMode = MeleeHitMode.Collider;

    [Header("부채꼴")]
    [SerializeField] private float _range = 2f;

    [SerializeField] private float _angle = 90f;
    [SerializeField] private LayerMask _targetMask;

    [Header("슬래시 이펙트")]
    [SerializeField] private TrailRenderer _slashTrail;

    private void Awake()
    {
        _player = GetComponentInParent<Player>();
        _animator = GetComponentInChildren<Animator>();
        _meleeLayerIndex = _animator.GetLayerIndex("Melee Layer");

        // 타격 판정과 궤적은 애니메이션 이벤트 구간에서만 활성화
        _collider.enabled = false;
        if (_slashTrail != null) _slashTrail.emitting = false;

        _collider.enabled = false;
    }

    private void Update()
    {
        if (GameManager.Instance.State != GameState.Playing) return;
        if (_player.WeaponMode != WeaponMode.Melee) return;

        if (Input.GetButtonDown("Fire2"))
        {
            _animator.SetTrigger("Slash");
        }
    }

    // 애니메이션 이벤트: 타격 판정 구간에 맞춰 콜라이더와 검 궤적 활성화
    public void ActivateCollider()
    {
        if (_meleeHitMode != MeleeHitMode.Collider) return;

        _collider.enabled = true;

        if (_slashTrail == null) return;
        _slashTrail.Clear(); // 이전 궤적 잔상 제거
        _slashTrail.emitting = true;
    }

    // 궤적은 emitting만 끄고 남은 꼬리는 Time 동안 자연스럽게 사라짐
    public void DeactivateCollider()
    {
        if (_meleeHitMode != MeleeHitMode.Collider) return;

        _collider.enabled = false;

        if (_slashTrail == null) return;
        _slashTrail.emitting = false;
    }

    public void FanAttack()
    {
        if (_meleeHitMode != MeleeHitMode.Fan) return;

        Vector3 origin = _player.transform.position;

        // Physics.OverlapSphere(원점, 반지름, 찾을 레이어들)
        Collider[] colliders = Physics.OverlapSphere(origin, _range, _targetMask);
        foreach (Collider col in colliders)
        {
            // 적의 타격점을 찾는다.
            Vector3 hitPoint = col.ClosestPoint(origin);

            // 방향을 판단한다.
            Vector3 direction = (hitPoint - origin).normalized;
            direction.y = 0f;
            float angle = Vector3.Angle(_player.transform.forward, direction);
            if (angle > _angle / 2f) continue;

            // 적과 나 사이에 벽이 있으면 때리지 않는다.
            if (Physics.Linecast(origin, hitPoint, LayerMask.NameToLayer("Wall"))) continue;

            if (col.TryGetComponent<IDamageable>(out IDamageable damageable))
            {
                DamageInfo damageInfo = new DamageInfo();
                damageInfo.Value = 25f;
                damageInfo.HitPoint = hitPoint;
                damageInfo.HitNormal = (origin - hitPoint).normalized;
                damageable.TakeDamage(damageInfo);
            }
        }
    }
}