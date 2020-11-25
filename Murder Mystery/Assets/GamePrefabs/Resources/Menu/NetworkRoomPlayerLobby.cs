using Mirror;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Scripts.MainMenu
{
    public class NetworkRoomPlayerLobby : NetworkBehaviour
    {
        [SerializeField] private GameObject lobbyUI = null;
        [SerializeField] private GameObject[] playerSlots = new GameObject[12];
        [SerializeField] private TMP_Text[] playerNameTexts = new TMP_Text[12];
        [SerializeField] private TMP_Text[] playerReadyTexts = new TMP_Text[12];
        [SerializeField] private Button startGameButton = null;
        private NetworkManagerLobby room;

        //Sync Var variables can only be changed on the server
        [SyncVar(hook = nameof(HandleDisplayNameChanged))]
        public string DisplayName = "Loading...";
        [SyncVar(hook = nameof(HandleReadyStatusChanged))]
        public bool IsReady = false;
        private bool isLeader;

        private void Start()
        {
            LoadPlayerSlots();
        }
        public bool IsLeader
        {
            set
            {
                isLeader = value;
                startGameButton.gameObject.SetActive(value);
            }
        }
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

        private void LoadPlayerSlots()
        {
            for (int i = 0; i < Room.maxPlayers; i++)
            {
                playerSlots[i].SetActive(true);
            }
        }

        [Command]
        private void CmdSetDisplayName(string displayName)
        {
            DisplayName = displayName;
        }

        [Command]
        public void CmdReadyUp()
        {
            IsReady = !IsReady;
            Room.NotifyPlayersOfReadyState();
        }

        [Command]
        public void CmdStartGame()
        {
            if (Room.RoomPlayers[0].connectionToClient != connectionToClient)
            {
                return;
            }
            Room.StartGame();
        }


        private void UpdateDisplay()
        {
            if (!hasAuthority)
            {
                foreach (var player in Room.RoomPlayers)
                {
                    if (player.hasAuthority)
                    {
                        player.UpdateDisplay();
                        break;
                    }
                }
                return;
            }

            for (int i = 0; i < playerNameTexts.Length; i++)
            {
                playerNameTexts[i].text = "Waiting For Player...";
                playerReadyTexts[i].text = string.Empty;
            }

            for (int i = 0; i < Room.RoomPlayers.Count; i++)
            {
                playerNameTexts[i].text = Room.RoomPlayers[i].DisplayName;
                playerReadyTexts[i].text = Room.RoomPlayers[i].IsReady ?
                    "<color=green>Ready</color>" :
                    "<color=red>Not Ready</color>";

            }
        }

        public override void OnStartAuthority()
        {
            CmdSetDisplayName(PlayerNameInput.DisplayName);
            lobbyUI.SetActive(true);
        }

        public override void OnStartClient()
        {
            Room.RoomPlayers.Add(this);
            UpdateDisplay();
        }

        [System.Obsolete]
        public override void OnNetworkDestroy()
        {
            Room.RoomPlayers.Remove(this);
            UpdateDisplay();
        }

        public void HandleReadyToStart(bool readyToStart)
        {
            if (!isLeader)
            {
                return;
            }
            startGameButton.interactable = readyToStart;
        }

        public void HandleReadyStatusChanged(bool oldValue, bool newValue) => UpdateDisplay();
        public void HandleDisplayNameChanged(string oldValue, string newValue) => UpdateDisplay();
    }
}

