using UnityEngine;

public class BombPool : MonoBehaviour
{
    // 싱글톤
    private static BombPool _instance = null;
    public static BombPool Instance => _instance;

    [Header("폭탄 프리팹")]
    [SerializeField] private Bomb _bombPrefab;

    [Header("폭탄 풀 사이즈")]
    [SerializeField] private int _bombPoolSize = 10;

    /// <summary>
    /// 생성한 폭탄을 담아둘 풀
    /// </summary>
    private Bomb[] _bombPool;

    private void Awake()
    {
        if (_instance != null)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;

        // 1. 폭탄 풀 생성
        _bombPool = new Bomb[_bombPoolSize];

        // 2. 폭탄을 미리 생성해서 풀에 저장
        for (int i = 0; i < _bombPoolSize; i++)
        {
            Bomb bomb = Instantiate(_bombPrefab, transform);

            // 3. 사용하지 않는 폭탄 비활성화
            bomb.gameObject.SetActive(false);

            _bombPool[i] = bomb;
        }
    }

    // 사용 가능한 폭탄 가져오기
    public Bomb GetBomb()
    {
        for (int i = 0; i < _bombPool.Length; i++)
        {
            Bomb bomb = _bombPool[i];

            // 비활성화된 폭탄 반환
            if (bomb.gameObject.activeSelf == false)
            {
                bomb.gameObject.SetActive(true);
                return bomb;
            }
        }

        // 사용 가능한 폭탄이 없는 경우
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