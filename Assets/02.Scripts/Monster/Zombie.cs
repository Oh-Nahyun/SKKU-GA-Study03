using System;
using System.Collections;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.AdaptivePerformance.Provider;
using Debug = UnityEngine.Debug;

// FSM(유한 상태 머신) : 유한개의 상태가 있고, 상태마다 행동을 다르게 함으로써 AI를 구현하는 유명한 방법
// - 상태 : 객체가 가질 수 있는 어떤 조건이나 상황
// - 전이 : 조건에 따라 현재 상태에서 다른 상태로 넘어가는 것
// - 그리고 위 방식에 따라 규칙을 세워둔 것을 "머신"이라고 한다.

// 좀비가 가질 수 있는 상태
public enum ZombieState
{
    Idle,
    Trace,
    Comeback,
    Attack,
    Hit,
    Death
}

public class Zombie : MonoBehaviour, IDamageable
{
    [Header("스탯")]
    [SerializeField] private float _detectDistance = 8f; // 플레이어를 발견하는 거리

    [SerializeField] private float _comebackDistance = 15f; // 처음 자리에서 이만큼 멀어지면 돌아가는 거리
    [SerializeField] private float _attackRange = 1.5f; // 공격 범위
    [SerializeField] private float _attackInterval = 1.2f; // 공격 간격
    [SerializeField] private float _moveSpeed = 5f; // 이동속도
    [SerializeField] private float _health = 30f;

    [Header("이펙트")]
    [SerializeField] private GameObject _bloodSprayPrefab;

    private CharacterController _controller;
    private Vector3 _startPosition;
    private GameObject _player;
    private ZombieState _state = ZombieState.Idle;
    private float _attackTimer = 0f;

    private void Start()
    {
        _controller = GetComponent<CharacterController>();
        _startPosition = transform.position;
        _player = GameObject.FindGameObjectWithTag("Player");
    }

    private void Update()
    {
        switch (_state)
        {
            case ZombieState.Idle:
                {
                    Idle();
                    break;
                }
            case ZombieState.Trace:
                {
                    Trace();
                    break;
                }
            case ZombieState.Comeback:
                {
                    Comeback();
                    break;
                }
            case ZombieState.Attack:
                {
                    Attack();
                    break;
                }
        }
    }

    private void Idle()
    {
        // TODO : Idle 애니메이션 실행

        // 대기 : 제자리에서 플레이어를 기다린다.
        // 플레이어와 가까워지면 Trace 상태로 전이한다.
        float distance = Vector3.Distance(transform.position, _player.transform.position);
        if (distance <= _detectDistance)
        {
            Debug.Log($"{_state} -> Trace");
            _state = ZombieState.Trace;
        }
    }

    private void Trace()
    {
        // TODO : Trace 애니메이션 실행

        // 처음 자리에서 너무 멀어지면 돌아간다.
        if (Vector3.Distance(transform.position, _startPosition) > _comebackDistance)
        {
            Debug.Log($"{_state} -> Comeback");
            _state = ZombieState.Comeback;
            return;
        }

        // 플레이어가 공격 거리 안이면 공격한다.
        if (Vector3.Distance(transform.position, _player.transform.position) <= _attackRange)
        {
            _attackTimer = 0f;
            Debug.Log($"{_state} -> Attack");
            _state = ZombieState.Attack;
            return;
        }

        // 추격 : 플레이어를 쫓아간다.
        Vector3 direction = _player.transform.position - transform.position;
        direction.y = 0f;
        direction.Normalize();

        transform.forward = direction;
        //_controller.Move(direction * _moveSpeed * Time.deltaTime); // 중력 없음
        _controller.SimpleMove(direction * _moveSpeed); // 중력 있음
    }

    private void Comeback()
    {
        // 처음 자리에 도착하면 Idle 상태가 된다.
        if (Vector3.Distance(transform.position, _startPosition) < 1f)
        {
            Debug.Log($"{_state} -> Idle");
            _state = ZombieState.Idle;
            return;
        }

        // 복귀 : 처음 자리로 돌아간다.
        Vector3 direction = _startPosition - transform.position;
        direction.y = 0f;
        direction.Normalize();

        transform.forward = direction;
        _controller.SimpleMove(direction * _moveSpeed);
    }

    private void Attack()
    {
        // 플레이어가 공격 사정 거리 밖으로 나가면 쫓아간다.
        float distance = Vector3.Distance(transform.position, _player.transform.position);
        if (distance > _attackRange)
        {
            Debug.Log($"{_state} -> Trace");
            _state = ZombieState.Trace;
        }

        _attackTimer += Time.deltaTime;
        if (_attackTimer >= _attackInterval)
        {
            // TODO : 공격 애니메이션 실행
            // TODO : 플레이어를 때려!
            // 공격 : 일정 시간마다 플레이어를 때린다.
            Debug.Log("플레이어를 때렸습니다.");
            _attackTimer = 0f;
        }
    }

    public void TakeDamage(DamageInfo damageInfo)
    {
        if (_state == ZombieState.Death)
        {
            return;
        }

        _health -= damageInfo.Value;
        StopAllCoroutines();
        if (_health > 0f)
        {
            // TODO : 맞은 부위에 피 스프레이를 실행하고 싶다.
            // 좀비의 자식으로 블러드 스프레이를 생성하도록 위치 수정
            GameObject bloodSpray = Instantiate(_bloodSprayPrefab, gameObject.transform);
            //bloodSpray.transform.SetParent(transform);
            bloodSpray.transform.position = damageInfo.HitPoint;
            bloodSpray.transform.forward = damageInfo.HitNormal;

            // 피격 : 공격(총알, 폭탄)을 받아 데미지를 받고 잠깐 멈칫한다.
            Debug.Log($"{_state} -> Hit");
            _state = ZombieState.Hit;
            StartCoroutine(Hit_Coroutine());
        }
        else
        {
            // 죽음 : 쓰러지고 잠시 뒤 사라진다.
            Debug.Log($"{_state} -> Death");
            _state = ZombieState.Death;
            StartCoroutine(Death_Coroutine());
        }
    }

    private IEnumerator Hit_Coroutine()
    {
        yield return new WaitForSeconds(0.3f);

        // 맞았으니 쫓아온다.
        Debug.Log($"{_state} -> Trace");
        _state = ZombieState.Trace;
    }

    private IEnumerator Death_Coroutine()
    {
        yield return new WaitForSeconds(0.5f);
        Destroy(gameObject);
    }
}