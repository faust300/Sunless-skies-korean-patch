# 설치기 및 직접 패치 작업

## 현재 구조

- 배포판은 BepInEx에 의존하지 않는다.
- 사용자용 설치 진입점은 `SunlessSkiesKoreanInstaller.exe` 하나만 제공한다. 구형 배치 파일과 PowerShell 설치기는 배포하지 않는다.
- C# 설치기가 게임 데이터를 백업한 뒤 번역 데이터와 정적 UI 에셋을 적용한다.
- 한국어 글꼴과 정적 에셋은 배포 `payload`에 포함한다.
- 설치기는 게임 폴더 자동 탐색에 실패하면 사용자가 경로를 입력할 수 있어야 한다.

## 주요 경로

- 설치기: `tools/SunlessSkiesKoreanInstaller/`
- 정적 UI 패처: `tools/UiAssetPatcher/`
- IL2CPP 문자열 패처: `tools/GlobalMetadataPatcher/`
- 조사 및 실험 도구: `tools/experimental/`
- 번역 사전: `translations/`
- 배포 자료: `payload/`
- Release 정적 파일 목록: `payload/release-static-assets.txt`
- Release 워크플로: `.github/workflows/release.yml`
- 정적 에셋 설명: `docs/static-asset-patcher.md`
- Release 용량 분석: `docs/release-size-optimization.md`

## 변경 시 원칙

- 게임 원본은 적용 전에 날짜가 포함된 백업 폴더에 보존한다.
- 정적 에셋의 오브젝트명과 런타임 조회 키를 무차별 치환하지 않는다.
- 글꼴 또는 `resources.assets` 변경은 로딩 정지와 크래시 위험이 있으므로 일반 번역과 분리한다.
- 설치기 옆 `payload` 구조와 GitHub Actions의 패키징 구조를 함께 확인한다.
- 원본과 SHA-256이 같은 파일은 `release-static-assets.txt`에 넣지 않는다.
- 코드 변경 때만 관련 프로젝트 빌드와 좁은 설치 시험을 수행한다.

## 설치 흐름

모든 파일 변경은 다음 순서를 지킨다.

1. 대상 파일과 입력 자료를 확인한다.
2. 대상과 같은 볼륨에 임시 결과 파일을 만든다.
3. 예상 해시가 있는 입력은 임시 결과의 SHA-256과 비교한다.
4. 기존 대상 파일을 날짜가 포함된 백업 폴더에 복사한다.
5. 검증된 임시 파일만 대상 위치로 교체한다.
6. 실패 여부와 관계없이 남은 임시 파일을 제거한다.

현재 완성 파일 복사와 번역 데이터 패치는 임시 결과의 SHA-256까지 검증한다. `resources.assets` 직접 패치도 임시 파일, 백업, 교체, 정리 흐름을 공통으로 사용한다. 향후 바이너리 델타는 매니페스트의 결과 SHA-256 검증까지 같은 흐름에 연결한다.

## 용량 최적화 진행 순서

1. [완료] `tools/experimental/MeasureBinaryDeltas.ps1`로 `xdelta3`와 `bsdiff`의 파일별 결과 크기와 복원 해시를 측정한다.
2. [완료] 지원 게임 버전의 원본 SHA-256과 패치 결과 SHA-256을 `payload/delta/manifest.json`으로 만든다.
3. [완료] 설치기에 매니페스트 조회와 지원하지 않는 버전 오류를 추가한다.
4. [완료] 원본을 직접 수정하지 않고 임시 파일에 델타를 적용한 뒤 결과 해시를 검증한다.
5. [완료] Release 워크플로가 전체 Unity 파일 대신 델타와 매니페스트를 패키징하도록 바꾼다.
6. 깨끗한 Steam 설치본에서 설치, 재설치, 실패 복구, 게임 실행을 확인한다.

델타 매니페스트 형식 2는 파일별 `sources` 목록으로 구버전 패치 결과에서 최신 결과로 가는 추가 델타를 지원한다. 새 Release에서 기존 패치 파일의 해시가 바뀌면 직전 공개 버전의 결과 해시와 업그레이드 델타를 함께 추가한다. 원본 게임 해시만 지원하면 재설치 사용자가 `Unsupported game version` 오류를 받게 된다.

델타 도구와 지원 버전 해시가 확정되기 전에는 기존 완성 파일 배포를 제거하지 않는다.

현재 선택한 통합 도구는 공식 `xdelta3` 3.2.0 Windows x86-64 빌드다. Release ZIP의 SHA-256은 `af8ef036cb077a48df080c9a8ac1be4a6e7511c32d11f8bec89b6803a9e52576`으로 고정한다.

Release 설치기는 `PublishTrimmed=true`, `TrimMode=full`을 사용한다. JSON 매니페스트는 트리밍 안전한 소스 생성 컨텍스트로 읽으며, `AssetsTools.NET` 경로를 포함한 full-trim 실행 시험을 통과해야 한다. `EnableCompressionInSingleFile`은 최종 ZIP이 더 커지므로 사용하지 않는다.
