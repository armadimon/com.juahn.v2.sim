namespace Juahn.V2.Sim
{
    /// <summary>
    /// tick마다 상태를 전진시키는 시스템. 생성 시 자기 테이블/config와 공유 <see cref="GameState"/> 조각만 받고,
    /// 다른 시스템은 직접 참조하지 않는다(소통은 커맨드/sim 이벤트로만). 실행 순서는 <see cref="GameSim"/>의 명시적 목록이 결정한다.
    /// </summary>
    public interface ITickSystem
    {
        /// <summary>1틱 분량의 상태 전진. GameSim이 tick 경계에서 명시적 순서대로 호출한다.</summary>
        void Tick();
    }
}
