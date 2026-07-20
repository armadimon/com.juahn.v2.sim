using System;

namespace Juahn.V2.Sim
{
    /// <summary>
    /// 순수 C# 시간 누산기. 실시간 delta-seconds(double)를 정수 tick 수로 변환하고, 남는 소수 시간을 누적한다.
    /// <b>Unity Time에서 완전히 분리되어 있다</b> — dt를 파라미터로 받고, 자체 속도 배율 필드를 가진다.
    ///
    /// <para>
    /// [Unity 연동 규칙 — 중요]
    /// 호출 측(Presentation의 MonoBehaviour 등)은 <see cref="Advance"/>에 반드시
    /// <c>Time.unscaledDeltaTime</c>를 넘겨야 한다. <c>Time.deltaTime</c>는 금지 —
    /// 3rd-party 에셋/광고 SDK/일시정지 UI가 <c>Time.timeScale</c>을 바꾸면 sim 속도가 오염되기 때문이다.
    /// 게임 내 배속/일시정지는 <see cref="SpeedMultiplier"/>로만 표현한다(0 = 일시정지, 1 = 기본, 2 = 2배속).
    /// 즉 sim에 먹이는 값은 개념적으로 <c>unscaledDeltaTime * SpeedMultiplier</c>이다.
    /// </para>
    ///
    /// <para>
    /// [죽음의 나선 방지]
    /// <see cref="MaxTicksPerAdvance"/>로 한 번의 <see cref="Advance"/>가 내놓는 tick 수를 상한한다.
    /// 프레임이 크게 튀어(hitch) dt가 폭증해도 sim이 한 번에 수천 tick을 처리하려다 다음 프레임을 더 늦추는
    /// 악순환을 막는다. 상한을 넘긴 잔여 시간의 처리 정책은 <see cref="HitchPolicy"/>로 고른다.
    /// </para>
    ///
    /// <para>
    /// [세션 중 hitch 처리]
    /// 상한 clamp가 걸리면(=순간 프레임 지연) 기본 정책(<see cref="HitchPolicy.Drop"/>)은 초과분을 버려
    /// 실시간과 살짝 어긋나더라도 sim이 매끄럽게 진행되게 한다(온라인 액션에 적합).
    /// <see cref="HitchPolicy.Accumulate"/>는 초과분을 누산기에 남겨 이후 프레임들에서 따라잡는다
    /// (오프라인/시뮬레이션 정확도 우선). 장시간 백그라운드 복귀 같은 대량 따라잡기는 이 clock이 아니라
    /// <see cref="GameSim.Advance"/>에 계산한 tick 수를 직접 넘겨 처리하는 것을 권장한다.
    /// </para>
    /// </summary>
    public sealed class SimClock
    {
        // 아직 tick으로 변환되지 못한 잔여 실시간(초). 프레임 간 이월된다.
        private double _accumulatorSeconds;

        private double _speedMultiplier = 1.0;
        private int _maxTicksPerAdvance = SimConstants.DefaultMaxTicksPerAdvance;

        /// <summary>1틱의 길이(초). = SimConstants.SecondsPerTick.</summary>
        public double SecondsPerTick => SimConstants.SecondsPerTick;

        /// <summary>세션 중 순간 hitch로 clamp가 걸렸을 때 초과 시간을 어떻게 다룰지.</summary>
        public HitchPolicy Policy { get; set; } = HitchPolicy.Drop;

        /// <summary>가장 최근 <see cref="Advance"/> 호출에서 clamp가 발동했는지(진단/연출 판단용).</summary>
        public bool LastAdvanceClamped { get; private set; }

        /// <summary>현재 누적된 미변환 잔여 시간(초). 진단/테스트용.</summary>
        public double PendingSeconds => _accumulatorSeconds;

        /// <summary>
        /// sim 속도 배율. 0 = 일시정지, 1 = 기본, 2 = 2배속. 음수는 허용하지 않는다.
        /// (Unity timeScale이 아니라 이 필드로 배속을 표현한다.)
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">음수 또는 NaN/Infinity를 설정할 때.</exception>
        public double SpeedMultiplier
        {
            get => _speedMultiplier;
            set
            {
                if (double.IsNaN(value) || double.IsInfinity(value) || value < 0.0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value, "SpeedMultiplier는 0 이상의 유한값이어야 합니다.");
                }

                _speedMultiplier = value;
            }
        }

        /// <summary>한 번의 <see cref="Advance"/>가 내놓을 수 있는 최대 tick 수(죽음의 나선 방지). 1 이상이어야 한다.</summary>
        /// <exception cref="ArgumentOutOfRangeException">1 미만을 설정할 때.</exception>
        public int MaxTicksPerAdvance
        {
            get => _maxTicksPerAdvance;
            set
            {
                if (value < 1)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value, "MaxTicksPerAdvance는 1 이상이어야 합니다.");
                }

                _maxTicksPerAdvance = value;
            }
        }

        /// <summary>
        /// 실시간 delta(초)를 받아 이번 프레임에 전진할 tick 수를 계산한다. 상태(누산기)만 갱신하고 <see cref="GameSim"/>은 건드리지 않는다.
        /// 반환값을 <c>GameSim.Advance(result)</c>에 넘기는 것이 일반적 사용법이다.
        /// </summary>
        /// <param name="deltaSeconds">
        /// 이번 프레임의 실시간 경과(초). Unity에서는 <c>Time.unscaledDeltaTime</c>을 넘긴다(Time.deltaTime 금지).
        /// 배속은 이 값이 아니라 <see cref="SpeedMultiplier"/>로 적용된다. 음수/NaN/Infinity는 0으로 취급(방어).
        /// </param>
        /// <returns>이번에 전진할 tick 수(0 이상, <see cref="MaxTicksPerAdvance"/> 이하).</returns>
        public int Advance(double deltaSeconds)
        {
            LastAdvanceClamped = false;

            // 방어: 비정상 dt는 시간 진행 없음으로 취급(프레임 초기화/기기 슬립 복귀 시 dt가 튀는 것을 흡수).
            if (double.IsNaN(deltaSeconds) || double.IsInfinity(deltaSeconds) || deltaSeconds <= 0.0)
            {
                return 0;
            }

            _accumulatorSeconds += deltaSeconds * _speedMultiplier;

            // 정확한 정수 tick 수 산출(누산기에서 정확히 tick 배수만 소비).
            var seconds = SecondsPerTick;
            long ticks = (long)(_accumulatorSeconds / seconds);
            if (ticks <= 0)
            {
                return 0;
            }

            _accumulatorSeconds -= ticks * seconds;

            if (ticks > _maxTicksPerAdvance)
            {
                LastAdvanceClamped = true;

                var dropped = ticks - _maxTicksPerAdvance;
                ticks = _maxTicksPerAdvance;

                // hitch 정책: 초과분을 이후 프레임에서 따라잡을지(Accumulate) 버릴지(Drop).
                if (Policy == HitchPolicy.Accumulate)
                {
                    _accumulatorSeconds += dropped * seconds;
                }
            }

            return (int)ticks;
        }

        /// <summary>누산기와 clamp 플래그를 리셋한다(속도 배율/상한 설정은 유지). 씬 전환·저장 로드 직후 사용.</summary>
        public void Reset()
        {
            _accumulatorSeconds = 0.0;
            LastAdvanceClamped = false;
        }
    }

    /// <summary>세션 중 순간 hitch로 <see cref="SimClock"/> clamp가 걸렸을 때 초과 시간의 처리 정책.</summary>
    public enum HitchPolicy
    {
        /// <summary>초과분을 버린다. 실시간과 미세하게 어긋나도 sim이 매끄럽게 진행(온라인 액션 권장).</summary>
        Drop,

        /// <summary>초과분을 누산기에 남겨 이후 프레임들이 따라잡는다(시뮬레이션 정확도 우선).</summary>
        Accumulate,
    }
}
