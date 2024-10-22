using System;
using System.Collections.Generic;
using System.Linq;
using XFramework.Utils;

namespace XFramework.Resource
{
    public static class ManifestSerilizer
    {
        public static byte[] SerializeToBytes(Manifest manifest)
        {
            ByteBufferWriter bufferWriter = new ByteBufferWriter(ResourceManagerConfig.ManifestFileMaxSize);

            // 写文件头
            bufferWriter.WriteBytes(ResourceManagerConfig.ManifestBinaryFileHeaderSign);
            // 写清单属性
            bufferWriter.WriteUTF8String(manifest.ResourceVersion);
            bufferWriter.WriteUTF8String(manifest.BuildPipeline);
            // 写 Bundles 信息
            bufferWriter.WriteInt32(manifest.Bundles.Count);
            foreach (ManifestBundle bundle in manifest.Bundles)
            {
                bufferWriter.WriteUTF8String(bundle.Hash);
                bufferWriter.WriteUTF8String(bundle.Name);
                bufferWriter.WriteInt64(bundle.FileSize);
                bufferWriter.WriteBool(bundle.IsEncrypted);
                bufferWriter.WriteUTF8StringArray(bundle.Tags);
                bufferWriter.WriteUTF8StringArray(bundle.DependentBundleNames);
            }
            // 写 Assets 信息
            bufferWriter.WriteInt32(manifest.Assets.Count);
            foreach (ManifestAsset asset in manifest.Assets)
            {
                bufferWriter.WriteUTF8String(asset.Hash);
                bufferWriter.WriteUTF8String(asset.Address);
                bufferWriter.WriteUTF8String(asset.Path);
                bufferWriter.WriteUTF8StringArray(asset.Tags);
                bufferWriter.WriteUTF8String(asset.BundleName);
            }

            return bufferWriter.Bytes;
        }

        public static Manifest DeserializeFromBytes(byte[] bytes)
        {
            var bufferReader = new ByteBufferReader(bytes);

            // 检查文件头
            byte[] fileHeaderSign = bufferReader.ReadBytes(3);
            if (!fileHeaderSign.SequenceEqual(ResourceManagerConfig.ManifestBinaryFileHeaderSign))
            {
                throw new ArgumentException("Deserialize failed. Invalid file header sign.", nameof(bytes));
            }
            // 读清单属性
            var manifest = new Manifest()
            {
                ResourceVersion = bufferReader.ReadUTF8String(),
                BuildPipeline = bufferReader.ReadUTF8String(),
            };
            // 读 Bundles 信息
            int bundleCount = bufferReader.ReadInt32();
            manifest.Bundles = new List<ManifestBundle>(bundleCount);
            for (int i = 0; i < bundleCount; i++)
            {
                var bundle = new ManifestBundle()
                {
                    Hash = bufferReader.ReadUTF8String(),
                    Name = bufferReader.ReadUTF8String(),
                    FileSize = bufferReader.ReadInt64(),
                    IsEncrypted = bufferReader.ReadBool(),
                    Tags = bufferReader.ReadUTF8StringArray(),
                    DependentBundleNames = bufferReader.ReadUTF8StringArray(),
                };
                manifest.Bundles.Add(bundle);
            }
            // 读 Assets 信息
            int assetCount = bufferReader.ReadInt32();
            manifest.Assets = new List<ManifestAsset>(assetCount);
            for (int i = 0; i < assetCount; i++)
            {
                var asset = new ManifestAsset()
                {
                    Hash = bufferReader.ReadUTF8String(),
                    Address = bufferReader.ReadUTF8String(),
                    Path = bufferReader.ReadUTF8String(),
                    Tags = bufferReader.ReadUTF8StringArray(),
                    BundleName = bufferReader.ReadUTF8String(),
                };
                manifest.Assets.Add(asset);
            }
            // 建立 Bundles 字典索引
            manifest.BundleForName = new Dictionary<string, ManifestBundle>(manifest.Bundles.Count);
            foreach (ManifestBundle bundle in manifest.Bundles)
            {
                if (manifest.BundleForName.ContainsKey(bundle.Name))
                {
                    throw new ArgumentException("Deserialize manifest failed. Duplicate bundle name found.", nameof(bytes));
                }
                manifest.BundleForName.Add(bundle.Name, bundle);
            }
            // 建立 Assets 字典索引
            manifest.AssetForPath = new Dictionary<string, ManifestAsset>(manifest.Assets.Count);
            manifest.AssetForAddress = new Dictionary<string, ManifestAsset>(manifest.Assets.Count);
            foreach (ManifestAsset asset in manifest.Assets)
            {
                if (manifest.AssetForPath.ContainsKey(asset.Path) || manifest.AssetForAddress.ContainsKey(asset.Address))
                {
                    throw new ArgumentException("Deserialize manifest failed. Duplicate asset path or address found.", nameof(bytes));
                }
                manifest.AssetForPath.Add(asset.Path, asset);
                manifest.AssetForAddress.Add(asset.Address, asset);
            }

            return manifest;
        }
    }
}