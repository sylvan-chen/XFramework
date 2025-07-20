using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace SimpleDressup
{
    /// <summary>
    /// 简单的纹理装箱算法 - 负责将多个纹理片段排列到图集中
    /// 使用简化的矩形装箱算法
    /// </summary>
    public class TexturePacker
    {
        private int _atlasSize;
        private List<PackedRectangle> _packedRects;
        private List<FreeRectangle> _freeRects;

        /// <summary>
        /// 装箱结果
        /// </summary>
        public struct PackResult
        {
            public bool Success;
            public int UsedWidth;
            public int UsedHeight;
            public Dictionary<TextureFragment, Rect> FragmentUVs;
        }

        /// <summary>
        /// 已装箱的矩形
        /// </summary>
        private struct PackedRectangle
        {
            public int X, Y, Width, Height;
            public TextureFragment Fragment;

            public PackedRectangle(int x, int y, int width, int height, TextureFragment fragment)
            {
                X = x;
                Y = y;
                Width = width;
                Height = height;
                Fragment = fragment;
            }
        }

        /// <summary>
        /// 空闲矩形区域
        /// </summary>
        private struct FreeRectangle
        {
            public int X, Y, Width, Height;

            public FreeRectangle(int x, int y, int width, int height)
            {
                X = x;
                Y = y;
                Width = width;
                Height = height;
            }
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        public TexturePacker(int atlasSize = 1024)
        {
            _atlasSize = atlasSize;
            Reset();
        }

        /// <summary>
        /// 重置装箱器
        /// </summary>
        public void Reset()
        {
            _packedRects = new List<PackedRectangle>();
            _freeRects = new List<FreeRectangle>
            {
                new FreeRectangle(0, 0, _atlasSize, _atlasSize)
            };
        }

        /// <summary>
        /// 装箱纹理片段列表
        /// </summary>
        public PackResult PackFragments(List<TextureFragment> fragments)
        {
            Reset();

            // 按像素数量从大到小排序 - 大的先装箱
            var sortedFragments = fragments
                .Where(f => f.IsValid())
                .OrderByDescending(f => f.PixelCount)
                .ToList();

            var result = new PackResult
            {
                Success = true,
                FragmentUVs = new Dictionary<TextureFragment, Rect>()
            };

            // 逐个装箱
            foreach (var fragment in sortedFragments)
            {
                var mainTexture = fragment.MainTexture;
                if (mainTexture == null) continue;

                int width = mainTexture.width;
                int height = mainTexture.height;

                var bestRect = FindBestFreeRect(width, height);
                if (bestRect.Width == 0) // 装不下了
                {
                    result.Success = false;
                    XFramework.Utils.Log.Warning($"TexturePacker: 无法为片段 {fragment.FragmentName} 找到空间");
                    continue;
                }

                // 放置纹理
                var packedRect = new PackedRectangle(bestRect.X, bestRect.Y, width, height, fragment);
                _packedRects.Add(packedRect);

                // 计算UV坐标 (0-1范围)
                float uvX = (float)bestRect.X / _atlasSize;
                float uvY = (float)bestRect.Y / _atlasSize;
                float uvW = (float)width / _atlasSize;
                float uvH = (float)height / _atlasSize;

                var uvRect = new Rect(uvX, uvY, uvW, uvH);
                result.FragmentUVs[fragment] = uvRect;

                // 更新空闲区域
                UpdateFreeRects(bestRect.X, bestRect.Y, width, height);

                // 更新已使用区域
                result.UsedWidth = Mathf.Max(result.UsedWidth, bestRect.X + width);
                result.UsedHeight = Mathf.Max(result.UsedHeight, bestRect.Y + height);
            }

            XFramework.Utils.Log.Debug($"TexturePacker: 装箱完成 - 成功:{result.Success}, 使用尺寸:{result.UsedWidth}x{result.UsedHeight}");
            return result;
        }

        /// <summary>
        /// 找到最适合的空闲矩形 - 使用最佳长边适应算法
        /// </summary>
        private FreeRectangle FindBestFreeRect(int width, int height)
        {
            var bestRect = new FreeRectangle();
            int bestLongSideFit = int.MaxValue;
            int bestShortSideFit = int.MaxValue;

            foreach (var rect in _freeRects)
            {
                if (rect.Width >= width && rect.Height >= height)
                {
                    int leftoverHorizontal = rect.Width - width;
                    int leftoverVertical = rect.Height - height;
                    int longSideFit = Mathf.Max(leftoverHorizontal, leftoverVertical);
                    int shortSideFit = Mathf.Min(leftoverHorizontal, leftoverVertical);

                    if (longSideFit < bestLongSideFit ||
                        (longSideFit == bestLongSideFit && shortSideFit < bestShortSideFit))
                    {
                        bestRect = rect;
                        bestLongSideFit = longSideFit;
                        bestShortSideFit = shortSideFit;
                    }
                }
            }

            return bestRect;
        }

        /// <summary>
        /// 更新空闲矩形列表
        /// </summary>
        private void UpdateFreeRects(int x, int y, int width, int height)
        {
            var newFreeRects = new List<FreeRectangle>();

            foreach (var rect in _freeRects)
            {
                // 检查是否与新放置的矩形相交
                if (rect.X < x + width && rect.X + rect.Width > x &&
                    rect.Y < y + height && rect.Y + rect.Height > y)
                {
                    // 相交，需要分割
                    SplitFreeRect(rect, x, y, width, height, newFreeRects);
                }
                else
                {
                    // 不相交，保留
                    newFreeRects.Add(rect);
                }
            }

            _freeRects = newFreeRects;
        }

        /// <summary>
        /// 分割空闲矩形
        /// </summary>
        private void SplitFreeRect(FreeRectangle rect, int x, int y, int width, int height, List<FreeRectangle> newRects)
        {
            int rectRight = rect.X + rect.Width;
            int rectBottom = rect.Y + rect.Height;
            int newRight = x + width;
            int newBottom = y + height;

            // 左边剩余区域
            if (rect.X < x)
            {
                newRects.Add(new FreeRectangle(rect.X, rect.Y, x - rect.X, rect.Height));
            }

            // 右边剩余区域
            if (rectRight > newRight)
            {
                newRects.Add(new FreeRectangle(newRight, rect.Y, rectRight - newRight, rect.Height));
            }

            // 上边剩余区域
            if (rect.Y < y)
            {
                newRects.Add(new FreeRectangle(rect.X, rect.Y, rect.Width, y - rect.Y));
            }

            // 下边剩余区域
            if (rectBottom > newBottom)
            {
                newRects.Add(new FreeRectangle(rect.X, newBottom, rect.Width, rectBottom - newBottom));
            }
        }
    }
}
