# 측정 절차서 (Before / After 공통)

> 과제 핵심은 "직접 실험하고 측정해 개선을 증명"하는 것이다. 이 문서의 순서대로 **한 번에 하나만 바꾸고** 측정한 뒤 `result-table.md`에 기록한다.
> 수치는 실측값만 적는다. 개선되지 않았거나 나빠진 경우도 그대로 적고 원인을 분석한다.

## 0. 준비
- Unity 6000.3, GameScene, PC 퀄리티 레벨(`Edit > Project Settings > Quality`에서 PC 선택).
- Game 뷰: 해상도 **Full HD(1920×1080)** 고정, **VSync 끔**(Game 뷰 상단 VSync 토글), Stats 버튼 켜기.
- 다른 에디터 창(Scene 뷰 등)은 닫거나 Game 뷰만 보이게 한다. (Scene 뷰가 열려 있으면 렌더링 비용이 섞인다.)
- 캡처 파일은 `Assets/` 밖 `Docs/Report/img/`에 저장한다. (`Assets` 안에 두면 `.meta`가 생기고 에셋으로 임포트된다.)
- **주의**: 측정 후 `git diff ProjectSettings`로 의도하지 않은 설정 변경이 없는지 확인한다.

## 1. 측정 시나리오 (모든 측정 동일)
1. 플레이 시작 → "준비/시작!" 연출이 끝나 `Playing`이 될 때까지 대기.
2. **시작 위치에서 이동·사격 없이** 대기하여 좀비가 상한(10마리)까지 스폰될 때까지 약 30초 기다린다. (스폰 간격 3초)
3. 이후 **10초 구간**을 측정한다.
4. 같은 조건으로 **3회** 반복하고 **중앙값**을 기록한다.

> 좀비는 랜덤 스폰이라 매 회 완전히 같지 않다. 값이 크게 튀면 3회 중 중앙값을 쓰고, 표에 편차(최소~최대)도 같이 적는다.

## 2. 수집 항목과 도구
| 항목 | 도구 | 어디서 |
|---|---|---|
| FPS, CPU main/render thread(ms), **Batches**, **SetPass calls**, **Tris**, **Shadow casters**, Visible skinned meshes | **Game 뷰 Stats** | Game 뷰 우상단 `Stats` 버튼 → 스크린샷 |
| 드로우콜·패스 목록 | **Frame Debugger** | `Window > Analysis > Frame Debugger` → Enable, 이벤트 총 개수와 패스 구성 캡처 |
| CPU/렌더링/GC | **Profiler** | `Window > Analysis > Profiler` → CPU Usage, Rendering, Memory 모듈. **GC Alloc/프레임**은 CPU Usage 하단 Hierarchy에서 `GC Alloc` 열로 정렬 |
| 메모리(Texture 등) | **Profiler Memory 모듈** (Simple 보기) 또는 Memory Profiler 패키지 | Texture Memory, Mesh Memory, Total Used Memory 기록 |
| GPU 시간 | Stats의 GPU ms(GPU 타이밍이 지원되는 환경에서만) | 표시되지 않으면 "측정 불가"로 기록 |

- 스크린샷에는 **FPS/Batches/SetPass/Tris가 한 장에 보이게** 한다.
- Profiler는 **프레임 하나를 선택(Pause)** 해서 CPU Usage 계층 구조까지 보이게 캡처한다.
- 가능하면 **Development Build**(`File > Build Profiles`, Development Build 체크)도 한 번 측정해 에디터 값과 병기한다. 에디터 값은 참고용이다.

## 3. Before 기준점 2개
1. **최적화 전 성능 기준 = 현재 HEAD 상태**
   - 이번에 코드를 수정해 두었으므로, Before는 변경을 잠시 치워서 잰다.
     ```bash
     # Before 상태로 만들기 (스크립트 4개만 임시 보관)
     git stash push -- Assets/02.Scripts/Monster/Zombie.cs Assets/02.Scripts/Monster/ZombieHealthBar.cs Assets/02.Scripts/UI/UI_PlayerWeapons.cs Assets/02.Scripts/Player/PlayerZoom.cs
     # ... Unity가 재컴파일을 끝내면 Before 측정 ...
     # After 상태로 되돌리기
     git stash pop
     ```
   - 다른 파일(씬 등)을 바꿨다면 `git stash push -- <경로>`에 그 경로도 추가한다. 작업 중이던 `CullingTestScene.unity` 등은 경로에 넣지 않는다.
2. **조명·후처리 시각 비교용 = 환경 개선 전 커밋 `4376841`**
   - 기본 스카이박스 + 빈 Volume 상태이다. 현재 프로젝트를 건드리지 않으려면 별도 폴더에 worktree를 만든다.
     ```bash
     git worktree add ../SKKU-GA-Study03-before 4376841
     ```
   - 해당 폴더를 Unity로 열면 Library를 새로 만들어 첫 임포트가 오래 걸린다. 시간이 부족하면 **Volume 컴포넌트의 Profile을 비우고 Skybox를 `Default-Skybox`로 바꿔** 현재 프로젝트에서 임시로 비교하고, 비교 후 되돌려도 된다(변경을 저장하지 말 것).

## 4. 적용 순서와 방법 (한 번에 하나, 적용 → 측정 → 기록)

### Phase 1. 조명
| 단계 | 방법 | 메모 |
|---|---|---|
| 1-1 | Hierarchy에서 `Directional Light`가 2개인지 확인. 하나만 남기고 나머지는 비활성화. `Window > Rendering > Lighting > Environment > Sun Source`에 남긴 라이트 지정 | 라이트 A(Mixed, 강도 1.5, 흰색)가 `Map_v1` 프리팹 안에 있는지 확인 |
| 1-2 | `Window > Rendering > Lighting > Scene` 탭 `New Lighting Settings`로 에셋 생성. Lightmapper **Progressive GPU**(안 되면 CPU), **Mixed Lighting: Baked Indirect 또는 Shadowmask**, Lightmap Resolution 약 10~20 texels/unit, Max Lightmap Size 2048, Ambient Occlusion 체크. 정적인 오브젝트만 `Static > Contribute GI` | 플레이어·좀비·움직이는 오브젝트는 Static 해제. 베이크 후 `Generate Lighting` |
| 1-3 | `GameObject > Light > Light Probe Group`을 플레이어·좀비가 다니는 영역에 격자로 배치. `GameObject > Light > Reflection Probe` 1~2개(컨테이너/드럼 근처) | |
| 1-4 | 그림자 단독 비교: `PC_RPAsset`의 Cascade Count 4→2, Max Distance 50→35, Soft Shadows Quality 낮춤. **하나씩** 바꿔 측정 | 화질 차이도 스크린샷으로 비교 |

### Phase 2. 포스트프로세싱 (`Assets/Settings/AoTVolumeProfile.asset`)
- 효과별 **단독 On/Off**(Bloom, Vignette, Color Adjustments, Tonemapping)를 비교한다. SSAO는 `Assets/Settings/PC_Renderer.asset`의 Renderer Feature를 체크 해제해 비교한다.
- 튜닝 세트: **Quality**(현재) / **Balanced**(Bloom Downscale 올림·Max Iterations 6→4, SSAO Downsample 체크·Samples 낮춤) / **Performance**(SSAO 끔).
- 각 세트의 FPS/ms와 화질 스크린샷을 비교한다.

### Phase 3. 최적화
| ID | 방법 (Unity에서) | 확인할 지표 |
|---|---|---|
| G1 미니맵 카메라 | `MinimapCamera` 카메라: Rendering > **Render Shadows 끔**, **Depth Texture / Opaque Texture 끔**. `Assets/09.Materials/MinimapRenderTexture` 크기 512→256 | Batches, SetPass, Frame Debugger의 미니맵 패스 |
| G2 좀비 SMR 그림자 | `Assets/03.Prefabs/Monster/Zombie.prefab`(variant)에서 작은 SkinnedMeshRenderer의 **Cast Shadows 끔**. 외부 에셋 원본(`Assets/Zombie/...`)은 수정하지 않는다 | Batches, Shadow casters |
| G3 URP 에셋 | `Assets/Settings/PC_RPAsset`: General > **Opaque Texture 끔**, Shadows > Cascade Count 2, Max Distance 35, Soft Shadows 품질 낮춤 | CPU/GPU ms, Shadow casters |
| G4 실험 | `Assets/Settings/PC_Renderer`: Rendering Path **Deferred → Forward+**. 느려질 수 있으므로 반드시 전후 비교 | GPU ms, 화질(AA) |
| C1~C3 코드 | 이미 코드에 반영됨(3절의 stash로 Before/After 비교) | Profiler CPU Usage(Scripts), GC Alloc/프레임 |
| R1 텍스처 | 산업 팩에서 크게 쓰이는 소품 텍스처(컨테이너·파이프·덤프스터·오일탱크 등) Inspector의 **Max Size 2048→1024**, `Quality > Texture Streaming(Streaming Mipmaps)` 켬 | Profiler Memory의 Texture Memory |
| R2 임포트 | 플레이어 `normal.png`, `normal 1.png`의 Texture Type을 **Normal map**으로. 플레이어/검 FBX(`Assets/05.Models`)의 Import BlendShapes·Cameras·Lights·Animation 끔 | Texture/Mesh Memory, 시각 확인 |

> **로그 변경(C1) 효과 확인 방법**: `Zombie.cs`의 로그를 `GameLogger.Log`로 바꿨지만, 이 함수는 `ENABLE_LOG` 심볼이 정의돼 있으면 그대로 출력한다. 현재 Standalone에 `ENABLE_LOG`가 정의돼 있으므로 `Edit > Project Settings > Player > Scripting Define Symbols`에서 이 심볼을 **제거한 상태**에서 측정해야 효과가 보인다. 측정 후 필요하면 되돌린다. (이 변경도 `ProjectSettings`가 바뀌므로 기록해 둔다.)

## 5. 기록 방법
- 항목마다 `Docs/Report/result-table.md`의 표에 Before/After를 채운다.
- 스크린샷 파일명 규칙: `img/<단계>_<before|after>_<내용>.png` 예) `G1_before_stats.png`, `G1_after_stats.png`.
- 개선이 없거나 저하된 경우: **무엇을 바꿨고 → 어떤 지표가 어떻게 바뀌었고 → 왜 그렇게 되었는지(추정이면 추정이라고)** 한 문단으로 기록한다.

## 6. 끝난 뒤 확인
- 기능 회귀: 좀비 추적·공격·사망, 폭탄/총/검 공격, 박스 파괴, 미니맵 표시, 체력/스태미너/탄약 UI.
- 콘솔에 에러/경고가 없는지, `git diff ProjectSettings`에 의도하지 않은 변경이 없는지 확인.
