using System;
using System.IO;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace XFramework.Utils
{
    public static class FileHelper
    {
        public static string ReadAllText(string path)
        {
            return ReadAllText(path, Encoding.UTF8);
        }

        public static string ReadAllText(string path, Encoding encoding)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException("ReadAllText failed. Path is null or empty.");
            }
            if (!File.Exists(path))
            {
                throw new InvalidOperationException($"ReadAllText failed. File '{path}' not found.");
            }

            return File.ReadAllText(path, encoding);
        }

        public static async UniTask<string> ReadAllTextAsync(string path)
        {
            return await ReadAllTextAsync(path, Encoding.UTF8);
        }

        public static async UniTask<string> ReadAllTextAsync(string path, Encoding encoding)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException("ReadAllTextAsync failed. Path is null or empty.");
            }

            if (!File.Exists(path))
            {
                throw new InvalidOperationException($"ReadAllTextAsync failed. File '{path}' not found.");
            }

            return await File.ReadAllTextAsync(path, encoding);
        }

        public static byte[] ReadAllBytes(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException("ReadAllBytes failed. Path is null or empty.");
            }
            if (!File.Exists(path))
            {
                throw new InvalidOperationException($"ReadAllBytes failed. File '{path}' not found.");
            }

            return File.ReadAllBytes(path);
        }

        public static async UniTask<byte[]> ReadAllBytesAsync(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException("ReadAllBytesAsync failed. Path is null or empty.");
            }
            if (!File.Exists(path))
            {
                throw new InvalidOperationException($"ReadAllBytesAsync failed. File '{path}' not found.");
            }

            return await File.ReadAllBytesAsync(path);
        }

        public static void WriteAllText(string path, string content)
        {
            WriteAllText(path, content, Encoding.UTF8);
        }

        public static void WriteAllText(string path, string content, Encoding encoding)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException("WriteAllText failed. Path is null or empty.");
            }

            CreateFileDirectoryIfNotExist(path);
            byte[] bytes = encoding.GetBytes(content);
            File.WriteAllBytes(path, bytes);
        }

        public static async UniTask WriteAllTextAsync(string path, string content)
        {
            await WriteAllTextAsync(path, content, Encoding.UTF8);
        }

        public static async UniTask WriteAllTextAsync(string path, string content, Encoding encoding)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException("WriteAllTextAsync failed. Path is null or empty.");
            }

            CreateFileDirectoryIfNotExist(path);
            byte[] bytes = encoding.GetBytes(content);
            await File.WriteAllBytesAsync(path, bytes);
        }

        public static void WriteAllBytes(string path, byte[] bytes)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException("WriteAllBytes failed. Path is null or empty.");
            }

            CreateFileDirectoryIfNotExist(path);
            File.WriteAllBytes(path, bytes);
        }

        public static async UniTask WriteAllBytesAsync(string path, byte[] bytes)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException("WriteAllBytesAsync failed. Path is null or empty.");
            }

            CreateFileDirectoryIfNotExist(path);
            await File.WriteAllBytesAsync(path, bytes);
        }

        /// <summary>
        /// 如果文件所在的目录不存在，则创建目录
        /// </summary>
        /// <param name="path">文件路径</param>
        public static void CreateFileDirectoryIfNotExist(string path)
        {
            string directory = Path.GetDirectoryName(path);
            CreateDirectoryIfNotExist(directory);
        }

        /// <summary>
        /// 如果目录不存在，则创建目录
        /// </summary>
        /// <param name="directory">目录路径</param>
        public static void CreateDirectoryIfNotExist(string directory)
        {
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }
    }
}