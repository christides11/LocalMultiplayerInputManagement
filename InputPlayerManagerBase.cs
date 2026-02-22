using UnityEngine;

namespace CT.LocalInputManagement
{
    public partial class InputPlayerManagerBase : MonoBehaviour
    {
        public enum NavigationType
        {
            Controller_Or_Keyboard,
            Mouse
        }

        public NavigationType lastNavigationType = NavigationType.Controller_Or_Keyboard;

        public delegate void DelegateNavigationStyleChange(InputPlayerManagerBase inputPlayer,
            NavigationType navigationType);
        public DelegateNavigationStyleChange onNavigationStyleChanged;

        public delegate void DelegateDeviceChanged();

        public DelegateDeviceChanged onCurrentDeviceChanged;
        
        public int Id { get; protected set; } = 0;

        public bool autoSwitchControlSchemes = true;
        public int navigationStyleUpdateRate = 10;
        
        public virtual void Initialize(int id)
        {
            Id = id;
        }

        public virtual void Reinitalize()
        {
            
        }

        public virtual void Teardown()
        {
            DeactivateInput();
        }

        protected virtual void OnDestroy()
        {
            
        }

        public virtual void Vibrate(float vibrateTime)
        {
            
        }
        
        public virtual void SetUIRoot(GameObject uiRoot)
        {
            
        }
        
        public virtual void RemoveAllDevices(bool updateDevices = true)
        {
            
        }
        
        public virtual bool UpdateDevices()
        {
            return false;
        }

        public virtual void ActivateInput()
        {
            
        }

        public virtual void DeactivateInput()
        {
            
        }
        
        public virtual void SetID(int id)
        {
            Id = id;
        }
    }
}