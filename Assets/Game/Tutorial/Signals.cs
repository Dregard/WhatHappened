namespace Game.Tutorial
{
    public static class Signals
    {
        public class TutorialCompletedSignal
        {
        }
        
        public class TutorialStepCompletedSignal
        {
            public string StepId;
            public int LevelNumber;
            public int LevelAttempt;
            public float LevelProgress;
        }
        
        public class ItemEatenSignal
        {
            public string ItemId { get; }
            public ItemEatenSignal(string itemId) => ItemId = itemId;
        }

        public class ButtonClicked
        {
            public string Id { get; set; }
        }

        public class ShowButtonRequested
        {
            public string Id { get; set; }
        }
    }
}