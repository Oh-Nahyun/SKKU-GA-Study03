using UnityEngine;
using UnityEngine.UI;

public class UI_PlayerStats : MonoBehaviour
{
    // TODO : ZombieHealthBar를 이용해서 체력과 스태미나 UI 구현
    [SerializeField] private Slider _healthSlider;
    [SerializeField] private Slider _staminaSlider;

    private Player _player;
    private PlayerMove _playerMove;

    private void Awake()
    {
        GameObject player = GameObject.FindWithTag("Player");
        _player = player.GetComponent<Player>();
        _playerMove = player.GetComponent<PlayerMove>();
    }

    private void LateUpdate()
    {
        _healthSlider.value = _player.Health / Player.MaxHealth;
        _staminaSlider.value = _playerMove.Stamina / PlayerMove.MaxStamina;
    }
}