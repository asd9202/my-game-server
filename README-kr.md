# MMO 코어 서버 포트폴리오

C++23 기반 MMO 코어 서버 포트폴리오 (`Login + Gateway + Chat + World`)

English version: `README.md`
에이전트 규칙: `AGENTS.md`

## 1) 프로젝트 개요

- 목표: 대규모 MMORPG 서버 엔지니어링 역량(권한형 월드, 운영, 장애 대응, 배포 자동화) 증명
- 기간: 8주
- 핵심 포인트:
  - World authoritative simulation
  - Chat + Action Log(MongoDB) 설계
  - Docker 빌드 + Kubernetes 배포
  - 메트릭 기반 검증(성능/신뢰성)

## 2) 아키텍처 개요

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

## 3) 서비스 구성

- **Login**
  - 인증/토큰 발급, 버전 체크, 차단 검증
- **Gateway**
  - 클라이언트 세션 관리, 월드 라우팅, 재접속 처리
- **Chat**
  - global/zone/party 채팅, mute/ban 정책
- **World**
  - Tick loop, AOI, 권한형 이동/전투 검증
- **Admin (후반 단계)**
  - 공지, 제재, 세션 강제 종료, 상태 조회

## 4) 기술 스택

- 언어: `C++23`
- 네트워크: `Asio`, `Protobuf`, `gRPC` (내부 제어 통신)
- DB/Cache: `MySQL`, `Redis`, `MongoDB` (`chat_messages`, `action_events`)
- 빌드: `CMake`, `vcpkg`
- 인프라: `Docker` (multi-stage), `Kubernetes`
- 관측: `Prometheus`, `Grafana`
- 테스트: `GoogleTest`, bot load generator

## 5) 실행 방법

### 로컬 (k8s 없이)

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

## 6) 8주 실행 계획 (체크리스트 + DoD + KPI)

### Week 1 - 기반/계약 정의

- [ ] `Login/Gateway` 서비스 스켈레톤 생성
- [ ] Protobuf 스키마 초안 작성
- [ ] 토큰 발급/검증 플로우 구현
- [ ] 공통 에러 코드/로그 포맷 정의

**DoD**

- [ ] Login -> Gateway 인증 통과가 E2E로 성공

**KPI**

- [ ] Login handshake p95 < 200ms
- [ ] 인증 성공률 >= 99% (N=100)

---

### Week 2 - Chat + Mongo 로그 v1

- [ ] global/zone 채팅 구현
- [ ] `chat_messages` 저장
- [ ] `action_events` 비동기 큐 적재 추가

**DoD**

- [ ] 채팅 송수신 + 로그 저장이 동시에 동작
- [ ] 서비스 재시작 후에도 로그 조회 가능

**KPI**

- [ ] 1000 msg/min에서 메시지 유실 0
- [ ] 로그 쓰기 실패율 < 0.1%

---

### Week 3 - Chat/Log 고도화

- [ ] mute/ban 구현
- [ ] 관리자 최소 조회 API 추가
- [ ] Mongo 인덱스 + TTL 적용
- [ ] batch write 튜닝

**DoD**

- [ ] 제재 정책이 즉시 반영
- [ ] 최근 채팅/액션 로그 조회 API 동작

**KPI**

- [ ] 로그 조회 p95 < 250ms
- [ ] queue backlog 평균 <= 1s

---

### Week 4 - World MVP

- [ ] tick loop 구현
- [ ] 권한형 이동 검증 구현
- [ ] 단일 zone 상태 동기화 구현

**DoD**

- [ ] 10~30 bot 기준 안정 동작

**KPI**

- [ ] tick drift p95 < 5ms (20Hz 기준)
- [ ] 이상 이동 탐지 시나리오 100% 탐지

---

### Week 5 - World Core

- [ ] AOI(그리드) 구현
- [ ] 권한형 전투/스킬 경로 1개 구현
- [ ] 재접속/세션 복구 구현

**DoD**

- [ ] AOI 범위 기준 상태 전파 확인
- [ ] 재접속 시 캐릭터 상태 복원 확인

**KPI**

- [ ] 상태 동기화 지연 p95 < 80ms
- [ ] 재접속 성공률 >= 95%

---

### Week 6 - Docker/Compose 통합

- [ ] 서비스별 multi-stage Dockerfile 작성
- [ ] docker-compose 스택 구성(MySQL/Redis/Mongo + 서비스)
- [ ] 헬스체크 및 기동 순서 정리

**DoD**

- [ ] `docker compose up` 1회로 전체 스택 기동

**KPI**

- [ ] 전체 기동 시간 < 2분
- [ ] World runtime 이미지 목표 < 250MB

---

### Week 7 - Kubernetes + 관측

- [ ] Login/Chat/Admin를 `Deployment`로 구성
- [ ] Gateway를 L4 `Service`로 구성
- [ ] World를 `Deployment`로 구성 (Agones 확장 포인트 문서화)
- [ ] Prometheus/Grafana 대시보드 구축

**DoD**

- [ ] dev 클러스터에서 배포/업데이트/복구 검증

**KPI**

- [ ] 핵심 대시보드 1개 이상(tick, p95/p99, CCU, 에러율)
- [ ] pod 재시작 후 복구 <= 30s

---

### Week 8 - 부하/장애 훈련/포트폴리오 패키징

- [ ] bot 부하 테스트 실행
- [ ] 장애 훈련 실행(pod kill, DB 지연)
- [ ] 트러블슈팅 리포트 작성
- [ ] 포트폴리오 문서 최종 정리(README, architecture, sequence)

**DoD**

- [ ] 문제-설계-검증-결과 내러티브 완성

**KPI**

- [ ] 선정한 CCU에서 p99 목표 충족
- [ ] MTTR 및 재접속 성공률 문서화

---

## 7) 성능 및 신뢰성 결과

| 항목 | 목표 | 결과 | 비고 |
|---|---:|---:|---|
| Login p95 | < 200ms | - | |
| Chat 유실률 | 0% | - | |
| Tick drift p95 | < 5ms | - | |
| World sync p95 | < 80ms | - | |
| 재접속 성공률 | >= 95% | - | |
| MTTR | <= 30s | - | |

## 8) 장애 훈련 체크리스트

- [ ] Gateway pod kill 이후 재접속 성공
- [ ] World pod 재시작 이후 세션/상태 복구
- [ ] Mongo 지연 상황에서 게임 루프 격리(비동기 로그 큐)
- [ ] Redis 장애 시 세션 fallback 동작 확인

## 9) 트러블슈팅 로그

### <YYYY-MM-DD> <Issue Title>

- 증상:
- 근본 원인:
- 수정:
- 재발 방지:
- 관련 커밋/문서:

## 10) 문서 링크

- 아키텍처 다이어그램: `docs/architecture-kr.md`
- 시퀀스 다이어그램: `docs/sequence-kr.md`
- 부하 테스트 리포트: `docs/load-test-kr.md`
- 장애 대응 훈련 리포트: `docs/failure-drill-kr.md`

## 11) 면접 1페이지 요약 포인트

- 왜 이 서비스 분리(`Login/Gateway/Chat/World`)를 선택했는가
- 왜 하이브리드 전송(내부 gRPC + 핫패스 소켓)을 선택했는가
- 메트릭 기반으로 병목을 어떻게 찾고 개선했는가
- 장애를 어떻게 재현/복구했고 MTTR은 얼마였는가

## 12) 라이선스

MIT
