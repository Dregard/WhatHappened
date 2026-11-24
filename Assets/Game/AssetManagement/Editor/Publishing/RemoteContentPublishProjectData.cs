using UnityEditor;
using UnityEngine;

namespace Game.AssetManagement.Publishing
{
    /// <summary>Project-wide holder persisted to ProjectSettings/RemoteContentPublish.asset. Contains no secrets.</summary>
    [FilePath("ProjectSettings/RemoteContentPublish.asset", FilePathAttribute.Location.ProjectFolder)]
    public sealed class RemoteContentPublishProjectData : ScriptableSingleton<RemoteContentPublishProjectData>
    {
        public const string DataFieldName = "_data";

        [SerializeField] private RemoteContentPublishData _data = new RemoteContentPublishData();

        public RemoteContentPublishData Data { get { return _data; } }

        /// <summary>Writes the current settings to ProjectSettings as text so they can be reviewed in Git.</summary>
        public void SaveSettings()
        {
            Save(true);
        }
    }
}
