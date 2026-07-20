namespace Juahn.V2.Sim
{
    /// <summary>
    /// 시스템 설정(POCO config)을 타입으로 공급한다. 구현체(게임의 Data 계층)가 밸런스 테이블을 config로 변환해 담는다.
    /// 각 시스템은 자기 config를 <see cref="Get{T}"/>로 꺼낸다 — 개별 파라미터 대신 이 하나만 주입받는다.
    /// config 타입은 Unity 타입이 아니라 순수 POCO여야 한다(sim 순수성 유지).
    /// </summary>
    public interface IConfigProvider
    {
        /// <summary>타입 <typeparamref name="T"/>의 config를 꺼낸다. 없으면 null(호출 측이 기본값으로 대체).</summary>
        T Get<T>() where T : class;
    }
}
