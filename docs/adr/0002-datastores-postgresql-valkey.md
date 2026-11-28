# 0002. 데이터 저장소 버전과 환경별 구성: PostgreSQL 19 + Valkey 9.2

- 상태: 채택
- 날짜: 2026-11-28

## 배경

Login Server는 PostgreSQL(계정)과 Redis(세션 토큰)를, Chat Server는 Redis(Pub/Sub, 상태)를 사용할 예정이다(AGENTS.md §1.1).
다음 두 가지를 정해야 했다.

1. 어떤 버전·제품을 쓸 것인가.
2. 로컬 개발, test/qa, prod 환경에서 저장소를 어떻게 띄울 것인가.
   로컬 개발은 개발자 PC에서, test/qa 이후는 별도의 클라우드 또는 베어메탈 서버에서 운영할 예정이다.

Redis는 7.4부터 라이선스가 오픈소스(BSD)가 아닌 RSAL/SSPL로 바뀌었다(8.0부터 AGPL 선택 가능).
주요 클라우드 관리형 서비스는 Redis와 프로토콜이 호환되는 BSD 라이선스 포크인 Valkey를 지원한다.

## 결정

### 버전

| 저장소 | 제품 | 버전 | 로컬 이미지 |
|---|---|---|---|
| 관계형 DB | PostgreSQL | 19 | `postgres:19` |
| 키-값 저장소 | Valkey (Redis 프로토콜 호환) | 9.2 | `valkey/valkey:9.2` |

- 이미지 태그는 **메이저(PostgreSQL) / 메이저.마이너(Valkey)까지 고정**하고 패치 버전은 따라간다. `latest`는 쓰지 않는다.
- 모든 환경(local, dev, qa, prod)에서 **고정한 단위까지 같은 버전**을 쓴다. PostgreSQL은 메이저(19), Valkey는 메이저.마이너(9.2)가 같아야 한다.
  관리형 서비스를 쓸 때도 엔진 버전을 이에 맞춘다. (Valkey는 마이너 버전에서도 기능·기본값이 바뀌므로 메이저만 같아서는 부족하다)
- 문서와 코드에서 말하는 "Redis"(키 규칙 §5.2, `StackExchange.Redis`, redis-plus-plus 등)는 Redis 프로토콜을 뜻하며, 실제 서버는 Valkey다.

### 환경별 구성

| 환경 | 구성 |
|---|---|
| local | `deploy/docker/docker-compose.yml`로 컨테이너 실행 |
| dev / qa / prod (클라우드) | 관리형 서비스 우선 (PostgreSQL 관리형 DB, Valkey 관리형 캐시) |
| dev / qa / prod (베어메탈) | Docker Compose + named volume + 백업, 또는 호스트 직접 설치. 서버를 준비할 때 결정한다 |

어떤 환경이든 서비스는 **접속 정보를 환경 변수로만** 받는다(§7.1). 환경마다 달라지는 것은 주입되는 값뿐이고, 코드와 `config/<환경>/` 구조는 같다.

### 로컬 Compose 규칙

- 접속 정보(계정, 비밀번호, DB 이름)는 `deploy/docker/.env`에서 읽는다. 저장소에는 자리표시자만 담은 `.env.example`만 커밋한다.
  값이 없으면 `${VAR:?메시지}` 문법으로 Compose가 바로 실패한다.
  `POSTGRES_USER`/`POSTGRES_PASSWORD`/`POSTGRES_DB`는 볼륨이 비어 있을 때(최초 초기화)만 적용된다는 점에 주의한다.
- 포트는 `127.0.0.1`에만 바인딩해 같은 네트워크의 다른 기기에서 접근하지 못하게 한다.
- 로컬에서도 Valkey에 비밀번호(`requirepass`)를 건다. 관리형 서비스는 보통 인증을 요구하므로 연결 코드가 환경마다 갈라지지 않게 하기 위해서다.
  비밀번호는 `command` 인자가 아니라 `VALKEY_EXTRA_FLAGS` 환경 변수로 넘긴다. 명령줄 인자는 같은 호스트의 모든 사용자가 프로세스 목록으로 볼 수 있기 때문이다.
  이 변수는 단어 분리되므로 비밀번호에 공백·글롭 문자를 쓰지 않는다.
- 컨테이너는 non-root로 실행한다(AGENTS.md §8). 공식 이미지의 진입 스크립트가 권한을 낮추므로, `command`를 바꿀 때 이 동작을 우회하지 않게 한다.
- 헬스체크는 클라이언트와 같은 경로(TCP)로 확인한다. PostgreSQL은 `pg_isready -h 127.0.0.1`을 쓴다.
- 데이터는 named volume에 저장한다. PostgreSQL 18 이상 이미지는 데이터 경로가 `/var/lib/postgresql/<메이저>/docker`로 바뀌었으므로 볼륨은 `/var/lib/postgresql`에 마운트한다.

## 이유

- **로컬에서 Docker를 쓰는 이유**: 팀원 모두가 같은 버전을 쓰고, 초기화가 쉽고, 개발 PC에 데몬을 설치하지 않아도 된다. 통합 테스트(§6.6)도 같은 방식으로 저장소를 띄울 수 있다.
- **클라우드에서 관리형을 우선하는 이유**: 백업, 패치, 장애 조치, 모니터링을 직접 운영하지 않아도 된다. 컨테이너로 DB를 띄워도 이 운영 작업은 그대로 남는다.
- **Valkey를 고른 이유**: Redis와 명령·프로토콜이 호환되어 클라이언트 라이브러리를 그대로 쓸 수 있고, BSD 라이선스이며, 클라우드 관리형 서비스에서도 제공된다.
- **PostgreSQL 19를 고른 이유**: 새 프로젝트이므로 지원 기간이 가장 긴 최신 메이저 버전에서 시작한다.

## 결과

- 마이그레이션 도구(AGENTS.md §0)는 이 결정과 별개다. Compose는 `deploy/db/migrations/`를 자동 실행하지 않는다.
  도구가 정해지면 같은 마이그레이션 파일을 모든 환경에 적용한다.
- PostgreSQL 메이저 버전을 올리면 기존 볼륨 데이터를 그대로 쓸 수 없다(`pg_upgrade` 또는 덤프/복원 필요). 로컬에서는 볼륨을 초기화하고 다시 시작한다.
- Valkey 고유 기능(Redis에는 없는 명령)은 쓰지 않는다. 필요해지면 이 ADR을 갱신한다.
- 베어메탈 운영 시 Valkey 권장 커널 설정(`vm.overcommit_memory=1`, Transparent Huge Pages 비활성화)은 컨테이너가 아니라 호스트에서 해야 한다.
