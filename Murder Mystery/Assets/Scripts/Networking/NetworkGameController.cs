using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System;
using System.Linq;
using Scripts.Input;
using Scripts.Player;

namespace Scripts.Networking
{
    public class NetworkGameController : NetworkBehaviour
    {

        private NetworkManagerLobby room;
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

        #region Server

        public override void OnStartServer()
        {
            NetworkManagerLobby.OnServerStopped += CleanUpServer;
            NetworkManagerLobby.OnServerReadied += CheckToStartRound;

            Life.OnDeath += HandleDeath;
        }

        [Server]
        private void TestForWinner()
        {
            if (Room.Murderers.Count > (Room.LivingPlayers.Count - Room.Murderers.Count))
            {
                  
            }
        }

        [Server]
        private void HandleDeath(object sender, DeathEventArgs e)
        {
            Debug.Log("OnDeathTriggered");
            if (Room.LivingPlayers.Count == 1)
            {
                return;
            }
            foreach (var player in Room.LivingPlayers)
            {
                if (player == null || player.connectionToClient == e.ConnectionToClient)
                {
                    Room.LivingPlayers.Remove(player);
                    break;
                }
            }
            foreach (var player in Room.LivingPlayers)
            {
                Debug.Log(player.DisplayName);
            }
            TestForWinner();

        }

        [ServerCallback]
        private void OnDestroy() => CleanUpServer();

        [Server]
        private void CleanUpServer()
        {
            NetworkManagerLobby.OnServerStopped -= CleanUpServer;
            NetworkManagerLobby.OnServerReadied -= CheckToStartRound;
        }


        IEnumerator WaitForAnimation()
        {
            yield return new WaitForSeconds(4);
            RpcStartRound();
        }

        

        [Server]
        private void CheckToStartRound(NetworkConnection conn)
        {
            if (Room.GamePlayers.Count(x => x.connectionToClient.isReady) != Room.GamePlayers.Count)
            {
                return;
            }
            Room.ChooseRoles();
            
            foreach (var player in Room.LivingPlayers)
            {
                player.playerGameManager.rolePopup.StartAnimation();
            }
            StartCoroutine("WaitForAnimation");
            
        }
        #endregion

        #region Client

        [ClientRpc]
        private void RpcStartRound()
        {
            Debug.Log("Start");
            InputManager.Remove(ActionMapNames.Player);
        }

        #endregion
    }
}

