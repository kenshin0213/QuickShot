# QuickShot

Windows 10/11 환경에서 전역 단축키를 통해 즉각적인 가상 화면 프리징, 사각형 영역 선택, 인라인 벡터 주석 편집, 클립보드 복사 및 자동 파일 저장을 수행하는 시스템 트레이 상주형 고속 화면 캡처 도구입니다.

## Language

**CaptureSession**:
단축키 입력 시점부터 전체 가상 화면을 동결하고 오버레이 창을 활성화하여 캡처 완료(복사/저장) 또는 취소(ESC)로 종료될 때까지의 단일 실행 생명주기.
_Avoid_: 캡처 인스턴스, 캡처 프로세스

**SelectionBox**:
사용자가 마우스 드래그를 통해 지정한 화면 상의 2차원 사각형 픽셀 좌표 영역(X, Y, Width, Height).
_Avoid_: 크롭 박스, 캡처 영역, 사각틀

**SelectionHandle**:
확정된 SelectionBox의 4변 및 4모서리에 표시되어 마우스 드래그를 통해 크기를 미세 조정(Resize)하는 상호작용 제어점.
_Avoid_: 리사이즈 포인트, 조절점, 앵커

**VirtualCanvas**:
다중 모니터 전체를 아우르는 가상 화면(Virtual Screen) 좌표계를 1:1로 수용하는 단일 최상위 테두리 없는 오버레이 윈도우.
_Avoid_: 배경 창, 전체화면 오버레이

**DimmerLayer**:
캡처 세션 동안 전체 화면을 반투명하게 어둡게 덮어 SelectionBox 영역을 시각적으로 부각시키는 마스크 레이어.
_Avoid_: 암전 레이어, 딤 레이어

**AnnotationElement**:
SelectionBox 내부에서 사용자 드로잉을 통해 추가되는 벡터 그래픽 요소(사각형, 화살표, 텍스트, 형광펜) 및 픽셀 마스크.
_Avoid_: 주석 객체, 스티커, 드로잉

**PixelateMask**:
SelectionBox 내부의 민감 정보(개인정보, 비밀번호 등) 영역을 격자 블록 형태로 비식별화하는 래스터 조작 영역.
_Avoid_: 모자이크 박스, 블러 영역

**CaptureOutput**:
원본 화면 비트맵에 모든 AnnotationElement를 단일 비트맵으로 무손실 합성(Baking)하여 클립보드 복사 및 파일 저장에 전달하는 최종 산출 이미지.
_Avoid_: 스닙, 최종 캡처본

**AutoArchiveManager**:
CaptureOutput이 생성되었을 때 사용자의 지정 로컬 디렉터리에 타임스탬프 파일명(yyyyMMdd_HHmmss.png)으로 백그라운드 자동 저장을 수행하는 관리 모듈.
_Avoid_: 파일 세이버, 저장소 매니저

**SettingsStore**:
전역 단축키, 기본 저장 폴더 경로, 윈도우 시작 시 자동 실행 여부 등 사용자 환경 구성을 관리하는 로컬 설정 저장소.
_Avoid_: 환경설정 모듈, 옵션 매니저
