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
    [SerializeField] private float _sprintSpeed = 12f;
    [SerializeField] private float _jumpPower = 10f;
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

        // 3. 땅에 서있지 않을때만 중력을 누적한다.
        if (_characterController.isGrounded == false)
        {
            _yVelocity += Gravity * Time.deltaTime;
        }
        else
        {
            // 땅에 서있으면 중력 초기화
            _yVelocity = -1f;
        }

        // 4. 만약 점프키를 누르고 땅인 경우, 점프한다.
        // ★ Input.GetButtonDown("Jump") : 중재자 패턴 ★
        if (Input.GetButtonDown("Jump") && _characterController.isGrounded)
        {
            _yVelocity = _jumpPower;
        }

        // 5. 속도를 구한다.
        float currentSpeed = (Input.GetButton("Sprint")) ? _sprintSpeed : _moveSpeed;
        Vector3 velocity = direction * currentSpeed;
        velocity.y = _yVelocity;

        // 6. 속도(방향과 속력)에 따라 이동한다.
        //transform.position += direction * _moveSpeed * Time.deltaTime;
        _characterController.Move(velocity * Time.deltaTime);
    }
}