# 미번역 최종 검사 기록 (2026-07-14)

## 검사 목적

2026-07-14 작업의 마지막 검사로 현재 설치된 게임 파일과 패키지 번역 사전을 다시 대조했다.

이번 검사는 기존 `m_text` 중심 검사에서 놓칠 수 있는 `LandmarkName`, `LandmarkDescription` 등의 직렬화 문자열까지 확인하기 위해 다음 범위를 함께 점검했다.

- Unity 정적 `level` 에셋
- `resources.assets`
- `sharedassets3.assets`
- IL2CPP `global-metadata.dat`
- 로딩 팁
- `StreamingAssets/BuildInStorage/data/*.bytes`

이번 작업에서는 검사와 기록만 수행했다. 패처 코드, 번역 사전, 패키지 및 실제 게임 파일은 수정하지 않았다.

## 검사 기준

### 기존 UI 필드 검사

현재 `UiAssetPatcher`가 일반적으로 처리하는 필드는 다음과 같다.

```text
m_text
Message
```

로딩 팁은 다음 예외 조건으로 처리한다.

```text
pathId=41666
fieldName=data
```

이 방식은 일반 UI와 로딩 팁에는 유효하지만 지도 및 기타 게임 전용 MonoBehaviour의 표시 필드를 모두 포괄하지 못한다.

### 확장 정적 문자열 검사

최종 검사에서는 `release-static-assets.txt`에 포함된 정적 에셋을 대상으로 Unity 객체 내부의 길이 접두 UTF-8 문자열을 번역 사전과 정확히 대조했다.

이 검사는 기존 패처가 놓친 번역 가능 문자열을 찾는 용도다. 결과에는 표시 문자열뿐 아니라 내부 이름과 조회 키가 포함될 수 있으므로 자동 일괄 적용 결과로 사용하면 안 된다.

## 정적 `level` 에셋 결과

19개 `level` 파일에서 번역 사전과 정확히 일치하지만 현재 영어로 남아 있는 문자열을 확인했다.

- 대상 객체: 585개
- 문자열 치환 후보: 624개

| 에셋 | 대상 객체 | 치환 후보 |
| --- | ---: | ---: |
| `level0` | 1 | 1 |
| `level10` | 24 | 29 |
| `level11` | 21 | 23 |
| `level12` | 29 | 32 |
| `level13` | 29 | 32 |
| `level14` | 22 | 23 |
| `level16` | 112 | 121 |
| `level17` | 24 | 24 |
| `level20` | 26 | 31 |
| `level21` | 41 | 46 |
| `level22` | 29 | 32 |
| `level23` | 27 | 29 |
| `level24` | 31 | 34 |
| `level3` | 31 | 34 |
| `level4` | 20 | 21 |
| `level5` | 22 | 24 |
| `level6` | 29 | 31 |
| `level7` | 17 | 24 |
| `level9` | 30 | 33 |
| 합계 | 585 | 624 |

### 해석

이 624개는 번역 사전에 없는 문장이 아니다. 번역 사전에는 대응 번역이 있지만 현재 정적 에셋의 해당 문자열 인스턴스에 적용되지 않은 항목이다.

기존 `UiAssetPatcher`는 허용된 일부 필드만 처리하므로 다음과 같은 게임 전용 표시 필드를 놓칠 수 있다.

```text
LandmarkName
LandmarkDescription
```

다른 `level` 파일에도 유사한 이름의 표시 필드가 있을 가능성이 높다.

## 지도 랜드마크 사례

뉴 윈체스터 지도 스크린샷에서 다음 지명이 영어로 표시됐다.

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

`level10`의 주요 객체:

```text
pathId=9469
Base.LandmarkName = Victory Hall
Base.LandmarkDescription = The seat of the Colonial Assembly, which represents the independent settlers of the region.
```

두 문구의 번역은 이미 사전에 있다. 지도 미번역은 `LandmarkName`과 `LandmarkDescription`이 현재 패처의 허용 필드에 포함되지 않아 발생했다.

상세 기록:

```text
docs/screenshot-review-20260714-map-landmarks.md
```

## `resources.assets` 결과

확장 원시 문자열 검사에서는 다음 결과가 나왔다.

- 대상 객체: 586개
- 번역 사전과 일치하는 문자열 인스턴스: 3,396개

이 수치를 실제 미번역 UI 개수로 해석하면 안 된다. `resources.assets`에는 다음 항목이 함께 들어 있을 수 있다.

- 실제 화면 표시 문자열
- 중복 텍스트
- 내부 에셋 이름
- 런타임 조회 키
- 직렬화된 설정 데이터
- 사용되지 않는 개발·테스트 데이터

따라서 3,396개를 원시 문자열 방식으로 전부 치환하는 것은 위험하다. 필드명, 객체 유형 및 Path ID를 기준으로 실제 표시 문자열만 허용해야 한다.

### 로딩 팁

`resources.assets`의 다음 객체에는 로딩 팁 40개가 있다.

```text
pathId=41666
Base.LoadingPageTips.Array.data
```

40개 모두 번역 사전과 일치하며 수정된 `UiAssetPatcher`로 치환할 수 있다. 그러나 현재 설치된 `resources.assets`에는 영어 원문이 남아 있다.

판정:

- 번역 사전 누락: 아님
- 패처 지원: 준비됨
- 현재 설치 파일 적용: 되지 않음

### 일반 UI 후보

일반 `m_text` 검사에서 남은 영문 28개는 대부분 다음과 같은 개발·테스트용 플레이스홀더다.

```text
A label name
Hello World
New Text
TEST
Testing testing
This is placeholder text...
Tip description...
VERSION VERSION
```

실제 게임 화면에 노출된 증거가 없어 현재 번역 대상에서 제외한다.

## `sharedassets3.assets` 결과

확장 정적 문자열 검사 결과:

- 대상 객체: 0개
- 추가 치환 후보: 0개

현재 번역 사전 기준으로 추가 처리할 정적 문자열은 확인되지 않았다.

## IL2CPP 결과

현재 `global-metadata.dat`의 활성 문자열 리터럴을 `metadata_ui_round2.txt`와 다시 대조했다.

결과:

```text
Patched literals: 0
Satisfied translations: 6/28
```

`Patched literals: 0`은 검사 대상 영어 원문이 활성 리터럴로 남아 있지 않다는 뜻이다. 기존 패치로 한국어가 활성화된 항목은 추가 치환되지 않았다.

번역 파일의 28개 키 중 6개만 현재 메타데이터의 활성 문자열로 확인됐다. 나머지는 현재 게임 버전의 활성 리터럴에 없거나 다른 저장 위치·형태를 사용하는 항목으로 본다.

파일 전체에서 영어 바이트가 검색되더라도 패처가 새 한국어 문자열을 추가하고 리터럴 테이블의 참조를 변경한 뒤 남은 비활성 원본 바이트일 수 있다. 따라서 단순 바이너리 검색만으로 활성 미번역이라고 판정하면 안 된다.

## 별도 게임 데이터 결과

`StreamingAssets/BuildInStorage/data/*.bytes`의 직전 재검사 결과를 최종 판정에 함께 반영한다.

| 파일 | 영문 발생 수 | 사전 누락 발생 수 | 고유 누락 수 |
| --- | ---: | ---: | ---: |
| `areas.bytes` | 6 | 3 | 3 |
| `bargains.bytes` | 259 | 251 | 140 |
| `domiciles.bytes` | 2 | 2 | 1 |
| `events.bytes` | 31,590 | 31,148 | 27,752 |
| `exchanges.bytes` | 193 | 154 | 142 |
| `personas.bytes` | 4 | 4 | 3 |
| `prospects.bytes` | 162 | 154 | 52 |
| `qualities.bytes` | 6,894 | 6,894 | 4,695 |
| `settings.bytes` | 15 | 15 | 13 |
| 합계 | 39,125 | 38,625 | 32,801 |

이 수치에는 내부 키, JSON, 사용되지 않는 이벤트 및 문장 조각이 포함된다. 실제 미번역 화면 개수로 사용할 수 없다.

우선 검수 가치가 높은 파일:

1. `prospects.bytes`
2. `bargains.bytes`
3. `exchanges.bytes`
4. `qualities.bytes`
5. `events.bytes`

상세 기록:

```text
docs/untranslated-candidate-reaudit-20260714.md
```

## 최종 원인 분류

### 1. 번역은 있으나 필드가 패처 대상이 아님

대표 사례:

```text
LandmarkName
LandmarkDescription
```

이 유형이 이번 최종 검사에서 가장 중요한 신규 확인 사항이다.

### 2. 번역과 패처는 준비됐으나 현재 설치 파일에 미적용

대표 사례:

```text
resources.assets의 로딩 팁 40개
```

### 3. 실제 번역 사전 누락 후보

대표 범위:

```text
prospects.bytes
bargains.bytes
exchanges.bytes
qualities.bytes
events.bytes
```

### 4. 번역하지 않아도 되는 개발·내부 데이터

대표 사례:

```text
Hello World
TEST
New Text
REUSE
AlgernonBaroque default persona
```

## 권장 수정 순서

1. 정적 에셋의 실제 표시 문자열 필드 목록을 추출한다.
2. `LandmarkName`과 `LandmarkDescription`을 안전한 허용 필드에 추가한다.
3. 다른 `level` 파일의 624개 후보를 필드명별로 분류한다.
4. 내부 조회 키와 `GameObject.m_Name`은 기본적으로 제외한다.
5. 로딩 팁 40개가 번역된 `resources.assets`를 설치 대상에 반영한다.
6. 허용 필드가 확정되면 19개 `level` 파일을 다시 생성한다.
7. 각 정적 파일의 `vcdiff`와 결과 SHA-256을 갱신한다.
8. 설치 후 지도, 지역 접근 메시지, 로딩 화면을 우선 확인한다.
9. 이후 `prospects`, `bargains`, `exchanges`의 실제 미번역을 번역한다.
10. `qualities`와 `events`는 화면 제보 또는 콘텐츠 묶음 단위로 장기 처리한다.

## 인게임 최종 확인 항목

- 지도 상시 지명
- 랜드마크 선택 제목과 설명
- 지역 접근 시 배경 메시지
- 로딩 팁
- 바자 사업과 거래 제목·설명
- 상점 이름과 상점 소개
- 품질 이름과 툴팁
- 한국어 지명 겹침, 줄바꿈 및 잘림
- 대문자·스몰캡 스타일과 한글 글꼴 표시

## 최종 판정

- 가장 중요한 잔여 원인: 정적 에셋 표시 필드가 패처 허용 목록에서 누락됨
- 정적 `level` 치환 후보: 19개 파일, 585개 객체, 624개 문자열
- `resources.assets` 원시 일치 후보: 586개 객체, 3,396개 문자열
- `resources.assets` 일괄 치환 안전성: 안전하지 않음
- 로딩 팁: 40개 번역 준비 완료, 현재 설치본 미적용
- 일반 UI 개발용 후보: 28개
- `sharedassets3.assets` 추가 후보: 0개
- IL2CPP 검사 대상의 추가 활성 영어 치환: 0개
- 별도 게임 데이터의 파일별 고유 사전 누락 후보 합계: 32,801개
- 다음 구현 작업: 표시 필드 분류, 패처 허용 목록 확장, 정적 에셋과 델타 재생성

## 적용 결과 (2026-07-14)

이 문서 작성 뒤 반영된 패키지를 기준으로 다시 검사하고 다음 작업을 적용했다.

### 표시 필드 분류

번역 사전과 일치하지만 기존 허용 목록 밖에 있던 레벨 문자열을 필드명과 Path ID 단위로 다시 추출했다.

| 필드 | 판정 | 처리 |
| --- | --- | --- |
| `AmbientMessage` | 실제 배경 메시지 | 허용 목록 추가 |
| `InteractVerb` | 실제 상호작용 버튼 | 허용 목록 추가 |
| `WarpGateTo` | 이동 대상 내부 키 | 제외 |
| `BazaarItemTags` | 바자 조회 태그 | 제외 |

감사 용도로 `UiAssetPatcher`에 선택적 상세 보고 인자를 추가했다. 기본 실행의 패치 범위에는 영향을 주지 않으며, 허용 목록 밖의 번역 가능 필드와 허용 필드의 미번역 문구를 필드명과 함께 기록한다.

### 새 번역

`translations/static_display_fields_20260714.txt`에 표시 문구 60개를 추가했다.

- 레벨 상호작용 동사: 3개
- `resources.assets`의 배경 메시지·상호작용·랜드마크 표시 문구: 57개

끝 공백이 저장된 원문은 `\s`를 번역값에도 유지했다. 기존 번역과 본문이 같고 끝 공백만 다른 항목은 기존 번역을 재사용했다.

### 의도적 제외

상세 보고에서 다음 30개는 실제 게임 문구가 아닌 개발·테스트 데이터로 확인해 번역하지 않았다.

- 개발·테스트 `m_text`: 28개
- 프리팹 구현 설명 `Message`: 2개

예: `Hello World`, `TEST`, `New Text`, 레이아웃 엔진 설명, prospect 슬롯 구현 메모, Highway Quality 프리팹 메모.

### 정적 에셋 재생성

새 필드 허용과 번역을 적용한 결과 레벨 표시 필드의 미번역 보고는 0건이 됐다. 기존 패키지와 비교해 실제 변경된 레벨은 다음 10개다.

```text
level6
level11
level12
level13
level14
level16
level17
level21
level23
level24
```

레벨에서는 `AmbientMessage`와 `InteractVerb` 15개가 새로 치환됐다.

`resources.assets`에서는 126개 객체의 표시 필드 263개가 치환됐다.

| 필드 | 치환 수 |
| --- | ---: |
| `Message` | 109 |
| `AmbientMessage` | 91 |
| `InteractVerb` | 62 |
| `LandmarkName` | 1 |

로딩 팁은 재실행에서 추가 치환이 발생하지 않아 현재 패키지에 이미 적용된 상태로 판정했다. 문서 앞부분의 “로딩 팁 현재 설치 파일 미적용” 판정은 적용 전 검사 시점의 기록이다.

### 델타와 업그레이드

22개 정적 에셋의 xdelta와 `payload/delta/manifest.json`을 다시 생성하고, 모든 델타를 원본에 적용해 결과 SHA-256이 패키지 파일과 일치하는지 확인했다.

다음 입력에서 현재 패키지로 갱신할 수 있도록 보조 델타를 함께 생성했다.

- 게임 원본
- v1.1.7 설치본
- v1.1.8 설치본
- 기존 메타데이터 패치본

v1.1.7 상태의 현재 게임을 대상으로 설치기 dry-run을 실행한 결과 정적 에셋 19개가 모두 `would patch`로 판정됐고, 지원하지 않는 해시 오류는 발생하지 않았다.

최종적으로 번역 대상 표시 필드의 미번역은 0건이며, 보고서에 남은 영문 30개는 위에서 분류한 개발·테스트 데이터뿐이다.
