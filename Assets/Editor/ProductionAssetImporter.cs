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

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(HighQualityRoot, System.StringComparison.Ordinal)) return;

            var importer = (TextureImporter)assetImporter;
            bool normal = assetPath.Contains("_nor_dx_");
            bool mask = assetPath.Contains("_rough_") ||
                        assetPath.Contains("_ao_") ||
                        assetPath.Contains("_disp_");

            importer.textureType = normal
                ? TextureImporterType.NormalMap
                : mask ? TextureImporterType.SingleChannel : TextureImporterType.Default;
            importer.sRGBTexture = !normal && !mask;
            importer.mipmapEnabled = true;
            importer.streamingMipmaps = true;
            importer.streamingMipmapsPriority = assetPath.Contains("_diff_") ? 2 : 1;
            importer.wrapMode = TextureWrapMode.Repeat;
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
                : mask ? TextureImporterFormat.BC4 : TextureImporterFormat.BC7;
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
            if (!assetPath.Equals(ProductionAnt, System.StringComparison.Ordinal)) return;
            var importer = (ModelImporter)assetImporter;
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
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
        }
    }
}
