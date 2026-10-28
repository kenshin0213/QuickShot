# C# .NET 8 WPF 기반 시스템 트레이 상주형 아키텍처 채택

## Context
고속 화면 캡처 유틸리티는 OS 레벨의 전역 단축키 수신, 실시간 다중 모니터 고DPI 좌표 제어, 즉각적인 화면 오버레이 및 부드러운 벡터 주석(화살표, 텍스트, 박스 등) 렌더링이 필수적입니다. C++ Win32는 단일 바이너리 장점이 있으나 벡터 UI와 텍스트 박스, 실행 취소(Undo) 구현 비용이 높고, Electron은 캡처 반응 속도와 메모리 상주 비용이 지나치게 큽니다.

## Decision
C# / .NET 8 WPF(Windows Presentation Foundation)를 주 개발 스택으로 채택하고 시스템 트레이 상주형(NotifyIcon) 애플리케이션으로 구현합니다.

## Consequences
- .NET 8 런타임 종속성이 발생하지만 Windows 10/11 환경에서 기본 제공되거나 신속히 배포 가능합니다.
- WPF의 풍부한 벡터 그래픽 파이프라인(Canvas, Shape, TextBlock)을 통해 주석 엔진과 실시간 Undo 스택을 견고하고 신속하게 구현할 수 있습니다.
- Win32 P/Invoke(`RegisterHotKey`, `BitBlt`)를 통해 OS 네이티브 캡처 기능에 지연 없이 직결할 수 있습니다.
