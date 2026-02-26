# Load Test Report

## 1) Purpose

- Validate capacity and latency under target concurrent users.
- Identify bottlenecks across Gateway, World, Chat, and data stores.
- Produce reproducible evidence for portfolio and interview discussion.

## 2) Test Environment

| Item | Value |
|---|---|
| Date | YYYY-MM-DD |
| Commit | `<git-sha>` |
| Deployment | local compose / k8s dev |
| Node spec | CPU / Memory |
| Services | Login, Gateway, World, Chat |
| Data stores | MySQL, Redis, MongoDB |
| Tool | custom bot runner / k6 / locust |

## 3) Workload Profiles

### Profile A - Baseline Movement

- Clients: 50, 100, 200
- Behavior: connect, move in random direction at fixed tick interval
- Duration: 15 min each step

### Profile B - Movement + Chat

- Clients: 100, 200, 300
- Behavior: movement + chat burst every N seconds
- Duration: 20 min each step

### Profile C - Reconnect Storm

- Clients: 200
- Behavior: forced disconnect for X% clients, reconnect within 5 seconds
- Duration: 10 min warm-up + 15 min scenario

## 4) Metrics to Collect

- End-to-end latency: p50/p95/p99
- Tick metrics: target tick, tick drift p95/p99
- Throughput: packets/sec, chat messages/sec
- Reliability: error rate, reconnect success rate
- Runtime: CPU, memory, GC or allocator stats

## 5) Acceptance Criteria

| Metric | Target |
|---|---:|
| Login handshake p95 | < 200ms |
| World sync p95 | < 120ms (150 CCU baseline) |
| Tick drift p95 (20Hz) | < 8ms (100 CCU baseline) |
| Message loss rate | 0% |
| Reconnect success rate | >= 97% |
| Error rate at target CCU | < 1% |

## 6) Results

### Summary Table

| Scenario | CCU | p95 Latency | p99 Latency | Tick Drift p95 | Error Rate | Notes |
|---|---:|---:|---:|---:|---:|---|
| Profile A | - | - | - | - | - | |
| Profile B | - | - | - | - | - | |
| Profile C | - | - | - | - | - | |

### Observations

- Observation 1:
- Observation 2:
- Observation 3:

## 7) Bottleneck Analysis

| Component | Symptom | Root Cause | Fix Applied | Outcome |
|---|---|---|---|---|
| Gateway | | | | |
| World | | | | |
| Chat | | | | |
| Mongo Log Writer | | | | |

## 8) Optimization Log

### Change 1

- Hypothesis:
- Change:
- Before:
- After:
- Conclusion:

### Change 2

- Hypothesis:
- Change:
- Before:
- After:
- Conclusion:

## 9) Reproduction Steps

```bash
# Example placeholders
# make build
# docker compose up -d
# ./tools/bot_runner --scenario profile_a --ccu 200 --duration 15m
```

## 10) Artifacts

- Dashboard screenshot links:
- Raw metrics export path:
- Bot run logs path:
- Related issue or PR links:
