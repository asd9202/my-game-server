# AGENTS Guide

This document defines mandatory execution rules for agents and automation tools in this repository.

## 1) Project Objective

- Build a C++23 MMO core server portfolio.
- Core services: `Login`, `Gateway`, `Chat`, `World`, and `Admin` (late phase).
- Primary proof points: authoritative world simulation, operational metrics, failure recovery, and deployment automation.

## 2) Architecture Rules (Mandatory)

- External login path MUST use `HTTPS`.
- Internal control communication MUST use `gRPC + Protobuf`.
- Real-time hot path (movement/combat) MUST use `custom socket + Protobuf`.
- Logging MUST be asynchronous (queue + batch writes) and MUST NOT block the game loop.

## 3) Service Ownership

### Login

- Authentication, version check, and ban validation.
- Issue short-lived token for Gateway attachment.

### Gateway

- Client session lifecycle ownership.
- Routing between client and World/Chat.
- Reconnect handling and baseline rate limiting.

### Chat

- Global/zone/party chat fanout.
- Moderation policies (`mute`, `ban`).
- Emit `chat_messages` and `action_events` to the log pipeline.

### World

- Authoritative tick loop.
- AOI computation and state propagation.
- Movement/combat/skill validation.

## 4) Data and Storage Policy

- `MySQL`: source-of-truth data (account/character/inventory).
- `Redis`: session cache, route hints, short-lived runtime state.
- `MongoDB`: append-heavy logs (`chat_messages`, `action_events`).

## 5) Branch Strategy (Strict)

- Default branches: `main`, `feature/*`, `hotfix/*`.
- `main` MUST stay releasable at all times.
- Direct push to `main` is NOT allowed.
- Feature branches MUST be short-lived and merged quickly.
- `hotfix/*` MUST branch from `main` and be merged back immediately after verification.
- Squash merge is preferred to keep history clean.

Example branch names:

- `feature/world-aoi`
- `feature/chat-mongo-log`
- `fix/gateway-reconnect`
- `chore/ci-cache`

## 6) Priority Order (8-Week Plan)

1. Foundations and contracts (`Login/Gateway`, proto, token flow)
2. Chat + Mongo logging v1
3. Chat/log hardening (mute/ban, indexes, TTL)
4. World MVP (tick, movement validation, single zone)
5. World core (AOI, one combat/skill path, reconnect)
6. Docker/Compose integration
7. Kubernetes + observability
8. Load/failure drills + portfolio packaging

## 7) Coding and Documentation Standards (Strict)

- Agents MUST follow existing project patterns before introducing new ones.
- Agents MUST NOT introduce style-only churn unrelated to the task.
- All new C++ code MUST target `C++23` syntax and semantics.
- Build configuration MUST keep C++ standard as `23` (for CMake: `CMAKE_CXX_STANDARD 23` and `CMAKE_CXX_STANDARD_REQUIRED ON`).
- Changes MUST NOT downgrade language level or add compatibility fallbacks for older standards unless explicitly requested and documented.
- Hot-path logic MUST NOT include blocking I/O.
- Temporary bypasses that hide real problems are prohibited.
- English and Korean docs MUST stay link-consistent.

## 8) Mandatory Deliverables

- `README.md`
- `README-kr.md`
- `docs/architecture.md`
- `docs/architecture-kr.md`
- `docs/sequence.md`
- `docs/sequence-kr.md`
- `docs/load-test.md`
- `docs/load-test-kr.md`
- `docs/failure-drill.md`
- `docs/failure-drill-kr.md`

## 9) Definition of Done (Hard Gate)

Before closing any task, all items below MUST be true:

- [ ] File paths and links are valid.
- [ ] Related docs are updated.
- [ ] C++ build settings still enforce C++23 after the change.
- [ ] KPI/metric sections are not stale when behavior changes.
- [ ] Failure/recovery scenarios remain accurate.
- [ ] Scope is fully completed (no partial TODO left without clear note).

## 10) Commit and PR Policy (Strict)

- Commit messages MUST be written in Korean.
- Recommended commit format:
  - ``<type>: <변경 의도>``
  - Example: ``feat: 월드 AOI 셀 기반 전파 로직 추가``
- Commits MUST be atomic and focused.
- PR description MUST include:
  - Purpose
  - Impact scope
  - Verification steps
  - Remaining risks

## 11) Prohibited Actions

- Hiding issues with temporary suppressions or unsafe shortcuts.
- Deleting failing tests to force green status.
- Ending work with mismatched code and documentation.
- Merging unverified changes into `main`.
