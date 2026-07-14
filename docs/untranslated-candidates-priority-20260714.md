# 우선 미번역 후보 추출 결과 (2026-07-14)

## 산출물

- 전체 후보: `docs/untranslated-candidates-priority-20260714.tsv`
- 재현 스크립트: `tools/experimental/Extract-UntranslatedCandidates.ps1`
- 입력 데이터: 현재 설치된 게임의 `StreamingAssets/BuildInStorage/data/*.bytes`
- 대조 사전: 패키지의 `translations/*.txt`

TSV 열은 `file`, `status`, `count`, `byteOffsets`, `text` 순서다. 줄바꿈, 탭, 끝 공백은 각각 `\r`, `\n`, `\t`, `\s`로 기록했다.

## 추출 결과

| 파일 | REVIEW | SINGLE | INTERNAL | TEST | 합계 |
| --- | ---: | ---: | ---: | ---: | ---: |
| `areas.bytes` | 2 | 0 | 0 | 0 | 2 |
| `prospects.bytes` | 40 | 0 | 3 | 6 | 49 |
| `bargains.bytes` | 82 | 12 | 6 | 7 | 107 |
| `exchanges.bytes` | 133 | 4 | 0 | 4 | 141 |
| 합계 | 257 | 16 | 9 | 17 | 299 |

상태 의미:

- `REVIEW`: 문장 또는 복수 단어 제목으로, 화면 노출 가능성이 높은 후보
- `SINGLE`: 한 단어 이름·태그로, 표시명인지 내부 값인지 문맥 확인 필요
- `INTERNAL`: 제작자명 또는 코드형 식별자로 판단되는 값
- `TEST`: 테스트·재사용 표식이 명확한 값

## 우선 확인할 내용

### `areas.bytes`

```text
Pretty mansions of stone and glass rise above the verdant gardens, while below, machines whirr and groan. A persistent sound of hammering pounds through the air.\s\s
The head offices of a lumber concern that imports bronzewood from the Reach.
```

첫 문장은 끝 공백 두 칸 때문에 기존 키와 정확히 일치하지 않는 후보로 보인다. 두 번째 문장은 현재 사전에 정확한 원문 키가 없는지 확인할 가치가 있다.

### `prospects.bytes`

사업 도입부와 배달 결과 문장이 다수 포함돼 있다. 대표 후보:

```text
A fuel filled ending!
Your Devil wants entertainments during his secondment.
Your Devil has requests for his enterprise at the Avid Horizon.
```

`Reuse`, `testing`, `Contraband Test` 등은 `TEST`로 분리했다. `FailbetterJames`는 `INTERNAL`로 분리했다.

### `bargains.bytes`

거래 제목, 짧은 광고 문구, 상세 설명과 줄임말이 함께 들어 있다. 대표 후보:

```text
Deep in the Achlean marsh lies a garden of beauty and poison...
A 'spirifer' is an unlicensed trader in souls...
An Experimental Horticulturist is selling a mountain of seeds...
Is it tea?
```

`SINGLE`에는 `Avon`, `Brabazon`, `Empyrean`, `Magdalenes`처럼 화면 표시명일 수도 있는 값이 있으므로 자동 번역하지 않는다.

### `exchanges.bytes`

상점명, 상점 소개, 기관차 및 장비 설명이 가장 안정적으로 추출됐다. 대표 후보:

```text
An engine of considerable ferocity.
An engine that'll get the job done.
A splendid engine; a product of visionary engineering.
The Butler's Discretion
The Night Market
```

`Combat Arena` 관련 네 항목은 `TEST`로 분리했다.

## 기존 재검사 문서와 수치가 다른 이유

기존 재검사 문서는 넓은 문자열 필터의 고유 사전 누락 수를 기록했다. 이번 TSV는 사람이 바로 검토할 목록을 만들기 위해 다음 항목을 제외했다.

- 소문자로만 구성된 상품·품질 내부 ID
- 한글이 함께 들어 있는 문자열
- 번역 사전에 정확한 원문 키가 이미 존재하는 문자열
- 길이 접두사와 직렬화 경계가 확인되지 않는 바이너리 조각

따라서 이번 수치는 전체 미번역률이 아니라 우선 검수용 후보 수로 사용한다.

## 번역 적용 결과

2026-07-14 후속 작업에서 `REVIEW` 257개와 `SINGLE` 16개, 총 273개를 번역 사전에 추가했다.

- `translations/bytes_priority_20260714.txt`: `areas` 2개, `prospects` 40개
- `translations/bytes_bargains_20260714.txt`: `bargains` 94개
- `translations/bytes_exchanges_a_20260714.txt`: `exchanges` 전반 69개
- `translations/bytes_exchanges_b_20260714.txt`: `exchanges` 후반 68개

재추출 결과 `REVIEW`와 `SINGLE` 잔여 후보는 0개다. 남은 것은 내부 식별자 9개와 테스트 데이터 17개뿐이며, 런타임 조회 키 손상을 피하기 위해 번역하지 않았다.

통합 검사 결과:

- 대상 원문: 273개
- 번역 행 및 고유 원문: 273개
- 누락·추가·중복·빈 번역: 0개
- HTML 태그, `[dir:...]`, 줄바꿈 및 자리표시자 불일치: 0개
- 한글이 없는 번역값: 0개
