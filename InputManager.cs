using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CT.LocalInputManagement
{
    public partial class InputManager : MonoBehaviour
    {
        public enum ControlSchemeType
        {
            KEYBOARD_MOUSE,
            GAMEPAD
        }
        
        public List<InputPlayerManager> playerInputManagers = new();
        public int autoAssignDevicesTo = 0;

        public static InputManager instance;
        public static bool initialized = false;

        public bool initializeOnAwake = true;
        public bool createStaticInstance = true;

        public virtual void Awake()
        {
#if UNITY_EDITOR
            EditorApplication.playModeStateChanged += OnExitPlayMode;
#endif
            
            if(initializeOnAwake) Initialize();
        }
        
#if UNITY_EDITOR
        public static void OnExitPlayMode(PlayModeStateChange state)
        {
            if(state == PlayModeStateChange.ExitingPlayMode)
            {
                EditorApplication.playModeStateChanged -= OnExitPlayMode;
                instance = null;
                initialized = false;
            }
        }
#endif

        public virtual bool Initialize()
        {
            if (createStaticInstance)
            {
                if (instance != null)
                {
                    GameObject.Destroy(gameObject);
                    return false;
                }
                instance = this;
            }
            playerInputManagers = new(4);
            InitializeSystemPlayer();
            initialized = true;
            var systemPlayer = GetSystemPlayer();
            systemPlayer.ActivateInput();
            ReturnAllDevicesToSystem();
            InputSystem.onDeviceChange += onInputDeviceChange;
            return true;
        }

        protected virtual void OnDestroy()
        {
            InputSystem.onDeviceChange -= onInputDeviceChange;
        }
        
        public virtual void InitializeSystemPlayer()
        {
            GameObject go = new GameObject("System Player");
            go.transform.SetParent(transform, false);
            var ipm = go.AddComponent<InputPlayerManager>();
            ipm.Initialize(0);

            playerInputManagers.Add(ipm);
        }
        
        public virtual void AddPlayer()
        {
            GameObject go = new GameObject($"Player {playerInputManagers.Count}");
            go.transform.SetParent(transform, false);
            var ipm = go.AddComponent<InputPlayerManager>();
            
            playerInputManagers.Add(ipm);
            ipm.Initialize(playerInputManagers.Count-1);
        }

        public virtual void RemovePlayer(int player)
        {
            if (player == 0) return;
            playerInputManagers[player].Teardown();
            GameObject.Destroy(playerInputManagers[player].gameObject);
            playerInputManagers.RemoveAt(player);
            RefreshPlayerIDs();
        }

        public virtual void SetPlayerCount(int count)
        {
            count += 1;
            while (playerInputManagers.Count < count)
            {
                AddPlayer();
            }

            while (playerInputManagers.Count > count)
            {
                RemovePlayer(playerInputManagers.Count-1);
            }
        }

        public virtual int GetPlayerCount()
        {
            return playerInputManagers.Count - 1;
        }
        
        protected virtual void RefreshPlayerIDs()
        {
            for (int i = 0; i < playerInputManagers.Count; i++)
            {
                if (i == 0) continue;
                playerInputManagers[i].SetID(i);
            }
        }

        public virtual InputPlayerManager GetSystemPlayer()
        {
            return playerInputManagers[0];
        }
        
        public virtual InputPlayerManager GetPlayer(int playerId)
        {
            if (playerId == 0 || playerId >= playerInputManagers.Count) return null;
            return playerInputManagers[playerId];
        }

        public virtual List<InputPlayerManager> GetPlayers()
        {
            var l = new List<InputPlayerManager>();
            for (int i = 1; i < playerInputManagers.Count; i++)
            {
                l.Add(playerInputManagers[i]);
            }
            return l;
        }
        
        public virtual void ReturnAllDevicesToSystem()
        {
            for (int i = 1; i < playerInputManagers.Count; i++)
            {
                playerInputManagers[i].RemoveAllDevices();
            }

            var inputPlayer = playerInputManagers[0] as InputPlayerManager;
            inputPlayer.AssignInputDevices(Gamepad.all.ToArray());
            inputPlayer.AssignKeyboardAndMouse();
        }

        public virtual void ReturnPlayerDevicesToSystem(int player)
        {
            if (player == 0) return;
            var playerManager = playerInputManagers[player] as InputPlayerManager;
            var systemPlayer = playerInputManagers[0] as InputPlayerManager;
            var dList = playerManager.assignedDevices.ToArray();
            playerManager.RemoveAllDevices();
            systemPlayer.AssignInputDevices(dList);
        }
        
        public void RemoveDeviceFromPlayers(InputDevice device)
        {
            for (int i = 1; i < playerInputManagers.Count; i++)
            {
                (playerInputManagers[i] as InputPlayerManager).RemoveDevice(device);
            }
            (playerInputManagers[0] as InputPlayerManager).AssignInputDevice(device);
        }
        
        public void AssignDevicesToPlayer(InputDevice[] devices, int player)
        {
            if (player == 0) return;
            (playerInputManagers[0] as InputPlayerManager).RemoveDevices(devices);
            (playerInputManagers[player] as InputPlayerManager).AssignInputDevices(devices);
        }
        
        public virtual void AssignAllDevicesToPlayer(int player)
        {
            ReturnAllDevicesToSystem();
            TransferAllDevicesFromSystemTo(player);
        }

        public virtual void TransferAllDevicesFromSystemTo(int player)
        {
            if (player == 0) return;
            var aDevices = (playerInputManagers[0] as InputPlayerManager).assignedDevices.ToArray();
            (playerInputManagers[0] as InputPlayerManager).RemoveDevices(aDevices);
            (playerInputManagers[player] as InputPlayerManager).AssignInputDevices(aDevices);
        }
        
        public virtual int IsDeviceAssignedToAnyPlayer(InputDevice device)
        {
            for (int i = 0; i < playerInputManagers.Count; i++)
            {
                var pim = playerInputManagers[i];
                if (pim.DeviceIsAssigned(device)) return i;
            }
            return -1;
        }

        public void SetAutoAssignDevicesPlayer(int playerIndex)
        {
            autoAssignDevicesTo = playerIndex;
        }
        
        protected virtual void onInputDeviceChange(InputDevice device, InputDeviceChange change)
        {
            if (autoAssignDevicesTo >= playerInputManagers.Count) autoAssignDevicesTo = 0;
            
            switch (change)
            {
                case InputDeviceChange.Added:
                    var devicePlayer = IsDeviceAssignedToAnyPlayer(device);
                    if (devicePlayer == -1)
                    {
                        Debug.Log($"Device added {device}. Assigning to Player Index {autoAssignDevicesTo}.",
                            playerInputManagers[autoAssignDevicesTo]);
                        (playerInputManagers[autoAssignDevicesTo]).AssignInputDevice(device);
                    }
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