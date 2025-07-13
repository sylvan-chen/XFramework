using UnityEngine;
using XFramework.Utils;

namespace XFramework
{
    public enum UIPanelType
    {
        /// <summary>
        /// 全屏界面 - 参与覆盖逻辑，会暂停其他全屏界面
        /// </summary>
        FullScreen,

        /// <summary>
        /// 弹窗界面 - 不参与覆盖逻辑，可以与其他界面共存
        /// </summary>
        Popup,

        /// <summary>
        /// 固定界面 - 常驻界面，不参与覆盖逻辑
        /// </summary>
        Fixed
    }

    /// <summary>
    /// UI 界面基类
    /// </summary>
    public abstract class UIPanelBase : MonoBehaviour
    {

    }
}
