using TMPro;
using UnityEngine;

// 화면 위쪽에 "처치한 좀비 수 / 최대 좀비 수"를 보여 준다.
// 스포너의 OnKilledZombieCountChanged 이벤트를 구독하므로 매 프레임 갱신하지 않는다.
public class UI_ZombieCount : MonoBehaviour
{
    [SerializeField] private ZombieSpawner _zombieSpawner;
    [SerializeField] private TextMeshProUGUI _countText;

    private void OnEnable()
    {
        _zombieSpawner.OnKilledZombieCountChanged += Refresh;

        // 처음에도 "0 / 최대 수"가 보이도록 한 번 갱신한다.
        Refresh(_zombieSpawner.KilledZombieCount, _zombieSpawner.MaxZombieCount);
    }

    private void OnDisable()
    {
        _zombieSpawner.OnKilledZombieCountChanged -= Refresh;
    }

    private void Refresh(int killedCount, int maxCount)
    {
        _countText.text = $"처치 {killedCount} / {maxCount}";
    }
}
