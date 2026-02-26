# 아키텍처

## 1) 목표

- 권한형 월드 시뮬레이션 역량을 보여주는 MMO 코어 서버 포트폴리오를 구축한다.
- 메트릭, 장애 훈련, 운영 도구를 통해 신뢰성을 증명한다.
- 전송 계층을 실용적으로 분리한다: 외부 로그인 HTTP, 내부 제어 gRPC, 핫패스 소켓.

## 2) 시스템 컨텍스트

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

## 3) 서비스 책임

### Login

- 계정 인증, 차단/버전 검사 수행.
- Gateway 연결에 사용할 단기 토큰 발급.

### Gateway

- 클라이언트 세션 라이프사이클 및 재접속 처리 담당.
- World/Chat 서비스로 트래픽 라우팅.
- 기본 rate limit 및 패킷 형태 검증 적용.

### World

- 권한형 tick loop 및 AOI 수행.
- 이동/전투/스킬 요청 검증.
- 플레이어에게 상태 스냅샷 또는 델타 브로드캐스트.

### Chat

- global/zone/party 채팅 fanout 처리.
- 제재 정책(mute/ban) 적용.
- 채팅/행동 이벤트를 비동기 로그 파이프라인으로 전송.

### Admin

- 공지 발송, 제재 수행, 활성 세션 조회.
- 라이브 서비스 시뮬레이션에 필요한 운영 제어 기능 제공.

## 4) 데이터 설계

### MySQL (정합성 기준 데이터)

- 계정, 캐릭터 프로필, 성장, 인벤토리.
- 강한 정합성 및 관계형 제약 사용.

### Redis (운영 핫데이터)

- 세션 캐시, 라우팅 힌트, 짧은 TTL 상태값.
- 런타임 조회를 위한 고속 read/write.

### MongoDB (append 중심 로그)

- `chat_messages`: 채널 중심 채팅 이력.
- `action_events`: 감사/분석용 게임 액션 이벤트.
- write 경로는 큐 + 배치 flush로 구성해 게임 루프 지연을 분리.

## 5) 통신 모델

- Client -> Login: HTTPS.
- Client -> Gateway: custom socket.
- Gateway <-> World 핫패스: custom socket + Protobuf.
- 서비스 제어 호출: gRPC + Protobuf.

## 6) 배포 모델

- 로컬: docker-compose로 전체 스택 기동.
- 클러스터: Kubernetes.
  - Login/Chat/Admin: Deployment.
  - Gateway: Deployment + L4 Service.
  - World: 초기 Deployment, 이후 Agones 확장 가능.

## 7) 신뢰성과 복구

- Gateway 재시작: 토큰 기반 재접속 플로우로 세션 재부착.
- World 재시작: 재접속 핸드셰이크로 캐릭터 컨텍스트 재바인딩.
- Mongo 지연: 비동기 로그 라이터로 tick loop 블로킹 방지.

## 8) 관측성

- 메트릭: tick drift, p95/p99 latency, CCU, 재접속 성공률, 에러율.
- 대시보드: Prometheus + Grafana.
- 로그: 구조화 서비스 로그 + MongoDB 액션 트레일.

## 9) 현재 계획 스냅샷

- 개발 순서(8주): 기반/계약 정의 -> Chat + Mongo 로그 v1 -> Chat/Log 고도화 -> Docker/Compose 통합 -> World MVP -> World Core -> Kubernetes + 관측 -> 관리자 페이지 -> 부하/장애 훈련 + 포트폴리오 패키징.
- 프로토콜 분리: 외부 로그인 `HTTPS`, 내부 제어 `gRPC + Protobuf`, 핫패스 `custom socket + Protobuf`.
- 코어 스택: `C++23`, `Asio`, `Protobuf`, `gRPC`, `MySQL`, `Redis`, `MongoDB`, `Docker`, `Kubernetes`.
- 관리자 스택(후반): 백엔드 `NestJS + TypeScript`, 프론트엔드 `React` (권장 `Next.js + TypeScript`).

## 10) 트레이드오프

- gRPC만 전면 적용하면 계약 관리는 쉬우나 이동 핫패스에는 비효율적일 수 있다.
- 커스텀 소켓만 전면 적용하면 제어력은 높지만 운영 복잡도가 커진다.
- 선택한 하이브리드 구조는 지연, 유지보수성, 면접 설명력의 균형을 맞춘다.
