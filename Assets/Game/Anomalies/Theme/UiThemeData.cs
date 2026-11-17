using TMPro;
using UnityEngine;

namespace Game.Anomalies
{
    /// <summary>Shared authored art, font and colors of the search HUD, popups and chapter result screens.</summary>
    [CreateAssetMenu(fileName = "UiThemeData", menuName = "Game/UI/UiThemeData")]
    public sealed class UiThemeData : ScriptableObject
    {
        private const string ResourcePath = "UiThemeData";
        private static UiThemeData _instance;

        [Header("Backgrounds and frames")]
        [Tooltip("Full-screen purple pattern behind the HUD and the search board.")]
        public Sprite Background;
        [Tooltip("Sliced white frame drawn around the search board.")]
        public Sprite BoardFrame;
        [Tooltip("Sliced dark rounded panel used inside popups and result screens.")]
        public Sprite PanelDark;
        [Tooltip("Sliced cream popup body.")]
        public Sprite PopupBody;
        [Tooltip("Sliced cream popup body used for tall windows such as the investigation.")]
        public Sprite PopupBodyTall;
        [Tooltip("Light strip reserved above the screen bottom for the banner advertisement.")]
        public Sprite BannerStrip;

        [Header("Plates and decorations")]
        [Tooltip("Sliced pill behind HUD counters.")]
        public Sprite PlateCounter;
        [Tooltip("Sliced pill behind the chapter location title.")]
        public Sprite PlateChapter;
        [Tooltip("Sliced pill behind the task text beneath the board.")]
        public Sprite PlateTask;
        [Tooltip("Paper strip behind narrative text; its tape corners are part of the sprite.")]
        public Sprite PlateNarrative;
        [Tooltip("Red tape label carrying the word ORIGINAL.")]
        public Sprite TapeRed;
        [Tooltip("Red pin holding the original photo.")]
        public Sprite PinRed;
        [Tooltip("Polaroid frame of the original photo thumbnail.")]
        public Sprite Polaroid;
        [Tooltip("Sliced red ribbon carrying popup titles.")]
        public Sprite RibbonRed;

        [Header("Buttons and icons")]
        [Tooltip("Sliced green primary button.")]
        public Sprite ButtonGreen;
        [Tooltip("Sliced purple rewarded-advertisement button.")]
        public Sprite ButtonPurple;
        [Tooltip("Red cross closing a popup.")]
        public Sprite CloseIcon;
        [Tooltip("Round green badge carrying a count or a plus.")]
        public Sprite BadgeGreen;
        [Tooltip("Gold coin icon.")]
        public Sprite CoinIcon;
        [Tooltip("Magnifier icon of the Hint booster and its counter.")]
        public Sprite HintIcon;
        [Tooltip("Hint booster pack shown in the out-of-hints popup.")]
        public Sprite HintPackIcon;
        [Tooltip("Camera icon next to the original photo.")]
        public Sprite CameraIcon;
        [Tooltip("Colored camera shown in the out-of-photos popup.")]
        public Sprite CameraPackIcon;
        [Tooltip("Rewarded-advertisement clapper icon.")]
        public Sprite AdsIcon;
        [Tooltip("Pointing finger of the start prompt.")]
        public Sprite FingerIcon;
        [Tooltip("Notepad icon of the evidence counter. Empty hides the icon: the delivered art contains no notepad.")]
        public Sprite ClueCounterIcon;

        [Header("Search board")]
        [Tooltip("Empty closed slot of an unfound change; a question mark is drawn over it.")]
        public Sprite SlotEmpty;
        [Tooltip("Green frame of a found change slot.")]
        public Sprite SlotFoundFrame;
        [Tooltip("Green check on a found change slot.")]
        public Sprite CheckIcon;
        [Tooltip("Ring marking a correct tap.")]
        public Sprite MarkCorrect;
        [Tooltip("Ring pulsing around the hinted change.")]
        public Sprite HintTargetRing;
        [Tooltip("Ring surrounding the photo countdown number.")]
        public Sprite TimerRing;

        [Header("Evidence and results")]
        [Tooltip("Gold ring framing a found evidence.")]
        public Sprite EvidenceRing;
        [Tooltip("Cream disc behind an evidence icon.")]
        public Sprite EvidenceDisc;
        [Tooltip("Dashed empty circle standing for an unfound evidence, with a question mark.")]
        public Sprite EvidenceEmpty;
        [Tooltip("Large dashed circle of the unsolved culprit.")]
        public Sprite EvidenceBigContour;
        [Tooltip("White sunburst behind a celebrated item; tinted at runtime.")]
        public Sprite Sunburst;
        [Tooltip("Game logo shown on the investigation and solved screens.")]
        public Sprite Logo;
        [Tooltip("Red CASE SOLVED stamp over the culprit scene.")]
        public Sprite CaseSolvedStamp;
        [Tooltip("Particle sprite used by the all-evidence effect.")]
        public Sprite Particle;
        [Tooltip("Glowing streak of an evidence flying to the culprit slot.")]
        public Sprite TrailGlow;

        [Header("Typography")]
        [Tooltip("Bold display font of every label.")]
        public TMP_FontAsset Font;
        [Tooltip("Text color of labels on dark backgrounds.")]
        public Color LightTextColor = Color.white;
        [Tooltip("Outline color of labels on dark backgrounds.")]
        public Color LightOutlineColor = new(0.1f, 0.05f, 0.2f, 1f);
        [Tooltip("Text color of labels on cream popups.")]
        public Color DarkTextColor = new(0.31f, 0.16f, 0.07f, 1f);
        [Tooltip("Text color of highlighted headings such as You found the evidence.")]
        public Color GoldTextColor = new(1f, 0.85f, 0.25f, 1f);
        [Tooltip("Outline color of highlighted headings.")]
        public Color GoldOutlineColor = new(0.35f, 0.17f, 0.02f, 1f);
        [Range(0f, 1f), Tooltip("Outline thickness of outlined labels.")]
        public float OutlineWidth = 0.28f;
        [Tooltip("Renders labels with the bold weight of the font to approach the heavy mockup lettering.")]
        public bool BoldText = true;
        [Tooltip("Extra spacing between bold characters in TextMeshPro units; negative values tighten the wide bold lettering.")]
        public float BoldCharacterSpacing = -7f;

        [Header("Colors")]
        [Tooltip("Dim veil behind popups and result screens.")]
        public Color VeilColor = new(0.06f, 0.03f, 0.14f, 0.82f);
        [Tooltip("Nearly opaque veil behind the original-photo overlay, hiding the board to focus on the photo.")]
        public Color PhotoVeilColor = new(0.05f, 0.03f, 0.12f, 0.96f);
        [Tooltip("Tint of closed slots that were not yet found.")]
        public Color ClosedSlotColor = new(1f, 1f, 1f, 1f);

        /// <summary>Returns the shared theme, or an empty in-memory theme when the asset is missing so presentation still builds.</summary>
        public static UiThemeData LoadOrDefault()
        {
            var theme = Load();
            if (theme == null)
                theme = _instance = CreateInstance<UiThemeData>();
            return theme;
        }

        /// <summary>Loads the shared theme once from Resources; returns null when it has not been created.</summary>
        public static UiThemeData Load()
        {
            if (_instance == null)
                _instance = Resources.Load<UiThemeData>(ResourcePath);
            return _instance;
        }
    }
}
