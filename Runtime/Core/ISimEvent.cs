namespace Juahn.V2.Sim
{
    /// <summary>
    /// sim 내부 이벤트(시스템 간 1:N 반응용)의 표식. "이벤트도 데이터":
    /// 생산 시스템이 <see cref="GameState.EmitSim"/>으로 쌓고, 소비 시스템들이 tick 순서상 뒤에서 읽으며,
    /// 매 tick 끝에 <see cref="GameSim"/>이 소거한다.
    ///
    /// Presentation용 <see cref="IDomainEvent"/>(연출·버퍼 유지)와 구분된다 —
    /// 이건 sim 내부에서 그 tick 안에 소비되며, 결과(상태)에 영향을 줄 수 있는 축이다.
    /// </summary>
    public interface ISimEvent
    {
    }
}
