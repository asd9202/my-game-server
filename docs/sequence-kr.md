# 시퀀스 다이어그램

이 문서는 포트폴리오 프로젝트의 핵심 서비스 상호작용을 정리한다.

## 1) 로그인 및 세션 부착

```mermaid
sequenceDiagram
    participant C as Client
    participant L as Login
    participant M as MySQL
    participant G as Gateway
    participant R as Redis
    participant W as World

    C->>L: HTTPS /login (id, credential)
    L->>M: 계정/상태 검증
    M-->>L: account ok
    L-->>C: authToken (short TTL)

    C->>G: Connect + authToken
    G->>R: 토큰/세션 검증
    R-->>G: valid
    G->>W: AttachSession(playerId, gatewayId)
    W-->>G: AttachAck(zoneId, spawnState)
    G-->>C: JoinWorldAck
```

## 2) 채팅 전송 및 로그 저장

```mermaid
sequenceDiagram
    participant C1 as Sender Client
    participant G as Gateway
    participant C as Chat
    participant C2 as Receiver Clients
    participant Q as Log Queue
    participant LW as Log Writer
    participant MG as MongoDB

    C1->>G: ChatSend(channelId, text)
    G->>C: ForwardChat(playerId, channelId, text)
    C->>C2: ChatFanout(message)
    C->>Q: Enqueue(chat_message, action_event)
    Q->>LW: BatchFlush trigger
    LW->>MG: bulkWrite(chat_messages, action_events)
    MG-->>LW: write result
```

## 3) 권한형 이동 검증

```mermaid
sequenceDiagram
    participant C as Client
    participant G as Gateway
    participant W as World
    participant A as AOI Set

    C->>G: MoveInput(seq, dir, timestamp)
    G->>W: ForwardMove(playerId, seq, dir)
    W->>W: 쿨다운/속도/충돌 검증
    W->>W: Tick에서 이동 반영
    W->>A: 주변 수신자 계산
    W-->>G: StateDelta(player, neighbors)
    G-->>C: ReconcileAck(seq, position)
    G-->>C: NeighborStateDelta
```

## 4) 재접속 및 상태 복구

```mermaid
sequenceDiagram
    participant C as Client
    participant G as Gateway
    participant R as Redis
    participant W as World

    C->>G: Reconnect(authToken, lastSeq)
    G->>R: 세션/라우트 조회
    R-->>G: session metadata
    G->>W: RecoverSession(playerId, lastSeq)
    W-->>G: RecoveryState(position, hp, inventory)
    G-->>C: ReconnectAck + snapshot
```

## 5) 메모

- 재현성을 위해 timestamp와 sequence number를 반드시 로그로 남긴다.
- 핫패스 핸들러에서 tick 실행 중 직접 DB write를 피한다.
- 재접속 플로우는 idempotent 연산을 우선한다.
