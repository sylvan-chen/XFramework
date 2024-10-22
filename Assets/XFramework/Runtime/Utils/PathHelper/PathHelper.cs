using System.IO;

namespace XFramework.Utils
{

    public static class PathHelper
    {
        /// <summary>
        /// 获取规范化的路径
        /// </summary>
        /// <remarks>
        /// 将路径中的 '\\' 全部替换为 '/'。
        /// </remarks>
        public static string GetRegularPath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return path;
            }

            return path.Replace("\\", "/");
        }

        public static string RemoveExtension(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return path;
            }

            int lastDotIndex = path.LastIndexOf('.');
            if (lastDotIndex == -1)
            {
                return path;
            }
            else
            {
                return path.Substring(0, lastDotIndex);
            }
        }

        public static string Combine(string[] paths)
        {
            return Path.Combine(paths);
        }

        public static string Combine(string path1, string path2)
        {
            return Path.Combine(path1, path2);
        }

        public static string Combine(string path1, string path2, string path3)
        {
            return Path.Combine(path1, path2, path3);
        }

        public static string Combine(string path1, string path2, string path3, string path4)
        {
            return Path.Combine(path1, path2, path3, path4);
        }

        #region 老代码

        /// <summary>
        /// 获取远程文件格式的路径（'file://' 前缀）
        /// </summary>
        public static string GetRemoteFilePath(string path)
        {
            return GetRemotePathInternal(path, "file:///");
        }

        /// <summary>
        /// 获取远程 HTTP 格式的路径（'http://' 前缀）
        /// </summary>
        public static string GetRemoteHttpPath(string path)
        {
            return GetRemotePathInternal(path, "http://");
        }

        private static string GetRemotePathInternal(string path, string prefix)
        {
            string regularPath = GetRegularPath(path);
            if (regularPath == null)
            {
                return null;
            }

            if (regularPath.StartsWith(prefix))
            {
                return regularPath;
            }
            else
            {
                string fullPath = prefix + regularPath;
                // 去掉重复的斜杠
                return fullPath.Replace(prefix + "/", prefix);
            }
        }

        #endregion
    }
}