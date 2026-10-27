using UnityEngine;

public class ZombiePool : MonoBehaviour
{
    // 싱글톤
    private static ZombiePool _instance = null;
    public static ZombiePool Instance => _instance;

    [Header("좀비 프리팹")]
    [SerializeField] private Zombie _zombiePrefab;

    [Header("좀비 풀 사이즈")]
    [SerializeField] private int _zombiePoolSize = 10;

    /// <summary>
    /// 생성한 좀비를 담아둘 풀
    /// </summary>
    private Zombie[] _zombiePool;

    private void Awake()
    {
        if (_instance != null)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;

        // 씬에 있는 좀비를 넣으면 죽을 때 비활성화되어 복제 원본으로 쓸 수 없음 (프리팹 에셋 필수)
        if (_zombiePrefab == null || _zombiePrefab.gameObject.scene.IsValid())
        {
            Debug.LogError("[ZombiePool] 좀비 프리팹 에셋(Project 창)을 할당해야 합니다.");
            return;
        }

        // 1. 좀비 풀 생성
        _zombiePool = new Zombie[_zombiePoolSize];

        // 2. 좀비를 미리 생성해서 풀에 저장
        for (int i = 0; i < _zombiePoolSize; i++)
        {
            Zombie zombie = Instantiate(_zombiePrefab, transform);

            // 3. 사용하지 않는 좀비 비활성화
            zombie.gameObject.SetActive(false);

            _zombiePool[i] = zombie;
        }
    }

    // 사용 가능한 좀비를 지정한 위치에 배치해서 가져오기
    public Zombie GetZombie(Vector3 position, Quaternion rotation)
    {
        if (_zombiePool == null) return null;

        for (int i = 0; i < _zombiePool.Length; i++)
        {
            Zombie zombie = _zombiePool[i];

            // 비활성화된 좀비 반환
            if (zombie.gameObject.activeSelf == false)
            {
                // 활성화 전에 배치해야 좀비가 이 자리를 "처음 자리"로 기억함
                zombie.transform.SetPositionAndRotation(position, rotation);
                zombie.gameObject.SetActive(true);
                return zombie;
            }
        }

        // 사용 가능한 좀비가 없는 경우
        return null;
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }
}
