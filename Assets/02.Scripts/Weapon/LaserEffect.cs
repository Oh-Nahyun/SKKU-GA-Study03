using System.Collections;
using UnityEngine;

// 총을 쏠 때 총구에서 맞은 곳까지 레이저 선이 짧게 번쩍이게 한다.
[RequireComponent(typeof(LineRenderer))]
public class LaserEffect : MonoBehaviour
{
    [SerializeField] private float _showDuration = 0.05f; // 선이 보이는 시간(초)
    [SerializeField] private float _lineWidth = 0.02f; // 선의 굵기

    private LineRenderer _lineRenderer;
    private Coroutine _hideCoroutine = null;

    private void Awake()
    {
        _lineRenderer = GetComponent<LineRenderer>();
        _lineRenderer.positionCount = 2;
        _lineRenderer.useWorldSpace = true;
        _lineRenderer.startWidth = _lineWidth;
        _lineRenderer.endWidth = _lineWidth;
        _lineRenderer.enabled = false;
    }

    // 시작점에서 끝점까지 선을 그리고, 잠깐 뒤에 숨긴다.
    public void Show(Vector3 start, Vector3 end)
    {
        _lineRenderer.SetPosition(0, start);
        _lineRenderer.SetPosition(1, end);
        _lineRenderer.enabled = true;

        // 연사 중이면 이전에 예약한 숨기기를 취소하고 다시 센다.
        if (_hideCoroutine != null)
        {
            StopCoroutine(_hideCoroutine);
        }
        _hideCoroutine = StartCoroutine(Hide_Coroutine());
    }

    private IEnumerator Hide_Coroutine()
    {
        yield return new WaitForSeconds(_showDuration);

        _lineRenderer.enabled = false;
        _hideCoroutine = null;
    }
}
