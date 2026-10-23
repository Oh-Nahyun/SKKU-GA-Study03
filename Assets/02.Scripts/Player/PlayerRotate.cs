using System;
using UnityEngine;

public class PlayerRotate : MonoBehaviour
{
    [Header("플레이어 회전 속도")]
    [SerializeField] private float _rotationSpeed = 200f;

    private float _accumulationX = 0f; // 좌우 회전 각도

    private void Update()
    {
        if (GameManager.Instance.State != GameState.Playing) return;

        float mouseX = Input.GetAxis("Mouse X");
        _accumulationX += mouseX * _rotationSpeed * Time.deltaTime;
        transform.eulerAngles = new Vector3(0f, _accumulationX, 0f);
    }
}