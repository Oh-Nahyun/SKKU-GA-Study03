# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 프로젝트 개요

Unity 6 (`6000.3.22f1`, URP) 기반 3D FPS 학습용 게임 프로젝트이다. 플레이어가 총·폭탄으로 좀비를 처치하며, 씬은 `Assets/01.Scenes/GameScene.unity` 하나뿐이다. 직접 작성하는 게임 코드는 모두 `Assets/02.Scripts/` 아래에 있고, `BloodDecalsAndEffects`, `Minimap_Resource`, `Pools`(Lean Pool) 등은 외부 에셋이므로 수정하지 않는다.

답변과 코드 주석은 한국어로 작성한다.

## 명령어

빌드와 실행은 Unity 에디터에서 한다. CLI로 할 수 있는 작업은 아래와 같다.

```bash
# 코드 스타일 검사 (pre-commit 훅과 동일)
dotnet format SKKU-GA-Study03.sln --verify-no-changes --severity warn --include Assets/02.Scripts/

# 스타일 자동 수정
dotnet format SKKU-GA-Study03.sln --severity warn --include Assets/02.Scripts/

# Husky.Net 훅 설치 (clone 후 1회)
dotnet tool restore && dotnet husky install
```

`.csproj`/`.sln`은 Unity가 자동 생성한다. 새 스크립트를 추가했는데 `dotnet format`이 인식하지 못하면 Unity 에디터에서 프로젝트를 다시 열어 재생성한다. 테스트 코드는 아직 없다(`com.unity.test-framework`는 설치만 되어 있음).

## Git 규칙

- **commit-msg 훅**: 커밋 메시지는 `type: 설명` 형식이어야 한다. 허용 type은 `asset|build|chore|ci|docs|feat|fix|perf|refactor|revert|style|test`이다. 설명은 한국어로 작성한다.
- **pre-commit 훅**: 위의 `dotnet format --verify-no-changes`를 실행하므로 스타일 위반이 있으면 커밋이 실패한다.
- **CI**: `main`에 `Assets/02.Scripts/**/*.cs` 변경을 push하면 Gemini가 해당 커밋을 리뷰하고 커밋 코멘트를 단다(`.github/workflows/gemini-commit-review.yml`).
- 요청받지 않았다면 코드를 직접 변경하거나 커밋하지 않는다.

## 코딩 컨벤션 (`.editorconfig`, `Assets/02.Scripts/**.cs`에만 적용)

- private 필드: `_camelCase` / private `const`·`static readonly`: `PascalCase` / 지역 변수: `camelCase`
- 타입 이름에 `_`를 쓰지 않는다. 단, 기존 UI 클래스는 `UI_` 접두사(`UI_Minimap`, `UI_HitEffect` 등)를 사용하므로 새 UI 스크립트도 이 관례를 따른다.
- 중괄호는 항상 새 줄에 둔다(Allman). 한 줄짜리 조기 리턴(`if (...) return;`)만 중괄호를 생략할 수 있다.
- `switch`의 `case`는 `{ }` 블록으로 감싼다.
- Inspector 노출 값은 `[SerializeField] private`로 선언하고 `[Header("...")]`로 묶는다.
- 코루틴 메서드 이름은 `XXX_Coroutine` 형식을 사용한다.

## 아키텍처

### 게임 상태와 싱글톤
- `GameManager`(싱글톤)가 `GameState`(`Ready → Playing → GameOver`)를 관리한다. 시작 시 "준비/시작!" 연출 코루틴이 끝나야 `Playing`이 된다.
- 입력이나 AI를 처리하는 `Update()`는 모두 `if (GameManager.Instance.State != GameState.Playing) return;`로 시작한다. 새 게임플레이 스크립트도 이 패턴을 따른다.
- 싱글톤은 `GameManager`, `BombPool` 두 개이며, `Awake`에서 중복 인스턴스를 파괴하는 같은 구조로 작성되어 있다.

### 데미지 시스템
- 데미지를 받는 모든 오브젝트(`Player`, `Zombie`, `Drum`, `Box`)는 `IDamageable.TakeDamage(DamageInfo)`를 구현한다.
- `DamageInfo` 구조체는 `Value`, `HitPoint`, `HitNormal`을 담는다. 피격 이펙트의 위치와 방향은 `HitPoint`/`HitNormal`로 결정한다.
- 공격하는 쪽은 구체 타입을 몰라도 되도록 `GetComponent<IDamageable>()?.TakeDamage(...)`로 호출한다. 총은 Raycast, 폭탄은 `OverlapSphere`(`Drum`, `Zombie` 레이어)를 사용한다.

### 플레이어
- 역할별로 컴포넌트를 나눈다: `Player`(체력, 사망 시 `GameManager.GameOver()` 호출), `PlayerMove`, `PlayerRotate`, `PlayerGunFire`, `PlayerBombFire`.
- 입력은 레거시 `Input` 클래스를 사용한다(`Fire1`, `Fire2`, `Reroll` 등은 Input Manager 축 이름). Player Settings가 Both 모드라 Input System 패키지도 함께 설치되어 있지만 게임 코드에서는 사용하지 않는다.
- `CursorManager`: 기본은 커서 잠금이며, Left Alt를 누르는 동안만 커서를 보여 준다. 사격과 폭탄 투척은 `Cursor.visible`이 `true`면 막힌다(UI 버튼 클릭과 발사가 겹치지 않도록 하기 위함).

### 몬스터
- `Zombie`는 `ZombieState` enum과 `switch`를 이용한 FSM으로 동작한다(Idle/Trace/Comeback/Attack/Hit/Death). 상태 전이는 `_state`에 값을 직접 대입하고, Hit/Death 후속 처리는 코루틴으로 한다. 이동은 `CharacterController.SimpleMove`를 사용한다.
- `ZombieHealthBar`는 체력바만 카메라를 향하게 회전시킨다(좀비 본체를 회전시키지 않도록 주의).

### 오브젝트 풀링
- `BombPool`은 `Awake`에서 폭탄을 미리 생성해 두고 `GetBomb()`으로 비활성 폭탄을 꺼내 준다. 풀이 비면 `null`을 반환한다. 폭탄은 폭발 후 `SetActive(false)`로 풀에 돌아간다.
- 이펙트(피격·폭발·피 스프레이)는 아직 `Instantiate`로 생성하고, 파티클의 Stop Action을 통해 스스로 파괴된다.

### 카메라와 UI
- `CameraFollow`/`CameraRotate`는 메인 FPS 카메라를 담당한다. `MinimapCamera`는 `LateUpdate`에서 플레이어 머리 위를 따라다니는 직교 카메라이며, `ZoomIn`/`ZoomOut`을 `UI_Minimap` 버튼과 키보드(0/9)에서 함께 호출한다.
- UI 스크립트는 `Assets/02.Scripts/UI/`에 있으며, 텍스트는 TextMeshPro를 사용한다.

### 기타
- `GameLogger.Log`는 `[Conditional("ENABLE_LOG")]`가 붙어 있어 해당 심볼이 정의된 경우에만 컴파일된다.
- `Assets/02.Scripts/Example/`은 C# 문법 학습용 예제이며 게임 로직과 관계없다.

## 코드 리뷰

코드 리뷰를 요청받으면 `Docs/CodeReviewGuide.md`의 기준(중요도 분류 Critical/High/Medium/Low, 결과 형식: 총평 → 발견된 문제 → 설계 개선 제안 → 잘 작성된 점)을 따른다. 이 가이드는 Gemini CI 리뷰에서도 함께 사용한다.

@Docs/CodeReviewGuide.md
