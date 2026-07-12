# 직렬화 에셋 및 게임 데이터 미번역 검사 (2026-07-12)

## 검사 목적

IL2CPP `global-metadata.dat` 검사와 별도로 Unity 직렬화 에셋 및 `StreamingAssets/BuildInStorage/data/*.bytes`에 남아 있는 영어 문자열을 확인한다.

원본 게임 파일은 수정하지 않고 임시 출력 파일과 검사 보고서만 생성했다.

## 검사 대상

### Unity 직렬화 에셋

- `resources.assets`
- `level0`부터 `level25`
- 용량이 100KB를 넘는 주요 `sharedassets*.assets`
- `sharedassets0.assets`
- `sharedassets3.assets`부터 `sharedassets24.assets`까지의 주요 파일

검사 도구:

- `tools/UiAssetPatcher/UiAssetPatcher.csproj`
- 실제 게임 파일을 입력으로 사용
- 출력과 보고서는 `_tmp_serialized_audit_20260712`에 생성
- 원본에는 적용하지 않음

### 별도 게임 데이터

- `areas.bytes`
- `bargains.bytes`
- `domiciles.bytes`
- `events.bytes`
- `exchanges.bytes`
- `personas.bytes`
- `prospects.bytes`
- `qualities.bytes`
- `settings.bytes`

경로:

```text
Sunless Skies_Data/StreamingAssets/BuildInStorage/data
```

## Unity 직렬화 에셋 검사 결과

### 번역 사전으로 치환 가능한 현재 영문 UI

현재 설치된 게임 에셋에서 번역 사전과 정확히 일치해 임시 출력에서 치환된 UI 필드는 총 615개다.

| 에셋 | 치환 가능 UI 수 |
| --- | ---: |
| `level0` | 1 |
| `level10` | 16 |
| `level11` | 38 |
| `level12` | 48 |
| `level13` | 47 |
| `level14` | 46 |
| `level17` | 26 |
| `level20` | 34 |
| `level21` | 57 |
| `level22` | 54 |
| `level23` | 48 |
| `level3` | 56 |
| `level6` | 58 |
| `level7` | 37 |
| `level9` | 48 |
| `sharedassets3.assets` | 1 |
| 합계 | 615 |

그 밖의 검사 대상 에셋에서는 치환 가능한 UI 필드가 0개였다.

### 해석

- 위 615개는 번역 사전에 없는 문장이 아니다.
- 번역은 이미 존재하지만 현재 설치된 게임의 직렬화 에셋에는 아직 적용되지 않은 문자열이다.
- 최신 직접 패치를 적용하면 치환될 대상이다.
- 막달렌 병원 접근 대사가 영어로 표시된 현상도 최신 직렬화 에셋 패치가 설치되지 않은 상태와 관련됐을 가능성이 높다.

### 번역 사전에 없는 직렬화 UI 후보

`resources.assets`에서 영문 문자열 28개가 발견됐다. 확인 결과 모두 개발·테스트용 샘플 또는 사용되지 않는 플레이스홀더로 판단된다.

대표 항목:

```text
A label name
A label name:
aaaaaaa
Body. It's the body text. With a bod-bod-bod and a bod-bod-bod. Bod.
Dr Random
Header
Hello World
Label
New Text
TEST
Testing testing
This is headerline description
This is placeholder text...
Tip description...
VERSION VERSION
Warning body, who knows how large this'll eventually become?
```

현재 실제 게임 UI에 노출된 증거가 없으므로 번역 대상에서 제외한다.

### 직렬화 에셋 판정

- 번역 사전 누락으로 판단되는 실사용 UI: 0개
- 개발·테스트용 잔여 문자열: 28개
- 번역은 있으나 현재 게임에 미적용된 UI 필드: 615개
- 필요한 조치: 최신 직접 패치 적용 후 인게임 재확인

## `StreamingAssets/*.bytes` 검사 결과

현재 게임 데이터에서 영어를 포함하며 번역 사전의 원문 키와 정확히 일치하지 않는 문자열 후보를 추출했다.

| 파일 | 후보 발생 수 | 고유 문자열 수 |
| --- | ---: | ---: |
| `areas.bytes` | 3 | 3 |
| `bargains.bytes` | 268 | 156 |
| `domiciles.bytes` | 2 | 1 |
| `events.bytes` | 31,299 | 27,847 |
| `exchanges.bytes` | 154 | 142 |
| `personas.bytes` | 4 | 3 |
| `prospects.bytes` | 162 | 60 |
| `qualities.bytes` | 6,894 | 4,695 |
| `settings.bytes` | 15 | 13 |
| 합계 | 38,801 | 32,833 |

### 수치 해석 시 주의사항

위 수치는 화면에 표시되는 미번역 문장의 정확한 개수가 아니다. 다음 항목도 포함한다.

- JSON 형태로 저장된 상태별 문장 묶음
- 런타임에서 다른 필드와 결합되는 문장 조각
- 같은 문장의 줄바꿈·태그·구두점 변형
- 내부 식별자와 개발용 데이터
- 현재 플레이 경로에서 사용되지 않는 이벤트
- 원문의 일부만 기존 번역 사전 키와 다른 항목
- 직접 패치 데이터에 이미 포함됐으나 사전 키로는 관리되지 않는 항목

따라서 32,833개를 그대로 번역하거나 진행률로 계산하면 안 된다.

### 파일별 판정

#### `areas.bytes`

- 고유 후보 3개
- 규모가 작아 우선 수동 검수에 적합하다.
- 지역명, 지역 설명 및 접근 문구와 연관됐을 가능성이 있다.

#### `settings.bytes`

- 고유 후보 13개
- 메뉴·설정 UI일 가능성이 있어 우선 검수 대상이다.

#### `personas.bytes` 및 `domiciles.bytes`

- 후보 수가 매우 적어 빠르게 전수 확인할 수 있다.

#### `prospects.bytes`, `bargains.bytes`, `exchanges.bytes`

- 거래, 사업, 바자 및 상점 관련 콘텐츠다.
- 반복 노출되는 UI와 실제 게임 진행에 직접 영향을 주므로 이벤트 장문보다 먼저 선별할 가치가 있다.

#### `events.bytes`

- 고유 후보 27,847개로 가장 크다.
- 장문 서사와 선택지, 결과, 상태별 JSON이 대량 포함되어 있다.
- 자동 일괄 번역보다 지역 또는 이벤트 묶음 단위로 검수해야 한다.

#### `qualities.bytes`

- 고유 후보 4,695개다.
- 품질 이름, 상태 설명, 툴팁 및 단계별 JSON 값이 포함될 수 있다.
- 런타임에서 표시되는 평문 값과 JSON 원문을 함께 확인해야 한다.

## 막달렌 병원 접근 문구와의 관계

제보된 문구:

```text
Magdalene's - a place of peace, contemplation and rest. The Driver twitches miserably.
```

이 문구의 번역 키는 이미 `translations/eueeeeeeeek_legacy_missing_20260711.txt`에 존재한다. 따라서 사전 누락이라기보다 다음 가능성이 높다.

1. 최신 직접 패치가 현재 게임 에셋에 적용되지 않았다.
2. 원문이 직렬화 에셋이 아닌 별도 데이터에서 다른 형태로 저장됐다.
3. 런타임에서 지역 설명과 장교 반응을 결합해 완성 문장을 만든다.
4. 기존 설치 데이터가 번역 사전의 정확한 원문 키와 다른 중간 형태다.

직렬화 검사에서 번역 가능하지만 미적용된 UI 필드 615개가 발견된 점을 고려하면, 먼저 최신 패치를 적용한 뒤 같은 위치에서 다시 확인해야 한다.

## 권장 작업 순서

1. 최신 `translations`와 생성된 `payload`를 사용해 직접 패치를 적용한다.
2. 막달렌 병원 접근 대사를 다시 확인한다.
3. `areas.bytes`, `domiciles.bytes`, `personas.bytes`, `settings.bytes` 후보를 전수 수동 검수한다.
4. `prospects.bytes`, `bargains.bytes`, `exchanges.bytes`를 거래 콘텐츠 묶음으로 검수한다.
5. `qualities.bytes`에서 실제 화면 평문과 JSON 상태값을 함께 검수한다.
6. `events.bytes`는 지역·인물·이벤트 ID 단위로 나누어 장기적으로 번역한다.
7. 인게임 스크린샷에서 발견된 문구를 정확한 원문 키와 연결해 우선 반영한다.

## 최종 판정

- Unity 직렬화 에셋의 실사용 UI 번역 사전 누락: 발견되지 않음
- 직렬화 에셋의 최신 번역 미적용: 615개 확인
- 개발·테스트용 잔여 영문: 28개
- 별도 게임 데이터의 미번역 가능성이 높은 고유 후보: 32,833개
- 실제 작업 우선순위: 최신 패치 재적용, 소규모 데이터 전수 검수, 거래·품질 데이터, 장문 이벤트 순

## 반영 결과

- `level0`과 `sharedassets3.assets`를 정적 에셋 생성 및 설치 대상에 추가했다.
- 설치기가 새 에셋을 백업한 뒤 함께 교체하도록 목록을 확장했다.
