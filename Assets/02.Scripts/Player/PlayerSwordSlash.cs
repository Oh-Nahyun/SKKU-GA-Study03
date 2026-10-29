using UnityEngine;

// 지정한 키를 누르면 검 휘두르기 애니메이션 3개 중 하나를 랜덤으로 재생
public class PlayerSwordSlash : MonoBehaviour
{
    [Header("입력")]
    [SerializeField] private KeyCode _slashKey = KeyCode.F;

    [Header("애니메이션")]
    [SerializeField] private int _slashCount = 3; // Slash Layer의 Slash0 ~ SlashN 상태 개수

    private const string SlashLayerName = "Slash Layer";
    private const string EmptyStateName = "Empty";
    private static readonly int SlashTrigger = Animator.StringToHash("Slash");
    private static readonly int SlashIndex = Animator.StringToHash("SlashIndex");

    private Animator _animator;
    private int _slashLayerIndex;

    private void Awake()
    {
        _animator = GetComponentInChildren<Animator>();
        _slashLayerIndex = _animator.GetLayerIndex(SlashLayerName);
    }

    private void Update()
    {
        if (GameManager.Instance.State != GameState.Playing) return;

        // 사격과 폭탄 투척처럼 커서가 보이는 동안은 입력 무시
        if (Cursor.visible) return;

        // 휘두르는 도중에는 다음 동작 예약 방지
        if (!_animator.GetCurrentAnimatorStateInfo(_slashLayerIndex).IsName(EmptyStateName)) return;

        if (Input.GetKeyDown(_slashKey))
        {
            // Range(int, int)의 최댓값은 포함되지 않음
            _animator.SetInteger(SlashIndex, Random.Range(0, _slashCount));
            _animator.SetTrigger(SlashTrigger);
        }
    }
}
