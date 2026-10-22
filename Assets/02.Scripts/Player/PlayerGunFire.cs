using System;
using System.Collections;
using UnityEngine;

// 마우스 왼쪽 버튼을 누르면 바라보는 방향으로 총알을 발사하고 싶다.
public class PlayerGunFire : MonoBehaviour
{
    [SerializeField] private ParticleSystem _hitEffectPrefab;

    [Header("스탯")]
    [SerializeField] private float _range = 100f; // 사거리

    [SerializeField] private float _damage = 10f; // 공격력
    [SerializeField] private float _fireInterval = 0.1f; // 발사 속도
    [SerializeField] private int _maxBulletCount = 30; // 총알 최대 갯수 (탄창)
    [SerializeField] private float _rerollTime = 3f; // 총알 장전 시간

    private float _fireTimer = 0f;
    public int BulletCount = 0;
    private Coroutine _rerollCoroutine = null;

    private void Start()
    {
        BulletCount = _maxBulletCount;
    }

    private void Update()
    {
        // 0. 발사 타이머를 지난 시간만큼 줄인다.
        _fireTimer -= Time.deltaTime;

        // 1. 마우스 왼쪽 버튼 입력 받기
        if (Input.GetButton("Fire1") && _fireTimer <= 0f && BulletCount > 0)
        {
            _fireTimer = _fireInterval;
            BulletCount--;
            Shoot();

            if (_rerollCoroutine != null)
            {
                //StopAllCoroutines(); // 이 방식은 이 모노비헤이비어가 등록한 모든 코루틴을 실행 취소
                StopCoroutine(_rerollCoroutine);
                _rerollCoroutine = null;
                Debug.Log("재장전 취소!");
            }
        }

        // 2. R키 누르면 총알 재장전 시작
        if (Input.GetButtonDown("Reroll"))
        {
            _rerollCoroutine = StartCoroutine(Reroll_Coroutine());
        }
    }

    private void Shoot()
    {
        // Ray : 레이저(시작 위치, 날아갈 방향)
        // Physics.Raycast : 레이저를 발사
        // RaycastHit : 레이저가 물체와 부딛히면 그 정보를 저장하는 구조체

        // 2. 레이(광선)를 생성하고 발사될 위치와 진행 방향 설정하기
        Transform cameraTransform = Camera.main.transform;
        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);

        // 3. 레이가 부딛힌 대상의 정보를 저장할 변수 생성하기
        RaycastHit hitInfo = new RaycastHit();

        // 4. 레이를 발사하고, 만일 부딛힌 물체가 있다면 이펙트 표시하기
        bool isHit = Physics.Raycast(ray, out hitInfo, _range);
        if (isHit)
        {
            // 맞은 자리에 총알 자국 이펙트 만들기
            // Debug.Log($"{hitInfo.collider.name}이 총알을 맞았다. 데미지 : {_damage}");
            ParticleSystem effect = Instantiate(_hitEffectPrefab);
            effect.transform.position = hitInfo.point; // 맞은 자리
            effect.transform.forward = hitInfo.normal; // 수직 위치로 회전 (법선 벡터)
            //effect.Play(); > 프리팹의 Play On Awake 체크
            //Destroy(effect.gameObject, 2f); > 프리팹의 Stop Action을 Destroy로 설정

            // 맞은 대상이 좀비라면 TakeDamage
            if (hitInfo.collider.TryGetComponent(out Zombie zombie))
            {
                zombie.TakeDamage(_damage);
            }
        }
    }

    private IEnumerator Reroll_Coroutine()
    {
        Debug.Log("재장전 시작!");

        yield return new WaitForSeconds(_rerollTime);

        BulletCount = _maxBulletCount;
        _rerollCoroutine = null;

        Debug.Log("재장전 완료!");
    }
}