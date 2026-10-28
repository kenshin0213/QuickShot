# Project Instructions

이 문서는 프로젝트의 기술 스택, 빌드/검증 환경, 아키텍처 및 바이너리 제약을 정의합니다.
`AGENTS.md` 및 `COMMON_AGENT_INSTRUCTIONS.md`에서 참조하는 프로젝트별 기술 명세의 단일 출처입니다.
새 프로젝트 생성 시 아래 항목을 해당 프로젝트의 실제 환경에 맞게 기입하여 사용합니다.

---

## 1. Technology Stack

| 항목 | 명세 |
|------|------|
| 언어 | C# 12 (.NET 8.0) |
| 프레임워크/런타임 | WPF (.NET 8.0 Windows Desktop), PresentationFramework |
| 컴파일러/엔진 | .NET 8 SDK (Roslyn / MSBuild) |
| 타겟 플랫폼/아키텍처 | Windows 10 (1809+) / Windows 11, x64 |
| 주요 의존성/라이브러리 | Win32 P/Invoke (`user32.dll`, `gdi32.dll`), `System.Text.Json` |

---

## 2. Build & Verification Environment

- **빌드 도구/명령**: `dotnet build src/QuickShot/QuickShot.csproj -c Release`
- **단위 테스트 명령 (TDD 로직 검증)**: `dotnet test tests/QuickShot.Tests/QuickShot.Tests.csproj`
- **공식 시스템/빌드 검증 명령 (TDD 대체 기준)**: `dotnet build src/QuickShot/QuickShot.csproj -c Release --no-incremental`
- **필수 환경/사전 런타임**: .NET 8.0 SDK (Windows Desktop Workload), Windows OS

---

## 3. Architecture & Binary Constraints

- **아키텍처 제약**: Windows 전용 (`net8.0-windows`), Per-Monitor DPI Aware V2 매니페스트 필수, 다중 모니터 VirtualScreen 좌표계 대응
- **실행/런타임 제약**: Windows 단일 인스턴스 뮤텍스(Mutex), 시스템 트레이 상주(NotifyIcon), 전역 단축키(`RegisterHotKey`) 메시지 루프 연동
- **보안 및 인증 제약**: 외부 네트워크 통신 없음, 개인정보 마스킹(PixelateMask) 시 비가역적 픽셀레이션 보장
- **외부 리소스 종속성**: 트레이 아이콘 리소스(`app.ico`), 설정 파일(`%APPDATA%\QuickShot\settings.json`)
