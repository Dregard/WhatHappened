using System.Collections.Generic;
using System.IO;
using Game.Anomalies;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Game.UI.Editor
{
    /// <summary>Copies the delivered UI art into the theme domain, configures sprite import and fills the shared UiThemeData asset.</summary>
    public static class UiThemeBuilder
    {
        private const string SourceFolder = "Assets/TempAssets/Assets";
        private const string ThemeFolder = "Assets/Game/Anomalies/Theme";
        private const string ArtFolder = ThemeFolder + "/Art";
        private const string ResourcesFolder = ThemeFolder + "/Resources";
        private const string ThemePath = ResourcesFolder + "/UiThemeData.asset";
        private const string FontPath = "Assets/Game/Text/PleasureFonts - Boomtown Bold SDF.asset";

        /// <summary>Imports sprites from the working delivery folder and assigns them to the theme; safe to repeat.</summary>
        [MenuItem("Game/UI/Build Theme Data")]
        public static void Build()
        {
            EnsureFolder(ThemeFolder, "Art");
            EnsureFolder(ThemeFolder, "Resources");
            foreach (var path in Directory.GetFiles(SourceFolder, "*.png"))
            {
                var name = Path.GetFileName(path);
                var target = $"{ArtFolder}/{name}";
                if (name == "Daily_Btn.png" || File.Exists(target))
                    continue;
                AssetDatabase.CopyAsset(path.Replace('\\', '/'), target);
            }
            AssetDatabase.Refresh();
            foreach (var path in Directory.GetFiles(ArtFolder, "*.png"))
                Configure(path.Replace('\\', '/'));
            AssetDatabase.Refresh();

            var theme = AssetDatabase.LoadAssetAtPath<UiThemeData>(ThemePath);
            if (theme == null)
            {
                theme = ScriptableObject.CreateInstance<UiThemeData>();
                AssetDatabase.CreateAsset(theme, ThemePath);
            }
            theme.Background = Sprite("bg_main");
            theme.BoardFrame = Sprite("frame_field");
            theme.PanelDark = Sprite("panel_dark");
            theme.PopupBody = Sprite("popup_body");
            theme.PopupBodyTall = Sprite("popup_body_tall");
            theme.BannerStrip = Sprite("banner_ad");
            theme.PlateCounter = Sprite("plate_counter");
            theme.PlateChapter = Sprite("plate_chapter");
            theme.PlateTask = Sprite("plate_task");
            theme.PlateNarrative = Sprite("plate_narrative");
            theme.TapeRed = Sprite("tape_red");
            theme.PinRed = Sprite("pin_red");
            theme.Polaroid = Sprite("polaroid");
            theme.RibbonRed = Sprite("ribbon_red");
            theme.ButtonGreen = Sprite("btn_green");
            theme.ButtonPurple = Sprite("btn_purple");
            theme.CloseIcon = Sprite("FTS_Cross_Icon");
            theme.BadgeGreen = Sprite("badge_green");
            theme.CoinIcon = Sprite("icon_coin_dollar");
            theme.HintIcon = Sprite("Hints_Icon_1");
            theme.HintPackIcon = Sprite("kit_Hints_Icon_2_shine");
            theme.CameraIcon = Sprite("icon_camera");
            theme.CameraPackIcon = Sprite("icon_camera_color");
            theme.AdsIcon = Sprite("kit_ADS_icon");
            theme.FingerIcon = Sprite("kit_finger");
            theme.SlotEmpty = Sprite("slot_empty");
            theme.SlotFoundFrame = Sprite("slot_found_frame");
            theme.CheckIcon = Sprite("kit_Done_Icon_Big");
            theme.MarkCorrect = Sprite("mark_correct");
            theme.HintTargetRing = Sprite("hint_target_ring");
            theme.TimerRing = Sprite("timer_ring");
            theme.EvidenceRing = Sprite("ev_ring");
            theme.EvidenceDisc = Sprite("ev_disc");
            theme.EvidenceEmpty = Sprite("ev_empty");
            theme.EvidenceBigContour = Sprite("ev_big_contour");
            theme.Sunburst = Sprite("kit_shining");
            theme.Logo = Sprite("logo_what_happened");
            theme.CaseSolvedStamp = Sprite("stamp_case_solved");
            theme.Particle = Sprite("particles");
            theme.TrailGlow = Sprite("trail_glow");
            theme.Font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            theme.OutlineWidth = 0.28f;
            EditorUtility.SetDirty(theme);
            AssetDatabase.SaveAssets();
            Debug.Log("UiThemeData built: " + ThemePath);
        }

        /// <summary>Creates a missing child folder through the AssetDatabase.</summary>
        private static void EnsureFolder(string parent, string child)
        {
            if (!AssetDatabase.IsValidFolder($"{parent}/{child}"))
                AssetDatabase.CreateFolder(parent, child);
        }

        /// <summary>Loads a copied sprite by file name without extension.</summary>
        private static Sprite Sprite(string name)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtFolder}/{name}.png");
            if (sprite == null)
                Debug.LogWarning("Missing theme sprite " + name);
            return sprite;
        }

        /// <summary>Imports one texture as a single sprite; stretchable panels get nine-slice borders.</summary>
        private static void Configure(string path)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            var sliced = new Dictionary<string, float>
            {
                { "plate_task", 0.5f }, { "plate_counter", 0.5f }, { "plate_chapter", 0.5f },
                { "btn_green", 0.5f }, { "btn_purple", 0.5f },
                { "popup_body", 0.1f }, { "popup_body_tall", 0.1f }, { "panel_dark", 0.1f }, { "frame_field", 0.08f },
            };
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.maxTextureSize = 2048;
            var name = Path.GetFileNameWithoutExtension(path);
            if (sliced.TryGetValue(name, out var fraction))
            {
                importer.GetSourceTextureWidthAndHeight(out var width, out var height);
                var edge = Mathf.Min(width, height) * fraction;
                importer.spriteBorder = new Vector4(edge, edge, edge, edge);
            }
            if (name == "polaroid")
                importer.spriteBorder = new Vector4(45, 95, 45, 40);
            importer.SaveAndReimport();
        }
    }
}
