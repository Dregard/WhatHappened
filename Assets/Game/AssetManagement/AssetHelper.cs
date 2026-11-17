using System;
using UnityEngine;
using UnityEngine.UI;

namespace Game.AssetManagement
{
    public static class AssetHelper
    {
        public static Func<string, Sprite> GetSprite;

        public static void SetSprite(this Image image, string spriteId)
        {
            if (spriteId == null)
                return;
        
            image.sprite = GetSprite(spriteId);
        }
    }
}