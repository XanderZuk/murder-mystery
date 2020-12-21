using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Mirror;
using Scripts.Input;

namespace Scripts.Player
{
    public class Life : NetworkBehaviour
    {
        [SyncVar]
        public bool IsAlive;
        [SyncVar]
        public bool IsMurderer;
        [SyncVar]
        public bool IsDetective;

        [SerializeField] private GameObject playerBody = null;
        private PlayerGameManager playerGameManager; 
        private PlayerMovementController playerMovementController;
        public static event EventHandler<DeathEventArgs> OnDeath;
        

        public override void OnStartServer()
        {
            playerMovementController = gameObject.GetComponent<PlayerMovementController>();
            IsAlive = true;

        }

        [ServerCallback]
        private void OnDestroy()
        {
            OnDeath?.Invoke(this, new DeathEventArgs { ConnectionToClient = connectionToClient });
        }

        [Server]   
        public void KillPlayer()
        {
            IsAlive = false;
            OnDeath?.Invoke(this, new DeathEventArgs { ConnectionToClient = connectionToClient });
            RpcHandleDeath();
            
        }

        [ClientRpc]
        private void RpcHandleDeath()
        {
            GameObject playerInstance = Instantiate(playerBody, gameObject.transform.position, gameObject.transform.rotation);
            gameObject.SetActive(false);
        }
    }
}
   
    
