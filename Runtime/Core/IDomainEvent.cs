namespace Juahn.V2.Sim
{
    /// <summary>
    /// sim이 상태를 바꾸며 쌓는 "연출용" 도메인 이벤트의 표식(공격, 사망, 레벨업 등).
    /// Presentation이 <see cref="GameSim.DrainEvents"/>로 자기 타이밍에 소비해 연출한다. sim은 소비자의 존재를 모른다.
    ///
    /// 핵심 규칙: "연출 구독자를 전부 꺼도 게임 결과가 동일"해야 한다 —
    /// 즉 도메인 이벤트는 결과(상태)에 영향을 주지 않는 순수 통지이며, sim은 자신이 발행한 이벤트를 다시 읽지 않는다.
    ///
    /// 시스템 간 소통(tick 내부 반응)에 쓰는 <see cref="ISimEvent"/>와 구분된다:
    /// 그건 tick 안에 소비되어 상태에 영향을 줄 수 있고, 이건 Presentation 전용 출력이다.
    /// </summary>
    public interface IDomainEvent
    {
    }
}
