using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UI_Loading : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Slider _progressSlider; // 진행률 바
    [SerializeField] private TextMeshProUGUI _progressText; // 진행률 글자

    [Header("연출")]
    [SerializeField] private float _fillSpeed = 1f; // 표시 진행률이 실제 진행률을 따라가는 속도 (초당 비율, 1이면 0→100%에 최소 1초)

    private void Start()
    {
        StartCoroutine(Load_Coroutine());
    }

    private IEnumerator Load_Coroutine()
    {
        Application.backgroundLoadingPriority = ThreadPriority.Low;

        // 비동기 씬 로드!
        // 반환 : 씬의 비동기 진행 사항을 확인할 수 있는 객체 (오퍼레이션)
        AsyncOperation operation = SceneManager.LoadSceneAsync(SceneLoader.NextSceneName);

        // 수많은 객체의 생성을 쪼개서 하나하나 생성할 준비를 하는데..
        operation.allowSceneActivation = false;

        float displayValue = 0f; // 화면에 표시하는 진행률

        while (!operation.isDone)
        {
            // 0에서 1 사이의 값으로 보간
            float value = Mathf.Clamp01(operation.progress / 0.9f);

            // 표시 진행률이 실제 진행률을 일정 속도로 따라감 (로딩이 빨라도 바가 차오르는 모습 표시)
            displayValue = Mathf.MoveTowards(displayValue, value, _fillSpeed * Time.deltaTime);

            _progressSlider.value = displayValue;
            _progressText.text = $"불러오는 중 {Mathf.FloorToInt(displayValue * 100)}%";

            Debug.Log(Time.frameCount);
            Debug.Log(operation.progress);

            // 표시가 100%에 도달하면 씬 전환 (100%는 실제 로딩 완료(progress 0.9) 이후에만 도달)
            if (displayValue >= 1f)
            {
                operation.allowSceneActivation = true;
                yield break;
            }

            yield return null;
        }
    }
}