namespace Juahn.V2.Sim
{
    /// <summary>
    /// PD 단위 저장 서비스. 각 시스템이 자기 PD를 <see cref="Load{T}"/>로 받아 소유하고, 변경 시 <see cref="MarkDirty"/>로 표시한다.
    /// 실제 직렬화·백엔드·플러시는 이 패키지 밖(게임의 Backend 계층) 구현이 담당한다 — sim은 인터페이스만 안다(async 금지).
    /// 플러시 타이밍(주기/종료/씬전환)은 외부(Presentation)가 결정한다.
    /// </summary>
    public interface ISaveService
    {
        /// <summary>저장본이 있으면 역직렬화, 없으면 새 인스턴스. 반환된 PD를 dirty 추적에 등록한다.</summary>
        T Load<T>() where T : PersistentData, new();

        /// <summary>PD 변경 통지. Immediate면 그 PD만 즉시 저장, Deferred면 다음 FlushAll까지 표시만.</summary>
        void MarkDirty(PersistentData data, SavePriority priority = SavePriority.Deferred);
    }
}
