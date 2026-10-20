using System;
using UnityEngine;

// 마우스 왼쪽 버튼을 누르면 바라보는 방향으로 총알을 발사하고 싶다.
public class PlayerGunFire : MonoBehaviour
{
    /// <summary>
    /// 사거리
    /// </summary>
    private float _range = 100f;

    private float _damage = 10f;

    private void Update()
    {
        // 1. 마우스 왼쪽 버튼 입력 받기
        if (Input.GetButtonDown("Fire1"))
        {
            Shoot();
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
            Debug.Log($"{hitInfo.collider.name}이 총알을 맞았다. 데미지 : {_damage}");
            // TODO : 이펙트 생성
        }
    }
}