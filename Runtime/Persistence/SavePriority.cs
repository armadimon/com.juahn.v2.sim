namespace Juahn.V2.Sim
{
    /// <summary>PD 저장 우선순위.</summary>
    public enum SavePriority
    {
        /// <summary>표시만. 다음 일괄 플러시(FlushAll) 시점에 묶어서 저장.</summary>
        Deferred,

        /// <summary>즉시 저장(결제 등 유실 불가 데이터).</summary>
        Immediate,
    }
}
