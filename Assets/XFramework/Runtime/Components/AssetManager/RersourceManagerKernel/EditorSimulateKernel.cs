using System;
using System.Reflection;
using Cysharp.Threading.Tasks;
using XFramework.Utils;

namespace XFramework.Resource
{
    /// <summary>
    /// 编辑器模拟模式内核
    /// </summary>
    public sealed class EditorSimulateKernel : IResourceManagerKernel
    {
        private readonly EditorSimulateBuildPipeline _buildPipeline;
        private readonly EditorFileSystem _editorFileSystem = new();

        public EditorSimulateKernel(EditorSimulateBuildPipeline buildPipeline)
        {
            _buildPipeline = buildPipeline;
        }

        public async UniTask InitAsync()
        {
            // 编辑器模拟模式每次运行时都会首先进行一次模拟构建
            SimulateBuild(_buildPipeline.ToString());

            await _editorFileSystem.InitAsync();
        }

        private void SimulateBuild(params object[] parameters)
        {
            Type simulateBuilder = TypeHelper.GetType("XFramework.XAsset.EditorSimulateAssetBundleBuilder");
            if (simulateBuilder == null)
            {
                throw new InvalidOperationException("Simulate build failed. Type EditorSimulateAssetBundleBuilder not found.");
            }

            MethodInfo buildMethod = simulateBuilder.GetMethod("SimulateBuild", BindingFlags.Public | BindingFlags.Static);
            if (buildMethod == null)
            {
                throw new InvalidOperationException("Simulate build failed. Method SimulateBuild() of EditorSimulateAssetBundleBuilder not found.");
            }

            buildMethod.Invoke(null, parameters);
        }
    }
}