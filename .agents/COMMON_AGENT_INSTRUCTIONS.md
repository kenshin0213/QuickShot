# Common Agent Instructions

This document defines project-independent work principles, development discipline, communication standards, and context management protocols.

**Definitions**:
- **BLOCKED** = 파일 쓰기·수정·삭제 및 상태 변경 도구 실행 금지, 즉시 중단하고 사용자 확인 대기 (단, 사실 확인 및 분석을 위한 읽기·검색 기능은 상시 허용).

---

## 1. Instruction Priority

1. Host platform sandbox & approval policies (Platform Sandbox & Approval Policy)
2. Current user explicit request
3. Host agent entry point (`AGENTS.md`, `CLAUDE.md`, etc.)
4. Project-specific technical instructions (`.agents/PROJECT_INSTRUCTIONS.md`)
5. Common agent instructions (this document)

---

## 2. Communication Standards

- **RESPONSE INDEXING (Mandatory Response Identifier)**: Output a unique identifier in the format `[SEQ-{NN}: {한국어 핵심 주제 3~4단어}]` at the very beginning of every response before any heading or text (`{NN}` starts from `01` and increments by 1).
  * (Example): `[SEQ-05: 제안 우선 프로토콜 적용]`
- **ANSWER FIRST RULE (Direct Answer Priority)**: When the user asks a question, place the exact heading `[질문 답변 및 원인 분석]` immediately below the `[SEQ: ...]` line, followed directly by the answer before any task reports.
- **POST-ANSWER CONFLICT REPORTING**: When a conflict between instructions occurs, output the direct answer first, then append a `[지침 충돌 원인 및 조치 방안]` section stating: (1) conflicting rules with file links, (2) why the conflict occurred, and (3) the rationale behind the chosen approach.
- **LANGUAGE & LOGS**: All user-facing responses and deliverables MUST be written 100% in Korean unless requested otherwise. Agent instruction documents (AGENTS.md, COMMON, etc.) may use English. Terminal and command execution logs MUST remain in English.
- **DRY TONE**: Use a strictly objective, professional, and neutral tone. Limit acknowledgments to facts: "확인했습니다" or "수정하겠습니다".
- **NO EMOJIS**: Use only plain Markdown text and structural symbols (-, *, >). Do not use emojis or emoticons.

---

## 3. Technical Analysis and Design Principles

- **RETRIEVAL-LED REASONING**: Before relying on internal model knowledge, first search internal documentation (`README.md`, `docs/`) and actual source code via file reading and search capabilities, and answer based on those results.
- **FACT/INFERENCE/OPINION SEPARATION**: Clearly distinguish confirmed facts (command and tool execution logs), code-based inferences, and recommended opinions.

---

## 4. Strict Execution & Verification Standards

- **Skill Fidelity**: Execute all mandatory steps of agreed plans/skills. Completion criterion: every checkbox in the plan is `[x]` with verification log attached to each.
- **Verification Discipline by Work Type**:
  - **Logic & Algorithmic Code (TDD Hard Gate)**:
    1. **[RED]**: Write a failing test first and execute it to obtain failure/compilation error evidence.
    2. **[GREEN]**: Write only the minimum production code required to pass the test.
    3. **[REFACTOR]**: Clean up code and verify all tests continue to pass.
  - **System, UI, COM & Build Configurations**: Pure unit tests are not forced for external bindings, UI components, or binary integrations. Instead, official verification specified in `.agents/PROJECT_INSTRUCTIONS.md` (e.g., successful compilation, build logs, and smoke test runs) is recognized as the completion criterion.
- **Task-by-Task Execution**: Execute one task at a time (Test/Implement → Verify → Update plan checkbox `- [ ]` to `- [x]`).
- **Evidence Before Assertions**: Collect build or test logs in context before marking any task as complete.

---

## 5. Code Review and Verification Standards

- **CODE LINK & LOCATION COMPATIBILITY**: When describing code locations or conducting reviews, use the unified path-with-line format `[path:line-range]` (e.g., `[src/index.ts:10-25]`, `[AGENTS.md:50]`) across all host environments.
- **TEST PASS VS SPEC SATISFACTION**: Strictly distinguish 'test/build passing' from 'spec satisfaction'; explicitly report unverified edge cases or scenarios even if all tests/builds pass.

---

## 6. Context Budget Management

- **Session Compaction & Refresh**: When context becomes long or complex, recommend or execute host-native session compaction (e.g., `/compact`) or guide the user to refresh the session.
- **Checkpoints for Large Tasks**: For multi-phase tasks spanning long sessions, save an execution checkpoint to `.agents/checkpoints/<goal-id>.md` upon completing a logical unit, and resume from the checkpoint (reading only necessary state rather than full history).

---

## 7. Tier 3 On-Demand Documents Index (Load Only When Needed)

| Task Type | Trigger Condition | Reference Document | 상태 |
|---|---|---|---|
| Compound Engineering | Recording/promoting reusable lessons | `.agents/compound/POLICY.md` | 활성 |

---

## 8. Safety & Security

- **NO HARDCODING**: Load passwords, authentication tokens, and DB credentials via environment variables or configuration files. Direct embedding in source code → BLOCKED.

---

## 9. Terminal & Execution Safety

- **FILESYSTEM READ PRIORITY**: Prefer dedicated file reading and search capabilities over shell commands for inspecting repository files.
- **COMMAND TIMEOUT & RECOVERY**:
  1. Primary: Strictly specify and respect host-level execution timeout options.
  2. Secondary: For commands with potential hang risks or long-running tasks, run through the OS shell timeout wrapper script (see `.agents/shell-templates.md`).
- **MANDATORY ERROR REPORTING PRIORITY**: If a command is canceled, timed out, or fails, output the structured error block at the top of the response (template: `.agents/shell-templates.md`).

---

## 10. Deliverable Management

- **STRICT TWO-STAGE DELIVERABLE GATES (Scope Lock Enforcement)**:
  - **Stage 1 (Document Deliverables)**: Generate directories and markdown documents under `docs/YYYY-MM-DD/NN_[TaskName]/` ONLY when the user explicitly requests documentation, reports, or analysis deliverables (e.g., "보고서 작성", "문서화", "리포트 생성"). Routine code modifications, refactoring, and configuration edits MUST NOT generate cascading documentation.
  - **Stage 2 (Standalone HTML Rendering)**: Generate a 1:1 matching standalone HTML file alongside the Markdown document ONLY when the user explicitly requests HTML output (e.g., "HTML 생성", "HTML로 산출"). Standalone HTML files must be self-contained (modern clean theme, responsive layout, styled tables).
- **CHANGELOG SYNC**: When task deliverables are explicitly produced under `docs/YYYY-MM-DD/`, append a link and a one-line summary to `docs/CHANGELOG.md` (created alongside the first deliverable if not present).
