# Changelog

All notable changes to this package are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.1.0] - 2026-07-20

### Added
- Initial release of the pure C# deterministic simulation core (`Juahn.V2.Sim`).
- `SimConstants` — loop constants (`TicksPerSecond`, `SecondsPerTick`, buffer/clamp limits).
- `GameState` — command queue, tick counter, sim-event buffer, and overflow-guarded domain-event buffer.
- `GameSim` — orchestrator with `Advance(long ticks)` (drains commands, runs `ITickSystem` list in explicit order, clears per-tick sim events, increments tick) and `DrainEvents(List)` for presentation.
- `SimClock` — pure real-seconds → integer-tick accumulator, decoupled from Unity `Time`, with `SpeedMultiplier`, `MaxTicksPerAdvance` clamp (spiral-of-death guard), and `HitchPolicy` (Drop/Accumulate) for mid-session hitches.
- Interfaces: `ICommand`, `ICommandDispatcher`, `ISimEvent`, `IDomainEvent`, `ITickSystem`, `IConfigProvider`, `ISaveService`.
- Persistence primitives: `PersistentData` base, `SavePriority` enum.
- Assembly definition `juahn.v2.Sim` with `noEngineReferences: true` and no other package references.
