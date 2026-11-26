# 0001. ASP.NET Core API 스타일: Minimal API

- 상태: 채택
- 날짜: 2026-11-26

## 배경

Login Server(ASP.NET Core)의 HTTP API 작성 스타일이 미정이었다(AGENTS.md §0). 후보는 Minimal API와 Controller(`[ApiController]`)였다.

## 결정

**Minimal API**를 사용한다.

- 엔드포인트는 `Endpoints/<영역>Endpoints.cs`의 `Map<영역>Endpoints()` 확장 메서드로 정의하고, `MapGroup("/v1/<영역>")`으로 묶는다.
- protobuf 바디는 공용 바인더 `ProtobufBody<T>`(요청)와 `ProtobufResult<T>`(응답)로만 처리한다. 엔드포인트에서 직접 파싱하지 않는다.
- 엔드포인트에는 `.Accepts<T>("application/x-protobuf")`를 붙여 다른 Content-Type은 라우팅 단계에서 415로 거부한다.

## 이유

- 로그인 서버는 엔드포인트 수가 적고, 바디가 JSON이 아닌 protobuf라 Controller의 모델 바인딩·포매터 이점이 작다.
- 보일러플레이트가 적고 시작 속도·성능 면에서 유리하다.
- 필터(`IEndpointFilter`), 그룹, 레이트 리미터(`RequireRateLimiting`) 등 필요한 기능을 Minimal API에서도 모두 쓸 수 있다.

## 결과

- Controller 기반 필터·컨벤션은 쓰지 않는다. 공통 처리는 엔드포인트 필터나 미들웨어로 구현한다.
- 엔드포인트가 크게 늘어나 구조화가 어려워지면 이 결정을 재검토한다.
