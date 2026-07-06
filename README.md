# Sunless Skies Korean Patch

Sunless Skies 한국어 패치입니다.

현재는 초반 튜토리얼, 메뉴, 선장 생성, 일부 뉴 윈체스터 구간을 중심으로 번역되어 있습니다.

## 번역 진행률

수동 검수 번역 기준: **약 3.52%**

- 전체 추출 문자열: 36,755개
- 수동 정적 번역: 1,293개
- 자동번역 캐시는 진행률에 포함하지 않았습니다.

## 설치 방법

1. Sunless Skies를 종료합니다.
2. Release에서 `SunlessSkies-KoreanPatch.zip`을 내려받습니다.
3. 압축을 풉니다.
4. `INSTALL_KOREAN_PATCH.bat`을 더블클릭합니다.
5. 설치가 끝나면 게임을 실행합니다.

설치 프로그램이 게임 폴더를 자동으로 찾지 못하면, 화면에 안내가 나오며 직접 게임 폴더를 입력할 수 있습니다.

기본 Steam 설치 경로는 보통 아래와 같습니다.

```text
C:\Program Files (x86)\Steam\steamapps\common\Sunless Skies
```

## 현재 번역된 주요 범위

- 메인 메뉴 일부
- 일시정지 메뉴 일부
- 튜토리얼 초반부
- 선장 생성 화면
- 야망 선택 화면 일부
- 뉴 윈체스터 도착 및 초반 항구 메뉴 일부
- 일부 지역명, 품질명, UI 공통 문구

## 설치되는 파일

- BepInEx IL2CPP 로더
- XUnity AutoTranslator 플러그인
- 한국어 표시용 AutoTranslator 설정
- 한국어 정적 번역 파일

기존 파일은 게임 폴더 안의 `_backup_korean_patch_YYYYMMDD_HHMMSS` 폴더에 백업됩니다.

## 참고

- 첫 실행은 BepInEx가 파일을 준비하느라 조금 오래 걸릴 수 있습니다.
- 영어가 남아 있다면 아직 번역되지 않은 문장일 가능성이 큽니다.
- 같은 문장이 조각으로 있을 때와 화면에 합쳐져 표시될 때가 달라, 번역이 누락되어 보일 수 있습니다.
- 한글이 깨지면 `BepInEx/config/AutoTranslatorConfig.ini`에서 `FallbackFontTextMeshPro=arialuni_sdf_u2019`인지 확인해 주세요.
