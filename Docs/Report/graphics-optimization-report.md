# 3DFPSGame 조명·포스트 프로세싱·최적화 실습 보고서

## 개요

화질은 그대로 두고 렌더링 부담을 줄였다. 최종적으로 GPU 시간은 19%, Batches는 33%, 그림자 대상은 60%, 텍스처 메모리는 10 MB 줄었다. 조명과 포스트 프로세싱 단계는 화질 단계라 성능이 거의 변하지 않았고, 성능 이득은 대부분 최적화 단계(미니맵 그림자 제거 등)에서 나왔다.

- **프로젝트**: 3DFPSGame (Unity 6000.3, URP). 산업 지대 맵(Map_v1)에서 좀비와 싸우는 FPS의 GameScene.
- **실습**: 1) 조명 적용, 2) 포스트 프로세싱 분석, 3) 프로젝트 최적화(CPU·GPU·RAM).
- **목표 기기**: 보급형~중급 PC (`PC_RPAsset` 기준).
- **사용한 AI**: Claude Code. 프로젝트 파일 분석, 변경 제안, 설정·코드 수정, Unity 에디터를 직접 조작한 측정.
- **측정 환경**: i9-14900HX / RTX 4060 Laptop(VRAM 8GB) / RAM 32GB / Windows 11, 에디터 Play 모드, 1920×1080, VSync 끔.

## 측정 방법

| 방법 | 내용 | 용도 |
|---|---|---|
| 좀비 고정 | 좀비 10마리를 원형(반경 5·7·9 m)에 세우고 `timeScale=0`, 시선 고정 | 조명·후처리·GPU 항목의 렌더링 비용만 비교 |
| 한 세션 A/B | 한 Play 세션에서 설정 값만 바꿔 5~10초×3창 측정 | 한 기능의 정확한 비용 |
| ABBA 반복 | 기준–변경–변경–기준 순서로 두 번씩 재서 평균 | 에디터 프레임 시간 드리프트 상쇄 |
| UnityStats / FrameTimingManager | Batches, SetPass, Tris, Shadow Casters, GPU ms를 프레임마다 샘플링 | Game 창 Stats와 같은 소스 |
| ProfilerRecorder | `Update.ScriptRunBehaviourUpdate`, GC Alloc, `Texture.currentTextureMemory` | CPU 스크립트·메모리 |
| 스크린샷 | 시작 위치에서 3방향(시작 지점·컨테이너 앞·좀비 방향), 1920×1080 | 화질 비교 |

모든 측정은 에디터에서 했다. 같은 설정인데도 세션 중 프레임 시간이 7.7 ms에서 10.5 ms까지 계속 올라가서(열·클럭 변동 추정), 0.5 ms 이내의 CPU 프레임 시간 차이는 판단에서 뺐다. 비교는 GPU ms, Batches, SetPass, Shadow Casters, 메모리로 한다. 이 환경은 CPU가 병목(CPU 8~10 ms, GPU 약 2 ms)이라 GPU 이득이 FPS로 바로 이어지지 않는다.

## 핵심 결과

| 단계 | 화질 변화 | 성능 변화 |
|---|---|---|
| 1. 조명 | 보조광 제거로 어두워짐, 라이트맵·AO·프로브로 간접광·접촉부 보강 | 프레임 시간 변화 없음(7.72 → 7.94 ms, 노이즈 범위). 라이트맵 `.exr` 약 32 MB 추가 |
| 2. 포스트 프로세싱 | 변경 없음(효과별 비용만 측정). SSAO를 끄면 접촉 그림자가 사라짐 | SSAO가 GPU 비용의 대부분: 끄면 2.25 → 1.59 ms. 나머지 효과는 ±0.05 ms |
| 3. 최적화 | 변화 없음(육안) | GPU 2.24 → 1.81 ms, Batches 643 → 433, Shadow Casters 745 → 297, 텍스처 메모리 109.3 → 99.6 MB |

---

## 1. 조명 적용

Light A(보조광)를 정리하고 Mixed·Baked 조명을 얹었다. 조명 전에는 라이트맵·Light Probe·Reflection Probe가 없었고, `Map_v1` 안의 Directional Light와 Environment의 Directional Light, 총 2개가 켜져 있었다. 화질 개선이 목적이어서 성능이 좋아지지는 않았다.

### 적용한 기술

| 기능 | 적용 내용 | 역할 |
|---|---|---|
| Directional Light 정리 | `Map_v1` 안 Light A(Mixed, 강도 1.5, 흰색) 비활성, `RenderSettings.sun`에 Light B 명시 | 메인 라이트를 하나로 고정 |
| Mixed Light | Light B(강도 2.4, 따뜻한 색)를 Realtime → Mixed, Distance Shadowmask | 가까운 그림자는 실시간, 먼 그림자는 베이크 |
| Lightmap | 2048 1장, 10 texel/unit, 베이크 AO, Progressive GPU (베이크 17초) | 간접광·접촉 그림자 |
| Light Probe | 50개: 6 m 간격 격자(±12 m) × 높이 1.2 m·3.0 m | 좀비·플레이어에 간접광 전달 |
| Reflection Probe | Baked 2개(24×10×24 m, 40×12×40 m), 128 px, Box Projection | 금속·컨테이너 반사 |

설정 에셋은 `Assets/Settings/GameLightingSettings.lighting`, 프로브는 씬의 `Environment/Probes` 아래에 있다. Map_v1의 정적 오브젝트 402개만 Contribute GI이고, 플레이어·검은 제외했다.

### 화면 비교

플레이어 시작 위치에서 같은 방향을 찍었다(1920×1080, 좀비 정지).

📷 **Light A 제거 전 / 후**: 컨테이너 앞 `04 / 05`, 좀비 방향 `06 / 07`
📷 **라이트맵 베이크 전 / 후**: 컨테이너 앞 `08 / 09`, 좀비 방향 `10 / 11`
📷 **프로브 추가 후 3방향**: `12`(시작 지점) `13`(컨테이너 앞) `14`(좀비 방향)

Light A를 끄자 화면이 눈에 띄게 어두워졌다. 베이크·프로브 후에는 차이가 작았고, 라이트맵 AO로 접촉부가 약간 어두워졌다.

### 개선 결과

| 단계 | 변경 | Frame ms | Batches | SetPass | Shadow casters |
|---|---|---|---|---|---|
| 1-1 | Light A 제거 | 8.17 → 8.03 | 635 → 635 | 93 → 93 | 657 → 657 |
| 1-2 | Mixed + 라이트맵 | 7.72 → 7.77 | 641 → 641 | 90 → 90 | 745 → 745 |
| 1-3 | Light Probe + Reflection Probe | 7.77 → 7.94 | 641 → 643 | 90 → 90 | 745 → 745 |

(1-1과 1-2 이후의 좀비 배치가 달라 Tris·Shadow casters의 절대값은 서로 비교하지 않는다.)

### 분석

- **성능은 그대로, 화질만 변했다.** 세 단계 모두 프레임 시간이 노이즈(±0.3~0.5 ms) 안이었다.
- **Light A 제거는 성능이 아니라 화질 트레이드오프다.** Forward+에서 추가 Directional Light 하나는 비용이 작고, 그림자는 메인 라이트만 드리운다(추정). 그래서 이득 없이 어두워지기만 했다.
- **Distance Shadowmask 효과가 없었다.** 소형 맵이라 대부분의 오브젝트가 그림자 거리(50 m) 안에 있어서, 베이크 그림자로 대체될 "먼 그림자"가 거의 없다(추정).
- **프로브 비용은 작다.** Reflection Probe 2개로 Batches가 2 늘었다.

---

## 2. 포스트 프로세싱 분석

이 프로젝트에는 이미 AoT 스타일 Volume(`AoTVolumeProfile.asset`)과 SSAO가 적용돼 있었다. 새 효과를 추가하는 대신, 효과를 하나씩 켜고 꺼서 **각 효과의 비용과 화질 기여**를 측정했다. 설정은 측정 후 원래 값으로 되돌렸다.

### 현재 효과 구성

| 효과 | 설정 | 역할 |
|---|---|---|
| Tonemapping | ACES | 영화 같은 대비, 밝은 부분 롤오프 |
| Color Adjustments | 대비 +18, 채도 +10 | 색 보정 |
| Bloom | Threshold 1, 강도 0.45, Scatter 0.65, 반복 6, Half 해상도 | 밝은 부분 번짐 |
| Vignette | 기본값 | 시선을 가운데로 |
| Shadows/Midtones/Highlights | 적용 | 색 대비 |
| SSAO (렌더러 기능) | 강도 0.4, 반경 0.3, 풀해상도 | 모서리·바닥 접점의 그늘 |

### 화면 비교

📷 **전체 켬 / SSAO 끔**: 컨테이너 앞 `18 / 19`
📷 **Tonemapping 끔**: `20` (대비가 빠지고 밝고 뿌연 화면)
📷 **Bloom 끔**: `21` (거의 차이 없음)
📷 **Balanced / Performance 세트**: `22 / 23`
📷 **Vignette 끔 / Color Adjustments 끔 / Shadows·Midtones·Highlights 끔**: `24 / 25 / 26` (전체 켬 `18`과 비교, 화질 변화가 작아 육안 비교는 하지 않음)

### 개선 결과: 효과를 켜고 끈 비용

같은 세션, 좀비 정지·시선 고정, GPU Frame Time 기준.

| 구성 | GPU ms | Batches | SetPass | Tris | 화질 변화 |
|---|---|---|---|---|---|
| 전체 켬 (기준) | 2.25 | 640 | 87 | 7.75M | 기준 |
| Bloom 끔 | 2.20 | 623 | 71 | 7.75M | 거의 없음 |
| **SSAO 끔** | **1.59 (−29%)** | **479** | 70 | **5.70M** | 접촉 그림자가 사라져 장면이 평평해짐 |
| Vignette 끔 | 2.25 | 639 | 87 | 7.75M | 가장자리 어두움만 제거 |
| Color Adjustments 끔 | 2.24 | 640 | 87 | 7.75M | 색 보정 제거 |
| Tonemapping(ACES) 끔 | 2.22 | 640 | 87 | 7.75M | 대비 상실, 룩에 가장 큰 영향 |
| Shadows/Midtones/Highlights 끔 | 2.25 | 639 | 87 | 7.75M | (육안 비교 미수행) |
| Balanced 세트 | 2.05 (−9%) | 633 | 81 | 7.75M | (육안 비교 미수행) |
| Performance 세트 | 1.57 (−30%) | 473 | 64 | 5.70M | SSAO 끔과 같은 룩 |

Balanced = Bloom 반복 6→4·Downscale Half→Quarter, SSAO Downsample 켬·Samples Medium→Low. Performance = Balanced의 Bloom + SSAO 끔.

### 분석

- **Volume 효과(ACES, Color, Vignette, SMH)는 사실상 무료다.** 켜고 끈 GPU 차이가 0.03 ms 안쪽이다. URP가 이 효과들을 하나의 포스트 패스로 합쳐 처리하기 때문으로 본다(추정).
- **가장 비싼 것은 SSAO다.** 끄면 GPU가 29% 줄고 Batches가 160 줄었다. 깊이·노멀 프리패스가 같이 빠지기 때문으로 본다(추정). 반대로 접촉 그림자라는 화질 기여가 가장 눈에 띄어서 "룩 vs 성능"의 선택이 된다.
- **Tonemapping은 비용이 거의 없지만 룩에는 가장 영향이 크다.**
- **Balanced 세트는 화질을 크게 안 잃고 GPU −9%** 였다. 이 보고서에서는 세트를 적용하지 않고 현재 구성(SSAO 유지)을 그대로 두었다. 접촉 그림자가 룩에 가장 크게 기여하고, 이 PC에서는 GPU가 줄어도 FPS가 거의 달라지지 않기 때문이다. 다만 더 약한 GPU를 목표로 한다면 SSAO를 끄기 전에 Balanced 세트(GPU −9%)를 먼저 적용하는 것이 화질 손실이 가장 작은 타협점이라고 본다.

---

## 3. 프로젝트 최적화

렌더링 구조를 조사해 비용이 큰 곳만 골라 적용했다. 한 번에 하나씩 바꿔 측정했고, 효과가 없으면 적용하지 않았다.

### 조사로 찾은 병목

| 분류 | 찾은 것 |
|---|---|
| GPU | 미니맵 카메라가 맵 전체를 매 프레임 한 번 더, 그림자·Depth/Opaque Texture까지 켜고 그린다. |
| GPU | 그림자 캐스케이드 4, 소프트 쉐도우 High, Opaque Texture 켬(쓰는 셰이더는 없음). |
| GPU (가설) | 좀비 한 마리가 SkinnedMeshRenderer 14개, 전부 그림자 캐스팅. → 측정해 보니 병목이 아니었다. |
| 렌더링 경로 | 계획 때는 "Deferred"로 읽었지만 실제는 **Forward+**(`m_RenderingMode: 2`). 측정 중 바로잡았다. |
| CPU | 스크립트 Update 합계가 약 0.12 ms로 이미 가볍다. 매 프레임 `SetDestination`, 문자열 생성, `Camera.main`은 있으나 비중이 작다. |
| RAM | 산업 팩 텍스처 28개(Map_v1이 사용, 2048² 위주, 합계 약 44 MB), 플레이어 normal map이 Default+sRGB 타입. 플레이어·검 FBX 메시가 각각 34.6만·30.1만 버텍스(약 103 MB). |

### 적용한 기술과 변경 사항

| 분류 | 항목 | 변경 내용 | 적용 |
|---|---|---|---|
| GPU | 미니맵 카메라 | Render Shadows 끔, Depth/Opaque Texture 끔(RT 512 유지) | 적용 |
| GPU | URP 에셋 | Opaque Texture 끔, 캐스케이드 4→2, 소프트 쉐도우 High→Low | 적용 |
| RAM | 텍스처 4개 | Hangar_v3·v4, Cargo_container_v1, Industrial_pipe_v1 2048→1024 | 적용 |
| RAM | 플레이어 normal 2장 | Texture Type을 Normal map으로, 2048→1024 | 적용 |
| CPU | 좀비 AI·무기 UI·`Camera.main` | 경로 갱신 0.25초 주기, 값 변경 시에만 UI 갱신, 카메라 캐시 | 적용 (이미 코드에 반영) |
| GPU | 좀비 손·팔뚝 그림자 끔 | Batches −61, 프레임 시간 이득 없음 | 미적용 |
| GPU | 미니맵 RT 512→256 | GPU −0.04 ms, 화질 우선 | 미적용 |
| GPU | 그림자 거리 50→35 | Batches가 오히려 증가(643→704) | 미적용 |
| GPU | Forward+ → Deferred | GPU −0.11 ms, 화질 미확인 | 미적용 |
| RAM | Streaming Mipmaps, FBX 임포트 옵션 | 메모리 변화 없음 | 원복 |

### 개선 결과: 항목별 (같은 세션 A/B, 핵심은 ABBA)

기준 = 조명 적용 후 상태 + URP 원본값. 좀비 고정, 시선 고정.

| 항목 | Frame(CPU) ms | GPU ms | Batches | SetPass | Tris | Shadow casters |
|---|---|---|---|---|---|---|
| 미니맵 그림자 끔 (단독) | - | 2.23 → 1.87 | 643 → 473 | 90 → 83 | 7.75M → 5.69M | 745 → 484 |
| **미니맵 합계 (그림자+텍스처)** | **8.63 → 7.93 (−8%)** | 2.24 → 1.82 | 643 → 472 | 90 → 81 | 7.75M → 5.69M | 745 → 484 |
| 캐스케이드 4→2 (단독) | - | 2.26 → 2.06 | 643 → 545 | 90 → 83 | 7.75M → 6.70M | 745 → 467 |
| **URP 에셋 합계 (Opaque+캐스케이드+소프트)** | 8.30 → 8.17 (−1.7%) | 2.24 → 2.05 | 643 → 543 | 90 → 80 | 7.75M → 6.70M | 745 → 467 |

CPU 코드 변경(C1~C3)은 코드를 되돌린 HEAD와 비교했다(좀비가 움직이는 상태 10초×3창).

| 지표 | HEAD | 적용 후 |
|---|---|---|
| 스크립트 Update 합계 | 0.12 ms | 0.13 ms |
| GC Alloc / 프레임 (중앙값) | 16.9 KB | 16.7 KB |

텍스처 메모리(`Texture.currentTextureMemory`, 에디터 값):

| 변경 | 메모리 |
|---|---|
| 기준 | 109.3 MB |
| 텍스처 4개 1024 | 102.3 MB (−7.0) |
| + 플레이어 normal 타입·1024 | **99.6 MB (−9.7)** |

📷 **컨테이너 1024 적용 후**: `27` / **최종 3방향**: `28 29 30`

### 분석

- **가장 큰 효과는 미니맵 그림자였다.** 미니맵 카메라가 메인 카메라와 같은 그림자 패스를 한 번 더 돌고 있었다. 체크박스 몇 개를 끈 것만으로 CPU −0.7 ms, GPU −0.4 ms, Batches −171을 얻었다.
- **캐스케이드 축소로 GPU가 줄었지만 CPU는 그대로다.** Shadow Casters가 745→467, GPU −0.2 ms인데 프레임 시간은 변하지 않았다. 이 PC에서는 CPU가 병목이라서다. 약한 GPU에서는 차이가 클 것으로 추정한다.
- **좀비 그림자는 병목이 아니었다.** 좀비 메시가 작아(14개 합쳐 약 3.5k 버텍스) 드로우콜이 61 줄어도 프레임 시간이 −0.1 ms(노이즈)였고, 팔 그림자만 사라져서 적용하지 않았다.
- **CPU 코드 개선은 측정으로 확인되지 않았다.** 모든 스크립트 Update 합계가 프레임의 약 1.5%라 줄일 몫이 작았다. GC 값은 에디터·샘플러 자체 할당(약 16 KB)이 바닥이라 문자열 몇 개 차이는 가려진다. 코드 품질·안전성 개선이지 성능 증거는 없다.
- **RAM은 텍스처가 맞았지만 예상보다 작았다.** 4개 축소 이론치(약 −14 MB)보다 실측(−7.0 MB)이 작았다. normal map을 올바른 타입으로 바꾸면 압축 포맷이 DXT5nm로 바뀌어 메모리가 오히려 2배(+10.7 MB)가 되어, 1024로 낮춰 상쇄했다(원본 대비 −5.3 MB).
- **Streaming Mipmaps는 이 맵에서 효과가 없었다.** 맵이 작고 예산(512 MB)이 커서 모든 밉이 필요하다고 판단됐다.

---

## 종합 분석과 결론

최종 상태는 처음보다 화질을 거의 건드리지 않으면서 그리는 양(Batches·삼각형·그림자 대상)이 크게 줄었다. 줄어든 비용은 대부분 미니맵 그림자와 그림자 캐스케이드, 그리고 SSAO를 끄지 않은 채 얻은 것이다. 조명 단계와 포스트 프로세싱 단계는 화질을 위한 단계라 비용이 거의 늘지도 줄지도 않았다.

### 단계별 수치 추이 (좀비 정지, 같은 시점, 새 배치 기준)

| 지표 | 조명 정리 후 | 베이크+프로브 후 | 최적화 후 | 변화율 (정리 후→최적화) |
|---|---|---|---|---|
| Batches | 641 | 643 | **433** | −32% |
| SetPass | 90 | 90 | **76** | −16% |
| Tris | 7.75M | 7.75M | **5.16M** | −33% |
| Shadow Casters | 745 | 745 | **297** | −60% |
| GPU ms | - | 2.24 | **1.81** | −19% (베이크 후 기준) |
| Frame(CPU) ms | 7.72 | 7.94 | 7.47 | −3% (드리프트 포함) |
| 텍스처 메모리 | - | 109.3 MB | **99.6 MB** | −9% |

### 측정하며 알게 된 점

- **파일 값만 읽으면 틀린다.** AI가 `m_RenderingMode: 2`를 Deferred로 읽어 "Deferred → Forward+" 실험을 계획했는데, 실제는 이미 Forward+였다. 실행 중 값으로 교차 확인해서 바로잡았다.
- **측정 조건이 흔들리면 비교할 수 없다.** 좀비가 움직이면 같은 설정에서도 Batches가 395~490으로 바뀌었다. 좀비를 고정하고 같은 세션에서 값만 바꿔 비교하자 결과가 안정됐다.
- **에디터 프레임 시간은 믿기 어렵다.** 같은 설정에서도 세션 중에 7.7 → 10.5 ms로 올라갔다. GPU ms·Batches·SetPass는 안정적이었고, 핵심 항목은 ABBA 순서로 상쇄했다.
- **AI의 제안이 항상 측정으로 확인되는 것은 아니다.** 좀비 그림자 정리, CPU 코드 개선, Streaming Mipmaps는 제안은 합리적이었지만 이득이 없었다. 효과가 없는 항목은 적용하지 않거나 원복했다.
- **Light A 제거처럼 성능과 무관하게 룩이 바뀌는 변경도 있다.** 이득 없이 어두워지기만 했다.

### 한계와 미수행

- Development Build 측정, Frame Debugger·Profiler 창 캡처는 하지 못했다(대신 `UnityStats`·`ProfilerRecorder` 수치 사용).
- 환경 개선 전 커밋(`4376841`)과의 화질 비교는 하지 못했다.
- 플레이어 FBX 메시(약 103 MB)는 이번에 건드리지 않았다. 가장 큰 RAM 항목이므로 후속 과제다.

### 느낀 점

가장 크게 남은 것은 "짐작과 측정은 다르다"는 점이다. 처음에는 좀비 한 마리가 부위별 메시 14개로 그려지는 것이 가장 큰 문제라고 생각했고 AI도 같은 후보를 제안했다. 그런데 직접 켜고 꺼 보니 좀비 그림자를 끄면 Batches는 줄어도 프레임 시간은 거의 변하지 않았다. 반대로 별 생각 없이 지나쳤던 미니맵 카메라의 그림자 옵션 하나를 끄는 것만으로 Batches가 171, CPU 시간이 0.7 ms 줄었다. 눈에 띄는 것보다 보이지 않는 곳에서 한 번 더 그리는 것이 비쌌다.

AI의 분석도 그대로 믿으면 안 된다는 것을 겪었다. 렌더링 경로를 Deferred로 잘못 읽은 채 실험 계획을 세웠고, 실행 중 값을 확인하고서야 이미 Forward+였다는 것을 알았다. 코드 최적화(SetDestination 주기, 문자열 생성, `Camera.main` 캐시)는 이론상 맞는 개선이었지만 스크립트 Update 전체가 0.12 ms라 측정으로는 차이가 없었다. 그래도 효과가 없다는 것을 숫자로 확인한 것이 "이론상 좋다"를 믿고 넘어가는 것보다 낫다고 생각한다.

측정 자체도 쉽지 않았다. 에디터 프레임 시간이 같은 설정에서도 7.7 ms에서 10.5 ms까지 올라가서 처음에는 모든 비교가 의미 없어 보였다. 좀비를 고정하고, 같은 세션에서 값만 바꾸고, 순서를 바꿔 두 번씩 재서 평균을 내고 나서야 비교할 수 있었다. 이 환경은 CPU가 병목이라 GPU가 19% 줄어도 FPS는 거의 달라지지 않았다는 점도 아쉬웠다. 더 약한 GPU나 빌드에서 다시 재 보고 싶다.

품질 면에서는 크게 양보한 것이 없다. 그림자 캐스케이드를 4에서 2로 줄여도 스크린샷으로 구분이 안 되었고, 텍스처 4개를 1024로 줄여도 컨테이너 화질이 괜찮았다. 반대로 Light A를 제거했을 때처럼 성능 이득 없이 화면만 어두워지는 변경도 있어서, 최적화와 화질 개선을 구분해서 보는 눈이 생겼다. 후처리는 SSAO가 비용의 대부분이었고 접촉 그림자가 룩에 중요해서, 끄는 대신 Balanced 세트(GPU −9%)를 현실적인 타협점으로 보았다.

다음에는 가장 큰 RAM 항목인 플레이어·검 FBX(합계 약 103 MB)를 손보고, 빌드에서 Frame Debugger와 Profiler 캡처까지 포함해 다시 측정해 보고 싶다.

---

## 부록

### 변경된 파일·에셋

| 단계 | 파일 | 내용 |
|---|---|---|
| 조명 | `Assets/01.Scenes/GameScene.unity` | Light A 비활성, `m_Sun`, Light B Mixed, `Environment/Probes`(Light Probe Group, Reflection Probe 2개) |
| 조명 | `Assets/Settings/GameLightingSettings.lighting` | Lighting Settings(새 파일) |
| 조명 | `Assets/01.Scenes/GameScene/` (`LightingData.asset`, `Lightmap-0_*`, `ReflectionProbe-*`) | 베이크 결과(약 33 MB) |
| 최적화 | `Assets/01.Scenes/GameScene.unity` | `MinimapCamera`의 Render Shadows·Depth·Opaque Texture 끔 |
| 최적화 | `Assets/Settings/PC_RPAsset.asset` | Opaque Texture 끔, 캐스케이드 2, 소프트 쉐도우 Low |
| 최적화 | `Assets/RPG_FPS_game_assets_industrial/.../Hangar_v3·Hangar_v4·Cargo_container_v1·Industrial_pipe_v1` `.meta` | Max Size 1024 |
| 최적화 | `Assets/05.Models/normal.png`, `normal 1.png` `.meta` | Normal map 타입, 1024 |
| 최적화 | `Zombie.cs`, `ZombieHealthBar.cs`, `PlayerZoom.cs`, `UI_PlayerWeapons.cs` | 경로 갱신 주기, 값 변경 시에만 UI 갱신, `Camera.main` 캐시 |

### 측정·스크린샷 원본

- 수치 원본: `Docs/Report/result-table.md`, 스크린샷 번호 대응표: `Docs/Report/img/upload/README.txt`
- 본문의 📷 `NN`은 `img/upload/NN_*.png`의 번호다.
