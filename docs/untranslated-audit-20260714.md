# 미번역 잔여 검사 기록 (2026-07-14)

## 검사 목적

현재 설치된 게임과 최신 번역 사전을 다시 대조해 아직 영어로 표시될 수 있는 UI, 지역 접근 메시지, 로딩 팁 및 서사 데이터의 잔여 범위를 확인한다.

이번 작업은 검사와 기록만 수행했으며 게임 파일, 번역 사전 및 패처 코드는 수정하지 않았다.

## 현재 설치 상태

설치기 드라이런 결과:

- 번역 사전 키: 7,410개
- 모든 레벨 및 정적 에셋 델타: 설치 완료
- IL2CPP `global-metadata.dat` 델타: 설치 완료
- 글꼴 에셋: 설치 완료
- 게임 설치 경로의 `StreamingAssets/*.bytes`: 추가 치환 0개
- LocalLow 캐시의 `storage/data/*.bytes`: 추가 치환 0개
- 전체 추가 치환: 0개

따라서 현재 설치된 게임은 저장소가 제공하는 최신 델타와 번역 사전 기준으로 최신 상태다.

## 지역 접근 배경 메시지

이전에 제보된 대표 문구:

```text
Magdalene's - a place of peace, contemplation and rest. The Driver twitches miserably.
```

확인 결과:

- 원본 위치: `Sunless Skies_Data/level13`
- 번역 키: `translations/eueeeeeeeek_legacy_missing_20260711.txt`
- 현재 `level13` 델타: 설치 완료
- 현재 활성 에셋에서 위 영어 원문: 확인되지 않음

지역 접근 메시지는 최신 레벨 델타에 포함된 것으로 판정한다. 이후 영어 문구가 다시 보이면 정확한 화면과 지역·장교 조합을 기준으로 별도 키를 추적한다.

## IL2CPP UI 문자열

다음 주요 UI 후보는 현재 활성 IL2CPP 문자열 리터럴에서 영어 원문이 제거되고 한국어가 존재하는 것을 확인했다.

```text
Are you sure that you want to permanently delete this save file?
Choose an ambition.
That item does not fit in this equipment slot.
LaunchScout error: You do not have enough supplies!
```

활성 한국어 문자열:

```text
이 저장 파일을 영구적으로 삭제하시겠습니까?
야망을 선택하세요.
이 물품은 해당 장비 슬롯에 장착할 수 없습니다.
정찰대를 보낼 수 없습니다. 보급품이 부족합니다!
```

따라서 기존 IL2CPP UI 후보는 현재 패치에 정상 반영된 것으로 판정한다.

## 로딩 팁 미번역

### 저장 위치

로딩 화면에 표시되는 팁은 `resources.assets`의 다음 MonoBehaviour에 저장돼 있다.

```text
pathId=41666
Base.LoadingPageTips.Array.data
```

총 40개의 로딩 팁 문자열이 들어 있다.

### 현재 패처가 놓치는 이유

`tools/UiAssetPatcher/Program.cs`는 현재 문자열 필드 중 이름이 다음과 정확히 일치하는 필드만 처리한다.

```text
m_text
Message
```

로딩 팁은 `LoadingPageTips.Array.data`에 들어 있어 현재 선택 조건에 포함되지 않는다. 그 결과 번역 사전에 문장이 존재해도 `resources.assets`에서는 영어로 남는다.

### 번역 사전 대조 결과

- 로딩 팁 전체: 40개
- 번역 사전과 정확히 일치: 37개
- 정확히 일치하지 않음: 3개

정확히 일치하지 않는 세 문장은 원문 끝에 공백이 있다.

```text
On long journeys, activate your locomotive's cruise control. 
Locomotives with white running lights are not hostile. 
Discovering a new port for the first time will reduce your Terror. 
```

XUnity 형식의 원문 키에 끝 공백을 보존하려면 `\s` 변형을 추가해야 한다.

### 로딩 팁 목록

```text
Sending out your scout will cost Supplies.
When you are overheated, you cannot dodge.
When you are overheated, firing your weapons will damage your engine.
The more Crew you have, the more Supplies you consume.
Visit the Bazaar at Stations to find Prospects and Bargains, and make your fortune!
Fuel Efficiency affects how long your Fuel lasts. Some locomotives are more efficient than others.
The weight of your cargo affects Fuel Efficiency. The more you carry, the more Fuel you'll need.
Look out for Horrors, which cause Terror to rise more quickly, and Wonders, which reduce it.
In order to interact with certain discoveries, your locomotive will need appropriate equipment.
Don't let Terror reach 100.
Many enemies react to your headlight. Turning it off will make you less noticeable, but causes Terror to increase quickly.
Shining your headlight on a vessel or a creature will reveal its name.
If your headlight can pass over or under an object, so can you.
In Eleutheria, some enemies will attack you if your headlight is on. Others will attack you if it is off.
On long journeys, activate your locomotive's cruise control. 
Don't forget to dodge! Dodging is crucial to survival in combat.
Locomotives with white running lights are not hostile. 
Once every 15 minutes, visiting a region's hub port will reduce your Terror.
Discovering a new port for the first time will reduce your Terror. 
Enemies won't pursue you forever. If you are outmatched, try running away.
Experience gained from discovering landmarks is passed on to all your future captains.
Your Affiliations can unlock new Bargains and Prospects. They also determine which items your next captain inherits.
A glowing arrow on your character portrait means you are ready to level up.
If you run out of Fuel, you may be able to burn something else.
Keep your locomotive well crewed, or face difficult choices about which parts of it to maintain.
When planning a journey, check your chart for Stations that sell Supplies and Fuel. Not all supply both.
The Reach's sun - the Garden-King - died long ago. Its domains have run wild.
Londoners first arrived in the heavens through the Avid Horizon. Look for it in Albion.
When London came to the heavens it conquered Albion's Sun - the King of Hours - and built a clockwork replacement.
The sun of Eleutheria has turned against its peers and plunged its domains into lawless night.
The sun of the Blue Kingdom reigns over the domains of the dead in splendour and majesty.
In the Blue Kingdom you may encounter the Convictions of the Sapphire'd King: rays of thought from the sun's own brow.
Eleutheria's celestial library was sundered long ago. If you raid its remains, beware of Scrive-Spinsters.
You can use Otherworldly Artefacts to open Vaults in the Blue Kingdom.
Enemies that ram can be persuaded to crash into terrain, or even other enemies.
Chorister-Bees will always attack if you are carrying chorister-nectar.
More advanced engines enable Full Steam Mode, allowing you to go faster at an extra cost in fuel.
You can purchase faster engines at certain major ports.
Your locomotive has a horn. You may toot it. It does nothing.
Your progress is saved whenever you dock or undock from a station, platform or relay.
```

## 번역 오탈자

현재 번역 사전에 다음 조사 오류가 남아 있다.

```text
Sending out your scout will cost Supplies.=정찰대을 내보내려면 보급품이 듭니다.
```

권장 수정:

```text
Sending out your scout will cost Supplies.=정찰대를 내보내려면 보급품이 듭니다.
```

## 정적 UI 검사

`resources.assets`의 일반 `m_text` 필드 검사에서 남은 영문 28개는 다음 종류의 개발·테스트 플레이스홀더다.

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

현재 실제 게임 화면에 노출된 증거가 없어 번역 대상에서 제외한다.

## `StreamingAssets/*.bytes` 잔여 후보

현재 번역 사전 기준으로 영어가 포함되고 정확한 원문 키가 없는 후보는 다음과 같다.

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

이 수치에는 JSON 묶음, 동적 문장 조각, 내부 식별자, 중복 변형 및 사용되지 않는 이벤트가 포함된다. 실제 화면 미번역 개수나 번역률로 직접 사용하면 안 된다.

다만 `events.bytes`와 `qualities.bytes`에 아직 번역되지 않은 서사·상태 설명이 대량으로 남아 있는 것은 확실하다.

## 권장 수정 순서

1. `UiAssetPatcher`에서 `pathId=41666`의 `LoadingPageTips.Array.data`만 명시적으로 번역 대상으로 허용한다.
2. 모든 문자열 배열을 일괄 치환하지 않는다. 오브젝트명과 런타임 조회 키 손상을 방지한다.
3. 끝 공백이 있는 로딩 팁 3개에 `\s` 원문 변형을 추가한다.
4. `정찰대을`을 `정찰대를`로 수정한다.
5. 패치한 `resources.assets`를 임시 출력으로 생성한다.
6. 40개 영어 로딩 팁이 모두 사라지고 한국어 40개가 존재하는지 검사한다.
7. 게임을 실행해 여러 차례 화면을 전환하며 로딩 팁 표시, 글꼴, 줄바꿈과 잘림을 확인한다.
8. 이후 `areas`, `settings`, `prospects`, `bargains` 순으로 작은 데이터부터 수동 검수한다.

## 최종 판정

- 최신 패치 설치 여부: 정상
- 지역 접근 배경 메시지: 최신 델타 반영 확인
- IL2CPP 주요 UI 후보: 한국어 활성 문자열 확인
- 일반 정적 UI의 실사용 미번역: 추가 발견 없음
- 로딩 팁: 40개 모두 현재 패처에서 누락
- 로딩 팁 번역 사전 보유: 37개
- 끝 공백 키 변형 필요: 3개
- 즉시 수정할 번역 오탈자: 1개
- 장기 검수 대상: `events.bytes`, `qualities.bytes` 중심의 서사 데이터

## 적용 결과

2026-07-14 후속 작업에서 다음 항목을 적용했다.

- `UiAssetPatcher`가 `pathId=41666`의 `LoadingPageTips.Array.data`만 추가로 처리하도록 제한했다.
- 로딩 팁 40개를 모두 한국어로 치환한 `resources.assets`를 생성했다.
- 끝 공백이 있는 로딩 팁 3개에 `\s` 원문 키 변형을 추가했다.
- `정찰대을`을 `정찰대를`로 수정했다.
- `resources.assets`를 Release 정적 에셋 및 바이너리 델타 대상에 추가했다.
- xdelta 복원 결과가 payload의 SHA-256과 일치하는 것을 확인했다.

설치기 드라이런에서는 `resources.assets`가 `would patch`로 정상 판정됐다. 이후 현재 설치본의 이전 패치 버전 `global-metadata.dat`가 새 델타의 원본 또는 결과 해시와 일치하지 않아 전체 드라이런은 중단됐다. 이는 로딩 팁 델타와 별개인 기존 패치 버전 간 메타데이터 업그레이드 호환 문제다.
