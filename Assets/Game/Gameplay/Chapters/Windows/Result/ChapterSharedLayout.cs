using UnityEngine;

namespace Game.Gameplay.Chapters
{
    /// <summary>Normalized rectangle (origin bottom-left) of a full-stretch layout.</summary>
    public readonly struct ChapterAnchors
    {
        public readonly Vector2 Min;
        public readonly Vector2 Max;

        public ChapterAnchors(float minX, float minY, float maxX, float maxY)
        {
            Min = new Vector2(minX, minY);
            Max = new Vector2(maxX, maxY);
        }
    }

    /// <summary>Where the evidence row sits for a stage, in mockup pixels (1080 wide, ReferenceHeight tall, origin top-left).</summary>
    public readonly struct ChapterRowLayout
    {
        public readonly float CenterY;
        public readonly float Diameter;
        public readonly float Width;
        public readonly float GapFactor;
        public readonly float ReferenceHeight;

        public ChapterRowLayout(float centerY, float diameter, float width, float gapFactor, float referenceHeight)
        {
            CenterY = centerY;
            Diameter = diameter;
            Width = width;
            GapFactor = gapFactor;
            ReferenceHeight = referenceHeight;
        }
    }

    /// <summary>Where each element shared by several result stages must be during a stage; null means hidden. Shared elements keep their identity and move between layouts instead of being rebuilt per stage.</summary>
    public sealed class ChapterSharedLayout
    {
        public ChapterRowLayout? Row;
        public ChapterAnchors? Sunburst;
        public ChapterAnchors? Logo;
        public ChapterAnchors? Contour;
        public ChapterAnchors? Question;
        public ChapterAnchors? Button;
        public string ButtonText;
        /// <summary>The contour stays in place while the stage fades in and only then fades out, so the stage's own ring takes over the same spot.</summary>
        public bool ContourFadesWithStage;
    }
}
