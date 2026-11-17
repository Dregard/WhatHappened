using System.Collections.Generic;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Game.AssetManagement
{
    public class AssetModel
    {
        public string AssetId;
        public HashSet<string> ContentKeysSet;
        public AsyncOperationHandle LoadHandle;
        public bool IsScene;
    }
}
