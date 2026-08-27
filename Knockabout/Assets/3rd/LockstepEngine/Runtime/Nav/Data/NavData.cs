using System;
using System.Collections.Generic;

namespace Lockstep.Nav
{
    /// <summary>
    /// 可序列化的离线导航数据。agentType 是项目自行约定的代理配置编号，
    /// triangles 保存几何、普通邻接和离线跳转链接；运行时 NavMap 会据此构建索引和 BVH。
    /// </summary>
    [Serializable]
    public class NavData
    {
        public int agentType;
        public List<Triangle> triangles = new List<Triangle>();
    }
}
