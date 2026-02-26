# Sequence Diagrams

This document captures core service interactions used in the portfolio project.

## 1) Login and Session Attach

```mermaid
sequenceDiagram
    participant C as Client
    participant L as Login
    participant M as MySQL
    participant G as Gateway
    participant R as Redis
    participant W as World

    C->>L: HTTPS /login (id, credential)
    L->>M: Validate account and status
    M-->>L: account ok
    L-->>C: authToken (short TTL)

    C->>G: Connect + authToken
    G->>R: Verify token/session
    R-->>G: valid
    G->>W: AttachSession(playerId, gatewayId)
    W-->>G: AttachAck(zoneId, spawnState)
    G-->>C: JoinWorldAck
```

## 2) Chat Message and Log Write

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

## 3) Authoritative Movement Validation

```mermaid
sequenceDiagram
    participant C as Client
    participant G as Gateway
    participant W as World
    participant A as AOI Set

    C->>G: MoveInput(seq, dir, timestamp)
    G->>W: ForwardMove(playerId, seq, dir)
    W->>W: Validate cooldown/speed/collision
    W->>W: Tick apply movement
    W->>A: Compute nearby recipients
    W-->>G: StateDelta(player, neighbors)
    G-->>C: ReconcileAck(seq, position)
    G-->>C: NeighborStateDelta
```

## 4) Reconnect and State Recovery

```mermaid
sequenceDiagram
    participant C as Client
    participant G as Gateway
    participant R as Redis
    participant W as World

    C->>G: Reconnect(authToken, lastSeq)
    G->>R: Resolve session and route
    R-->>G: session metadata
    G->>W: RecoverSession(playerId, lastSeq)
    W-->>G: RecoveryState(position, hp, inventory)
    G-->>C: ReconnectAck + snapshot
```

## 5) Notes

- All timestamps and sequence numbers should be logged for replayability.
- Hot-path handlers should avoid direct DB writes inside tick execution.
- Reconnect flow should prefer idempotent operations.
