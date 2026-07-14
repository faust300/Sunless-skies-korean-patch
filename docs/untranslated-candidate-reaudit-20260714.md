# 미번역 후보 재검사 기록 (2026-07-14)

## 검사 목적

현재 설치된 게임 데이터와 패키지의 번역 사전을 다시 대조하여 영어로 남아 있는 문자열을 다음 두 종류로 구분한다.

1. 번역은 존재하지만 현재 설치 파일에 적용되지 않은 문자열
2. 번역 사전에 원문 키가 없는 미번역 후보

이 수치는 실제 게임 화면에서 확인된 미번역 개수와 같지 않다. 내부 식별자, 개발용 데이터, 사용되지 않는 콘텐츠, JSON 묶음 및 런타임 조합 문자열도 포함될 수 있다.

이번 작업에서는 검사와 기록만 수행했으며 번역 파일, 패처 코드 및 게임 파일은 수정하지 않았다.

## 검사 대상

### Unity 직렬화 에셋

- `Sunless Skies_Data/resources.assets`
- `tools/UiAssetPatcher`
- 패키지의 `translations/*.txt`

### 별도 게임 데이터

경로:

```text
Sunless Skies_Data/StreamingAssets/BuildInStorage/data/*.bytes
```

검사 파일:

- `areas.bytes`
- `bargains.bytes`
- `domiciles.bytes`
- `events.bytes`
- `exchanges.bytes`
- `personas.bytes`
- `prospects.bytes`
- `qualities.bytes`
- `settings.bytes`

현재 게임 데이터에서 62,582개 문자열을 추출했고, 그중 번역 대상 필터를 통과한 문자열은 48,465개였다.

## `resources.assets` 검사 결과

### 로딩 팁

`resources.assets`의 다음 필드에 로딩 팁 40개가 저장돼 있다.

```text
pathId=41666
Base.LoadingPageTips.Array.data
```

패처 재검사 결과 40개 모두 번역 사전과 일치하여 한국어로 치환할 수 있었다. 그러나 현재 설치된 `resources.assets`에는 아직 영어 원문이 남아 있다.

따라서 로딩 팁은 번역 사전 누락이 아니라 **생성된 `resources.assets` 패치가 현재 설치본에 적용되지 않은 상태**로 판정한다.

### 일반 UI 후보

일반 `m_text` 필드에서 번역 사전에 없는 영문 문자열 28개가 다시 발견됐다.

대표 항목:

```text
A label name
Body. It's the body text. With a bod-bod-bod and a bod-bod-bod. Bod.
Dr Random
Hello World
New Text
TEST
Testing testing
This is placeholder text...
Tip description...
VERSION VERSION
```

대부분 개발·테스트용 플레이스홀더다. 실제 화면에 노출된 증거가 없어 현재 번역 대상에서 제외한다.

## `StreamingAssets/*.bytes` 재검사 결과

영문자가 포함되고 한글이 없는 추출 문자열을 번역 사전의 원문 키와 정확히 대조했다.

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

`고유 누락 수` 합계는 파일별 고유 문자열 수를 더한 값이다. 여러 파일에 같은 문자열이 있으면 중복 계산될 수 있다.

## 파일별 판정

### `areas.bytes`

현재 영어로 추출되는 주요 지역 설명:

```text
The old deserted house floating above Port Avon.
Devils occupy the caves beneath this crumbled temple. The air is always slow with the scent of roses. Mirrors are sacred, here.
Pretty mansions of stone and glass rise above the verdant gardens, while below, machines whirr and groan. A persistent sound of hammering pounds through the air.  
A coalition of smugglers, bound by crimson oaths of silence.
```

이 네 문장의 번역은 이미 사전에 존재한다. 현재 영어로 남은 원인은 다음과 같다.

- 최신 `.bytes` 패치가 현재 설치 파일에 적용되지 않음
- 원문 끝 공백 차이로 정확한 키가 일치하지 않음

나머지 `L`, `REUSE`는 내부·개발용 표식으로 판단한다.

### `settings.bytes`, `personas.bytes`, `domiciles.bytes`

다음과 같은 내부 설정명과 기본 데이터가 대부분이다.

```text
AlgernonBaroque default persona
AlgernonBaroque default domicile
AlgernonBaroque default setting
Tutorial persona
```

다만 `settings.bytes`에는 다음 지역·콘텐츠 이름도 포함된다.

```text
Polmear & Plenty's Inconceivable Circus
The Forecourt
Little Hybras
The High Wilderness
The Most Serene Mausoeum
Combat Arena
Achyls
Wit & Vinegar
```

이 값이 화면 표시명인지 런타임 조회 키인지 먼저 확인해야 한다. 확인 없이 일괄 치환하면 데이터 참조가 손상될 수 있다.

### `prospects.bytes`

고유 사전 누락 후보는 52개다. 사업 제목, 사업 설명, 배달 결과 및 실패 결과처럼 실제 플레이 중 표시될 가능성이 높은 평문이 포함돼 있다.

대표 후보:

```text
Revolution and Homesickness: Starshine for Port Prosper
The New Astrology: Starshine to Prosper
The Pages Vault: Illicit Literature to the Empyrean
Quid Pro Quo: Red Honey for the Empyrean
The Prosper Revolution: Illicit Literature to Port Prosper
Your Devil wants entertainments during his secondment.
Your Devil has requests for his enterprise at the Avid Horizon.
```

`New prospect`, `REUSE THIS`, `Test Smuggling Prospect`처럼 개발용으로 보이는 항목도 섞여 있으므로 번역 전 선별이 필요하다.

### `bargains.bytes`

고유 사전 누락 후보는 140개다. 거래 제목, 거래 요약 및 상세 설명이 다수 포함돼 있어 우선 번역 가치가 높다.

대표 후보:

```text
Deep in the Achlean marsh lies a garden of beauty and poison...
Prisoner's dilemmas
A burglar's bargain
A spirifer's accident
Langley's upholstery
A Curator's treasure trove
What remains of the Guinevere
Horrors of the heavens
Experimental seeds for a song
The cut-throat tea-trade
```

`New bargain`, `Another bargain for testing`, `Run of the mill bargareeno` 같은 테스트 데이터는 제외할 수 있다.

### `exchanges.bytes`

고유 사전 누락 후보는 142개다. 상점명, 상점 소개, 기관차 설명 및 장비점 문구가 포함된다.

대표 후보:

```text
An engine of considerable ferocity.
An engine that'll get the job done.
A splendid engine; a product of visionary engineering.
Fast as Hell's chargers.
The Butler's Discretion
The Night Market
Daedalian Engineering
Nightingale Engine Yard
The Blue Heaven Arsenal
Illuminated Engineering
```

`Combat Arena`와 내부 전투 시험용 교환소처럼 개발용 데이터도 있으므로 실사용 후보와 분리해야 한다.

### `qualities.bytes`

고유 사전 누락 후보는 4,695개다. 품질 이름, 단계별 상태 문구, 툴팁 및 JSON 상태값이 포함될 수 있다.

전체를 자동 번역하지 말고 다음 순서로 선별한다.

1. 짧은 평문 품질명
2. 화면에 직접 표시되는 설명과 툴팁
3. 단계별 상태 문구
4. JSON 및 내부 데이터

### `events.bytes`

고유 사전 누락 후보는 27,752개로 가장 많다. 이벤트 제목, 본문, 선택지, 결과, 상태별 문장 묶음이 포함된다.

이 수치를 실제 미번역 문장 수로 사용하면 안 된다. 지역, 인물 또는 이벤트 ID 단위로 나누어 검수해야 한다.

## 이전 검사와의 차이

이전 기록의 파일별 고유 후보 합계는 32,833개였고 이번 재검사는 32,801개다.

이번 검사는 현재 게임 파일에서 다시 문자열을 추출하고 현재 패키지 번역 사전과 재대조했다. 데이터 적용 상태, 추출 대상 또는 번역 키가 이전 검사 이후 달라져 수치가 변한 것으로 본다.

수치 감소만으로 실제 번역 32개가 완료됐다고 단정할 수는 없다. 문자열 변형, 파일 간 중복 및 필터 결과 변화도 영향을 준다.

## 권장 처리 순서

1. 로딩 팁 40개가 번역된 `resources.assets`를 현재 게임에 적용한다.
2. `areas.bytes`의 끝 공백 변형을 보완하고 최신 데이터 패치를 적용한다.
3. `prospects.bytes`의 고유 후보 52개를 전수 분류·번역한다.
4. `bargains.bytes`의 고유 후보 140개를 전수 분류·번역한다.
5. `exchanges.bytes`의 고유 후보 142개를 전수 분류·번역한다.
6. `settings`, `personas`, `domiciles`의 표시 문자열과 내부 키를 구분한다.
7. `qualities.bytes`에서 실제 화면 노출 평문을 우선 선별한다.
8. `events.bytes`는 지역·인물·이벤트 묶음 단위로 장기 검수한다.
9. 인게임에서 로딩 화면, 바자, 사업, 거래, 상점 및 품질 툴팁을 집중 확인한다.

## 최종 판정

- 로딩 팁 번역 사전: 40개 모두 준비됨
- 현재 설치된 로딩 팁: 영어 원문 잔존
- 일반 정적 UI 실사용 미번역: 추가 확인 없음
- 일반 정적 UI 개발용 후보: 28개
- 별도 게임 데이터 영문 발생 수: 39,125개
- 번역 사전 누락 발생 수: 38,625개
- 파일별 고유 사전 누락 후보 합계: 32,801개
- 즉시 검수 가치가 높은 범위: `prospects`, `bargains`, `exchanges`
- 대규모 장기 검수 범위: `qualities`, `events`

