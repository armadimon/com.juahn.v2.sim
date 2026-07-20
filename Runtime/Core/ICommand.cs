namespace Juahn.V2.Sim
{
    /// <summary>
    /// 상태 변경 요청의 표식 인터페이스. 파사드/외부 코드가 <see cref="GameState.Enqueue"/>로 큐에 적재하고,
    /// <see cref="GameSim"/>이 tick 경계에서만 처리한다. 즉시 실행하지 않는 이유는 결정론(전진은 tick 단위)을 지키기 위함이다.
    ///
    /// 구현체는 불변(readonly 필드)으로 두는 것을 권장한다 — 큐에 들어간 뒤 변경되면 재현이 깨진다.
    /// 실제 처리는 이 패키지 밖(게임 계층)의 <see cref="ICommandDispatcher"/> 구현이 커맨드 타입별로 라우팅한다.
    /// </summary>
    public interface ICommand
    {
    }

    /// <summary>
    /// 커맨드를 소유 시스템으로 라우팅하는 디스패처. 게임 계층이 커맨드 타입 스위치로 구현한다.
    /// <see cref="GameSim"/>은 각 tick 경계에서 큐를 비우며 커맨드마다 <see cref="Dispatch"/>를 호출한다.
    /// </summary>
    public interface ICommandDispatcher
    {
        /// <summary>단일 커맨드를 처리한다. 던지는 예외는 GameSim이 커맨드 단위로 격리한다(다른 커맨드/틱에 전파 금지).</summary>
        void Dispatch(ICommand command);
    }
}
