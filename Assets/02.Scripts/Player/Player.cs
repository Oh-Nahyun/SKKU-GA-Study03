using System;
using UnityEngine;

public class Player : MonoBehaviour
{
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
}