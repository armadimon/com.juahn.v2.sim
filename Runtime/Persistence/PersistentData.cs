namespace Juahn.V2.Sim
{
    /// <summary>
    /// 저장 단위(서버 테이블 하나에 대응). 새 영속 영역은 이 클래스를 상속한다. <see cref="Key"/>가 저장/서버 테이블 식별자.
    ///
    /// PD 규칙: 독립 직렬화 가능해야 하며(다른 PD를 참조하지 말 것), 소유자 식별 필드(userId 등)를 두지 않는다
    /// — 유저 귀속은 서버 인증의 몫이다. 통짜 루트로 다루지 않고, 각 시스템이 자기 PD를 소유하며 변경 시 dirty 마크한다.
    /// </summary>
    public abstract class PersistentData
    {
        /// <summary>저장/서버 테이블 키. 기본은 타입명. 충돌하거나 마이그레이션이 필요하면 override한다.</summary>
        public virtual string Key => GetType().Name;
    }
}
