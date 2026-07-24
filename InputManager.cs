using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace CT.LocalInputManagement
{
    public partial class InputManager : MonoBehaviour
    {
        public static readonly int SystemPlayerID = 0;
        
        public UnityEvent<InputPlayerManager> onPlayerAdded, onPlayerRemoved = new();
        public UnityEvent onPlayersChanged = new();
        
        public List<InputPlayerManager> playerInputManagers = new();
        public int autoAssignDevicesTo = 0;

        public static InputManager instance;
        public static bool initialized = false;
        private static int idCounter = 1;

        public bool initializeOnAwake = true;
        public bool createStaticInstance = true;

        [Header("Debugging")]
        public bool debug;
        
        public virtual void Awake()
        {
            if(initializeOnAwake) Initialize();
        }
        
        [OnExitingPlayMode]
        public static void OnExitPlayMode()
        {
            instance = null;
            initialized = false;
            idCounter = 1;
        }

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
            ipm.Initialize(SystemPlayerID);

            playerInputManagers.Add(ipm);
            onPlayerAdded?.Invoke(ipm);
        }
        
        public virtual InputPlayerManager AddPlayer(bool callChangedEvent = true)
        {
            GameObject go = new GameObject($"Player {playerInputManagers.Count}");
            go.transform.SetParent(transform, false);
            var ipm = go.AddComponent<InputPlayerManager>();
            
            playerInputManagers.Add(ipm);
            ipm.Initialize(idCounter);
            idCounter++;
            
            onPlayerAdded?.Invoke(ipm);
            if(callChangedEvent) onPlayersChanged?.Invoke();
            return ipm;
        }

        public virtual void RemovePlayer(int player, bool callChangedEvent = true)
        {
            if (player == 0) return;
            var playerToRemove = playerInputManagers[player];
            playerToRemove.Teardown();
            playerInputManagers.Remove(playerToRemove);
            
            onPlayerRemoved?.Invoke(playerToRemove);
            GameObject.Destroy(playerToRemove.gameObject);
            
            if(callChangedEvent) onPlayersChanged?.Invoke();
        }

        public virtual void SetPlayerCount(int count)
        {
            count += 1;
            while (playerInputManagers.Count < count)
            {
                AddPlayer(callChangedEvent: false);
            }

            while (playerInputManagers.Count > count)
            {
                RemovePlayer(playerInputManagers.Count-1, callChangedEvent: false);
            }
            onPlayersChanged?.Invoke();
        }

        public virtual int GetPlayerCount()
        {
            return playerInputManagers.Count - 1;
        }

        public virtual InputPlayerManager GetSystemPlayer()
        {
            return playerInputManagers[0];
        }
        
        public virtual InputPlayerManager GetPlayer(int playerId)
        {
            if (playerId < 0 || playerId >= playerInputManagers.Count) return null;
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

            var inputPlayer = playerInputManagers[0];
            inputPlayer.AssignInputDevices(Gamepad.all.ToArray());
            inputPlayer.AssignKeyboardAndMouse();
        }

        public virtual void ReturnPlayerDevicesToSystem(int player)
        {
            if (player == 0) return;
            var playerManager = playerInputManagers[player];
            var systemPlayer = playerInputManagers[0];
            var dList = playerManager.assignedDevices.ToArray();
            playerManager.RemoveAllDevices();
            systemPlayer.AssignInputDevices(dList);
        }
        
        public void RemoveDeviceFromPlayers(InputDevice device)
        {
            for (int i = 1; i < playerInputManagers.Count; i++)
            {
                playerInputManagers[i].RemoveDevice(device);
            }
            playerInputManagers[0].AssignInputDevice(device);
        }
        
        public void AssignDevicesToPlayer(InputDevice[] devices, int player)
        {
            if (player == 0) return;
            playerInputManagers[0].RemoveDevices(devices);
            playerInputManagers[player].AssignInputDevices(devices);
        }
        
        public virtual void AssignAllDevicesToPlayer(int player)
        {
            ReturnAllDevicesToSystem();
            TransferAllDevicesFromSystemTo(player);
        }

        public virtual void TransferAllDevicesFromSystemTo(int player)
        {
            if (player == 0) return;
            var aDevices = playerInputManagers[0].assignedDevices.ToArray();
            playerInputManagers[0].RemoveDevices(aDevices);
            playerInputManagers[player].AssignInputDevices(aDevices);
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

        public virtual InputPlayerManager GetPlayerWithDevice(InputDevice device)
        {
            for (int i = 0; i < playerInputManagers.Count; i++)
            {
                var pim = playerInputManagers[i];
                if (pim.DeviceIsAssigned(device)) return pim;
            }
            return null;
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
                    if(debug) Debug.Log($"Device added {device}");
                    var devicePlayer = IsDeviceAssignedToAnyPlayer(device);
                    if (devicePlayer == -1)
                    {
                        if(debug)
                            Debug.Log($"{device}: Assigning to Player Index {autoAssignDevicesTo}.",
                            playerInputManagers[autoAssignDevicesTo]);
                        (playerInputManagers[autoAssignDevicesTo]).AssignInputDevice(device);
                    }
                    else
                    {
                        if(debug)
                            Debug.Log($"{device}: Already assigned to Player Index {devicePlayer}.");
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