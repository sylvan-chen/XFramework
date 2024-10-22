using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine.Networking;

namespace XFramework
{
    public static partial class BytesHelper
    {
        /// <summary>
        /// 异步加载字节流（UnitTask 版本）
        /// </summary>
        /// <param name="fileURI">文件路径</param>
        /// <param name="timeout">超时时间</param>
        public static async UniTask<LoadBytesResult> LoadBytesAsync(string fileURI, float timeout = 30f)
        {
            DateTime startTime = DateTime.UtcNow;
            UnityWebRequest www = UnityWebRequest.Get(fileURI);
            var cts = new CancellationTokenSource();
            cts.CancelAfterSlim(TimeSpan.FromSeconds(timeout));
            var (isCanceled, wwwResult) = await www.SendWebRequest().WithCancellation(cts.Token).SuppressCancellationThrow();

            float duration = (float)(DateTime.UtcNow - startTime).TotalSeconds;
            if (isCanceled)
            {
                return new LoadBytesResult(false, null, duration, $"Request timeout for {fileURI}.");
            }

            if (wwwResult.result == UnityWebRequest.Result.Success)
            {
                byte[] bytes = wwwResult.downloadHandler.data;
                return new LoadBytesResult(true, bytes, duration, null);
            }
            else
            {
                return new LoadBytesResult(false, null, duration, wwwResult.error);
            }
        }

        /// <summary>
        /// 异步加载字节流（回调版本）
        /// </summary>
        public static void LoadBytesAsync(string fileURI, LoadBytesSuccessCallback onSuccess, LoadBytesFailureCallback onFailure, object userData, float timeout = 30f)
        {
            LoadBytesInternal(fileURI, onSuccess, onFailure, userData, timeout).Forget();
        }

        private static async UniTaskVoid LoadBytesInternal(string fileURI, LoadBytesSuccessCallback onSuccess, LoadBytesFailureCallback onFailure, object userData, float timeout)
        {
            DateTime startTime = DateTime.UtcNow;

            UnityWebRequest www = UnityWebRequest.Get(fileURI);
            var cts = new CancellationTokenSource();
            cts.CancelAfterSlim(TimeSpan.FromSeconds(timeout));
            var (isCanceled, wwwResult) = await www.SendWebRequest().WithCancellation(cts.Token).SuppressCancellationThrow();
            if (isCanceled)
            {
                onFailure?.Invoke(fileURI, $"Request timeout for {fileURI}.", userData);
                return;
            }

            if (wwwResult.result == UnityWebRequest.Result.Success)
            {
                byte[] bytes = wwwResult.downloadHandler.data;
                float duration = (float)(DateTime.UtcNow - startTime).TotalSeconds;
                onSuccess?.Invoke(fileURI, bytes, duration, userData);
            }
            else
            {
                onFailure?.Invoke(fileURI, wwwResult.error, userData);
            }
        }
    }
}