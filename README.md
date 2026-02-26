# MMO Core Server Portfolio

C++23-based MMO core server portfolio (`Login + Gateway + Chat + World`)

Korean version: `README-kr.md`
Agent rules: `AGENTS.md`

## 1) Project Overview

- Goal: Prove large-scale MMORPG server engineering capability (authoritative world, operations, failure handling, deployment automation)
- Duration: 8 weeks
- Core focus:
  - World authoritative simulation
  - Chat + Action Log (MongoDB) design
  - Docker build + Kubernetes deployment
  - Metrics-driven validation (performance and reliability)

## 2) Architecture Overview

```text
Client
  | (HTTPS login)
  v
Login  ----(token verify)----> Gateway ----(TCP/UDP + Protobuf)----> World
  |                                 |
  |                                 +----> Chat
  |
  +----> MySQL (account/core data)

Chat / World ----(async log writer)----> MongoDB (chat_messages, action_events)
Gateway/World/Chat ----(cache/session)----> Redis

Internal control plane: gRPC + Protobuf
Hot path (movement/combat): custom socket + Protobuf
```

## 3) Services

- **Login**
  - Authentication/token issuance, version check, ban validation
- **Gateway**
  - Client session management, world routing, reconnect handling
- **Chat**
  - Global/zone/party chat, mute/ban policy
- **World**
  - Tick loop, AOI, authoritative movement/combat validation
- **Admin (late phase)**
  - Announcement, moderation, force session termination, status view

## 4) Tech Stack

- Language: `C++23`
- Network: `Asio`, `Protobuf`, `gRPC` (internal control communication)
- DB/Cache: `MySQL`, `Redis`, `MongoDB` (`chat_messages`, `action_events`)
- Build: `CMake`, `vcpkg`
- Infra: `Docker` (multi-stage), `Kubernetes`
- Observability: `Prometheus`, `Grafana`
- Test: `GoogleTest`, bot load generator

## Current Plan Snapshot

- Development order (8 weeks): foundations/contracts -> chat + mongo log v1 -> chat/log hardening -> docker/compose integration -> world MVP -> world core -> kubernetes + observability -> admin page -> load/failure drills + portfolio packaging.
- Protocol split: external login `HTTPS`, internal control `gRPC + Protobuf`, hot path `custom socket + Protobuf`.
- Core stack: `C++23`, `Asio`, `Protobuf`, `gRPC`, `MySQL`, `Redis`, `MongoDB`, `Docker`, `Kubernetes`.
- Admin stack (late phase): backend `NestJS + TypeScript`, frontend `React` (recommended `Next.js + TypeScript`).

## 5) Run

### Local (without k8s)

```bash
# TODO: project commands
# cmake -S . -B build -DCMAKE_BUILD_TYPE=Release
# cmake --build build -j
```

### Docker

```bash
# TODO: docker compose up -d --build
```

### Kubernetes

```bash
# TODO: kubectl apply -k infra/k8s/overlays/dev
```

---

## 6) 8-Week Execution Plan (Checklist + DoD + KPI)

KPI baseline for Weeks 4-8: dev environment (~100-200 bot CCU, single region). Adjust thresholds if hardware profile changes.

### Week 1 - Foundation and Contracts

- [ ] Create `Login/Gateway` service skeletons
- [ ] Draft Protobuf schemas
- [ ] Implement token issue/verify flow
- [ ] Define common error codes and log format

**DoD**

- [ ] Login -> Gateway auth pass succeeds end-to-end

**KPI**

- [ ] Login handshake p95 < 200ms
- [ ] Auth success rate >= 99% (N=100)

---

### Week 2 - Chat + Mongo Log v1

- [ ] Implement global/zone chat
- [ ] Persist `chat_messages`
- [ ] Add async queue ingestion for `action_events`

**DoD**

- [ ] Chat send/receive and log persistence work simultaneously
- [ ] Logs remain queryable after service restart

**KPI**

- [ ] Zero message loss at 1000 msg/min
- [ ] Log write failure rate < 0.1%

---

### Week 3 - Chat/Log Hardening

- [ ] Implement mute/ban
- [ ] Add minimal admin query APIs
- [ ] Apply Mongo indexes + TTL
- [ ] Tune batch writes

**DoD**

- [ ] Moderation applies immediately
- [ ] Recent chat/action logs query API works

**KPI**

- [ ] Log query p95 < 250ms
- [ ] Queue backlog average <= 1s

---

### Week 4 - Docker and Compose Integration

- [ ] Create service-level multi-stage Dockerfiles
- [ ] Build docker-compose stack (MySQL/Redis/Mongo + services)
- [ ] Add health checks and startup ordering

**DoD**

- [ ] Entire stack starts with one `docker compose up`

**KPI**

- [ ] Warm startup time < 2 min and clean rebuild startup < 8 min
- [ ] World runtime image target < 350MB

---

### Week 5 - World MVP

- [ ] Implement tick loop
- [ ] Implement authoritative movement validation
- [ ] Implement single-zone state sync

**DoD**

- [ ] Stable operation with 10-30 bots

**KPI**

- [ ] Tick drift p95 < 8ms (20Hz, 100 bot baseline)
- [ ] Abnormal movement detection = 100% for mandatory scenarios (speed, teleport, collision bypass)

---

### Week 6 - World Core

- [ ] Implement AOI (grid-based)
- [ ] Implement one authoritative combat/skill path
- [ ] Implement reconnect/session recovery

**DoD**

- [ ] State propagation respects AOI bounds
- [ ] Reconnect restores character state

**KPI**

- [ ] State sync latency p95 < 120ms (150 bot baseline)
- [ ] Reconnect success rate >= 97% (rejoin within 10s)
- [ ] AOI fanout packet reduction >= 40% vs full-broadcast baseline

---

### Week 7 - Kubernetes and Observability

- [ ] Login/Chat/Admin as `Deployment`
- [ ] Gateway as L4 `Service`
- [ ] World as `Deployment` (Agones extension point documented)
- [ ] Build Prometheus/Grafana dashboards

**DoD**

- [ ] Deploy/update/recover verified on dev cluster

**KPI**

- [ ] Core dashboard online + 4 alert rules (tick drift, error rate, reconnect drop, queue backlog)
- [ ] Recovery after pod restart <= 60s
- [ ] 30-min post-rollout soak with zero `CrashLoopBackOff`

---

### Week 8 - Admin Page + Load/Failure + Portfolio Packaging

- [ ] Implement Admin backend APIs (announcement, moderation, session controls)
- [ ] Implement Admin frontend screens (React/Next.js) for core operations
- [ ] Run bot load tests and failure drills (pod kill, DB delay)
- [ ] Write troubleshooting report and finalize portfolio docs

**DoD**

- [ ] Core admin workflows run end-to-end with role checks
- [ ] Complete problem-design-validation-results narrative

**KPI**

- [ ] p99 target satisfied at selected CCU (target: 200 CCU <= 180ms on world state sync path)
- [ ] MTTR <= 90s across planned drill scenarios
- [ ] Reconnect success >= 95% during failure drills
- [ ] Admin critical action success rate >= 99% in test scenarios

---

## 7) Performance and Reliability Results

| Item | Target | Result | Notes |
|---|---:|---:|---|
| Login p95 | < 200ms | - | |
| Chat loss rate | 0% | - | |
| Tick drift p95 | < 8ms | - | |
| World sync p95 | < 120ms | - | |
| Reconnect success | >= 97% | - | |
| MTTR | <= 90s | - | |

## 8) Failure Drill Checklist

- [ ] Reconnect succeeds after Gateway pod kill
- [ ] Session/state recovery after World pod restart
- [ ] Game loop isolation under Mongo delay (async log queue)
- [ ] Session fallback behavior under Redis failure

## 9) Troubleshooting Log

### <YYYY-MM-DD> <Issue Title>

- Symptom:
- Root cause:
- Fix:
- Prevention:
- Related commit/doc:

## 10) Document Links

- Architecture diagram: `docs/architecture.md`
- Sequence diagram: `docs/sequence.md`
- Load test report: `docs/load-test.md`
- Failure drill report: `docs/failure-drill.md`

## 11) Interview One-Page Summary

- Why this service split (`Login/Gateway/Chat/World`) was chosen
- Why hybrid transport (internal gRPC + hot-path socket) was chosen
- How bottlenecks were found and improved via metrics
- How failures were reproduced and recovered (with MTTR)

## 12) License

MIT
