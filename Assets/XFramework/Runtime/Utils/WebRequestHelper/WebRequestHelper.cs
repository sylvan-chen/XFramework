using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace XFramework.Utils
{
    public static class WebRequestHelper
    {
        public static string ConvertToWWWURI(string path)
        {
            string uri;
            switch (Application.platform)
            {
                case RuntimePlatform.WindowsEditor:
                    uri = $"file:///{path}";
                    break;
                case RuntimePlatform.Android:
                    uri = $"jar:file://{path}";
                    break;
                case RuntimePlatform.IPhonePlayer:
                case RuntimePlatform.WindowsPlayer:
                case RuntimePlatform.OSXPlayer:
                    uri = $"file://{path}";
                    break;
                default:
                    throw new NotImplementedException();
            }
            return uri;
        }

        public static async UniTask<WebRequestResult> WebGetAsync(string uri, float timeout = 60f)
        {
            UnityWebRequest www = UnityWebRequest.Get(uri);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.disposeDownloadHandlerOnDispose = true;

            var cts = new CancellationTokenSource();
            cts.CancelAfterSlim(TimeSpan.FromSeconds(timeout));
            var (isCanceled, _) = await www.SendWebRequest().WithCancellation(cts.Token).SuppressCancellationThrow();

            WebRequestResult result;
            if (isCanceled)
            {
                result = new WebRequestResult
                (
                    WebRequestStatus.TimeoutError,
                    $"Request for {uri} faild. Error: Time out.",
                    null,
                    null
                );
            }
            else if (www.result == UnityWebRequest.Result.Success)
            {
                result = new WebRequestResult
                (
                    WebRequestStatus.Success,
                    null,
                    www.downloadHandler.data,
                    www.downloadHandler.text
                );
            }
            else
            {
                WebRequestStatus status;
                switch (www.result)
                {
                    case UnityWebRequest.Result.ConnectionError:
                        status = WebRequestStatus.ConnectionError;
                        break;
                    case UnityWebRequest.Result.DataProcessingError:
                        status = WebRequestStatus.DataProcessingError;
                        break;
                    case UnityWebRequest.Result.ProtocolError:
                        status = WebRequestStatus.ProtocolError;
                        break;
                    default:
                        status = WebRequestStatus.UnknownError;
                        break;
                }
                result = new WebRequestResult
                (
                    status,
                    $"Request for {uri} failed. Error: {www.error}",
                    null,
                    null
                );
            }

            www.Dispose();
            return result;
        }
    }
}