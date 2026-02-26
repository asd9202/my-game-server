# Architecture

## 1) Goals

- Build an MMO core server portfolio that demonstrates authoritative world simulation.
- Prove reliability with metrics, failure drills, and operational tooling.
- Keep transport split practical: external HTTP login, internal gRPC control, socket hot path.

## 2) System Context

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
Hot path: custom socket + Protobuf
```

## 3) Service Responsibilities

### Login

- Account authentication and ban/version checks.
- Issues short-lived auth token for Gateway attach.

### Gateway

- Owns client session lifecycle and reconnect handling.
- Routes traffic to World and Chat services.
- Applies basic rate limits and packet shape validation.

### World

- Runs authoritative tick loop and AOI.
- Validates movement/combat/skill requests.
- Broadcasts state snapshots or deltas to players.

### Chat

- Handles global/zone/party chat fanout.
- Applies moderation policies (mute/ban).
- Emits chat/action events to async logging pipeline.

### Admin

- Announces messages, applies sanctions, inspects active sessions.
- Exposes operational controls needed for live service simulation.

## 4) Data Design

### MySQL (source of truth)

- Accounts, character profile, progression, inventory.
- Strong consistency and relational constraints.

### Redis (hot operational state)

- Session cache, route hints, short TTL state.
- Fast read/write for runtime lookups.

### MongoDB (append-heavy logs)

- `chat_messages`: channel-oriented chat history.
- `action_events`: gameplay events for audits and analysis.
- Write path uses queue + batch flush to isolate game loop latency.

## 5) Communication Model

- Client -> Login: HTTPS.
- Client -> Gateway: custom socket.
- Gateway <-> World hot path: custom socket + Protobuf.
- Service control calls: gRPC + Protobuf.

## 6) Deployment Model

- Local: docker-compose for full stack bring-up.
- Cluster: Kubernetes.
  - Login/Chat/Admin: Deployment.
  - Gateway: Deployment + L4 Service.
  - World: Deployment first, Agones as next-stage option.

## 7) Reliability and Recovery

- Gateway restart: token-based reconnect flow restores session attach.
- World restart: reconnect handshake rebinds character context.
- Mongo slowdown: async log writer prevents tick loop blocking.

## 8) Observability

- Metrics: tick drift, p95/p99 latency, CCU, reconnect success, error rate.
- Dashboards: Prometheus + Grafana.
- Logs: structured service logs and action trail in MongoDB.

## 9) Trade-offs

- Full gRPC everywhere is simpler for contracts but not ideal for hot movement path.
- Full custom socket everywhere increases control but raises operational complexity.
- Chosen hybrid balances latency, maintainability, and interview-proof rationale.
