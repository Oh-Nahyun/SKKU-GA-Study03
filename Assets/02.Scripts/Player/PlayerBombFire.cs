using System;
using UnityEngine;

// 마우스 오른쪽 버튼을 누르면 시선이 바라보는 방향으로 수류탄을 던지고 싶다.
public class PlayerBombFire : MonoBehaviour
{
    [SerializeField] private Transform _firePoint;
    [SerializeField] private float _throwPower = 15f;
    [SerializeField] private int _bombMaxCount = 10;

    private int _bombCount = 0;
    private float _timer = 0f;
    private Animator _animator;

    private void Awake()
    {
        _animator = GetComponentInChildren<Animator>();
    }

    private void Update()
    {
        if (GameManager.Instance.State != GameState.Playing) return;

        if (Input.GetButton("Fire2"))
        {
            _timer += Time.deltaTime;
        }

        // 1. 마우스 오른쪽 버튼 입력 받기
        bool isMouseVisible = Cursor.visible;
        if (!isMouseVisible && Input.GetButtonUp("Fire2") && _bombCount < _bombMaxCount)
        {
            Shoot();

            _animator.SetTrigger("Throw");
        }
    }

    private void Shoot()
    {
        // 2. 발사 위치에서 수류탄 생성하기
        Bomb bomb = BombPool.Instance.GetBomb();

        if (bomb == null) return;

        bomb.transform.position = _firePoint.position;
        bomb.transform.rotation = Quaternion.identity;

        // 3. 생성된 수류탄을 시선 방향으로 던지기
        Rigidbody rigidbody = bomb.GetComponent<Rigidbody>();
        rigidbody.linearVelocity = Vector3.zero;
        rigidbody.angularVelocity = Vector3.zero;
        rigidbody.AddForce(Camera.main.transform.forward * _throwPower * _timer, ForceMode.Impulse);

        _bombCount++;
        _timer = 0f;
    }
}