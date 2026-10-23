using System;
using UnityEngine;

public class Player : MonoBehaviour, IDamageable
{
    [SerializeField] private UI_HitEffect _uiHitEffect;

    /// <summary>
    /// 체력
    /// </summary>
    private float _health = 0f;

    public float Health => _health;

    /// <summary>
    /// 최대 체력
    /// </summary>
    public const float MaxHealth = 100f;

    private void Start()
    {
        _health = MaxHealth;
    }

    public void TakeDamage(DamageInfo damageInfo)
    {
        if (_health <= 0f) return;

        _health -= damageInfo.Value;
        _uiHitEffect.Fade(0.4f);

        Debug.Log($"플레이어가 맞았다. 데미지 {damageInfo.Value}, 남은 체력 {_health}");
        if (_health <= 0f)
        {
            Debug.Log("플레이어가 쓰러졌다!");
            // TODO : 게임 오버
        }
    }
}