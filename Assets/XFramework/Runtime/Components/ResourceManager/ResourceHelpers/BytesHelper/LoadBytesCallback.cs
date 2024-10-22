namespace XFramework
{
    /// <summary>
    /// 加载字节流成功回调
    /// </summary>
    /// <param name="fileURI">文件路径</param>
    /// <param name="bytes">字节流</param>
    /// <param name="duration">加载时间</param>
    /// <param name="userData">用户自定义数据</param>
    public delegate void LoadBytesSuccessCallback(string fileURI, byte[] bytes, float duration, object userData);

    /// <summary>
    /// 加载字节流失败回调
    /// </summary>
    /// <param name="fileURI">文件路径</param>
    /// <param name="error">错误信息</param>
    /// <param name="userData">用户自定义数据</param>
    public delegate void LoadBytesFailureCallback(string fileURI, string error, object userData);
}