# MMO Core Server Portfolio

C++23-based MMO core server portfolio (`Login + Gateway + Chat + World`)

Korean version: `README-kr.md`

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

### Week 4 - World MVP

- [ ] Implement tick loop
- [ ] Implement authoritative movement validation
- [ ] Implement single-zone state sync

**DoD**

- [ ] Stable operation with 10-30 bots

**KPI**

- [ ] Tick drift p95 < 5ms (20Hz baseline)
- [ ] Abnormal movement detection = 100% for test scenarios

---

### Week 5 - World Core

- [ ] Implement AOI (grid-based)
- [ ] Implement one authoritative combat/skill path
- [ ] Implement reconnect/session recovery

**DoD**

- [ ] State propagation respects AOI bounds
- [ ] Reconnect restores character state

**KPI**

- [ ] State sync latency p95 < 80ms
- [ ] Reconnect success rate >= 95%

---

### Week 6 - Docker and Compose Integration

- [ ] Create service-level multi-stage Dockerfiles
- [ ] Build docker-compose stack (MySQL/Redis/Mongo + services)
- [ ] Add health checks and startup ordering

**DoD**

- [ ] Entire stack starts with one `docker compose up`

**KPI**

- [ ] Full startup time < 2 min
- [ ] World runtime image target < 250MB

---

### Week 7 - Kubernetes and Observability

- [ ] Login/Chat/Admin as `Deployment`
- [ ] Gateway as L4 `Service`
- [ ] World as `Deployment` (Agones extension point documented)
- [ ] Build Prometheus/Grafana dashboards

**DoD**

- [ ] Deploy/update/recover verified on dev cluster

**KPI**

- [ ] At least one core dashboard (tick, p95/p99, CCU, error rate)
- [ ] Recovery after pod restart <= 30s

---

### Week 8 - Load, Failure Drills, and Portfolio Packaging

- [ ] Run bot load tests
- [ ] Run failure drills (pod kill, DB delay)
- [ ] Write troubleshooting report
- [ ] Finalize portfolio docs (README, architecture, sequence)

**DoD**

- [ ] Complete problem-design-validation-results narrative

**KPI**

- [ ] p99 target satisfied at selected CCU
- [ ] MTTR and reconnect success rate documented

---

## 7) Performance and Reliability Results

| Item | Target | Result | Notes |
|---|---:|---:|---|
| Login p95 | < 200ms | - | |
| Chat loss rate | 0% | - | |
| Tick drift p95 | < 5ms | - | |
| World sync p95 | < 80ms | - | |
| Reconnect success | >= 95% | - | |
| MTTR | <= 30s | - | |

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
