using System;
using System.Collections.Generic;

namespace XFramework
{
    /// <summary>
    /// 要丢弃的对象过滤器
    /// </summary>
    /// <typeparam name="T">对象类型</typeparam>
    /// <param name="candidateObjects">所有对象的列表</param>
    /// <param name="discardCount">需要丢弃的数量</param>
    /// <param name="expireTime">对象过期时间</param>
    /// <returns>需要丢弃的对象列表</returns>
    public delegate List<T> DiscardObjectFilter<T>(List<T> candidateObjects, int discardCount, float expireTime) where T : ObjectBase;
}