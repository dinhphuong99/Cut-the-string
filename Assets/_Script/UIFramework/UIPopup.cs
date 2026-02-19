using UnityEngine;

namespace UIFramework
{
    public class UIPopup : UIScreen
    {
        protected override void Awake()
        {
            base.Awake();
            isModal = true;
            requiresExclusiveInput = true;
        }
    }
}
