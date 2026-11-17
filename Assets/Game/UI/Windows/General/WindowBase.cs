using System;
using UnityEngine;

namespace Game.UI.Windows.General
{
    public abstract class WindowBase : MonoBehaviour
    {
        public abstract Type GetModelType();
        public abstract void Setup(IWindowModel model);
    }
}