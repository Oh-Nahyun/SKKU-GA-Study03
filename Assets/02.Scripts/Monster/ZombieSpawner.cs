using System;
using UnityEngine;

// 지정한 위치들 중 한 곳에 좀비를 주기적으로 생성하는 스포너
// - 최대 좀비 수만큼 일정 간격마다 한 마리씩 생성한다.
// - 좀비를 처치할 때마다 이벤트로 알린다. (UI 등이 구독)
public class ZombieSpawner : MonoBehaviour
{
    [Header("스폰 설정")]
    [SerializeField] private Transform[] _spawnPoints; // 비어 있으면 스포너 자신의 위치에서 생성한다.

    [SerializeField] private int _maxZombieCount = 10; // 생성할 좀비의 총 수
    [SerializeField] private float _spawnInterval = 3f; // 생성 간격(초)

    private int _spawnedZombieCount = 0;
    private int _killedZombieCount = 0;
    private float _spawnTimer = 0f;

    public int KilledZombieCount => _killedZombieCount;
    public int MaxZombieCount => _maxZombieCount;

    // (처치한 좀비 수, 최대 좀비 수)
    public event Action<int, int> OnKilledZombieCountChanged = null;

    private void Start()
    {
        // 게임이 시작되면 첫 좀비를 바로 생성한다.
        _spawnTimer = _spawnInterval;
    }

    private void Update()
    {
        if (GameManager.Instance.State != GameState.Playing) return;
        if (_spawnedZombieCount >= _maxZombieCount) return;

        _spawnTimer += Time.deltaTime;
        if (_spawnTimer >= _spawnInterval && TrySpawn())
        {
            _spawnTimer = 0f;
        }
    }

    private bool TrySpawn()
    {
        Transform spawnPoint = GetRandomSpawnPoint();

        // 풀에 쓸 수 있는 좀비가 없으면 이번에는 생성하지 않고, 다음 프레임에 다시 시도한다.
        Zombie zombie = ZombiePool.Instance.GetZombie(spawnPoint.position, spawnPoint.rotation);
        if (zombie == null) return false;

        zombie.OnDied += HandleZombieDied;

        _spawnedZombieCount++;
        return true;
    }

    private Transform GetRandomSpawnPoint()
    {
        if (_spawnPoints == null || _spawnPoints.Length == 0)
        {
            return transform;
        }

        return _spawnPoints[UnityEngine.Random.Range(0, _spawnPoints.Length)];
    }

    private void HandleZombieDied(Zombie zombie)
    {
        zombie.OnDied -= HandleZombieDied;

        _killedZombieCount++;
        OnKilledZombieCountChanged?.Invoke(_killedZombieCount, _maxZombieCount);
    }
}
