using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class SpriteProcessorWindow : EditorWindow
{
    private const string OutputFolderName = "Processed";

    private float contrast = 1.2f;
    private float saturation = 1.15f;
    private float brightness = 0f;
    private float edgeDarken = 0.25f;
    private float alphaCleanup = 0.03f;
    private float sharpen = 0.15f;
    private string previewSourcePath;
    private Color[] previewSourcePixels;
    private int previewWidth;
    private int previewHeight;
    private Texture2D previewTexture;

    [MenuItem("Tools/Pumpkins/Process Sprites")]
    public static void Open()
    {
        GetWindow<SpriteProcessorWindow>("Process Sprites");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Selected Sprites", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Select Texture2D or Sprite assets in Project. Processed PNGs are saved next to source files in a Processed folder.",
            MessageType.Info);
        EditorGUILayout.HelpBox(
            "Best for one sprite per PNG. Multi-sprite slicing is not copied.",
            MessageType.Warning);

        EditorGUILayout.Space();
        EditorGUI.BeginChangeCheck();
        contrast = EditorGUILayout.Slider("Contrast", contrast, 0.5f, 2.5f);
        saturation = EditorGUILayout.Slider("Saturation", saturation, 0f, 2.5f);
        brightness = EditorGUILayout.Slider("Brightness", brightness, -0.5f, 0.5f);
        edgeDarken = EditorGUILayout.Slider("Edge Darken", edgeDarken, 0f, 1f);
        alphaCleanup = EditorGUILayout.Slider("Alpha Cleanup", alphaCleanup, 0f, 0.5f);
        sharpen = EditorGUILayout.Slider("Sharpen", sharpen, 0f, 1f);

        if (EditorGUI.EndChangeCheck())
        {
            RebuildPreview();
        }

        DrawPreview();

        EditorGUILayout.Space();
        using (new EditorGUI.DisabledScope(GetSelectedTexturePaths().Count == 0))
        {
            if (GUILayout.Button("Process Selected Sprites"))
            {
                ProcessSelectedSprites();
            }
        }
    }

    private void OnSelectionChange()
    {
        InvalidatePreview();
        Repaint();
    }

    private void OnDisable()
    {
        DestroyPreviewTexture();
    }

    private void ProcessSelectedSprites()
    {
        List<string> paths = GetSelectedTexturePaths();

        if (paths.Count == 0)
        {
            EditorUtility.DisplayDialog("Process Sprites", "No Texture2D or Sprite assets selected.", "OK");
            return;
        }

        try
        {
            for (int i = 0; i < paths.Count; i++)
            {
                string path = paths[i];
                EditorUtility.DisplayProgressBar("Process Sprites", path, (float)i / paths.Count);
                ProcessTexture(path);
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Process Sprites", $"Processed {paths.Count} sprite texture(s).", "OK");
    }

    private void DrawPreview()
    {
        List<string> paths = GetSelectedTexturePaths();

        if (paths.Count == 0)
        {
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("Select at least one sprite texture to preview.", MessageType.Info);
            return;
        }

        if (previewSourcePath != paths[0])
        {
            LoadPreviewSource(paths[0]);
            RebuildPreview();
        }

        if (previewTexture == null)
        {
            return;
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Live Preview", EditorStyles.boldLabel);
        EditorGUILayout.LabelField(previewSourcePath, EditorStyles.miniLabel);

        float previewSize = Mathf.Min(position.width - 24f, 360f);
        Rect rect = GUILayoutUtility.GetRect(previewSize, previewSize, GUILayout.ExpandWidth(false));
        EditorGUI.DrawTextureTransparent(rect, previewTexture, ScaleMode.ScaleToFit);
    }

    private void LoadPreviewSource(string sourcePath)
    {
        InvalidatePreview();
        previewSourcePath = sourcePath;

        TextureImporter sourceImporter = AssetImporter.GetAtPath(sourcePath) as TextureImporter;

        if (sourceImporter == null)
        {
            return;
        }

        bool originalReadable = sourceImporter.isReadable;

        try
        {
            if (!sourceImporter.isReadable)
            {
                sourceImporter.isReadable = true;
                sourceImporter.SaveAndReimport();
            }

            Texture2D sourceTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(sourcePath);

            if (sourceTexture == null)
            {
                return;
            }

            previewWidth = sourceTexture.width;
            previewHeight = sourceTexture.height;
            previewSourcePixels = sourceTexture.GetPixels();
        }
        finally
        {
            RestoreReadable(sourceImporter, originalReadable);
        }
    }

    private void RebuildPreview()
    {
        if (previewSourcePixels == null || previewWidth <= 0 || previewHeight <= 0)
        {
            return;
        }

        Color[] processedPixels = ProcessPixels(previewSourcePixels, previewWidth, previewHeight);

        if (previewTexture == null || previewTexture.width != previewWidth || previewTexture.height != previewHeight)
        {
            DestroyPreviewTexture();
            previewTexture = new Texture2D(previewWidth, previewHeight, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
        }

        previewTexture.SetPixels(processedPixels);
        previewTexture.Apply();
    }

    private void InvalidatePreview()
    {
        previewSourcePath = null;
        previewSourcePixels = null;
        previewWidth = 0;
        previewHeight = 0;
        DestroyPreviewTexture();
    }

    private void DestroyPreviewTexture()
    {
        if (previewTexture == null)
        {
            return;
        }

        DestroyImmediate(previewTexture);
        previewTexture = null;
    }

    private List<string> GetSelectedTexturePaths()
    {
        HashSet<string> uniquePaths = new HashSet<string>();

        foreach (Object selectedObject in Selection.objects)
        {
            string path = AssetDatabase.GetAssetPath(selectedObject);

            if (string.IsNullOrEmpty(path))
            {
                continue;
            }

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;

            if (importer == null)
            {
                continue;
            }

            uniquePaths.Add(path);
        }

        return new List<string>(uniquePaths);
    }

    private void ProcessTexture(string sourcePath)
    {
        TextureImporter sourceImporter = (TextureImporter)AssetImporter.GetAtPath(sourcePath);
        bool originalReadable = sourceImporter.isReadable;

        if (!sourceImporter.isReadable)
        {
            sourceImporter.isReadable = true;
            sourceImporter.SaveAndReimport();
        }

        Texture2D sourceTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(sourcePath);

        if (sourceTexture == null)
        {
            RestoreReadable(sourceImporter, originalReadable);
            return;
        }

        Color[] sourcePixels = sourceTexture.GetPixels();
        Color[] processedPixels = ProcessPixels(sourcePixels, sourceTexture.width, sourceTexture.height);

        Texture2D outputTexture = new Texture2D(sourceTexture.width, sourceTexture.height, TextureFormat.RGBA32, false);
        outputTexture.SetPixels(processedPixels);
        outputTexture.Apply();

        string outputPath = GetOutputPath(sourcePath);
        string outputDirectory = GetAssetDirectory(outputPath);

        if (string.IsNullOrEmpty(outputPath) || string.IsNullOrEmpty(outputDirectory))
        {
            DestroyImmediate(outputTexture);
            RestoreReadable(sourceImporter, originalReadable);
            Debug.LogError($"Process Sprites: Could not create output path for {sourcePath}.");
            return;
        }

        Directory.CreateDirectory(outputDirectory);
        File.WriteAllBytes(outputPath, outputTexture.EncodeToPNG());
        DestroyImmediate(outputTexture);

        RestoreReadable(sourceImporter, originalReadable);
        AssetDatabase.ImportAsset(outputPath);
        CopySpriteImportSettings(sourceImporter, outputPath);
    }

    private Color[] ProcessPixels(Color[] sourcePixels, int width, int height)
    {
        Color[] adjustedPixels = new Color[sourcePixels.Length];

        for (int i = 0; i < sourcePixels.Length; i++)
        {
            adjustedPixels[i] = AdjustColor(sourcePixels[i]);
        }

        Color[] sharpenedPixels = sharpen > 0f
            ? ApplySharpen(adjustedPixels, width, height, sharpen)
            : adjustedPixels;

        if (edgeDarken <= 0f)
        {
            return sharpenedPixels;
        }

        Color[] outputPixels = new Color[sharpenedPixels.Length];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = y * width + x;
                Color color = sharpenedPixels[index];

                if (color.a <= 0f)
                {
                    outputPixels[index] = color;
                    continue;
                }

                float edgeAmount = GetEdgeAmount(adjustedPixels, width, height, x, y);
                float darken = Mathf.Clamp01(edgeAmount * edgeDarken);
                color.r *= 1f - darken;
                color.g *= 1f - darken;
                color.b *= 1f - darken;

                outputPixels[index] = color;
            }
        }

        return outputPixels;
    }

    private Color AdjustColor(Color color)
    {
        if (color.a <= alphaCleanup)
        {
            return new Color(0f, 0f, 0f, 0f);
        }

        float luminance = color.r * 0.299f + color.g * 0.587f + color.b * 0.114f;
        color.r = luminance + (color.r - luminance) * saturation;
        color.g = luminance + (color.g - luminance) * saturation;
        color.b = luminance + (color.b - luminance) * saturation;

        color.r = (color.r - 0.5f) * contrast + 0.5f + brightness;
        color.g = (color.g - 0.5f) * contrast + 0.5f + brightness;
        color.b = (color.b - 0.5f) * contrast + 0.5f + brightness;

        color.r = Mathf.Clamp01(color.r);
        color.g = Mathf.Clamp01(color.g);
        color.b = Mathf.Clamp01(color.b);

        return color;
    }

    private Color[] ApplySharpen(Color[] pixels, int width, int height, float amount)
    {
        Color[] outputPixels = new Color[pixels.Length];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = y * width + x;
                Color center = pixels[index];

                if (center.a <= 0f)
                {
                    outputPixels[index] = center;
                    continue;
                }

                Color blur = GetAverageNeighborColor(pixels, width, height, x, y);
                Color sharpened = center + (center - blur) * amount;
                sharpened.r = Mathf.Clamp01(sharpened.r);
                sharpened.g = Mathf.Clamp01(sharpened.g);
                sharpened.b = Mathf.Clamp01(sharpened.b);
                sharpened.a = center.a;

                outputPixels[index] = sharpened;
            }
        }

        return outputPixels;
    }

    private Color GetAverageNeighborColor(Color[] pixels, int width, int height, int x, int y)
    {
        Color total = Color.clear;
        int count = 0;

        for (int offsetY = -1; offsetY <= 1; offsetY++)
        {
            for (int offsetX = -1; offsetX <= 1; offsetX++)
            {
                int sampleX = Mathf.Clamp(x + offsetX, 0, width - 1);
                int sampleY = Mathf.Clamp(y + offsetY, 0, height - 1);
                total += pixels[sampleY * width + sampleX];
                count++;
            }
        }

        return total / count;
    }

    private float GetEdgeAmount(Color[] pixels, int width, int height, int x, int y)
    {
        float centerAlpha = pixels[y * width + x].a;
        float lowestNeighborAlpha = centerAlpha;

        for (int offsetY = -1; offsetY <= 1; offsetY++)
        {
            for (int offsetX = -1; offsetX <= 1; offsetX++)
            {
                if (offsetX == 0 && offsetY == 0)
                {
                    continue;
                }

                int sampleX = x + offsetX;
                int sampleY = y + offsetY;

                if (sampleX < 0 || sampleX >= width || sampleY < 0 || sampleY >= height)
                {
                    lowestNeighborAlpha = 0f;
                    continue;
                }

                lowestNeighborAlpha = Mathf.Min(lowestNeighborAlpha, pixels[sampleY * width + sampleX].a);
            }
        }

        return Mathf.Clamp01(centerAlpha - lowestNeighborAlpha);
    }

    private string GetOutputPath(string sourcePath)
    {
        string normalizedPath = sourcePath.Replace("\\", "/");
        string directory = GetAssetDirectory(normalizedPath);
        string fileName = SanitizeFileName(GetAssetFileNameWithoutExtension(normalizedPath));

        if (string.IsNullOrEmpty(directory) || string.IsNullOrEmpty(fileName))
        {
            return string.Empty;
        }

        string outputDirectory = $"{directory}/{OutputFolderName}";
        EnsureAssetFolder(directory, OutputFolderName);

        string baseOutputPath = $"{outputDirectory}/{fileName}_processed.png";

        return AssetDatabase.GenerateUniqueAssetPath(baseOutputPath);
    }

    private void EnsureAssetFolder(string parentDirectory, string folderName)
    {
        if (string.IsNullOrEmpty(parentDirectory) || string.IsNullOrEmpty(folderName))
        {
            return;
        }

        string folderPath = $"{parentDirectory}/{folderName}";

        if (AssetDatabase.IsValidFolder(folderPath))
        {
            return;
        }

        AssetDatabase.CreateFolder(parentDirectory, folderName);
    }

    private string GetAssetDirectory(string assetPath)
    {
        if (string.IsNullOrEmpty(assetPath))
        {
            return string.Empty;
        }

        string normalizedPath = assetPath.Replace("\\", "/");
        int slashIndex = normalizedPath.LastIndexOf('/');

        if (slashIndex < 0)
        {
            return string.Empty;
        }

        return normalizedPath.Substring(0, slashIndex);
    }

    private string GetAssetFileNameWithoutExtension(string assetPath)
    {
        if (string.IsNullOrEmpty(assetPath))
        {
            return string.Empty;
        }

        string normalizedPath = assetPath.Replace("\\", "/");
        int slashIndex = normalizedPath.LastIndexOf('/');
        int fileNameStart = slashIndex >= 0 ? slashIndex + 1 : 0;
        int dotIndex = normalizedPath.LastIndexOf('.');

        if (dotIndex < fileNameStart)
        {
            dotIndex = normalizedPath.Length;
        }

        return normalizedPath.Substring(fileNameStart, dotIndex - fileNameStart);
    }

    private string SanitizeFileName(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
        {
            return string.Empty;
        }

        char[] invalidChars = Path.GetInvalidFileNameChars();
        char[] chars = fileName.ToCharArray();

        for (int i = 0; i < chars.Length; i++)
        {
            for (int j = 0; j < invalidChars.Length; j++)
            {
                if (chars[i] == invalidChars[j])
                {
                    chars[i] = '_';
                    break;
                }
            }
        }

        return new string(chars);
    }

    private void RestoreReadable(TextureImporter importer, bool originalReadable)
    {
        if (importer.isReadable == originalReadable)
        {
            return;
        }

        importer.isReadable = originalReadable;
        importer.SaveAndReimport();
    }

    private void CopySpriteImportSettings(TextureImporter sourceImporter, string outputPath)
    {
        TextureImporter outputImporter = AssetImporter.GetAtPath(outputPath) as TextureImporter;

        if (outputImporter == null)
        {
            return;
        }

        outputImporter.textureType = TextureImporterType.Sprite;
        outputImporter.spriteImportMode = sourceImporter.spriteImportMode;
        outputImporter.spritePixelsPerUnit = sourceImporter.spritePixelsPerUnit;
        outputImporter.mipmapEnabled = sourceImporter.mipmapEnabled;
        outputImporter.filterMode = sourceImporter.filterMode;
        outputImporter.textureCompression = sourceImporter.textureCompression;
        outputImporter.alphaIsTransparency = true;
        outputImporter.SaveAndReimport();
    }
}
