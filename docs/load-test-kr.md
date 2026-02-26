# 부하 테스트 리포트

## 1) 목적

- 목표 동시접속 구간에서 용량과 지연을 검증한다.
- Gateway, World, Chat, 데이터 저장소의 병목을 식별한다.
- 포트폴리오와 면접 설명에 사용 가능한 재현 가능한 근거를 만든다.

## 2) 테스트 환경

| 항목 | 값 |
|---|---|
| 날짜 | YYYY-MM-DD |
| 커밋 | `<git-sha>` |
| 배포 형태 | local compose / k8s dev |
| 노드 스펙 | CPU / Memory |
| 서비스 | Login, Gateway, World, Chat |
| 데이터 저장소 | MySQL, Redis, MongoDB |
| 도구 | custom bot runner / k6 / locust |

## 3) 워크로드 프로파일

### Profile A - 기본 이동

- 클라이언트 수: 50, 100, 200
- 동작: 접속 후 고정 tick 간격으로 랜덤 방향 이동
- 시간: 단계별 15분

### Profile B - 이동 + 채팅

- 클라이언트 수: 100, 200, 300
- 동작: 이동 + N초마다 채팅 버스트
- 시간: 단계별 20분

### Profile C - 재접속 폭주

- 클라이언트 수: 200
- 동작: 일부(X%) 강제 끊김 후 5초 내 재접속
- 시간: 10분 워밍업 + 15분 시나리오

## 4) 수집 메트릭

- End-to-end latency: p50/p95/p99
- Tick 메트릭: 목표 tick, tick drift p95/p99
- 처리량: packets/sec, chat messages/sec
- 신뢰성: 에러율, 재접속 성공률
- 런타임: CPU, 메모리, GC 또는 allocator 통계

## 5) 합격 기준

| 메트릭 | 목표 |
|---|---:|
| Login handshake p95 | < 200ms |
| World sync p95 | < 120ms (150 CCU 기준) |
| Tick drift p95 (20Hz) | < 8ms (100 CCU 기준) |
| 메시지 유실률 | 0% |
| 재접속 성공률 | >= 97% |
| 목표 CCU 에러율 | < 1% |

## 6) 결과

### 요약 표

| 시나리오 | CCU | p95 지연 | p99 지연 | Tick Drift p95 | 에러율 | 비고 |
|---|---:|---:|---:|---:|---:|---|
| Profile A | - | - | - | - | - | |
| Profile B | - | - | - | - | - | |
| Profile C | - | - | - | - | - | |

### 관찰 내용

- 관찰 1:
- 관찰 2:
- 관찰 3:

## 7) 병목 분석

| 컴포넌트 | 증상 | 원인 | 적용한 수정 | 결과 |
|---|---|---|---|---|
| Gateway | | | | |
| World | | | | |
| Chat | | | | |
| Mongo Log Writer | | | | |

## 8) 최적화 로그

### 변경 1

- 가설:
- 변경 내용:
- 변경 전:
- 변경 후:
- 결론:

### 변경 2

- 가설:
- 변경 내용:
- 변경 전:
- 변경 후:
- 결론:

## 9) 재현 절차

```bash
# Example placeholders
# make build
# docker compose up -d
# ./tools/bot_runner --scenario profile_a --ccu 200 --duration 15m
```

## 10) 산출물

- 대시보드 스크린샷 링크:
- 원본 메트릭 export 경로:
- bot 실행 로그 경로:
- 관련 이슈 또는 PR 링크:
