using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CT.LocalInputManagement
{
    public partial class InputManagerUIM : InputManagerBase
    {
        public override bool Initialize()
        {
            if (!base.Initialize()) return false;
            var systemPlayer = GetSystemPlayer() as InputPlayerManagerUIM;
            systemPlayer.ActivateInput();
            //systemPlayer.ActivateUIHandling();
            InputSystem.onDeviceChange += onInputDeviceChange;
            return true;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            InputSystem.onDeviceChange -= onInputDeviceChange;
        }

        public override void InitializeSystemPlayer()
        {
            GameObject go = new GameObject("System Player");
            go.transform.SetParent(transform, false);
            var ipm = go.AddComponent<InputPlayerManagerUIM>();
            ipm.Initialize(0);

            playerInputManagers.Add(ipm);
        }

        public override void AddPlayer()
        {
            GameObject go = new GameObject($"Player {playerInputManagers.Count}");
            go.transform.SetParent(transform, false);
            var ipm = go.AddComponent<InputPlayerManagerUIM>();
            
            playerInputManagers.Add(ipm);
            ipm.Initialize(playerInputManagers.Count-1);
        }

        public override void RemovePlayer(int player)
        {
            if (player == 0) return;
            playerInputManagers[player].Teardown();
            GameObject.Destroy(playerInputManagers[player].gameObject);
            playerInputManagers.RemoveAt(player);
            RefreshPlayerIDs();
        }

        public override void ReturnAllDevicesToSystem()
        {
            for (int i = 1; i < playerInputManagers.Count; i++)
            {
                playerInputManagers[i].RemoveAllDevices();
            }
            
            //playerInputManagers[0].AssignInputDevices(Gamepad.all.ToArray());
            //playerInputManagers[0].AssignKeyboardAndMouse();
        }

        public override void ReturnPlayerDevicesToSystem(int player)
        {
            if (player == 0) return;
            var playerManager = playerInputManagers[player] as InputPlayerManagerUIM;
            var systemPlayer = playerInputManagers[0] as InputPlayerManagerUIM;
            var dList = playerManager.assignedDevices.ToArray();
            playerManager.RemoveAllDevices();
            systemPlayer.AssignInputDevices(dList);
        }
        
        public void RemoveDeviceFromPlayers(InputDevice device)
        {
            /*
            for (int i = 1; i < playerInputManagers.Count; i++)
            {
                playerInputManagers[i].RemoveDevice(device);
            }
            playerInputManagers[0].AssignInputDevice(device);*/
        }
        
        public void AssignDevicesToPlayer(InputDevice[] devices, int player)
        {
            /*
            if (player == 0) return;
            playerInputManagers[0].RemoveDevices(devices);
            playerInputManagers[player].AssignInputDevices(devices);*/
        }

        public override void TransferAllDevicesFromSystemTo(int player)
        {
            /*
            if (player == 0) return;
            var aDevices = playerInputManagers[0].assignedDevices.ToArray();
            playerInputManagers[0].RemoveDevices(aDevices);
            playerInputManagers[player].AssignInputDevices(aDevices);*/
        }

        protected virtual void onInputDeviceChange(InputDevice device, InputDeviceChange change)
        {
            if (autoAssignDevicesTo >= playerInputManagers.Count) autoAssignDevicesTo = 0;
            
            switch (change)
            {
                case InputDeviceChange.Added:
                    Debug.Log($"Device added {device}. Assigning to {autoAssignDevicesTo}.", playerInputManagers[autoAssignDevicesTo]);
                    (playerInputManagers[0] as InputPlayerManagerUIM).RemoveDevice(device);
                    (playerInputManagers[autoAssignDevicesTo] as InputPlayerManagerUIM).AssignInputDevice(device);
                    break;
                case InputDeviceChange.Removed:
                    Debug.Log("Device removed: " + device);
                    (playerInputManagers[autoAssignDevicesTo] as InputPlayerManagerUIM).RemoveDevice(device);
                    RemoveDeviceFromPlayers(device);
                    break;
                case InputDeviceChange.ConfigurationChanged:
                    Debug.Log("Device configuration changed: " + device);
                    break;
            }
        }
        
        public virtual void SetPlayersBasedOnDeviceLists(List<List<InputDevice>> players)
        {
            if (players.Count == 0) return;
            ReturnAllDevicesToSystem();
            SetPlayerCount(players.Count);
            
            for (int i = 0; i < players.Count; i++)
            {
                AssignDevicesToPlayer(players[i].ToArray(), i+1);
            }
        }
    }
}
