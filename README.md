# my-game-server

포트폴리오용 간단한 게임 서버 프로젝트입니다.
로그인 · 게이트웨이 · 채팅 서버를 우선 구축하고, 이후 게임 로직 서버로 확장하는 것을 목표로 합니다.

> 현재 상태: **초기 설계 단계** — 아래 내용은 목표 구조이며, 구현되면서 갱신됩니다.

---

## 1. 구성 서버 (1차 범위)

| 서버 | 역할 | 언어 / 런타임 | 클라이언트 ↔ 서버 | 서버 ↔ 서버 |
|---|---|---|---|---|
| **Login Server** | 계정 인증, 세션 토큰 발급, 게이트웨이 주소 안내 | ASP.NET Core (.NET 10 / C# 14) | HTTP | gRPC |
| **Gateway Server** | 클라이언트 TCP 연결 유지, 토큰 검증, 패킷 라우팅 | C++26 | TCP 소켓 | gRPC, TCP 소켓 |
| **Chat Server** | 채널/귓속말 등 채팅 처리, 메시지 브로드캐스트 | C++26 | (게이트웨이 경유) | gRPC, TCP 소켓 |

### 기본 흐름

```
Client ──HTTP──▶ Login Server ──▶ PostgreSQL (계정)
   │                  │
   │                  └──▶ Redis (세션 토큰 저장)
   │
   └──TCP──▶ Gateway Server ──gRPC──▶ Login Server (토큰 검증)
                   │
                   └──TCP / gRPC──▶ Chat Server ──▶ Redis (Pub/Sub, 상태)
```

1. 클라이언트가 Login Server에 HTTP로 로그인 → 세션 토큰과 게이트웨이 주소를 받습니다.
2. 클라이언트가 Gateway Server에 TCP로 접속하고 토큰으로 인증합니다.
3. 이후 채팅 패킷은 게이트웨이를 거쳐 Chat Server로 전달되고, 응답/브로드캐스트는 역방향으로 전달됩니다.

---

## 2. 기술 스택

| 분류 | 사용 기술 |
|---|---|
| 웹 서버 (HTTP) | ASP.NET Core — .NET 10 (LTS), C# 14 |
| 소켓 통신 서버 (TCP) | C++26 |
| 클라이언트 ↔ 서버 통신 | HTTP, TCP 소켓 |
| 서버 ↔ 서버 통신 | gRPC, TCP 소켓 |
| 통신 규격 | **Protocol Buffers** (모든 통신 메시지 정의를 `.proto`로 일원화) |
| 데이터베이스 | PostgreSQL 19 (영속 데이터), Valkey 9.2 — Redis 호환 (세션 / 캐시 / Pub/Sub) — [ADR 0002](docs/adr/0002-datastores-postgresql-valkey.md) |
| 메시지 큐 | 필요 시 요구 스펙에 따라 **Redis 큐(Streams)** 또는 **Kafka** 도입 |
| 배포 | Docker (서비스별 이미지 + Docker Compose) |
| 빌드 | 우선 로컬 빌드 후 테스트 → 이후 CI 등 다른 방식 검토 |

---

## 3. 디렉터리 구조 (모노레포)

```
my-game-server/
├── proto/                  # 모든 .proto 정의 (단일 진실 공급원)
├── services/
│   ├── login-server/       # ASP.NET Core HTTP 로그인 서버
│   ├── gateway-server/     # C++ TCP 게이트웨이 서버
│   └── chat-server/        # C++ 채팅 서버
├── libs/
│   ├── cpp/                # C++ 공용 라이브러리 (네트워크, 패킷 처리 등)
│   └── dotnet/             # .NET 공용 라이브러리
├── tools/
│   └── dummy-client/       # C# 더미 클라이언트 (대화형 · 시나리오 · 부하 테스트)
├── deploy/
│   ├── docker/             # Dockerfile, docker-compose
│   └── db/                 # DB 마이그레이션 / 초기화 스크립트
├── config/                 # 환경별 서비스 설정 (local / dev / prod, 비밀 값 제외)
├── scripts/                # 빌드 / 코드 생성 / 실행 보조 스크립트
└── docs/                   # 설계 문서, ADR
```

---

## 4. 브랜치 전략 (Git Flow + GitHub Flow 혼합)

| 브랜치 | 용도 |
|---|---|
| `master` | 배포 가능한 안정 버전. 릴리스 시 태그(`vX.Y.Z`) 부여 |
| `develop` | 다음 릴리스를 위한 통합 브랜치 |
| `feature/*` | 기능 개발. `develop`에서 분기 → PR로 `develop`에 병합 |
| `fix/*` | 일반 버그 수정. `develop`에서 분기 → PR로 병합 |
| `release/*` | (필요 시) 릴리스 준비. `develop` → `master` 병합 |
| `hotfix/*` | 운영 긴급 수정. `master`에서 분기 → `master`, `develop` 양쪽에 병합 |

- Git Flow의 `master` / `develop` 이원 구조와 릴리스·핫픽스 흐름을 유지하되,
- GitHub Flow처럼 **짧게 유지되는 작업 브랜치 + Pull Request 리뷰 후 병합**을 기본으로 합니다.

---

## 5. 빌드 & 배포

- **현재**: 각 서버를 로컬에서 빌드하고, Docker Compose로 PostgreSQL / Valkey와 함께 띄워 테스트합니다.
- **향후**: CI 파이프라인(예: GitHub Actions)과 이미지 레지스트리를 이용한 빌드/배포 방식을 검토합니다.

자세한 개발 규칙과 명령어는 [AGENTS.md](AGENTS.md)를 참고하세요.

---

## License

[LICENSE](LICENSE) 참고.
