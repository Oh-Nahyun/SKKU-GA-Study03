using UnityEngine;

public class SingletonBehaviour<T> : MonoBehaviour where T : class
{
    private static T _instance = null;
    public static T Instance => _instance;

    // true: 씬 전환 후에도 유지 / false: 씬과 함께 파괴되고 새 씬의 인스턴스로 교체
    protected virtual bool IsPersistent => true;

    protected virtual void Awake()
    {
        if (_instance != null)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this as T;

        if (IsPersistent)
        {
            DontDestroyOnLoad(gameObject);
        }
    }

    protected virtual void OnDestroy()
    {
        // 파괴된 인스턴스가 정적 참조로 남지 않도록 해제 (중복으로 파괴된 쪽은 제외)
        if (_instance == this as T)
        {
            _instance = null;
        }
    }
}
