using System;
using System.Collections.Generic;

namespace XFramework
{
    /// <summary>
    /// 要丢弃的池对象过滤器
    /// </summary>
    /// <param name="candidatePoolObjects">可丢弃的池对象列表</param>
    /// <param name="discardCount">需要丢弃的池对象数量</param>
    /// <param name="objectTTL">池对象生存时间</param>
    /// <returns>需要丢弃的池对象列表</returns>
    public delegate List<PoolObject> DiscardablePoolObjectFilter(List<PoolObject> candidatePoolObjects, int discardCount, float objectTTL);
}