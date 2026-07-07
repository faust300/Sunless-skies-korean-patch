# Sunless Skies Korean Patch

Sunless Skies 한국어 패치입니다.

아무도 안해주길래 AI번역 돌려서 내가 보려고 만듭니다.

현재는 초반 튜토리얼, 메뉴, 선장 생성, 일부 뉴 윈체스터 구간을 중심으로 수동 검수 번역이 적용되어 있습니다.

## 번역 진행률

2026-07-06 기준, 자동번역 캐시를 제외한 수동 번역 기준입니다.

- 고유 문장 기준: **6.78%** (`2,492 / 36,755`)
- 실제 등장 횟수 기준: **14.28%** (`7,494 / 52,476`)
- 수동 번역 키: `2,867`
- 런타임 보정/변형 키: `376`

## 설치 방법

1. Sunless Skies를 종료합니다.
2. Release에서 `SunlessSkies-KoreanPatch.zip`을 내려받습니다.
3. 압축을 풉니다.
4. `INSTALL_KOREAN_PATCH.bat`을 더블클릭합니다.
5. 설치가 끝나면 게임을 다시 실행합니다.

설치 프로그램이 게임 폴더를 자동으로 찾지 못하면 안내에 따라 직접 게임 폴더를 입력할 수 있습니다.

기본 Steam 설치 경로는 보통 아래와 같습니다.

```text
C:\Program Files (x86)\Steam\steamapps\common\Sunless Skies
```

## 현재 주요 번역 범위

- 메인 메뉴 일부
- 설정/일시정지 메뉴 일부
- 선장 생성 일부
- 초반 튜토리얼 일부
- 뉴 윈체스터 초반 이야기 일부
- 약속의 날들 및 거래 튜토리얼 일부

## 참고

- 자동번역은 배포 패키지에서 꺼져 있습니다.
- 수동 번역이 없는 문장은 영어로 표시될 수 있습니다.
- 한글이 깨지면 `BepInEx/config/AutoTranslatorConfig.ini`에서 `FallbackFontTextMeshPro=arialuni_sdf_u2019`인지 확인해 주세요.
