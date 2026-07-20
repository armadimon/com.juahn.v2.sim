using System;
using System.Collections.Generic;

namespace Juahn.V2.Sim
{
    /// <summary>
    /// 결정론적 tick 시뮬레이션의 오케스트레이터. 시간은 <see cref="Advance"/>(ticks)로만 전진하며,
    /// sim은 실시간(Unity Time.deltaTime)을 알지 못한다 — 실시간→틱 변환은 <see cref="SimClock"/>이 sim 밖에서 담당한다.
    ///
    /// 각 tick 경계에서: (1) 커맨드 큐를 모두 소비 → (2) 명시적 순서로 <see cref="ITickSystem"/> 목록 실행
    /// → (3) sim 내부 이벤트 소거 → (4) tick 카운터 증가. 이 순서가 곧 게임 규칙이며 한 곳에서만 결정된다.
    ///
    /// 이 클래스 자체는 게임 도메인을 모른다. 게임 계층이 <see cref="ICommandDispatcher"/>와 tick 시스템 목록을
    /// 주입해 조립한다(파사드/시스템은 게임 계층 소유).
    /// </summary>
    public sealed class GameSim
    {
        private readonly GameState _state;
        private readonly ICommandDispatcher _dispatcher;
        private readonly IReadOnlyList<ITickSystem> _tickOrder;
        private readonly Action<string> _errorLog;

        /// <summary>공유 상태(커맨드 적재·이벤트 버퍼). 파사드가 여기에 커맨드를 Enqueue한다.</summary>
        public GameState State => _state;

        /// <summary>현재까지 전진한 총 tick 수.</summary>
        public long CurrentTick => _state.Tick;

        /// <summary>
        /// sim을 조립한다.
        /// </summary>
        /// <param name="state">공유 상태. null이면 새로 만든다.</param>
        /// <param name="dispatcher">커맨드 라우터(게임 계층 구현). 필수.</param>
        /// <param name="tickOrder">
        /// tick 실행 순서 목록. 순서 = 게임 규칙(생산자 먼저, 소비자 뒤). null/빈 목록이면 커맨드 처리만 수행한다.
        /// 방어적 복사본을 보관하므로 호출 측이 이후 원본을 바꿔도 sim은 영향받지 않는다.
        /// </param>
        /// <param name="errorLog">커맨드 처리 중 격리된 예외를 보고할 로거(선택). null이면 조용히 무시.</param>
        /// <exception cref="ArgumentNullException"><paramref name="dispatcher"/>가 null일 때.</exception>
        public GameSim(
            GameState state,
            ICommandDispatcher dispatcher,
            IReadOnlyList<ITickSystem> tickOrder = null,
            Action<string> errorLog = null)
        {
            _state = state ?? new GameState();
            _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));

            // 방어적 복사: 외부가 넘긴 목록의 후속 변경으로 tick 순서가 흔들리지 않게 한다(불변 스냅샷).
            if (tickOrder != null && tickOrder.Count > 0)
            {
                var copy = new ITickSystem[tickOrder.Count];
                for (int i = 0; i < tickOrder.Count; i++)
                {
                    copy[i] = tickOrder[i]
                        ?? throw new ArgumentException($"tickOrder[{i}]가 null입니다.", nameof(tickOrder));
                }

                _tickOrder = copy;
            }
            else
            {
                _tickOrder = Array.Empty<ITickSystem>();
            }

            _errorLog = errorLog;
        }

        /// <summary>
        /// sim을 <paramref name="ticks"/>만큼 전진시킨다. 온라인 정상 플레이와 오프라인 대량 따라잡기가 동일 경로를 탄다.
        /// 각 tick마다 커맨드 소비 → tick 시스템 실행 → sim 이벤트 소거 → tick 증가.
        /// </summary>
        /// <param name="ticks">전진할 tick 수. 0 이하이면 아무것도 하지 않는다(음수는 오류로 취급하지 않고 no-op).</param>
        public void Advance(long ticks)
        {
            for (long i = 0; i < ticks; i++)
            {
                ProcessCommands();

                for (int s = 0; s < _tickOrder.Count; s++)
                {
                    _tickOrder[s].Tick();
                }

                // sim 내부 이벤트는 그 tick 안에 소비되고 끝에 소거된다. 소비 시스템은 tick 목록에서 생산자 뒤에 있다.
                _state.ClearSimEvents();

                _state.Tick++;
            }
        }

        /// <summary>
        /// 쌓인 연출용 도메인 이벤트를 <paramref name="buffer"/>로 옮기고 sim 내부 버퍼를 비운다.
        /// Presentation이 프레임마다 Advance 뒤에 호출해 자기 타이밍으로 연출한다.
        /// 반환값이 true면 버퍼 오버플로(대량 전진으로 이벤트 유실)가 있었으니, 소비자는 상태 스냅샷으로 재구성해 수렴한다.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="buffer"/>가 null일 때.</exception>
        public bool DrainEvents(List<IDomainEvent> buffer)
        {
            return _state.DrainDomainEvents(buffer);
        }

        // 쓰기 단일 관문: 커맨드를 tick 경계에서 디스패처로 라우팅한다.
        private void ProcessCommands()
        {
            while (_state.TryDequeueCommand(out var command))
            {
                // 커맨드별 예외 격리: 한 핸들러가 던져도 tick 루프·나머지 큐가 죽지 않게 한다.
                // 주의: 이건 롤백이 아니다 — 부분 변경이 남을 수 있으므로 크로스 시스템 핸들러는 validate-then-mutate로 짠다.
                try
                {
                    _dispatcher.Dispatch(command);
                }
                catch (Exception e)
                {
                    _errorLog?.Invoke($"[GameSim] 커맨드 처리 중 예외(격리하고 진행) — {command.GetType().Name}: {e}");
                }
            }
        }
    }
}
