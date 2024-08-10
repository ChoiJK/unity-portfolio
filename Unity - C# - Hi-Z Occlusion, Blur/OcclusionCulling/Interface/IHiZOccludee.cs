public interface IHiZOccludee
{
    int Index { get; set; }
    bool IsDirty();
    OccludeeDataCSInput GetOccludeeData();
    void SetOcclusionResult(uint result);
}
