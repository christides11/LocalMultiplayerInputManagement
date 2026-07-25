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
        
        protected List<InputPlayerManager> playerInputManagers = new();
        public int autoAssignDevicesTo = 0;

        public static InputManager instance;
        public static bool initialized = false;
        protected static int idCounter = 1;

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

        public virtual void RemovePlayer(int playerId, bool callChangedEvent = true)
        {
            if(playerId == SystemPlayerID) return;
            var playerToRemove = GetPlayer(playerId);
            if (playerToRemove == null) return;
            
            playerToRemove.Teardown();
            playerInputManagers.Remove(playerToRemove);

            if (autoAssignDevicesTo == playerToRemove.Id)
                autoAssignDevicesTo = SystemPlayerID;
            
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
                RemovePlayer(playerInputManagers[^1].Id, callChangedEvent: false);
            }
            onPlayersChanged?.Invoke();
        }

        public virtual int GetPlayerCount()
        {
            return playerInputManagers.Count - 1;
        }

        public virtual InputPlayerManager GetSystemPlayer()
        {
            return GetPlayer(SystemPlayerID);
        }
        
        public virtual InputPlayerManager GetPlayer(int playerId)
        {
            foreach (var pm in playerInputManagers)
            {
                if(pm.Id != playerId)
                    continue;
                return pm;
            }
            return null;
        }

        public virtual List<InputPlayerManager> GetPlayers(bool includeSystemPlayer = false)
        {
            var l = new List<InputPlayerManager>();
            for (int i = 0; i < playerInputManagers.Count; i++)
            {
                if(playerInputManagers[i].Id == SystemPlayerID && !includeSystemPlayer)
                    continue;
                
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

            var systemPlayer = GetSystemPlayer();
            systemPlayer.AssignInputDevices(Gamepad.all.ToArray());
            systemPlayer.AssignKeyboardAndMouse();
        }

        public virtual void ReturnPlayerDevicesToSystem(int playerId)
        {
            if (playerId == SystemPlayerID) return;
            var playerManager = GetPlayer(playerId);
            var systemPlayer = GetSystemPlayer();
            
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
            GetSystemPlayer().AssignInputDevice(device);
        }
        
        public void AssignDevicesToPlayer(InputDevice[] devices, int playerId)
        {
            if (playerId == 0) return;
            GetSystemPlayer().RemoveDevices(devices);
            GetPlayer(playerId).AssignInputDevices(devices);
        }
        
        public virtual void AssignAllDevicesToPlayer(int playerId)
        {
            ReturnAllDevicesToSystem();
            TransferAllDevicesFromSystemTo(playerId);
        }

        public virtual void TransferAllDevicesFromSystemTo(int playerId)
        {
            if (playerId == SystemPlayerID) return;
            var systemPlayer = GetSystemPlayer();
            var inputPlayer = GetPlayer(playerId);
            
            var aDevices = systemPlayer.assignedDevices.ToArray();
            systemPlayer.RemoveDevices(aDevices);
            inputPlayer.AssignInputDevices(aDevices);
        }
        
        public virtual int IsDeviceAssignedToAnyPlayer(InputDevice device)
        {
            for (int i = 0; i < playerInputManagers.Count; i++)
            {
                var pim = playerInputManagers[i];
                if (pim.DeviceIsAssigned(device)) return pim.Id;
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
        
        public virtual bool TryGetPlayerWithDevice(InputDevice device, out InputPlayerManager inputPlayer)
        {
            for (int i = 0; i < playerInputManagers.Count; i++)
            {
                var pim = playerInputManagers[i];
                if (pim.DeviceIsAssigned(device))
                {
                    inputPlayer = pim;
                    return true;
                }
            }
            inputPlayer = null;
            return false;
        }

        public void SetAutoAssignDevicesPlayer(int playerId)
        {
            autoAssignDevicesTo = playerId;
        }
        
        protected virtual void onInputDeviceChange(InputDevice device, InputDeviceChange change)
        {
            var defaultInputPlayer = GetPlayer(autoAssignDevicesTo);
            if (defaultInputPlayer == null && autoAssignDevicesTo != SystemPlayerID)
            {
                defaultInputPlayer = GetSystemPlayer();
            }

            if (defaultInputPlayer == null)
            {
                Debug.LogError("Could not get default assigning player for auto assigning devices.");
                return;
            }
            
            switch (change)
            {
                case InputDeviceChange.Added:
                    if(debug) Debug.Log($"Device added {device}");
                    
                    if (TryGetPlayerWithDevice(device, out var currentInputPlayer))
                    {
                        if(debug)
                            Debug.Log($"{device}: Already assigned to Player Index {currentInputPlayer.Id}.");
                    }
                    else
                    {
                        if(debug)
                            Debug.Log($"{device}: Assigning to Player Index {autoAssignDevicesTo}.",
                                defaultInputPlayer);
                        defaultInputPlayer.AssignInputDevice(device);
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
                AssignDevicesToPlayer(players[i].ToArray(), playerInputManagers[i+1].Id);
            }
        }
    }
}