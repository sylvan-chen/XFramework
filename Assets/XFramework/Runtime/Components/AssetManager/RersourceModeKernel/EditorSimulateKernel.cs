using System;
using System.Reflection;
using Cysharp.Threading.Tasks;
using XFramework.Utils;

namespace XFramework.Resource
{
    /// <summary>
    /// 编辑器模拟模式内核
    /// </summary>
    public class EditorSimulateKernel : IResourceModeKernel
    {
        private readonly EditorSimulateBuildPipeline _buildPipeline;

        private EditorFileSystem _editorFileSystem = null;

        public EditorSimulateKernel(EditorSimulateBuildPipeline buildPipeline)
        {
            _buildPipeline = buildPipeline;
        }

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="parameter">内置文件系统参数</param>
        public async UniTask<KernelInitResult> InitAsync()
        {
            // 编辑器模拟模式每次运行时都会首先进行一次模拟构建
            string outputRootDirectory = SimulateBuild(_buildPipeline.ToString());

            return await InitInternal(new EditorFileSystemParameter(outputRootDirectory));
        }

        private string SimulateBuild(params object[] parameters)
        {
            Type simulateBuilder = TypeHelper.GetType("EditorSimulateAssetBundleBuilder");
            if (simulateBuilder == null)
            {
                throw new InvalidOperationException("Simulate build failed. Type EditorSimulateAssetBundleBuilder not found.");
            }
            MethodInfo buildMethod = simulateBuilder.GetMethod("SimulateBuild", BindingFlags.Public | BindingFlags.Static);
            if (buildMethod == null)
            {
                throw new InvalidOperationException("Simulate build failed. Method SimulateBuild() of EditorSimulateAssetBundleBuilder not found.");
            }
            return (string)buildMethod.Invoke(null, parameters);
        }

        private async UniTask<KernelInitResult> InitInternal(EditorFileSystemParameter fileSystemParameter)
        {
            _editorFileSystem = new EditorFileSystem(fileSystemParameter);
            FSInitResult result = await _editorFileSystem.InitAsync();
            if (result.Status == TaskResultStatus.Success)
            {
                return KernelInitResult.Success();
            }
            else
            {
                return KernelInitResult.Failure(result.Error);
            }
        }
    }
}