using UnityEngine;
using Cysharp.Threading.Tasks;
using SimpleDressup;

namespace SimpleDressup.Examples
{
    /// <summary>
    /// SimpleDressup 快速测试脚本
    /// 最简单的使用示例，适合快速验证系统功能
    /// </summary>
    public class QuickDressupTest : MonoBehaviour
    {
        [Header("必需组件")]
        [SerializeField] private SimpleDressupController dressupController;

        [Header("测试装备")]
        [SerializeField] private GameObject testHair;
        [SerializeField] private GameObject testTop;
        [SerializeField] private GameObject testBottom;

        async void Start()
        {
            await TestBasicDressup();
        }

        /// <summary>
        /// 基础换装测试
        /// </summary>
        async UniTask TestBasicDressup()
        {
            Debug.Log("=== SimpleDressup 快速测试开始 ===");

            // 1. 等待系统初始化
            Debug.Log("1. 等待系统初始化...");
            while (!dressupController.IsInitialized)
            {
                await UniTask.NextFrame();
            }
            Debug.Log("✓ 系统初始化完成");

            // 2. 自动检测角色部件
            Debug.Log("2. 检测原始角色部件...");
            // dressupController.AutoDetectOriginalBodyParts();
            Debug.Log("✓ 角色部件检测完成");

            // 3. 添加测试装备
            Debug.Log("3. 添加测试装备...");
            if (testHair != null)
            {
                dressupController.SetDressupSlotFromGameObject(testHair, DressupSlotType.Hair);
                Debug.Log("✓ 添加头发");
            }

            if (testTop != null)
            {
                dressupController.SetDressupSlotFromGameObject(testTop, DressupSlotType.Top);
                Debug.Log("✓ 添加上衣");
            }

            if (testBottom != null)
            {
                dressupController.SetDressupSlotFromGameObject(testBottom, DressupSlotType.Bottom);
                Debug.Log("✓ 添加下装");
            }

            // 4. 应用换装
            Debug.Log("4. 应用换装...");
            bool success = await dressupController.ApplyCurrentDressupAsync();

            if (success)
            {
                Debug.Log("✅ 换装测试成功完成！");
            }
            else
            {
                Debug.LogError("❌ 换装测试失败！");
            }

            Debug.Log("=== SimpleDressup 快速测试结束 ===");
        }

        /// <summary>
        /// 手动测试按钮
        /// </summary>
        [ContextMenu("Run Test")]
        public void RunTest()
        {
            TestBasicDressup().Forget();
        }
    }
}
