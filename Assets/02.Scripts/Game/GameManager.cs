using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameState
{
    Ready,
    Playing,
    Pause,
    GameOver
}

public class GameManager : SingletonBehaviour<GameManager>
{
    private GameState _state = GameState.Ready;
    public GameState State => _state;

    [SerializeField] private TextMeshProUGUI _stateText;
    [SerializeField] private GameObject _optionPopup;
    [SerializeField] private GameObject _gameOverPopup;

    // 씬의 UI를 참조하므로 씬과 생명주기를 함께함 (재시작 시 새 씬의 GameManager가 담당)
    protected override bool IsPersistent => false;

    private void Start()
    {
        _optionPopup.SetActive(false);
        _gameOverPopup.SetActive(false);

        StartCoroutine(StartSequence_Coroutine());
    }

    private void Update()
    {
        //Debug.Log($"유니티 흐른 시간 : {Time.deltaTime}");
        //Debug.Log($"실제 흐른 시간 : {Time.unscaledDeltaTime}");

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (_state == GameState.Playing)
            {
                _state = GameState.Pause;
                Time.timeScale = 0; // 유니티에서 흐르는 게임 시간
                _optionPopup.SetActive(true);
                CursorManager.Instance.UnlockCursor();
            }
            else if (_state == GameState.Pause)
            {
                Resume();
            }
        }
    }

    private IEnumerator StartSequence_Coroutine()
    {
        _stateText.gameObject.SetActive(true);
        _stateText.text = "준비";
        yield return new WaitForSeconds(1.5f);
        _stateText.text = "시작!";
        yield return new WaitForSeconds(0.5f);
        _stateText.gameObject.SetActive(false);
        _state = GameState.Playing;
    }

    public void GameOver()
    {
        _state = GameState.GameOver;
        _gameOverPopup.SetActive(true);

        // 팝업 버튼을 누를 수 있도록 커서 표시
        CursorManager.Instance.UnlockCursor();
    }

    public void Resume()
    {
        _state = GameState.Playing;
        Time.timeScale = 1f;
        _optionPopup.SetActive(false);
        CursorManager.Instance.LockCursor();
    }

    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}