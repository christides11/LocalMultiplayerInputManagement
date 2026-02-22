using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CT.LocalInputManagement
{
    public partial class InputManagerBase : MonoBehaviour
    {
        public enum ControlSchemeType
        {
            KEYBOARD_MOUSE,
            GAMEPAD
        }
        
        public List<InputPlayerManagerBase> playerInputManagers = new();
        public int autoAssignDevicesTo = 0;

        public static InputManagerBase instance;
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
        private static void OnExitPlayMode(PlayModeStateChange state)
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
            initialized = false;
            playerInputManagers = new(4);
            InitializeSystemPlayer();
            initialized = true;
            return true;
        }

        protected virtual void OnDestroy()
        {
            
        }
        
        public virtual void InitializeSystemPlayer()
        {
            GameObject go = new GameObject("System Player");
            go.transform.SetParent(transform, false);
            var ipm = go.AddComponent<InputPlayerManagerBase>();
            ipm.Initialize(0);

            playerInputManagers.Add(ipm);
        }
        
        public virtual void AddPlayer()
        {
            GameObject go = new GameObject($"Player {playerInputManagers.Count}");
            go.transform.SetParent(transform, false);
            var ipm = go.AddComponent<InputPlayerManagerBase>();
            
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

        public virtual InputPlayerManagerBase GetSystemPlayer()
        {
            return playerInputManagers[0];
        }
        
        public virtual InputPlayerManagerBase GetPlayer(int playerId)
        {
            if (playerId == 0 || playerId >= playerInputManagers.Count) return null;
            return playerInputManagers[playerId];
        }

        public virtual List<InputPlayerManagerBase> GetPlayers()
        {
            var l = new List<InputPlayerManagerBase>();
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
        }

        public virtual void ReturnPlayerDevicesToSystem(int player)
        {
            
        }
        
        public virtual void AssignAllDevicesToPlayer(int player)
        {
            ReturnAllDevicesToSystem();
            TransferAllDevicesFromSystemTo(player);
        }

        public virtual void TransferAllDevicesFromSystemTo(int player)
        {
            
        }
    }
}