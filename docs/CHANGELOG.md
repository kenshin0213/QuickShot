# 산출물 변경 이력 (CHANGELOG)

QuickShot 프로젝트의 공식 문서 및 산출물 작성 이력입니다.

---

## 2026-09-18

* [MANUAL.md](file:///d:/Workspace/QuickShot/docs/MANUAL.md): QuickShot 정식 종합 상세 사용자 설명서 (제1장 개요/아키텍처부터 제8장 4대 다중 모니터 실전 활용 팁 및 FAQ까지 완비).
* [QuickGuide.html](file:///d:/Workspace/QuickShot/docs/QuickGuide.html): 어린이도 바로 쓰는 ELI5 4단계 시각 카드 반응형 HTML 퀵스타트 가이드 (4대 캡처 모드 및 모든 화면 지원 동기화).
* [publish/QuickGuide.html](file:///d:/Workspace/QuickShot/publish/QuickGuide.html): 배포 바이너리와 함께 제공되는 독립형 퀵가이드 HTML 동기화.
* **기능 업데이트 (v1.1)**: Windows 11 스타일 상단 플로팅 캡처 바(직사각형, 창, 전체화면), DWM 기반 스마트 1:1 창 감지 및 원클릭 캡처, 직전 캡처 모드 자동 기억, 편집 중 `Ctrl+C` 주석 수정본 클립보드 복사(창 유지 옵션 B) 추가.
* **결함 해결 및 기능 확장 (v1.2)**: 4개 다중 모니터 고DPI 환경에서 화면 확대/밀림 원천 해결(마우스 커서 위치 활성 모니터 1:1 정합), 상단 캡처 바 활성 모니터 상단 중앙 노출, 상단 바 및 트레이에 '모든 화면'(4개 모니터 통째로) 전체 캡처 기능 추가.
* **상세 매뉴얼 전면 개편 (v1.3)**: 4대 모니터 1:1 정합 및 DWM 창 감지 기술 원리, 4대 캡처 모드(직사각형/창/현재화면/모든화면), 주석 도구 5종(텍스트 12~40pt, 깍두기 모자이크, 화살표, 사각형, 형광펜), 클립보드 2단계 워크플로우(`Ctrl+C` 중간복사 & `Enter` 자동보관), `settings.json` 전체 필드 명세, 조작 치트시트 및 FAQ 총 8개 장으로 구성된 전문 매뉴얼 구축 완료.

## 2026-10-28

* **결함 해결 (v1.4)**: 해상도 배율(125%, 150% 등)이 적용된 모니터에서 캡처 시 화면이 밀리거나 확대되어 앞으로 튀어나오는 왜곡 현상 완전 해결.
  * `VirtualCanvasWindow` 생성 시 Win32 `SetWindowPos`를 `OnSourceInitialized`에서 직접 호출하여 대상 모니터의 물리 픽셀 좌표계(`PhysicalLeft, PhysicalTop, PhysicalWidth, PhysicalHeight`)로 1:1 강제 밀착.
  * `BackgroundImage`의 고정 크기를 해제하고 `Stretch="Fill"` 및 양방향 Stretch 정렬로 캔버스 1:1 무왜곡 렌더링 보장.
  * `SelectWholeScreen()` 및 `BakeCaptureOutput()`의 물리 해상도 정합성 강화.

