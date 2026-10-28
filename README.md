# AI Agent Instruction Framework & Project Skeleton

다중 AI 코딩 에이전트(Codex, Claude Code, Gemini CLI 등) 환경에서 일관되고 안전하게 동작하도록 설계된 **범용 AI 에이전트 작업 표준 및 프로젝트 스켈레톤**입니다.

특정 플랫폼이나 런타임 도구명에 종속되지 않는 기능 중심 추상화, 엄격한 실행 권한 통제(Strict Action Gate), TDD 및 공식 빌드 검증 기준 다원화, 2단계 산출물 통제 규칙을 제공합니다.

---

## 1. 저장소 구조

```text
Skeleton/
├── AGENTS.md                  # 최상위 운영 규칙 및 실행 권한 게이트 (Host Entry Point)
├── README.md                  # 스켈레톤 안내 및 시작 가이드 (본 문서)
└── .agents/                   # 에이전트 세부 지침 및 운영 자산
    ├── COMMON_AGENT_INSTRUCTIONS.md  # 공통 작업 원칙, 통신 규격, 검증 및 컨텍스트 관리
    ├── PROJECT_INSTRUCTIONS.md       # 프로젝트별 기술 스택, 빌드 명령, 바이너리 제약 템플릿
    ├── shell-templates.md            # OS 셸 타임아웃 래퍼 및 에러 블록 템플릿
    ├── core-directive-examples.md    # 모호성 해소 및 변경 범위 잠금(Scope Lock) 예시
    └── compound/
        └── POLICY.md                 # 재사용 가능한 교훈 기록 및 Compound Engineering 정책
```

> **참고**: 새 프로젝트 소스 코드 및 관련 파일은 프로젝트 요구사항에 맞추어 루트 또는 지정된 하위 디렉터리에 자유롭게 구성합니다.

---

## 2. 에이전트 지침 아키텍처 (3계층 구조)

에이전트는 작업 시 다음 3단계 계층 구조를 단일 출처(SSOT) 및 우선순위 체계로 준수합니다.

```text
[1. 최상위 게이트: AGENTS.md]
  │  - 실행 권한 통제 (APPROVAL KEYWORDS: "진행해", "수정해" 등)
  │  - 상태 변경 도구 차단 (BLOCKED) 및 읽기 검사 상시 허용
  │  - 단일 논리 단계 강제 (Single-Step) 및 작업 범위 잠금 (Scope Lock)
  ▼
[2. 공통 작업 규약: .agents/COMMON_AGENT_INSTRUCTIONS.md]
  │  - 통신 표준 (RESPONSE INDEXING, 건조한 팩트 위주 어조, 이모지 배제)
  │  - 작업 유형별 검증 다원화 (순수 논리: TDD / 시스템·바이너리: 공식 빌드 및 스모크 테스트)
  │  - 컨텍스트 예산 관리 (호스트 세션 압축 `/compact` 및 대규모 작업 체크포인트)
  │  - 2단계 조건부 산출물 생성 (명시적 문서화 요구 시에만 폴더 생성)
  ▼
[3. 프로젝트 기술 계약: .agents/PROJECT_INSTRUCTIONS.md]
     - 프로젝트별 기술 스택 (Language, Runtime, Target Arch)
     - 공식 빌드 및 단위/스모크 테스트 실행 명령
     - 아키텍처 및 바이너리 하드 제약 조건
```

---

## 3. 지원 호스트 런타임

본 스켈레톤은 특정 환경에 구애받지 않고 다양한 에이전트 런타임에서 즉시 동작합니다.

| 호스트 환경 | 주요 진입점 | 도구 및 실행 매핑 |
|---|---|---|
| **Codex CLI** | `AGENTS.md` (기본 진입점) | `shell_command`, 패치 기능, 호스트 타임아웃 옵션 |
| **Claude Code** | `CLAUDE.md` 또는 `AGENTS.md` | Bash 실행, 파일 편집, `/compact` 명령 |
| **Antigravity / Gemini** | `AGENTS.md` / `GEMINI.md` | 파일 읽기/수정 도구, 셸 실행 도구 |

---

## 4. 새 프로젝트 적용 방법 (Quick Start)

본 스켈레톤을 활용하여 새 프로젝트를 시작하는 3단계 절차입니다.

1. **저장소 복제 및 초기화**:
   - `Skeleton` 저장소를 새 프로젝트 디렉터리로 복제합니다.
2. **프로젝트 기술 명세 기입**:
   - `.agents/PROJECT_INSTRUCTIONS.md`를 열어 대상 프로젝트의 언어, 빌드 도구, 테스트 명령, 핵심 제약 사항의 괄호 힌트(`[...]`)를 실제 값으로 채웁니다.
3. **에이전트 작업 시작**:
   - 에이전트 CLI/IDE를 실행하고 작업을 시작합니다. 에이전트는 `AGENTS.md`의 엄격한 실행 게이트와 `COMMON_AGENT_INSTRUCTIONS.md`의 규칙에 따라 안전하고 일관되게 작업을 수행합니다.
