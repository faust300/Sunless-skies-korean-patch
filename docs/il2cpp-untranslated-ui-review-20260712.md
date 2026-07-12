# IL2CPP 미번역 UI 후보 검수 (2026-07-12)

## 검사 목적

`Sunless Skies_Data/il2cpp_data/Metadata/global-metadata.dat`의 실제 문자열 리터럴을 번역 사전과 다시 대조해, 화면에 노출될 가능성이 있는 미번역 UI 문구를 찾는다.

## 검사 결과 요약

- IL2CPP 문자열 리터럴: 13,497개
- 현재 번역 사전 키: 7,071개
- 번역 사전과 정확히 일치하지 않는 고유 영문 후보: 5,456개
- 대부분은 .NET, Unity, Rewired 및 서드파티 라이브러리의 내부 오류·진단 문자열이다.
- 모든 영문 리터럴을 일괄 번역하지 않고 실제 UI 가능성이 있는 문구만 선별한다.

현재 `payload/metadata_scout.txt`에는 다음 정찰 알림 1개만 등록되어 있다.

```text
<i>Your scout has discovered something.</i>=<i>정찰대가 무언가를 발견했습니다.</i>
```

## 우선 번역 후보

다음 문구는 게임 UI에서 직접 사용될 가능성이 높다.

### 저장 및 타이틀 화면

```text
Are you sure that you want to permanently delete this save file?
```

권장 번역:

```text
이 저장 파일을 영구적으로 삭제하시겠습니까?
```

```text
Are you sure you want to save and
return to the title screen?
```

권장 번역:

```text
저장한 뒤 타이틀 화면으로
돌아가시겠습니까?
```

```text
Delete Save File
```

권장 번역:

```text
저장 파일 삭제
```

```text
<b>Last saved:</b> 
```

권장 번역:

```text
<b>마지막 저장:</b> 
```

### 선장 생성 및 야망

```text
Choose an ambition.
```

권장 번역:

```text
야망을 선택하세요.
```

긴 결합 문장 `Choose an ambition.\n\nWhat does winning mean to you?`는 이미 `translations/captain_creation.txt`에 번역되어 있다. 위 단독 문자열은 별도 런타임 조각일 수 있으므로 따로 확인한다.

```text
Boldly done! You have chosen an Ambition! <strong>{0}</strong>
```

권장 번역:

```text
과감한 선택입니다! 야망을 선택했습니다! <strong>{0}</strong>
```

자리표시자 `{0}`와 `<strong>` 태그를 반드시 보존한다.

### 장비

```text
That item does not fit in this equipment slot.
```

권장 번역:

```text
이 물품은 해당 장비 슬롯에 장착할 수 없습니다.
```

### 정찰 및 보급품

```text
LaunchScout error: You do not have enough supplies!
```

권장 번역:

```text
정찰대를 보낼 수 없습니다. 보급품이 부족합니다!
```

화면에 `LaunchScout error:`까지 실제로 표시되는지 확인한다. 내부 로그 접두사라면 전체 문장 대신 사용자에게 보이는 본문만 처리해야 한다.

### 연료 안내

```text
The more you carry, the faster your engine consumes fuel.
```

권장 번역:

```text
화물을 많이 실을수록 기관차가 연료를 더 빠르게 소모합니다.
```

## 컨트롤러 설정 UI 후보

다음 문자열은 Rewired 내부 진단문이 아니라 실제 컨트롤러 설정 화면에 노출될 가능성이 있다. 키보드·마우스 플레이만으로는 확인하기 어려우므로 게임패드를 연결한 상태에서 검수한다.

```text
No joysticks detected.
Joystick Identification Required
Press any button or axis to assign it to this action.
Select an axis to begin.
First center or zero all sticks and axes and press any button or wait for the timer to finish.
Are you sure you want to remove this assignment?
Do you want to reassign or remove this assignment?
Assignment will be canceled in 
```

권장 번역:

```text
감지된 조이스틱이 없습니다.
조이스틱 식별 필요
이 동작에 할당할 버튼이나 축을 누르십시오.
시작하려면 축을 선택하십시오.
먼저 모든 스틱과 축을 중앙 또는 0 위치에 놓은 뒤 아무 버튼이나 누르거나 타이머가 끝날 때까지 기다리십시오.
이 할당을 제거하시겠습니까?
이 할당을 변경하거나 제거하시겠습니까?
할당이 취소되기까지: 
```

## 실제 UI 여부를 확인할 후보

다음 문구는 게임 기능과 관련 있어 보이지만 개발용·진단용일 가능성도 있다.

```text
Creation footer button Back
Creation footer button Continue
Creation footer button Randomise All
Players local data is from a newer version of the game than the executable they are running!
A thing has happened!
This error should never happen. Please contact support.
```

판정 기준:

- 화면에 문장이 그대로 보이면 번역한다.
- `Creation footer button ...`처럼 오브젝트명 또는 로그 식별자로만 사용되면 번역하지 않는다.
- 오류 상황에서 사용자에게 표시되는 경고라면 번역한다.
- 로그 파일에만 기록되는 진단문이면 제외한다.

## 레거시 또는 제외 후보

다음 문구는 과거 StoryNexus/Fate 시스템 또는 개발 도구의 잔재일 가능성이 높다.

```text
Fate can be used to give you more Actions, Opportunities and other lovely things. Explore the Nex tab above.
Fate... And thank you! It's folk like you who keep the game running.
...and thank you! It's folk like you who keep the game running.
[This is a metaquality! It will appear on your user profile, and may unlock new starting options in other worlds.]
```

현재 게임에서 실제로 노출된 증거가 없으므로 우선 번역 대상에서 제외한다.

## 적용 전 주의사항

- IL2CPP 문자열은 코드에서 결합되어 완성될 수 있으므로 문장 조각을 무차별 치환하지 않는다.
- 줄바꿈, 끝 공백, 태그와 `{0}` 같은 자리표시자를 원문 그대로 보존한다.
- 원문이 같은 번역 사전 키와 값으로 이미 치환된 상태인지 확인한다.
- 저장 및 컨트롤러 UI는 실제 화면에서 문자열 전체가 그대로 사용되는지 확인한다.
- 번역 대상이 확정되면 `payload/metadata_scout.txt`에 추가하고 `GlobalMetadataPatcher`로 반영한다.
- 패치 후 `MetadataValidator`로 메타데이터 구조를 검사하고 게임 실행·저장·불러오기를 확인한다.

## 후속 검사 순서

1. 저장 파일 삭제와 타이틀 화면 복귀 대화상자를 실제로 연다.
2. 선장 생성에서 야망 선택 전후 문구를 확인한다.
3. 맞지 않는 장비를 슬롯에 장착해 오류 문구를 확인한다.
4. 보급품이 부족한 상태에서 정찰대를 보내 본다.
5. 게임패드를 연결해 컨트롤러 할당 화면을 확인한다.
6. 실제 노출이 확인된 정확한 원문만 `metadata_scout.txt`에 추가한다.

## 판정

- IL2CPP 메타데이터에 미번역 UI 후보가 남아 있음: 확인
- 즉시 번역 가치가 높은 범위: 저장, 야망, 장비, 정찰, 연료 안내
- 별도 플레이 조건이 필요한 범위: 컨트롤러 설정 UI
- 우선 제외 범위: .NET·Unity 내부 오류, 개발 로그, StoryNexus/Fate 레거시 문구

## 반영 결과

- 저장 파일 삭제, 저장 후 타이틀 복귀, 마지막 저장 시각 문구를 메타데이터 패치에 추가했다.
- 야망 선택, 장비 슬롯 오류, 정찰 보급품 부족, 화물별 연료 소모 안내를 추가했다.
- 태그, 줄바꿈과 `{0}` 자리표시자는 원문 구조를 보존했다.
- 실제 노출이 확인되지 않은 개발·진단·레거시 문구와 게임패드 전용 후보는 보류했다.
