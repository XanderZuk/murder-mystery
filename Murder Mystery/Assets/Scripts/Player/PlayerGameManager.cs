using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Mirror;
using Scripts.Networking;

namespace Scripts.Player
{
    public class PlayerGameManager : NetworkBehaviour
    {
        public static event Action<PlayerGameManager> OnPlayerSpawned;
        public static event Action<PlayerGameManager> OnPlayerDespawned;

        [SerializeField] public RolePopup rolePopup = null;

        [SyncVar(hook = nameof(HandleOwnerSet))]
        private uint ownerId;
        public uint OwnerId => ownerId;
        public NetworkGamePlayerLobby gamePlayerLobby;
        public Life life;

        private void OnDestroy()
        {
            OnPlayerDespawned?.Invoke(this);
        }
        
        private void HandleOwnerSet(uint oldValue, uint newValue)
        {
            OnPlayerSpawned?.Invoke(this);
        }

    }
}

