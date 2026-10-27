using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
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

[RequireComponent(typeof(NavMeshAgent))]
public class Zombie : MonoBehaviour, IDamageable
{
    [Header("스탯")]
    [SerializeField] private float _detectDistance = 8f; // 플레이어를 발견하는 거리

    [SerializeField] private float _comebackDistance = 15f; // 처음 자리에서 이만큼 멀어지면 돌아가는 거리
    [SerializeField] private float _attackDamage = 10f; // 공격력
    [SerializeField] private float _attackRange = 1.5f; // 공격 범위
    [SerializeField] private float _attackInterval = 1.2f; // 공격 간격
    [SerializeField] private float _moveSpeed = 5f; // 이동속도
    [SerializeField] private float _maxhealth = 30f;
    [SerializeField] private float _health;

    [Header("이펙트")]
    [SerializeField] private GameObject _bloodSprayPrefab;

    //private CharacterController _controller;
    private Vector3 _startPosition;
    private GameObject _player;
    private ZombieState _state = ZombieState.Idle;
    private float _attackTimer = 0f;
    private Animator _animator;

    /// <summary>
    /// 길을 찾아 걷게 해주는 NavMeshAgent 컴포넌트
    /// </summary>
    private NavMeshAgent _agent;

    // 좀비의 자식으로 생성한 피 스프레이들 (풀로 돌아갈 때 정리하기 위해 기억해 둔다)
    private readonly List<GameObject> _bloodSprays = new();

    public float MaxHealth => _maxhealth;
    public float Health => _health;

    // 체력이 변할 때마다 구독 중인 UI(체력바 등)에게 알린다.
    public event Action OnDamaged = null;

    // 좀비가 죽는 순간 구독 중인 대상(스포너 등)에게 알린다.
    public event Action<Zombie> OnDied = null;

    private void OnEnable()
    {
        // 풀에서 꺼낼 때마다 새로 배치된 자리가 "처음 자리"가 된다.
        _startPosition = transform.position;
    }

    private void Start()
    {
        _health = _maxhealth;
        //_controller = GetComponent<CharacterController>();
        _player = GameObject.FindGameObjectWithTag("Player");
        _agent = GetComponent<NavMeshAgent>();
        _agent.speed = _moveSpeed;
        _animator = GetComponentInChildren<Animator>();
    }

    private void Update()
    {
        if (GameManager.Instance.State != GameState.Playing) return;

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
        StopAgent();

        // 대기 : 제자리에서 플레이어를 기다린다.
        // 플레이어와 가까워지면 Trace 상태로 전이한다.
        float distance = Vector3.Distance(transform.position, _player.transform.position);
        if (distance <= _detectDistance)
        {
            Debug.Log($"{_state} -> Trace");
            _animator.SetTrigger("IdleToMove");
            _state = ZombieState.Trace;
        }
    }

    private void Trace()
    {
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
            _animator.SetTrigger("MoveToAttackDelay");
            _state = ZombieState.Attack;
            return;
        }

        // // 추격 : 플레이어를 쫓아간다.
        // Vector3 direction = _player.transform.position - transform.position;
        // direction.y = 0f;
        // direction.Normalize();
        //
        // transform.forward = direction;
        // //_controller.Move(direction * _moveSpeed * Time.deltaTime); // 중력 없음
        // _controller.SimpleMove(direction * _moveSpeed); // 중력 있음

        // SetDestination은 목적지 설정
        // - 호출할 때마다 목적지를 향해 조금씩 이동 (X)
        // - 한 번 세팅하면 내부에서 계속해서 이동 (O)
        _agent.SetDestination(_player.transform.position);
    }

    private void Comeback()
    {
        // 처음 자리에 도착하면 Idle 상태가 된다.
        //if (_agent.remainingDistance <= _agent.stoppingDistance)
        //if (_agent.pathStatus == NavMeshPathStatus.PathComplete)
        if (Vector3.Distance(transform.position, _startPosition) < _agent.stoppingDistance)
        {
            Debug.Log($"{_state} -> Idle");
            _animator.SetTrigger("MoveToIdle");
            _state = ZombieState.Idle;
            return;
        }

        // // 복귀 : 처음 자리로 돌아간다.
        // Vector3 direction = _startPosition - transform.position;
        // direction.y = 0f;
        // direction.Normalize();
        //
        // transform.forward = direction;
        // _controller.SimpleMove(direction * _moveSpeed);

        _agent.SetDestination(_startPosition);
    }

    private void Attack()
    {
        StopAgent();

        // 플레이어가 공격 사정 거리 밖으로 나가면 쫓아간다.
        float distance = Vector3.Distance(transform.position, _player.transform.position);
        if (distance > _attackRange)
        {
            Debug.Log($"{_state} -> Trace");
            _animator.SetTrigger("AttackMove");
            _state = ZombieState.Trace;
        }

        _attackTimer += Time.deltaTime;
        if (_attackTimer >= _attackInterval)
        {
            _animator.SetTrigger("StartAttack");
            _attackTimer = 0f;
        }
    }

    public void AttackAction()
    {
        // 공격 : 일정 시간마다 플레이어를 때린다.
        DamageInfo damageInfo = new DamageInfo()
        {
            Value = _attackDamage,
            HitPoint = _player.transform.position,
            HitNormal = (transform.position - _player.transform.position).normalized
        };
        _player.GetComponent<IDamageable>()?.TakeDamage(damageInfo);
    }

    public void TakeDamage(DamageInfo damageInfo)
    {
        if (_state == ZombieState.Death)
        {
            return;
        }

        _health -= damageInfo.Value;
        OnDamaged?.Invoke();
        StopAllCoroutines();
        if (_health > 0f)
        {
            // TODO : 맞은 부위에 피 스프레이를 실행하고 싶다.
            // 좀비의 자식으로 블러드 스프레이를 생성하도록 위치 수정
            GameObject bloodSpray = Instantiate(_bloodSprayPrefab, gameObject.transform);
            //bloodSpray.transform.SetParent(transform);
            bloodSpray.transform.position = damageInfo.HitPoint;
            bloodSpray.transform.forward = damageInfo.HitNormal;
            _bloodSprays.Add(bloodSpray);

            // 피격 : 공격(총알, 폭탄)을 받아 데미지를 받고 잠깐 멈칫한다.
            Debug.Log($"{_state} -> Hit");
            _animator.SetTrigger("Hit");
            _state = ZombieState.Hit;
            StartCoroutine(Hit_Coroutine());
        }
        else
        {
            // 죽음 : 쓰러지고 잠시 뒤 사라진다.
            Debug.Log($"{_state} -> Death");
            _animator.SetTrigger("Death");
            _state = ZombieState.Death;
            OnDied?.Invoke(this);
            StartCoroutine(Death_Coroutine());
        }
    }

    private IEnumerator Hit_Coroutine()
    {
        StopAgent();
        yield return new WaitForSeconds(0.5f);

        // 맞았으니 쫓아온다.
        Debug.Log($"{_state} -> Trace");
        _state = ZombieState.Trace;
    }

    private IEnumerator Death_Coroutine()
    {
        StopAgent();
        yield return new WaitForSeconds(2f);
        ReturnToPool();
    }

    // 파괴하지 않고, 다음에 다시 쓸 수 있게 상태를 되돌린 뒤 비활성화한다.
    private void ReturnToPool()
    {
        // 아직 남아 있는 피 스프레이는 비활성화되면 스스로 사라지지 못하므로 직접 정리한다.
        foreach (GameObject bloodSpray in _bloodSprays)
        {
            Destroy(bloodSpray);
        }

        _bloodSprays.Clear();

        _health = _maxhealth;
        _state = ZombieState.Idle;
        _attackTimer = 0f;

        gameObject.SetActive(false);
    }

    private void StopAgent()
    {
        _agent.ResetPath(); // 에이전트의 목적지 삭제 (_agent.isStopped = true;)
        _agent.velocity = Vector3.zero; // 미끄럼 방지를 위해 가속도 0으로 변경
    }
}