using Game.UI.Windows.Controllers;

namespace Game.Gameplay.Chapters
{
    public sealed class ChapterWindowController : WindowController
    {
        public override bool CanCloseByReturn { get { return false; } }
    }
}
