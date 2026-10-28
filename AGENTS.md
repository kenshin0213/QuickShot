# Agent Instructions (AGENTS)

## Scope and Priority

- Technical constraints and binary contracts: follow `.agents/PROJECT_INSTRUCTIONS.md`.
- Shared work principles, TDD discipline, communication standards, and context management: follow `.agents/COMMON_AGENT_INSTRUCTIONS.md`.
- Canonical Compound Engineering policy: follow `.agents/compound/POLICY.md`.
- Priority order: `.agents/COMMON_AGENT_INSTRUCTIONS.md` §1 참조.

---

## 0. Core Directive (Top-Priority Control Gate)

**Definitions**:
- **APPROVAL KEYWORDS** = `"진행해"` | `"승인"` | `"수정해"` | `"실행해"` | `"삭제해"`
- **BLOCKED** = 쓰기·수정·상태변경 도구 실행 금지, 즉시 중단하고 사용자 확인 대기 (단, 사실 확인 및 분석을 위한 읽기·검색 도구 실행은 상시 허용).

- **AMBIGUITY RESOLUTION (Block on Ambiguity, Actively Confirm Intent)**: When target file/code or intent is not 100% clear, STOP immediately, analyze context/system state via read-only tools, and present 2-3 concrete options in Korean to confirm intent.
  * **[Multiple Context Conflict Handling]**: When demonstrative pronouns ("이거", "이 문서") are used or target is omitted, and multiple candidates exist, confirm the exact target explicitly before acting. (예시: `docs/agents/core-directive-examples.md` 참조)
- **STRICT ACTION GATE (Execution Authority Control)**: Wait for APPROVAL KEYWORDS before executing any file write/patch tool (`write_to_file`, `replace_file_content`, `apply_patch`, etc.) or state-changing shell/command calls (`run_command`, `shell_command`, etc.).
  * **[Read-Only Inspection Allowed]**: Read-only tools (`view_file`, `grep_search`, `list_dir`) and read-only shell commands can be executed freely without APPROVAL KEYWORDS to analyze questions, verify facts, and prepare proposals.
  * **[Deliverable Creation Gate Priority]**: When requested to create deliverables, reports, documents, or code without APPROVAL KEYWORDS, demand the keyword first. This gate overrides lower-level instructions (e.g., Artifact Utilization in `.agents/COMMON_AGENT_INSTRUCTIONS.md`).
- **NO RUNNING AHEAD (Single-Step Enforcement)**: Execute ONLY the one logical step explicitly requested per turn. After completing a step, confirm with the user whether to proceed and wait for approval.
- **VERIFY BEFORE STATING (Mandatory Pre-Verification)**: Gather tool evidence via dedicated read/search tools (`view_file`, `grep_search`) or shell commands (`shell_command`, `run_command`) before asserting file existence, causes, or system states. Explicitly state "미검증" until evidence is gathered.
- **TARGET SCOPE LOCK (No Implicit Cascade Edits)**: Modify ONLY the explicitly named target file; propose sync for related files in the next turn and wait for approval. (예시: `docs/agents/core-directive-examples.md` 참조)
- **PRESERVE FIRST**: Interpret review, modernization, cleanup, and improvement as minimal edits that preserve existing content and structure. Completion criterion: if the diff contains deleted lines outside the user's requested scope, the edit violates this rule.
- **CONFLICT GATE**: If a proposed rule conflicts with existing content, report the conflict before editing.
- **MINIMAL VERIFICATION**: Run only verification proportional to the requested change (변경된 파일 수 이하의 검증 명령만 실행). If the user asks to stop verification, stop tool execution immediately.

---

## Delegated Sections

| 주제 | 참조 문서 |
|------|-----------|
| 언어·소통 (Response Indexing, Answer-First, Language, Tone) | `.agents/COMMON_AGENT_INSTRUCTIONS.md` §2 |
| 기술 스택 | `.agents/PROJECT_INSTRUCTIONS.md` |
| 검색 기반 추론 (RETRIEVAL-LED REASONING) | `.agents/COMMON_AGENT_INSTRUCTIONS.md` §3 |
| 산출물 관리 (Date-Based Directory, HTML Rendering, Artifacts) | `.agents/COMMON_AGENT_INSTRUCTIONS.md` §10 |
| 스킬 충실도 (Skill Fidelity & Execution Discipline) | `.agents/COMMON_AGENT_INSTRUCTIONS.md` §4 |

---

## 1. Safety & Security (Absolute Safety Rules)

- **DELETE LOCK**: Execute file deletion commands only when the user explicitly typed `"삭제해"`. Otherwise → BLOCKED.
- **MANDATORY SHELL HARD TIMEOUT & ENVIRONMENT**: Every OS shell command via `run_command` MUST be isolated. PowerShell commands MUST use the timeout wrapper (템플릿: `docs/agents/shell-templates.md` 참조). Outer shell variable interpolation MUST be prevented by enclosing the script block in single quotes (`'`).
- **SYMLINK & JUNCTION SAFETY**: Avoid following symbolic links and NTFS junctions during copy or archive operations to prevent circular reference and I/O deadlocks.
- *(OS-independent safety rules such as No-Hardcoding, Filesystem Tool Priority, Watchdog, and Error Reporting follow `.agents/COMMON_AGENT_INSTRUCTIONS.md`.)*

---

## 2. Execution & Task Control (Task Control & Exception Handling)

- **HYBRID REQUEST PROTOCOL (Mixed Question + Command Input)**: If user input contains BOTH a question and an execution command:
  1. Step 1 (Answer): Output the direct answer to the question first.
  2. Step 2 (Ask): Ask the user whether to proceed with the actual work and wait for confirmation.
  3. Step 3 (Execute): Execute tools ONLY after the user provides APPROVAL KEYWORDS.
- **PROPOSAL-FIRST PROTOCOL (Mandatory Proposal & Approval Gate)**: When input contains a request for suggestions alongside creation commands:
  1. Step 1 (Propose): Output ONLY the requested suggestions or options (2-3 concrete alternatives).
  2. Step 2 (Ask): Ask the user to select an option or confirm their preference.
  3. Step 3 (Wait): Wait for the user's explicit reply. Tool execution prior to selection/approval → BLOCKED.

---

## Notes

- Canonical Compound Engineering policy: `.agents/compound/POLICY.md`
- 우선순위는 `.agents/COMMON_AGENT_INSTRUCTIONS.md` §1 참조.