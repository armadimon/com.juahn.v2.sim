using System;
using System.Collections.Generic;

namespace Juahn.V2.Sim
{
    /// <summary>
    /// 커맨드 버스 + tick 카운터 + sim 내부 이벤트 버퍼 + 연출용 도메인 이벤트 버퍼.
    /// 도메인 상태(재화/전투 등)는 이 클래스가 아니라 각 시스템의 PersistentData가 소유한다.
    /// 외부(파사드)가 커맨드를 적재하고, <see cref="GameSim"/>이 tick 경계에서 소비한다.
    /// 시스템은 이 GameState 조각(sim/domain 이벤트 버퍼)을 공유해 소통한다.
    /// </summary>
    public sealed class GameState
    {
        /// <summary>지금까지 전진한 총 tick 수(단조 증가). 결정론의 시간 좌표.</summary>
        public long Tick { get; internal set; }

        // 커맨드 큐: 외부가 Enqueue, GameSim이 tick 경계에서 Dequeue.
        private readonly Queue<ICommand> _commands = new Queue<ICommand>();

        // sim 내부 이벤트: 생산 시스템이 쌓고, 소비 시스템(tick 순서상 뒤)이 읽으며, GameSim이 각 tick 끝에 비운다.
        private readonly List<ISimEvent> _simEvents = new List<ISimEvent>();

        // 연출용 도메인 이벤트 버퍼. Advance 동안 시스템이 Emit으로 쌓고, Presentation이 DrainEvents로 비운다.
        // 결과에 영향을 주지 않으므로(순수 출력) 저장 대상이 아니다.
        // Queue를 쓰는 이유: 상한 초과 시 가장 오래된 것을 O(1)로 버리기 위함(List.RemoveAt(0)의 O(n) 회피 — 오프라인 대량 전진 대비).
        private readonly Queue<IDomainEvent> _domainEvents = new Queue<IDomainEvent>();

        /// <summary>대기 중인 커맨드 수. 주로 진단/테스트용.</summary>
        public int PendingCommandCount => _commands.Count;

        /// <summary>이번 tick에 쌓인 sim 내부 이벤트(읽기 전용 뷰). 소비 시스템이 tick 안에서 순회한다.</summary>
        public IReadOnlyList<ISimEvent> SimEvents => _simEvents;

        /// <summary>
        /// 상한을 넘겨(=소비 없이 대량 전진) 도메인 이벤트가 유실됐는지. Presentation은 true를 보면
        /// 상태 스냅샷 재구성으로 수렴한다(개별 이벤트 재생 포기). 다음 Drain에서 리셋된다.
        /// </summary>
        public bool DomainEventsOverflowed { get; private set; }

        /// <summary>커맨드를 큐에 적재한다(즉시 실행하지 않음). 파사드/외부가 호출.</summary>
        /// <exception cref="ArgumentNullException"><paramref name="command"/>가 null일 때.</exception>
        public void Enqueue(ICommand command)
        {
            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            _commands.Enqueue(command);
        }

        /// <summary>sim 내부 이벤트를 쌓는다. 같은 tick 안에서 소비 시스템이 읽는다.</summary>
        /// <exception cref="ArgumentNullException"><paramref name="simEvent"/>가 null일 때.</exception>
        public void EmitSim(ISimEvent simEvent)
        {
            if (simEvent == null)
            {
                throw new ArgumentNullException(nameof(simEvent));
            }

            _simEvents.Add(simEvent);
        }

        /// <summary>연출용 도메인 이벤트를 쌓는다. 소유 시스템이 상태 변경과 함께 호출한다.</summary>
        /// <exception cref="ArgumentNullException"><paramref name="domainEvent"/>가 null일 때.</exception>
        public void Emit(IDomainEvent domainEvent)
        {
            if (domainEvent == null)
            {
                throw new ArgumentNullException(nameof(domainEvent));
            }

            if (_domainEvents.Count >= SimConstants.MaxBufferedDomainEvents)
            {
                _domainEvents.Dequeue();
                DomainEventsOverflowed = true;
            }

            _domainEvents.Enqueue(domainEvent);
        }

        /// <summary>
        /// 쌓인 연출 이벤트를 <paramref name="buffer"/>로 옮기고 비운다(overflow 플래그도 리셋).
        /// Presentation이 프레임마다 Advance 뒤에 호출한다. 반환값이 true면 오버플로가 있었으니 스냅샷 재구성으로 수렴한다.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="buffer"/>가 null일 때.</exception>
        public bool DrainDomainEvents(List<IDomainEvent> buffer)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            var overflowed = DomainEventsOverflowed;
            while (_domainEvents.Count > 0)
            {
                buffer.Add(_domainEvents.Dequeue());
            }

            DomainEventsOverflowed = false;
            return overflowed;
        }

        // --- GameSim 전용 내부 접근자 (tick 루프에서만 사용) ---

        /// <summary>다음 대기 커맨드를 꺼낸다. 없으면 false. GameSim의 tick 경계 소비 전용.</summary>
        internal bool TryDequeueCommand(out ICommand command)
        {
            if (_commands.Count > 0)
            {
                command = _commands.Dequeue();
                return true;
            }

            command = null;
            return false;
        }

        /// <summary>이번 tick의 sim 내부 이벤트를 비운다. GameSim이 tick 끝에서만 호출.</summary>
        internal void ClearSimEvents()
        {
            _simEvents.Clear();
        }
    }
}
