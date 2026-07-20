namespace Juahn.V2.Sim
{
    /// <summary>
    /// 시뮬레이션 루프의 고정 상수 모음. 밸런스 수치가 아니라 결정론의 기준이 되는 값들이다.
    /// (게임별 밸런스 상수는 이 패키지가 아니라 각 게임 config에 둔다.)
    /// </summary>
    public static class SimConstants
    {
        /// <summary>초당 시뮬레이션 틱 수. sim 시간 해상도이자 결정론의 기준이다.</summary>
        public const int TicksPerSecond = 10;

        /// <summary>1틱의 실제 길이(초). = 1 / TicksPerSecond.</summary>
        public const double SecondsPerTick = 1.0 / TicksPerSecond;

        /// <summary>
        /// 소비되지 않은 연출 도메인 이벤트를 버퍼에 몇 개까지 쌓아둘지의 상한. 초과 시 가장 오래된 것부터 버린다.
        /// 온라인 정상 플레이(프레임마다 Drain)에선 절대 닿지 않고, 오프라인 대량 Advance에서만 발동해 메모리 폭주를 막는다.
        /// (밸런스 수치가 아니라 안전장치 상수)
        /// </summary>
        public const int MaxBufferedDomainEvents = 4096;

        /// <summary>
        /// SimClock 한 번의 Advance에서 소비 가능한 최대 틱 수 기본값. 죽음의 나선(spiral-of-death)을 막는다.
        /// 60초 분량(= TicksPerSecond * 60)을 기본으로 둔다. 오프라인 따라잡기는 이 경로가 아니라 별도 대량 Advance로 처리한다.
        /// </summary>
        public const int DefaultMaxTicksPerAdvance = TicksPerSecond * 60;
    }
}
