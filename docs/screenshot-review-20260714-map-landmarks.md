# 지도 랜드마크 미번역 검사 기록 (2026-07-14)

## 제보 화면

파일:

```text
C:/Users/faust/Pictures/Screenshots/스크린샷 2026-07-14 213353.png
```

뉴 윈체스터 주변의 지도 지명과 선택한 랜드마크의 제목·설명이 영어로 표시됐다.

화면에서 확인된 문구:

```text
Alexander-Yang Claim
Riekstead
Company House
Victory Hall
New Winchester
Cantor's Still
Bancroft's Forum
Scamps Narrow
Kisigar Gardens
```

선택한 랜드마크:

```text
VICTORY HALL
The seat of the Colonial Assembly, which represents the independent settlers of the region.
```

## 검사 결과

### 저장 위치

화면에 표시되는 지도 랜드마크 데이터는 다음 에셋에 저장돼 있다.

```text
Sunless Skies_Data/level10
```

`level10` 원시 문자열 검사에서 다음 항목을 확인했다.

| Path ID | 유형 | 내용 |
| ---: | ---: | --- |
| 1501 | 1 | `Victory Hall` |
| 9469 | 114 | `Victory Hall`, 랜드마크 설명 |
| 11754 | 114 | `Victory Hall` |

실제 화면 데이터를 가진 주요 MonoBehaviour:

```text
pathId=9469
Base.LandmarkName = Victory Hall
Base.LandmarkDescription = The seat of the Colonial Assembly, which represents the independent settlers of the region.
```

별도의 `GameObject.m_Name`에도 `Victory Hall`이 존재하지만, 지도 화면에 표시되는 값은 `LandmarkName`과 `LandmarkDescription`이다.

### 번역 사전 상태

스크린샷에 보이는 지명과 설명은 이미 번역 사전에 존재한다.

| 원문 | 현재 번역 |
| --- | --- |
| `Alexander-Yang Claim` | `알렉산더-양 채굴지` |
| `Riekstead` | `릭스테드` |
| `Company House` | `컴퍼니 하우스` |
| `Victory Hall` | `승리의 전당` |
| `New Winchester` | `뉴 윈체스터` |
| `Cantor's Still` | `칸토어의 증류소` |
| `Bancroft's Forum` | `밴크로프트 포럼` |
| `Scamps Narrow` | `스캠프스 내로` |
| `Kisigar Gardens` | `키시가르 정원` |
| `The seat of the Colonial Assembly, which represents the independent settlers of the region.` | `이 지역의 독립 정착민들을 대표하는 식민지 의회의 의석입니다.` |

관련 번역 파일:

```text
translations/tutorial_common.txt
translations/areas_001.txt
translations/screenshot_fixes_20260711.txt
translations/zz_glossary_overrides_20260711.txt
translations/eueeeeeeeek_legacy_missing_20260711.txt
```

따라서 이번 현상은 번역 문장 누락이 아니다.

## 미번역 원인

현재 `tools/UiAssetPatcher/Program.cs`가 일반적으로 번역하는 문자열 필드는 다음 두 가지다.

```text
m_text
Message
```

로딩 팁에 대해서만 별도 예외가 있다.

```text
pathId=41666
fieldName=data
```

지도 랜드마크가 사용하는 다음 필드는 처리 대상이 아니다.

```text
LandmarkName
LandmarkDescription
```

그 결과 다음 문제가 함께 발생한다.

1. 번역 사전에 지명과 설명이 있어도 치환되지 않는다.
2. 미번역 검사 보고서는 현재 `m_text`만 기록하므로 지도 문구를 미번역 후보로 보고하지 않는다.
3. `level10` 패처 실행 결과가 `UI text fields patched: 0`, `Untranslated English UI strings: 0`으로 나오지만 실제 지도에는 영어가 남는다.

즉, 기존 검사 결과의 0건은 지도 랜드마크 필드를 검사하지 않았기 때문에 발생한 거짓 음성이다.

## 패키지 상태

다음 두 파일의 SHA-256은 동일했다.

```text
현재 설치본: Sunless Skies_Data/level10
패키지 파일: payload/Sunless Skies_Data/level10
```

확인된 해시:

```text
49DA4F51B4ADF43C6FCD2EDE4FE951DB9BE14A87EBAC78128A16B4332D2076F0
```

패키지의 `level10` 자체에도 `Victory Hall`과 해당 영문 설명이 남아 있다. 따라서 현재 패키지를 다시 설치하는 것만으로는 이번 문제가 해결되지 않는다.

패키지 파일에는 일부 한국어 지명도 존재하지만, 화면에서 참조하는 `LandmarkName` 및 `LandmarkDescription` 인스턴스가 영어로 남아 있다.

## 권장 수정

### 패처 범위

`UiAssetPatcher`에서 다음 필드를 명시적으로 번역 대상으로 추가한다.

```text
LandmarkName
LandmarkDescription
```

모든 문자열 필드를 무조건 번역하지 않는다. `GameObject.m_Name`, 내부 조회 키 및 에셋 참조 이름까지 바꾸면 런타임 참조가 손상될 수 있다.

지도 표시용 필드만 허용 목록에 추가하는 방식이 안전하다.

### 미번역 보고서

미번역 영문 수집 조건도 `m_text`에만 한정하지 말고 다음 표시 필드를 포함하도록 조정한다.

```text
m_text
LandmarkName
LandmarkDescription
```

`Message`가 실제 표시 필드로 확인되는 에셋에서는 함께 보고할 수 있다.

## 수정 후 생성 대상

패처 수정 후 다음 파일을 다시 생성해야 한다.

```text
payload/Sunless Skies_Data/level10
payload/delta/level10.vcdiff
```

델타 매니페스트나 해시 목록을 별도로 관리하고 있다면 새 `level10`의 원본·결과 SHA-256도 갱신해야 한다.

## 검증 절차

1. 원본 `level10`을 입력으로 수정한 `UiAssetPatcher`를 실행한다.
2. 치환 로그에서 `LandmarkName`과 `LandmarkDescription`을 확인한다.
3. 생성된 `level10`에서 스크린샷의 영문 지명이 사라졌는지 검색한다.
4. 대응하는 한국어 지명과 설명이 존재하는지 검색한다.
5. 새 `level10.vcdiff`를 원본에 적용해 생성 파일과 SHA-256이 일치하는지 확인한다.
6. 설치기를 통해 실제 게임의 `level10`에 적용한다.
7. 뉴 윈체스터 지도를 열어 다음 항목을 육안 검사한다.

   - 지도에 상시 표시되는 지명
   - 랜드마크 선택 시 제목
   - 랜드마크 선택 시 설명
   - 긴 한국어 지명의 겹침과 잘림
   - 대문자 또는 스몰캡 스타일 표시

8. 다른 지역 지도에서도 동일한 필드를 사용하는 랜드마크를 표본 검사한다.

## 최종 판정

- 번역 사전 누락: 아님
- 원문 저장 위치: `Sunless Skies_Data/level10`
- 화면 표시 필드: `LandmarkName`, `LandmarkDescription`
- 직접 확인한 주요 객체: `pathId=9469`
- 기존 패처 처리 여부: 처리하지 않음
- 기존 미번역 보고서 탐지 여부: 탐지하지 않음
- 현재 패키지 재설치만으로 해결 가능: 불가능
- 필요한 조치: 패처 허용 필드 확장, `level10` 및 델타 재생성, 인게임 지도 검사

## 적용 결과 (2026-07-14)

`UiAssetPatcher`의 안전 허용 목록과 미번역 보고 대상에 다음 필드를 추가했다.

```text
LandmarkName
LandmarkDescription
```

26개 `level*` 에셋을 모두 다시 검사했다. 기존 번역을 지도 필드에 적용하고, 끝 공백·줄바꿈 없는 지명처럼 저장 형태가 달랐던 미번역 키 54개를 `translations/map_landmarks_20260714.txt`에 추가했다. 재검사 결과 지도 표시 필드의 미번역 영문 보고는 전 레벨 0건이다.

전체 재생성에서 332개 객체의 표시 문자열 필드 390개가 치환됐으며, 실제 바이너리가 변경된 레벨은 다음 17개다.

```text
level3
level4
level5
level6
level7
level9
level10
level11
level12
level13
level14
level16
level20
level21
level22
level23
level24
```

`level10`에서는 `LandmarkName`과 `LandmarkDescription` 20개가 치환됐다. 제보 화면의 `Victory Hall`은 path ID 9469의 제목과 설명이 각각 `승리의 전당`, `이 지역의 독립 정착민들을 대표하는 식민지 의회의 의석입니다.`로 바뀌었다. 내부 참조용 `GameObject.m_Name`의 영문은 의도적으로 유지했다.

새 `level10` SHA-256:

```text
95684298107731cc2d9ede4906c80df4d569137f3752a1a6ae9ea7760b43fe39
```

`level4`, `level5`, `level16`, `level24`를 `payload/release-static-assets.txt`에 추가했고, 총 22개 정적 에셋의 xdelta와 `payload/delta/manifest.json`을 다시 생성했다. 각 델타는 원본에 역적용해 패키지 대상 파일과 SHA-256이 일치하는지 확인했다. 직전 패치본에서 바로 갱신할 수 있도록 기존에 변경됐던 13개 레벨과 메타데이터의 보조 델타도 포함했다. 직전 패치가 설치된 게임을 대상으로 설치기 dry-run을 실행했을 때 새 17개 레벨이 모두 `would patch`로 판정되고 지원하지 않는 해시 오류는 발생하지 않았다.

남은 검증은 실제 게임에서 지도 지명과 선택 설명의 겹침·잘림을 육안 확인하는 단계다.
