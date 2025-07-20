using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using XFramework.Utils;

namespace SimpleDressup
{
    /// <summary>
    /// 图集生成器 - 负责将多个纹理片段合并到一张图集上
    /// 处理纹理装箱、渲染和UV映射
    /// </summary>
    public class AtlasGenerator
    {
        private int _atlasSize;
        private TexturePacker _texturePacker;
        private RenderTexture _atlasTexture;
        private Material _blitMaterial;

        public int AtlasSize => _atlasSize;
        public RenderTexture AtlasTexture => _atlasTexture;

        /// <summary>
        /// 图集生成结果
        /// </summary>
        public struct AtlasResult
        {
            public bool Success;
            public RenderTexture AtlasTexture;
            public Dictionary<TextureFragment, Rect> FragmentUVs;
            public Vector2 UsedSize;
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        public AtlasGenerator(int atlasSize = 1024)
        {
            _atlasSize = atlasSize;
            _texturePacker = new TexturePacker(atlasSize);

            // 创建用于纹理合并的材质
            CreateBlitMaterial();
        }

        /// <summary>
        /// 创建用于纹理混合的材质
        /// </summary>
        private void CreateBlitMaterial()
        {
            // 使用简单的 Unlit shader 进行纹理复制
            var shader = Shader.Find("Hidden/BlitCopy");
            if (shader == null)
            {
                shader = Shader.Find("Unlit/Texture");
            }

            if (shader != null)
            {
                _blitMaterial = new Material(shader);
            }
            else
            {
                Log.Warning("AtlasGenerator: 未找到合适的 Blit Shader");
            }
        }

        /// <summary>
        /// 生成图集
        /// </summary>
        public AtlasResult GenerateAtlas(List<TextureFragment> fragments)
        {
            var result = new AtlasResult
            {
                Success = false,
                FragmentUVs = new Dictionary<TextureFragment, Rect>()
            };

            if (fragments == null || fragments.Count == 0)
            {
                Log.Warning("AtlasGenerator: 纹理片段列表为空");
                return result;
            }

            // 1. 装箱计算
            Log.Debug($"AtlasGenerator: 开始装箱 {fragments.Count} 个纹理片段");
            var packResult = _texturePacker.PackFragments(fragments);

            if (!packResult.Success)
            {
                Log.Error("AtlasGenerator: 装箱失败，图集尺寸不够");
                return result;
            }

            // 2. 创建图集纹理
            CreateAtlasTexture();

            // 3. 渲染纹理到图集
            if (RenderFragmentsToAtlas(packResult.FragmentUVs))
            {
                result.Success = true;
                result.AtlasTexture = _atlasTexture;
                result.FragmentUVs = packResult.FragmentUVs;
                result.UsedSize = new Vector2(packResult.UsedWidth, packResult.UsedHeight);

                Log.Debug($"AtlasGenerator: 图集生成成功 - 尺寸:{_atlasSize}x{_atlasSize}, 使用:{result.UsedSize}");
            }
            else
            {
                Log.Error("AtlasGenerator: 图集渲染失败");
            }

            return result;
        }

        /// <summary>
        /// 创建图集纹理
        /// </summary>
        private void CreateAtlasTexture()
        {
            // 释放旧的纹理
            if (_atlasTexture != null)
            {
                RenderTexture.ReleaseTemporary(_atlasTexture);
            }

            // 创建新的图集纹理
            _atlasTexture = RenderTexture.GetTemporary(_atlasSize, _atlasSize, 0, RenderTextureFormat.ARGB32);
            _atlasTexture.name = "DressupAtlas";
            _atlasTexture.filterMode = FilterMode.Bilinear;

            // 清空纹理
            var oldRT = RenderTexture.active;
            RenderTexture.active = _atlasTexture;
            GL.Clear(true, true, Color.clear);
            RenderTexture.active = oldRT;
        }

        /// <summary>
        /// 将纹理片段渲染到图集上
        /// </summary>
        private bool RenderFragmentsToAtlas(Dictionary<TextureFragment, Rect> fragmentUVs)
        {
            if (_blitMaterial == null)
            {
                Log.Error("AtlasGenerator: BlitMaterial 为空");
                return false;
            }

            var oldRT = RenderTexture.active;
            RenderTexture.active = _atlasTexture;

            try
            {
                foreach (var kvp in fragmentUVs)
                {
                    var fragment = kvp.Key;
                    var uvRect = kvp.Value;

                    var mainTexture = fragment.MainTexture;
                    if (mainTexture == null) continue;

                    // 计算在图集中的像素位置
                    int pixelX = Mathf.RoundToInt(uvRect.x * _atlasSize);
                    int pixelY = Mathf.RoundToInt(uvRect.y * _atlasSize);
                    int pixelW = Mathf.RoundToInt(uvRect.width * _atlasSize);
                    int pixelH = Mathf.RoundToInt(uvRect.height * _atlasSize);

                    // 设置材质参数
                    _blitMaterial.mainTexture = mainTexture;
                    _blitMaterial.color = fragment.TintColor;

                    // 渲染到指定区域
                    RenderTextureToRegion(pixelX, pixelY, pixelW, pixelH);
                }

                return true;
            }
            catch (System.Exception e)
            {
                Log.Error($"AtlasGenerator: 渲染过程出错 - {e.Message}");
                return false;
            }
            finally
            {
                RenderTexture.active = oldRT;
            }
        }

        /// <summary>
        /// 渲染纹理到指定区域
        /// </summary>
        private void RenderTextureToRegion(int x, int y, int width, int height)
        {
            // 设置视口
            GL.Viewport(new Rect(x, y, width, height));

            // 开始渲染
            GL.PushMatrix();
            GL.LoadPixelMatrix(0, width, height, 0);

            // 使用材质渲染一个全屏四边形
            if (_blitMaterial.SetPass(0))
            {
                GL.Begin(GL.QUADS);

                GL.TexCoord2(0, 0);
                GL.Vertex3(0, 0, 0);

                GL.TexCoord2(1, 0);
                GL.Vertex3(width, 0, 0);

                GL.TexCoord2(1, 1);
                GL.Vertex3(width, height, 0);

                GL.TexCoord2(0, 1);
                GL.Vertex3(0, height, 0);

                GL.End();
            }

            GL.PopMatrix();
        }

        /// <summary>
        /// 清理资源
        /// </summary>
        public void Cleanup()
        {
            if (_atlasTexture != null)
            {
                RenderTexture.ReleaseTemporary(_atlasTexture);
                _atlasTexture = null;
            }

            if (_blitMaterial != null)
            {
                Object.DestroyImmediate(_blitMaterial);
                _blitMaterial = null;
            }
        }

        /// <summary>
        /// 析构函数
        /// </summary>
        ~AtlasGenerator()
        {
            Cleanup();
        }
    }
}
