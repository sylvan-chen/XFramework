using System;

namespace SimpleDressup
{
    /// <summary>
    /// 服装槽位类型 - 定义角色可以装备的部位
    /// </summary>
    [Flags]
    public enum DressupSlotType
    {
        None = 0,
        Face = 1 << 0,      // 脸部
        Hair = 1 << 1,      // 头发  
        Top = 1 << 2,       // 上衣
        Bottom = 1 << 3,    // 下衣
        Shoes = 1 << 4,     // 鞋子
        Gloves = 1 << 5,    // 手套
        Hat = 1 << 6,       // 帽子
        All = ~0            // 所有部位
    }
}
