using Mirror;
using Scripts.Player;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Scripts.Networking
{
    public class NetworkGamePlayerLobby : NetworkBehaviour
    {

        //Sync Var variables can only be changed on the server
        [SyncVar]
        public string DisplayName = "Loading...";
        [SyncVar]
        public bool IsMurderer;
        [SyncVar]
        public bool IsDetective;
        private NetworkManagerLobby room;
        public PlayerGameManager playerGameManager;
        
        private NetworkManagerLobby Room
        {
            get
            {
                if (room != null)
                {
                    return room;
                }
                return room = NetworkManager.singleton as NetworkManagerLobby;
            }
        }

        public override void OnStartClient()
        {
            DontDestroyOnLoad(gameObject);
            Room.GamePlayers.Add(this);
        }

        [System.Obsolete]
        public override void OnNetworkDestroy()
        {
            Room.GamePlayers.Remove(this);
        }

        [Server]
        public void SetDisplayName(string displayName)
        {
            this.DisplayName = displayName;
        }




    }
}

