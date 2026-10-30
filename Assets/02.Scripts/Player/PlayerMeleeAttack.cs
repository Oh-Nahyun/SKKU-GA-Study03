using System;
using UnityEngine;

public class PlayerMeleeAttack : MonoBehaviour
{
    private Player _player;
    private Animator _animator;
    private int _meleeLayerIndex;
    [SerializeField] private Collider _collider;

    [Header("슬래시 이펙트")]
    [SerializeField] private TrailRenderer _slashTrail;

    // 애니메이션 이벤트: 타격 판정 구간에 맞춰 콜라이더와 검 궤적 활성화
    public void ActivateCollider()
    {
        _collider.enabled = true;

        if (_slashTrail == null) return;
        _slashTrail.Clear(); // 이전 궤적 잔상 제거
        _slashTrail.emitting = true;
    }

    // 궤적은 emitting만 끄고 남은 꼬리는 Time 동안 자연스럽게 사라짐
    public void DeactivateCollider()
    {
        _collider.enabled = false;

        if (_slashTrail == null) return;
        _slashTrail.emitting = false;
    }

    private void Awake()
    {
        _player = GetComponentInParent<Player>();
        _animator = GetComponentInChildren<Animator>();
        _meleeLayerIndex = _animator.GetLayerIndex("Melee Layer");

        // 타격 판정과 궤적은 애니메이션 이벤트 구간에서만 활성화
        _collider.enabled = false;
        if (_slashTrail != null) _slashTrail.emitting = false;
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
}