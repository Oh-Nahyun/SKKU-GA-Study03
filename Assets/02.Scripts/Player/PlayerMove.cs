using System;
using UnityEngine;

public class PlayerMove : MonoBehaviour
{
    /// <summary>
    /// 중력
    /// </summary>
    private const float Gravity = -20f;

    /// <summary>
    /// 위아래 속도
    /// (매 프레임마다 중력 가속도가 붙음)
    /// </summary>
    private float _yVelocity = 0f;

    [SerializeField] private float _moveSpeed = 7f;

    [SerializeField] private CharacterController _characterController;

    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
    }

    private void Update()
    {
        // 1. 키보드 입력을 받는다.
        float h = Input.GetAxisRaw("Horizontal"); // -1, 0, 1
        float v = Input.GetAxisRaw("Vertical"); // -1, 0, 1

        // 2. 키보드 입력에 따라 방향을 구한다.
        Vector3 direction = new Vector3(h, 0, v);
        direction.Normalize(); // 대각선이어도 방향의 길이를 1로 바꾸기 위해
        direction = transform.TransformDirection(direction);
        // 로컬 좌표계 : 한 대상을 중심
        // 월드 좌표계 : 세상을 중심

        // 3. 중력을 누적한다.
        _yVelocity += Gravity * Time.deltaTime;

        // 4. 속도를 구한다.
        Vector3 velocity = direction * _moveSpeed;
        velocity.y = _yVelocity;

        // 5. 속도(방향과 속력)에 따라 이동한다.
        //transform.position += direction * _moveSpeed * Time.deltaTime;
        _characterController.Move(velocity * Time.deltaTime);
    }
}