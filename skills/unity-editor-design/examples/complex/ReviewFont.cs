using System.Linq;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.UIElements;

namespace UnityDesignExamples
{
    public static class ReviewFont
    {
        private static FontAsset regular;
        private static FontAsset bold;
        public static string Name => regular == null ? "Editor 默认字体" : regular.faceInfo.familyName;

        public static void Apply(VisualElement root)
        {
            if (regular == null && Font.GetOSInstalledFontNames().Contains("Microsoft YaHei UI"))
            {
                regular = FontAsset.CreateFontAsset("Microsoft YaHei UI", "Regular", 64);
                bold = FontAsset.CreateFontAsset("Microsoft YaHei UI", "Bold", 64);
                if (regular != null)
                {
                    regular.name = "ReviewFont-Regular";
                    regular.hideFlags = HideFlags.HideAndDontSave;
                    if (bold != null)
                    {
                        bold.name = "ReviewFont-Bold";
                        bold.hideFlags = HideFlags.HideAndDontSave;
                        regular.fontWeightTable[7].regularTypeface = bold;
                    }
                }
            }
            if (regular != null)
                root.style.unityFontDefinition = FontDefinition.FromSDFFont(regular);
        }

        public static void Release()
        {
            DestroyFont(regular);
            DestroyFont(bold);
            regular = null;
            bold = null;
        }

        private static void DestroyFont(FontAsset font)
        {
            if (font == null) return;
            foreach (var texture in font.atlasTextures)
                if (texture != null) UnityEngine.Object.DestroyImmediate(texture);
            if (font.material != null) UnityEngine.Object.DestroyImmediate(font.material);
            UnityEngine.Object.DestroyImmediate(font);
        }
    }
}
