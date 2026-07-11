# 설치기 및 직접 패치 작업

## 현재 구조

- 배포판은 BepInEx에 의존하지 않는다.
- C# 설치기가 게임 데이터를 백업한 뒤 번역 데이터와 정적 UI 에셋을 적용한다.
- 한국어 글꼴과 정적 에셋은 배포 `payload`에 포함한다.
- 설치기는 게임 폴더 자동 탐색에 실패하면 사용자가 경로를 입력할 수 있어야 한다.

## 주요 경로

- 설치기: `tools/SunlessSkiesKoreanInstaller/`
- 배포 자료: `payload/`
- Release 워크플로: `.github/workflows/release.yml`
- 정적 에셋 설명: `docs/static-asset-patcher.md`

## 변경 시 원칙

- 게임 원본은 적용 전에 날짜가 포함된 백업 폴더에 보존한다.
- 정적 에셋의 오브젝트명과 런타임 조회 키를 무차별 치환하지 않는다.
- 글꼴 또는 `resources.assets` 변경은 로딩 정지와 크래시 위험이 있으므로 일반 번역과 분리한다.
- 설치기 옆 `payload` 구조와 GitHub Actions의 패키징 구조를 함께 확인한다.
- 코드 변경 때만 관련 프로젝트 빌드와 좁은 설치 시험을 수행한다.
