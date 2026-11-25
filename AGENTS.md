# AGENTS.md

이 문서는 이 저장소에서 작업하는 **모든 AI 에이전트와 사람 기여자**를 위한 작업 지침입니다.
프로젝트 개요는 [README.md](README.md)에 있으며, 이 문서는 그것을 바탕으로 **어떻게 작업해야 하는지**를 정의합니다.

> 문서 우선순위: 사용자의 명시적 지시 > 이 문서(AGENTS.md) > 각 서비스 디렉터리의 하위 AGENTS.md(생기는 경우) > 일반 관례
> 이 문서와 실제 코드가 어긋나면 **임의로 한쪽에 맞추지 말고** 사용자에게 알리세요.

---

## 0. 현재 상태와 미확정 사항

프로젝트는 **초기 설계 단계**입니다. 아래 항목은 아직 확정되지 않았으며, 에이전트가 임의로 결정하지 않습니다.
결정이 필요한 작업이라면 선택지와 트레이드오프를 제시하고 사용자 확인을 받은 뒤, `docs/adr/`에 ADR로 기록합니다.

| 항목 | 상태 | 후보 |
|---|---|---|
| C++ HTTP 프레임워크 | 미정 | Drogon, Boost.Beast, (기타) |
| C++ 패키지 매니저 | 미정 | vcpkg(manifest 모드), Conan |
| 메시지 큐 | 필요 시 도입 | Redis Streams(경량·단순), Kafka(대용량·재처리·다수 컨슈머) |
| 게이트웨이 ↔ 채팅 전송 방식 | 미정 | gRPC 양방향 스트리밍, 자체 TCP 프로토콜 |
| CI/CD | 미정 (현재 로컬 빌드) | GitHub Actions + 컨테이너 레지스트리 |

결정이 내려지면 이 표를 갱신하세요.

---

## 1. 아키텍처

### 1.1 서비스

| 서비스 | 디렉터리 | 언어 | 외부(클라) 인터페이스 | 내부 인터페이스 | 저장소 |
|---|---|---|---|---|---|
| Login Server | `services/login-server` | C++ | HTTP (REST, protobuf 바디) | gRPC 서버 (토큰 검증 등) | PostgreSQL, Redis |
| Gateway Server | `services/gateway-server` | C# / .NET 10 | TCP 소켓 | gRPC 클라이언트, TCP 소켓 | Redis |
| Chat Server | `services/chat-server` | C# / .NET 10 | 없음 (게이트웨이 경유) | gRPC 서버, TCP 소켓 | Redis (+ 필요 시 PostgreSQL) |

### 1.2 통신 원칙

- **클라이언트 ↔ 서버**: HTTP(로그인 등 요청/응답성 작업), TCP 소켓(실시간 세션).
- **서버 ↔ 서버**: gRPC(요청/응답, 제어성 호출), TCP 소켓(고빈도 패킷 중계가 필요한 경우).
- **모든 메시지 스키마는 protobuf로 정의**합니다. JSON 등 별도 스키마를 새로 만들지 않습니다.
  (HTTP도 기본 바디는 protobuf. 디버깅 편의를 위한 JSON 매핑은 protobuf의 표준 JSON 매핑만 사용)
- 클라이언트는 **게이트웨이만** TCP로 접속합니다. 내부 서버(채팅 등)를 클라이언트에 직접 노출하지 않습니다.
- 서버 간 호출에는 항상 **타임아웃(deadline)** 을 지정합니다.

### 1.3 인증 흐름

1. `POST /v1/auth/login` (Login Server) → 계정 검증(PostgreSQL) → 세션 토큰 발급 → Redis에 TTL과 함께 저장 → 토큰 + 게이트웨이 주소 응답
2. 클라이언트가 게이트웨이에 TCP 접속 → 첫 패킷으로 토큰 전달
3. 게이트웨이가 토큰을 검증(gRPC로 Login Server 호출 또는 Redis 조회 — 구현 시 확정) → 세션 바인딩
4. 인증 전 연결은 인증 패킷 외에는 모두 거부하고, 일정 시간 내 인증하지 않으면 끊습니다.

### 1.4 메시지 큐 도입 기준

메시지 큐는 **필요가 생겼을 때만** 도입합니다. 도입 전 ADR을 작성합니다.

- **Redis Streams / 리스트 큐**: 단일 클러스터 내 비동기 작업, 소규모 이벤트 전달, 운영 부담 최소화가 우선일 때.
- **Kafka**: 이벤트 보존·재처리, 다수 컨슈머 그룹, 높은 처리량, 서비스 간 이벤트 소싱이 필요할 때.
- 채팅 브로드캐스트 같은 휘발성 팬아웃은 우선 **Redis Pub/Sub**로 처리합니다.

---

## 2. 저장소 구조

```
my-game-server/
├── AGENTS.md / CLAUDE.md / README.md
├── proto/                        # protobuf 단일 진실 공급원 (Single Source of Truth)
│   ├── common/                   # 공통 타입, 에러 코드, 패킷 ID
│   ├── client/                   # 클라이언트 ↔ 서버 메시지 (login, gateway, chat)
│   └── internal/                 # 서버 ↔ 서버 메시지 및 gRPC 서비스 정의
├── services/
│   ├── login-server/             # C++ (CMake)
│   │   ├── src/  include/  tests/
│   │   └── CMakeLists.txt
│   ├── gateway-server/           # .NET (src/, tests/)
│   └── chat-server/              # .NET (src/, tests/)
├── libs/
│   ├── cpp/                      # C++ 공용 코드 (로깅, 설정, DB 래퍼 등)
│   └── dotnet/                   # .NET 공용 코드 (TCP 네트워킹, 패킷 처리, 생성된 proto 등)
├── tools/                        # 더미 클라이언트, 부하 테스트 도구
├── deploy/
│   ├── docker/                   # 서비스별 Dockerfile, docker-compose.yml
│   └── db/migrations/            # PostgreSQL 마이그레이션 (버전 번호 순)
├── scripts/                      # proto 코드 생성, 빌드, 로컬 실행 스크립트
└── docs/
    └── adr/                      # 아키텍처 결정 기록 (NNNN-title.md)
```

규칙:
- 새 서비스는 `services/<name>-server/` 형식으로 추가합니다.
- 두 개 이상의 서비스가 쓰는 코드는 `libs/`로 올립니다. 서비스 간 소스 직접 참조는 금지합니다.
- **생성 코드(protobuf/gRPC 출력물)는 커밋하지 않습니다.** 빌드 시 생성합니다.
- 디렉터리가 아직 없다면 위 구조에 맞춰 생성하되, 구조 자체를 바꾸려면 사용자 확인을 받습니다.

---

## 3. Protobuf 규칙

`proto/`는 모든 언어가 공유하는 계약입니다. 변경 시 가장 신중해야 하는 영역입니다.

### 3.1 작성 규칙
- `syntax = "proto3";` 사용.
- 패키지: `mygame.<영역>.v<버전>` (예: `mygame.chat.v1`, `mygame.internal.auth.v1`).
- C# 네임스페이스 옵션: `option csharp_namespace = "MyGame.Protocol.Chat.V1";` 형식.
- 파일명 `snake_case.proto`, 메시지 `PascalCase`, 필드 `snake_case`, enum 값 `UPPER_SNAKE_CASE` + 타입명 접두사, 첫 값은 `<TYPE>_UNSPECIFIED = 0`.
- 요청/응답 쌍은 `XxxRequest` / `XxxResponse`로 명명합니다.
- 에러는 공통 `ErrorCode` enum(`proto/common`)을 사용합니다.

### 3.2 호환성 (반드시 준수)
- 이미 사용된 **필드 번호를 재사용하거나 타입을 변경하지 않습니다.**
- 필드 삭제 시 `reserved`로 번호와 이름을 예약합니다.
- 호환이 깨지는 변경은 새 버전 패키지(`v2`)로 추가합니다.
- proto를 변경하면 **영향받는 모든 서비스(C++/C#)를 함께 빌드**해 확인합니다.

### 3.3 TCP 패킷 프레이밍 (초안 — 구현 시 ADR로 확정)
```
+----------------+----------------+---------------------------+
| length (u32)   | packet_id (u16)| payload (protobuf bytes)  |
| big-endian     | big-endian     | length - 2 bytes          |
+----------------+----------------+---------------------------+
```
- `packet_id`는 `proto/common`의 enum으로 관리합니다. 숫자를 코드에 하드코딩하지 않습니다.
- 최대 패킷 크기를 설정값으로 두고, 초과 시 연결을 끊습니다.
- 수신 측은 부분 수신(fragmentation)과 다중 패킷 합쳐짐(coalescing)을 모두 처리해야 합니다.

---

## 4. 언어별 코딩 규칙

### 4.1 C++ (Login Server, `libs/cpp`)
- 표준: **C++26** (`CMAKE_CXX_STANDARD 26`, `CMAKE_CXX_STANDARD_REQUIRED ON`). 컴파일러가 해당 값을 지원하지 않으면 `-std=c++2c`를 사용합니다. 그 외 표준 플래그를 임의로 바꾸지 마세요.
  존재하지 않거나 지원되지 않는 표준 플래그를 임의로 넣지 마세요.
- 빌드 시스템: **CMake**(+ Ninja 권장). 패키지 매니저는 §0 결정에 따름.
- 경고: `-Wall -Wextra -Wpedantic`(MSVC는 `/W4`), 신규 코드는 경고 0 유지.
- 포맷/정적 분석: `.clang-format`, `.clang-tidy`를 저장소 루트에 두고 따릅니다.
- 명명: 타입 `PascalCase`, 함수/변수 `snake_case`, 멤버 변수 `snake_case_`, 상수 `kPascalCase`, 네임스페이스 `mygame::login`.
- 메모리: raw `new`/`delete` 금지, RAII와 스마트 포인터 사용. 소유권이 없는 참조는 참조/`std::span`/`std::string_view`.
- 에러 처리: 예상 가능한 실패는 `std::expected`, 예외는 진짜 예외 상황에만.
- 비동기 I/O 스레드에서 블로킹 호출(DB 동기 쿼리, `sleep` 등) 금지.
- 테스트: GoogleTest 또는 Catch2(최초 도입 시 하나로 통일) + CTest.

### 4.2 C# (Gateway / Chat Server, `libs/dotnet`)
- 대상 프레임워크 `net10.0`(.NET 10 LTS), 언어 버전은 SDK 기본값(C# 14) — `LangVersion`을 `preview`/`latest`로 올리지 않습니다. `Directory.Build.props`에서 공통 설정을 관리합니다.
- `<Nullable>enable</Nullable>`, `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`, `<ImplicitUsings>enable</ImplicitUsings>`.
- 패키지 버전은 `Directory.Packages.props`(Central Package Management)로 중앙 관리합니다.
- 포맷: `.editorconfig` 기준, `dotnet format`으로 검증.
- 명명: .NET 표준 규칙(타입/메서드/프로퍼티 `PascalCase`, 지역변수/매개변수 `camelCase`, private 필드 `_camelCase`, 비동기 메서드 `Async` 접미사).
- 비동기: `async`/`await` 사용, `.Result`/`.Wait()` 금지, `CancellationToken`을 끝까지 전달.
- 네트워킹: `System.IO.Pipelines` 기반 소켓 처리를 우선 고려하고, 핫패스에서 불필요한 할당을 피합니다(`ArrayPool`, `Span<T>`, `Memory<T>`).
- 호스팅: Generic Host(`Microsoft.Extensions.Hosting`) + DI + `ILogger` 구조화 로깅. 설정은 `IOptions<T>`.
- 주요 라이브러리(예정): `Grpc.AspNetCore` / `Grpc.Net.Client`, `Google.Protobuf` + `Grpc.Tools`(빌드 시 proto 생성), `Npgsql`, `StackExchange.Redis`.
- 테스트: xUnit(최초 도입 시 통일), `tests/<Project>.Tests`.

### 4.3 공통
- 로그는 구조화 로그로 남기며 **토큰, 비밀번호, 개인정보를 로그에 남기지 않습니다.**
- 매직 넘버 대신 설정값 또는 명명된 상수를 사용합니다.
- 주석은 "왜"를 설명합니다. 코드를 그대로 반복하는 주석은 쓰지 않습니다.
- 요청받지 않은 대규모 리팩터링, 무관한 파일 수정은 하지 않습니다.

---

## 5. 데이터 저장소 규칙

### 5.1 PostgreSQL
- 스키마 변경은 **반드시 마이그레이션 파일**로만 합니다: `deploy/db/migrations/V<번호>__<설명>.sql` (예: `V0001__create_accounts.sql`).
- 이미 적용된(커밋된) 마이그레이션 파일은 수정하지 않고 새 파일을 추가합니다.
- 테이블/컬럼은 `snake_case`, 테이블명은 복수형(`accounts`, `characters`).
- 시간은 `timestamptz`(UTC)로 저장합니다.
- 쿼리는 항상 **파라미터 바인딩** 사용. 문자열 연결로 SQL을 만들지 않습니다.
- 비밀번호는 Argon2id(또는 bcrypt)로 해시하여 저장합니다. 평문/가역 암호화 금지.

### 5.2 Redis
- 키 형식: `<서비스>:<도메인>:<식별자>` (예: `login:session:{token}`, `chat:channel:{id}:members`).
- 세션/캐시 키에는 **TTL을 반드시** 설정합니다.
- `KEYS *` 같은 O(N) 전체 스캔 명령을 서비스 코드에서 사용하지 않습니다(`SCAN` 사용).
- Pub/Sub 채널명도 같은 접두사 규칙을 따릅니다 (예: `chat:broadcast:{channelId}`).

---

## 6. 빌드 · 실행 · 테스트

> 아래 명령은 목표 형태입니다. 해당 설정 파일이 아직 없다면 이 형태에 맞춰 만들고, 실제와 달라지면 이 절을 갱신하세요.

### 6.1 사전 준비
- C++: 최신 Clang 또는 GCC(C++26 지원), CMake, Ninja
- .NET 10 SDK
- Docker / Docker Compose
- `protoc` 및 gRPC 플러그인(C++ 생성용; C#은 `Grpc.Tools`가 처리)

### 6.2 인프라(로컬)
```bash
docker compose -f deploy/docker/docker-compose.yml up -d postgres redis
```

### 6.3 C++ (Login Server)
```bash
cmake -S services/login-server -B build/login-server -G Ninja -DCMAKE_BUILD_TYPE=Debug
cmake --build build/login-server
ctest --test-dir build/login-server --output-on-failure
```

### 6.4 .NET (Gateway / Chat Server)
```bash
dotnet build MyGameServer.sln
dotnet test MyGameServer.sln
dotnet format MyGameServer.sln --verify-no-changes
dotnet run --project services/gateway-server/src/GatewayServer
```

### 6.5 전체 컨테이너 실행
```bash
docker compose -f deploy/docker/docker-compose.yml up --build
```

### 6.6 테스트 원칙
- 새 기능/버그 수정에는 테스트를 함께 추가합니다. 버그 수정은 재현 테스트를 먼저 작성합니다.
- 단위 테스트는 외부 의존(DB, Redis, 네트워크) 없이 실행 가능해야 합니다.
- 통합 테스트는 Docker로 띄운 PostgreSQL/Redis를 사용하며 별도로 구분합니다.
- `skip`/`only` 처리된 테스트, 빈 테스트, TODO 스텁은 "완료"로 간주하지 않습니다.

---

## 7. 설정 · 보안

### 7.1 민감 정보 하드코딩 절대 금지

다음 값은 **어떤 경우에도 실제 값을 그대로 작성하지 않습니다.** 예외는 없으며, 사용자가 값을 알려주더라도 코드에 직접 넣지 않고 환경 변수로 받도록 작성합니다.

| 금지 대상 | 예시 |
|---|---|
| 환경 파일 내용 | `.env`, `.env.*`의 실제 값 |
| 비밀 키·자격 증명 | 비밀번호, API 키, 토큰 서명 키(JWT secret 등), 암호화 키, 개인 키, 인증서(`*.pem`, `*.pfx`, `*.key`) |
| 접속 정보 | 실제 DB/Redis 연결 문자열, 계정명·비밀번호가 포함된 URL |
| 네트워크 주소 | 실제 서버·클라우드·사내망의 IP 주소, 호스트명, 도메인, 외부 노출 포트 |

적용 범위: 소스 코드, 설정 파일(`appsettings*.json`, CMake 옵션 등), `Dockerfile`, `docker-compose.yml`, 스크립트, 테스트 코드, 주석, 문서, 커밋 메시지, PR 본문, 로그 출력 — **저장소에 들어가거나 외부로 출력되는 모든 곳**.

작성 방법:
- 값은 **환경 변수**로 주입하고, 코드에는 변수 이름만 둡니다. (C#: `IConfiguration`/`IOptions<T>`, C++: 설정 로더를 통한 환경 변수 조회)
- 필요한 변수 목록은 `.env.example`에 **이름과 자리표시자만** 적습니다. (예: `POSTGRES_PASSWORD=change-me`, `REDIS_HOST=<redis-host>`)
- Compose에서는 `${VAR}` 치환과 `env_file`을 사용합니다.
- 필수 값이 없으면 기본값으로 대체하지 말고 **시작 시점에 명확한 에러로 실패**하게 합니다.

허용되는 예외 (로컬 개발 한정, 비밀이 아닌 값):
- 루프백 주소 `localhost`, `127.0.0.1`, `::1`, 바인딩용 `0.0.0.0`
- Docker Compose 내부 서비스명 (`postgres`, `redis`, `login-server` 등)
- 테스트 전용으로 명백히 가짜인 값 (예: `test-secret-do-not-use`)

발견·사고 시:
- 작업 중 하드코딩된 민감 정보를 발견하면 그 값을 출력하거나 인용하지 말고, **파일 위치만** 사용자에게 알립니다.
- 민감 정보가 커밋되었다면 파일에서 지우는 것만으로는 부족합니다(히스토리에 남음). 사용자에게 알리고 **해당 키·비밀번호 교체(rotation)** 를 권고합니다. 히스토리 재작성은 사용자 확인 후에만 합니다.

### 7.2 기타 보안 규칙

- 외부 입력(HTTP 바디, TCP 패킷)은 크기·형식·범위를 검증합니다. 역직렬화 실패는 연결 종료/에러 응답으로 처리합니다.
- 로그인 시도에는 레이트 리밋을 고려합니다.
- 내부 gRPC 포트는 외부에 노출하지 않습니다(Compose 네트워크 내부 전용).

---

## 8. Docker 배포 규칙

- 서비스마다 `deploy/docker/<service>.Dockerfile`(또는 서비스 디렉터리 내 `Dockerfile`)을 두고 **멀티 스테이지 빌드**를 사용합니다.
  - C++: 빌드 이미지에서 컴파일 → 경량 런타임 이미지로 바이너리만 복사.
  - .NET: `sdk` 이미지에서 `dotnet publish` → `aspnet`/`runtime` 이미지로 복사.
- 컨테이너는 non-root 사용자로 실행합니다.
- 설정은 환경 변수로 주입하고, 헬스체크를 정의합니다.
- 이미지 태그는 `latest`에만 의존하지 않고 버전/커밋 해시를 함께 사용합니다.
- 현재는 **로컬 빌드 → 로컬 Compose 테스트**가 기본입니다. CI/레지스트리 도입은 §0 결정 후 진행합니다.

---

## 9. Git 워크플로

### 9.1 브랜치 (Git Flow + GitHub Flow 혼합)
| 브랜치 | 분기 원천 | 병합 대상 | 비고 |
|---|---|---|---|
| `master` | — | — | 안정/배포 버전, 릴리스 태그 `vX.Y.Z` |
| `develop` | `master` | — | 통합 브랜치 |
| `feature/<짧은-설명>` | `develop` | `develop` (PR) | 짧게 유지 |
| `fix/<짧은-설명>` | `develop` | `develop` (PR) | |
| `release/<버전>` | `develop` | `master` + `develop` | 필요 시에만 |
| `hotfix/<짧은-설명>` | `master` | `master` + `develop` | 긴급 수정 |

- `master`, `develop`에 **직접 커밋/푸시하지 않습니다.** 모든 변경은 PR을 거칩니다.
- 작업 브랜치는 작고 짧게 유지하고, 병합 전 대상 브랜치를 최신으로 반영합니다.
- 에이전트는 사용자가 요청하지 않는 한 **커밋·푸시·PR 생성·병합을 하지 않습니다.** force push, 히스토리 재작성도 금지입니다.

### 9.2 커밋 메시지 (Conventional Commits)
```
<type>(<scope>): <요약>

<본문: 무엇을, 왜>
```
- `type`: `feat`, `fix`, `refactor`, `perf`, `test`, `docs`, `build`, `ci`, `chore`
- `scope`: `login`, `gateway`, `chat`, `proto`, `libs`, `deploy`, `db`, `docs` 등
- 예: `feat(chat): 채널 입장/퇴장 패킷 처리 추가`

### 9.3 PR
- 제목은 커밋 규칙과 동일한 형식.
- 본문: 변경 요약, 변경 이유, 테스트 방법/결과, proto·DB 스키마 변경 여부.
- proto 또는 DB 마이그레이션 변경이 있으면 PR 본문에 명시합니다.

---

## 10. 에이전트 작업 원칙

1. **먼저 읽고, 그다음 수정**: 관련 코드·proto·설정을 확인하고 기존 패턴을 따릅니다.
2. **범위 유지**: 요청된 작업만 수행합니다. 개선점이 보이면 수정하지 말고 제안합니다.
3. **미확정 사항은 결정하지 않기**: §0의 항목은 사용자 확인 없이 선택하지 않습니다.
4. **계약 변경 주의**: proto, DB 스키마, Redis 키 형식, 패킷 프레이밍 변경은 영향 범위를 설명하고 확인을 받습니다.
5. **검증 후 보고**: 빌드·테스트를 실제로 실행한 결과로 완료를 판단합니다. 실행하지 못했다면 그렇다고 명확히 보고합니다.
6. **비밀 정보 보호**: `.env`, 키, 인증서 등을 읽어 출력하거나 커밋하지 않으며, 환경 파일 내용·비밀 키·실제 IP/호스트를 코드에 절대 그대로 작성하지 않습니다 (§7.1).
7. **파괴적 작업 금지**: 데이터 삭제, `docker compose down -v`, 마이그레이션 롤백, `git reset --hard` 등은 사용자 확인 후에만.
8. **문서 동기화**: 구조·명령·결정이 바뀌면 README.md / AGENTS.md / ADR을 함께 갱신합니다.

### 완료 기준 (Definition of Done)
- [ ] 관련 서비스가 경고 없이 빌드된다
- [ ] 새/변경 로직에 대한 테스트가 있고 통과한다
- [ ] 포맷터(clang-format / dotnet format) 검증을 통과한다
- [ ] proto·DB·설정 변경 시 영향받는 모든 서비스가 빌드된다
- [ ] TODO 스텁, 비활성화된 테스트가 없다
- [ ] 비밀 값·환경 파일 내용·실제 IP/호스트가 하드코딩되어 있지 않다 (§7.1)
- [ ] 필요한 문서(README / AGENTS / ADR)가 갱신되었다
