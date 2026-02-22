using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.UI;
using UnityEngine.InputSystem.Users;

namespace CT.LocalInputManagement
{
    public partial class InputPlayerManagerUIM : InputPlayerManagerBase
    {
        public delegate void DelegateWhenControlSchemeChanged(InputPlayerManagerUIM inputPlayer,
            InputManagerBase.ControlSchemeType controlScheme);

        public DelegateWhenControlSchemeChanged onControlSchemeChanged;

        public InputUser User => playerInput.user;

        public PlayerInput playerInput = null;
        public InputActions inputActions;

        public HashSet<InputDevice> assignedDevices = new();
        public List<InputDevice> currentDevices = new List<InputDevice>();

        public MultiplayerEventSystem mpEventSystem = null;
        public InputSystemUIInputModule uiInputModule = null;

        public override void Initialize(int id)
        {
            base.Initialize(id);
            if (playerInput == null) playerInput = gameObject.AddComponent<PlayerInput>();
            inputActions?.Dispose();
            inputActions = new InputActions();
            playerInput.defaultActionMap = inputActions.UI.Get().name;
            playerInput.notificationBehavior = PlayerNotifications.InvokeCSharpEvents;
            playerInput.neverAutoSwitchControlSchemes = true;
            playerInput.actions = inputActions.asset;

            if (mpEventSystem == null) mpEventSystem = gameObject.AddComponent<MultiplayerEventSystem>();
            if (uiInputModule == null) uiInputModule = gameObject.AddComponent<InputSystemUIInputModule>();
            uiInputModule.actionsAsset = inputActions.asset;
            mpEventSystem.playerRoot = null;
            playerInput.uiInputModule = uiInputModule;

            InputUser.onChange += onInputDeviceChange;
            ++InputUser.listenForUnpairedDeviceActivity;
            InputUser.onUnpairedDeviceUsed += WhenUnpairedDeviceUsed;
        }

        public override void Reinitalize()
        {
            if (playerInput.user.valid) return;
            playerInput.actions = null;
            playerInput.actions = inputActions.asset;
        }

        public override void Teardown()
        {
            base.Teardown();
            playerInput.user.UnpairDevicesAndRemoveUser();
        }

        protected override void OnDestroy()
        {
            InputUser.onUnpairedDeviceUsed -= WhenUnpairedDeviceUsed;
            InputUser.onChange -= onInputDeviceChange;
            --InputUser.listenForUnpairedDeviceActivity;
        }

        public override void Vibrate(float vibrateTime)
        {
            foreach (var id in currentDevices)
            {
                if (id is not Gamepad gamepad) continue;
            }
        }

        public override void SetUIRoot(GameObject uiRoot)
        {
            mpEventSystem.playerRoot = uiRoot;
        }

        public virtual bool EventDataIsMine(BaseEventData eventData)
        {
            return eventData.currentInputModule == uiInputModule;
        }

        public override void RemoveAllDevices(bool updateDevices = true)
        {
            assignedDevices.Clear();
            if (updateDevices) UpdateDevices();
        }

        public override bool UpdateDevices()
        {
            inputActions.devices = assignedDevices.ToArray();
            for (int i = currentDevices.Count - 1; i >= 0; i--)
            {
                if (!assignedDevices.Contains(currentDevices[i])) currentDevices.RemoveAt(i);
            }

            Reinitalize();

            if (currentDevices.Count == 0 && assignedDevices.Count > 0)
                SwitchToDevice(assignedDevices.FirstOrDefault());
            else if (currentDevices.Count == 0)
                SwitchToDevice(null);
            return true;
        }

        public override void ActivateInput()
        {
            playerInput.ActivateInput();
        }

        public override void DeactivateInput()
        {
            playerInput.DeactivateInput();
        }
        
        public virtual void RemoveDevice(InputDevice inputDevice, bool updateDevices = true)
        {
            playerInput.user.UnpairDevice(inputDevice);
            assignedDevices.Remove(inputDevice);
            if(updateDevices) UpdateDevices();
        }

        public virtual void RemoveDevices(InputDevice[] inputDevices, bool updateDevices = true)
        {
            if (assignedDevices.Count == 0) return;
            
            foreach (var inputDevice in inputDevices)
            {
                if (inputDevice == Mouse.current || inputDevice == Keyboard.current)
                {
                    playerInput.user.UnpairDevice(Mouse.current);
                    playerInput.user.UnpairDevice(Keyboard.current);
                    assignedDevices.Remove(Mouse.current);
                    assignedDevices.Remove(Keyboard.current);
                    continue;
                }

                playerInput.user.UnpairDevice(inputDevice);
                assignedDevices.Remove(inputDevice);
            }

            if(updateDevices) UpdateDevices();
        }

        public virtual void AssignKeyboardAndMouse(bool updateDevices = true)
        {
            assignedDevices.Add(Keyboard.current);
            assignedDevices.Add(Mouse.current);
            if(updateDevices) UpdateDevices();
        }
        
        public virtual void AssignInputDevice(InputDevice inputDevice, bool updateDevices = true)
        {
            assignedDevices.Add(inputDevice);
            if (updateDevices) UpdateDevices();
        }

        public virtual void AssignInputDevices(InputDevice[] inputDeviceList, bool updateDevices = true)
        {
            foreach (var inputDevice in inputDeviceList)
            {
                assignedDevices.Add(inputDevice);
            }

            if (updateDevices) UpdateDevices();
        }

        public virtual void AssignInputDevices(Gamepad[] gamepadList, bool updateDevices = true)
        {
            foreach (var gamepad in gamepadList)
            {
                assignedDevices.Add(gamepad);
            }

            if (updateDevices) UpdateDevices();
        }
        
        public virtual void SwitchToDevice(InputDevice device)
        {
            if (device == null)
            {
                currentDevices.Clear();
                playerInput.SwitchCurrentControlScheme(Array.Empty<InputDevice>());
                onCurrentDeviceChanged?.Invoke();
                return;
            }

            if (!assignedDevices.Contains(device)) return;

            var dvs = device == Mouse.current || device == Keyboard.current
                ? new InputDevice[] { Keyboard.current, Mouse.current }
                : new InputDevice[] { device };

            try
            {
                playerInput.SwitchCurrentControlScheme(dvs);
            }
            catch (Exception e)
            {
                Debug.LogError("Exception throw while switching control scheme.", gameObject);
                Debug.LogException(e);
            }
            currentDevices = dvs.ToList();
            onCurrentDeviceChanged?.Invoke();
        }
        
        protected virtual void WhenUnpairedDeviceUsed(InputControl arg1, InputEventPtr arg2)
        {
            if (!autoSwitchControlSchemes || !assignedDevices.Contains(arg1.device)) return;
            if (playerInput.user.valid == false)
            {
                Debug.LogError("Player Input user isn't valid.", gameObject);
                return;
            }

            var dvs = arg1.device == Mouse.current || arg1.device == Keyboard.current
                ? new InputDevice[] { Keyboard.current, Mouse.current }
                : new InputDevice[] { arg1.device };

            playerInput.SwitchCurrentControlScheme(dvs);
            currentDevices = dvs.ToList();
            onCurrentDeviceChanged?.Invoke();
        }
        
        public virtual string GetBindingOverridesAsJson()
        {
            return inputActions.SaveBindingOverridesAsJson();
        }

        public virtual void ApplyBindingOverrides(string overrides)
        {
            inputActions.LoadBindingOverridesFromJson(overrides);
        }

        public virtual void ResetBindingOverrides()
        {
            inputActions.RemoveAllBindingOverrides();
        }

        protected virtual void onInputDeviceChange(InputUser user, InputUserChange change, InputDevice device)
        {
            
        }
    }
}