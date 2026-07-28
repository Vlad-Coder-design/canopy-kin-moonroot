using UnityEditor;
using UnityEngine;

namespace CanopyKin.Editor
{
    /// <summary>
    /// Full-resolution source textures are retained for Windows. WebGL receives
    /// an independently downscaled platform import, so browser limits never cap
    /// the primary edition's source quality.
    /// </summary>
    public sealed class ProductionAssetImporter : AssetPostprocessor
    {
        const string HighQualityRoot = "Assets/Resources/HighQuality/";
        const string ProductionAnt = "Assets/Resources/Models/Ant/CanopyKinProductionAnt.fbx";
        const string ProductionSpider =
            "Assets/Resources/Models/Creatures/CanopyKinFishingSpider.fbx";
        const string ProductionBeetle =
            "Assets/Resources/Models/Creatures/CanopyKinRhinocerosBeetle.fbx";
        const string DeadTreeTrunk =
            "Assets/Resources/HighQuality/PolyHaven/DeadTreeTrunk/dead_tree_trunk_4k.fbx";
        const string ProductionRootNetwork =
            "Assets/Resources/Models/Environment/CanopyKinRootNetwork.fbx";

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(HighQualityRoot, System.StringComparison.Ordinal)) return;

            var importer = (TextureImporter)assetImporter;
            bool normal = assetPath.Contains("_nor_dx_");
            bool packedMask = assetPath.Contains("_arm_");
            bool singleChannelMask = assetPath.Contains("_rough_") ||
                                     assetPath.Contains("_ao_") ||
                                     assetPath.Contains("_disp_");

            importer.textureType = normal
                ? TextureImporterType.NormalMap
                : singleChannelMask ? TextureImporterType.SingleChannel : TextureImporterType.Default;
            importer.sRGBTexture = !normal && !singleChannelMask && !packedMask;
            importer.mipmapEnabled = true;
            importer.streamingMipmaps = true;
            importer.streamingMipmapsPriority = assetPath.Contains("_diff_") ? 2 : 1;
            importer.wrapMode = assetPath.Contains("/FishingSpider/")
                ? TextureWrapMode.Clamp
                : TextureWrapMode.Repeat;
            importer.filterMode = FilterMode.Trilinear;
            importer.anisoLevel = 16;
            importer.maxTextureSize = 8192;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.crunchedCompression = false;

            var standalone = importer.GetPlatformTextureSettings("Standalone");
            standalone.name = "Standalone";
            standalone.overridden = true;
            standalone.maxTextureSize = 8192;
            standalone.resizeAlgorithm = TextureResizeAlgorithm.Mitchell;
            standalone.textureCompression = TextureImporterCompression.CompressedHQ;
            standalone.format = normal
                ? TextureImporterFormat.BC5
                : singleChannelMask ? TextureImporterFormat.BC4 : TextureImporterFormat.BC7;
            importer.SetPlatformTextureSettings(standalone);

            var web = importer.GetPlatformTextureSettings("WebGL");
            web.name = "WebGL";
            web.overridden = true;
            web.maxTextureSize = 2048;
            web.resizeAlgorithm = TextureResizeAlgorithm.Mitchell;
            web.textureCompression = TextureImporterCompression.CompressedHQ;
            web.format = TextureImporterFormat.Automatic;
            importer.SetPlatformTextureSettings(web);
        }

        void OnPreprocessModel()
        {
            var importer = (ModelImporter)assetImporter;
            if (assetPath.Equals(DeadTreeTrunk, System.StringComparison.Ordinal) ||
                assetPath.Equals(ProductionRootNetwork, System.StringComparison.Ordinal))
            {
                importer.globalScale = 1f;
                importer.useFileScale = true;
                importer.importAnimation = false;
                importer.animationType = ModelImporterAnimationType.None;
                importer.importCameras = false;
                importer.importLights = false;
                importer.addCollider = false;
                // Runtime traversal uses the high-detail scan as a MeshCollider.
                // Keeping this one landmark readable avoids a fake box collider
                // and is a deliberate Windows-quality memory tradeoff.
                importer.isReadable = true;
                importer.meshCompression = ModelImporterMeshCompression.Off;
                importer.importNormals = ModelImporterNormals.Import;
                importer.importTangents = ModelImporterTangents.CalculateMikk;
                importer.materialImportMode = ModelImporterMaterialImportMode.None;
                return;
            }

            bool productionAnt = assetPath.Equals(ProductionAnt, System.StringComparison.Ordinal);
            bool productionSpider = assetPath.Equals(ProductionSpider, System.StringComparison.Ordinal);
            bool productionBeetle = assetPath.Equals(ProductionBeetle, System.StringComparison.Ordinal);
            if (!productionAnt && !productionSpider && !productionBeetle) return;
            importer.globalScale = 1f;
            importer.useFileScale = true;
            importer.importAnimation = true;
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.optimizeGameObjects = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.addCollider = false;
            importer.isReadable = false;
            importer.meshCompression = ModelImporterMeshCompression.Off;
            importer.importNormals = ModelImporterNormals.Import;
            importer.importTangents = ModelImporterTangents.CalculateMikk;
            importer.materialImportMode = productionSpider || productionBeetle
                ? ModelImporterMaterialImportMode.None
                : ModelImporterMaterialImportMode.ImportStandard;
        }
    }
}
