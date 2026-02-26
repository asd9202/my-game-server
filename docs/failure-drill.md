# Failure Drill Report

## 1) Purpose

- Validate recovery behavior under realistic service failures.
- Measure MTTR and reconnect success under controlled chaos.
- Confirm world tick stability while dependencies degrade.

## 2) Scope

- Services: Login, Gateway, World, Chat.
- Dependencies: MySQL, Redis, MongoDB.
- Environment: local compose or k8s dev.

## 3) Success Criteria

| Metric | Target |
|---|---:|
| MTTR | <= 30s |
| Reconnect success | >= 95% |
| Data loss (chat/action logs) | 0 in tested window |
| Tick drift p95 during incident | < 2x baseline |

## 4) Drill Scenarios

### Scenario A - Gateway Pod Kill

- Hypothesis: clients reconnect through token/session flow with minimal interruption.
- Injection:
  - k8s: `kubectl delete pod <gateway-pod>`
  - compose: stop gateway container
- Expected:
  - reconnect success >= 95%
  - no permanent session orphaning

### Scenario B - World Pod Restart

- Hypothesis: player state rebind works and world attach recovers.
- Injection:
  - restart world instance during active session
- Expected:
  - state restoration succeeds for active players
  - bounded recovery time

### Scenario C - MongoDB Latency Spike

- Hypothesis: async log writer queue absorbs delay; game loop remains stable.
- Injection:
  - add artificial latency to Mongo network path
- Expected:
  - queue backlog rises then drains
  - world tick remains within target envelope

### Scenario D - Redis Unavailable

- Hypothesis: reconnect/session lookup uses fallback behavior and degrades gracefully.
- Injection:
  - terminate Redis service for N seconds
- Expected:
  - controlled error responses
  - no crash loops

## 5) Execution Log Template

### Run Metadata

- Date:
- Commit:
- Environment:
- Operators:

### Scenario Result Table

| Scenario | Start Time | End Time | MTTR | Reconnect Success | Tick Drift p95 | Result |
|---|---|---|---:|---:|---:|---|
| A | - | - | - | - | - | |
| B | - | - | - | - | - | |
| C | - | - | - | - | - | |
| D | - | - | - | - | - | |

## 6) Incident Details

### Incident <ID>

- Scenario:
- Symptom:
- Detection:
- Root cause:
- Recovery actions:
- Time to recover:
- Preventive action:

## 7) Follow-up Actions

| Priority | Action | Owner | Due Date | Status |
|---|---|---|---|---|
| High | | | | |
| Medium | | | | |
| Low | | | | |

## 8) Evidence

- Dashboard snapshot path:
- Logs path:
- Metrics export path:
- Related issue links:
