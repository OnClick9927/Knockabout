namespace Lockstep.Nav
{
    /// <summary>导航搜索的明确终止原因，调用方可区分数据错误、端点无效和不可达。</summary>
    public enum NavResult
    {
        Success,
        NavDataErr,
        StartNotInNavMesh,
        EndNotInNavMesh,
        NotFound
    }
}
