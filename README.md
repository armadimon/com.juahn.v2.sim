# Sim (`com.juahn.v2.sim`)

Layer 3 of the **JuahnFrameworkV2** stack — a **pure C# deterministic tick simulation core**.

- **No UnityEngine dependency** (`noEngineReferences: true`). No `MonoBehaviour`, no `Time`, no `Debug.Log`, no coroutines.
- **Deterministic by construction**: time advances only in integer tick units via `GameSim.Advance(long ticks)`. The same command sequence + same tick count always produces the same state.
- **Domain-agnostic**: ships only the loop primitives (command bus, tick systems, sim/domain events, save/config interfaces). Your game types live in a higher layer and are injected.

## Install (UPM git URL)

Add to `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.juahn.v2.sim": "https://github.com/armadimon/com.juahn.v2.sim.git"
  }
}
```

Or in Unity: **Window > Package Manager > + > Add package from git URL** → `https://github.com/armadimon/com.juahn.v2.sim.git`

This package has **no dependencies** — each JuahnFrameworkV2 package is independent.

## Where it sits in the 3-layer stack

```
Presentation  (MonoBehaviour, UI, input)     — feeds SimClock, drains events
      |  drives via ticks / reads via drain
Game/Domain   (your systems, facades, configs) — implements ICommandDispatcher, ITickSystem, ISaveService, IConfigProvider
      |  injected into
Sim (this)    (pure loop core, no engine refs) — GameSim + GameState + SimClock
```

The lower layer never references the upper. Presentation feeds the clock and drains presentation events; the game layer supplies the rules; this package only orchestrates the deterministic loop.

## Key concepts

| Type | Role |
|------|------|
| `SimConstants` | Loop constants (`TicksPerSecond`, buffer/clamp limits). Not balance values. |
| `GameState` | Command queue + tick counter + sim-event and domain-event buffers. |
| `ICommand` / `ICommandDispatcher` | State-change requests, enqueued by facades, routed at tick boundaries. |
| `ITickSystem` | One tick's worth of state advance. Explicit order = game rules. |
| `ISimEvent` | Intra-tick system-to-system signal, cleared at end of each tick. |
| `IDomainEvent` | Presentation-only output, buffered and drained by the view. |
| `GameSim` | Orchestrator: `Advance(ticks)` + `DrainEvents(buffer)`. |
| `SimClock` | **Pure** real-seconds → integer-ticks accumulator, decoupled from Unity `Time`. |
| `PersistentData` / `ISaveService` / `SavePriority` | Per-PD save unit + save interface. |
| `IConfigProvider` | Typed POCO config supply. |

## Quick usage

```csharp
using System;
using System.Collections.Generic;
using Juahn.V2.Sim;

// 1) Game layer: implement the command router and (optionally) tick systems.
sealed class MyDispatcher : ICommandDispatcher
{
    public void Dispatch(ICommand command)
    {
        switch (command)
        {
            case SpendGoldCommand c: /* validate then mutate your PD */ break;
            default: throw new InvalidOperationException($"unwired command: {command.GetType().Name}");
        }
    }
}

sealed class SpendGoldCommand : ICommand { public readonly long Amount; public SpendGoldCommand(long a){ Amount = a; } }

// 2) Compose the sim.
var state = new GameState();
var tickOrder = new ITickSystem[] { /* producer systems first, consumers last */ };
var sim = new GameSim(state, new MyDispatcher(), tickOrder, errorLog: Console.Error.WriteLine);

// 3) Enqueue a command (facades do this in a real game).
state.Enqueue(new SpendGoldCommand(100));

// 4) Drive it — from Unity, DECOUPLE from Time.timeScale:
//    feed unscaledDeltaTime, express game speed via SimClock.SpeedMultiplier.
var clock = new SimClock { SpeedMultiplier = 1.0 };      // 0 = paused, 2 = double speed
// clock.MaxTicksPerAdvance guards against the spiral-of-death on frame hitches.

// --- per Unity frame (in an Update on the Presentation layer) ---
// int ticks = clock.Advance(Time.unscaledDeltaTime);   // NOT Time.deltaTime!
int ticks = clock.Advance(0.016);                        // example dt
sim.Advance(ticks);

// 5) Drain presentation events after advancing.
var buffer = new List<IDomainEvent>();
bool overflowed = sim.DrainEvents(buffer);               // true => rebuild from a state snapshot
```

### Offline catch-up

For long background returns, do **not** funnel the elapsed time through `SimClock` (it clamps per advance). Compute the tick count from the elapsed server seconds and call `GameSim.Advance(ticks)` directly — the same deterministic path.

## License

MIT — see [LICENSE.md](LICENSE.md).
