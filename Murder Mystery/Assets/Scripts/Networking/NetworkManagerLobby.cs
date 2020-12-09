using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System;
using TMPro;
using System.Linq;
using System.IO;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Scripts.Map;

namespace Scripts.Networking
{
    public class NetworkManagerLobby : NetworkManager
    {
        [SerializeField] private int minPlayers = 2;
        [SerializeField] public int MaxNumOfPlayers { get; set; } = 2;
        [SerializeField] public int NumOfMurderers { get; set; } = 1;
        [SerializeField] public int NumOfDetectives { get; set; } = 0;

        [Scene] [SerializeField] private string menuScene = string.Empty;

        [Header("Maps")]
        [SerializeField] private int numberOfRounds = 1;
        [SerializeField] private MapSet mapSet = null;

        [Header("Room")]
        [SerializeField] private NetworkRoomPlayerLobby roomPlayerPrefab = null;
        [SerializeField] private Dropdown playerDropdown = null;
        [SerializeField] private Dropdown murdererDropdown = null;
        [SerializeField] private Dropdown detectiveDropdown = null;
        [SerializeField] private Button startButton = null;

        [Header("Game")]
        [SerializeField] private NetworkGamePlayerLobby gamePlayerPrefab = null;
        [SerializeField] private GameObject playerSpawnSystem = null;
        [SerializeField] private GameObject roundSystem = null;

        public List<NetworkRoomPlayerLobby> RoomPlayers { get; } = new List<NetworkRoomPlayerLobby>();
        public List<NetworkGamePlayerLobby> GamePlayers { get; } = new List<NetworkGamePlayerLobby>();
        public List<NetworkGamePlayerLobby> Murderers { get; } = new List<NetworkGamePlayerLobby>();
        public List<NetworkGamePlayerLobby> Detectives { get; } = new List<NetworkGamePlayerLobby>();
        public List<NetworkGamePlayerLobby> Innocents { get; } = new List<NetworkGamePlayerLobby>();
        public List<NetworkGamePlayerLobby> DeadPlayers { get; } = new List<NetworkGamePlayerLobby>();

        private MapHandler mapHandler = null;

        public static event Action OnClientConnected;
        public static event Action OnClientDisconnected;
        public static event Action<NetworkConnection> OnServerReadied;
        public static event Action OnServerStopped;

        public override void OnStartServer() => spawnPrefabs = Resources.LoadAll<GameObject>("SpawnablePrefabs").ToList();

        // Iterate over all items in folder to register them
        public override void OnStartClient()
        {
            var spawnablePrefabs = Resources.LoadAll<GameObject>("SpawnablePrefabs");

            foreach (var prefab in spawnablePrefabs)
            {
                ClientScene.RegisterPrefab(prefab);
            }
        }

        // Input validation for the host game button
        public void ActivateStartButton()
        {
            if (NumOfMurderers + NumOfDetectives >= MaxNumOfPlayers)
            {
                startButton.interactable = false;
                return;
            }
            startButton.interactable = true;
        }

        // Takes players from game player list and assigns them roles
        public void ChooseRoles()
        {
            
            foreach (var player in GamePlayers)
            {
                Innocents.Add(player);
            }

            // Recalculate roles if there are not enough players to fill them
            if ((NumOfDetectives + NumOfMurderers) >= Innocents.Count)
            {
                float murdererRatio = Innocents.Count / 4;
                if (murdererRatio <= 1.75)
                {
                    murdererRatio = 1;
                }
                else if ((murdererRatio >= 2) && (murdererRatio <= 2.5))
                {
                    murdererRatio = 2;
                }
                else
                {
                    murdererRatio = 3;
                }
               
                for (int i = 0; i < murdererRatio; i++)
                {
                    var random = new System.Random();
                    int randomIndex = random.Next(Innocents.Count);

                    Murderers.Add(Innocents[randomIndex]);
                    Innocents.Remove(Innocents[randomIndex]);
                }

                for (int i = 0; i < 1; i++)
                {
                    var random = new System.Random();
                    int randomIndex = random.Next(Innocents.Count);

                    Detectives.Add(Innocents[randomIndex]);
                    Innocents.Remove(Innocents[randomIndex]);
                }
                return;
            }

            for (int i = 0; i < NumOfMurderers; i++)
            {
                var random = new System.Random();
                int randomIndex = random.Next(Innocents.Count);

                Murderers.Add(Innocents[randomIndex]);
                Innocents.Remove(Innocents[randomIndex]);
            }
            for (int i = 0; i < NumOfDetectives; i++)
            {
                var random = new System.Random();
                int randomIndex = random.Next(Innocents.Count);

                Detectives.Add(Innocents[randomIndex]);
                Innocents.Remove(Innocents[randomIndex]);
            }
            
        }

        public override void OnClientConnect(NetworkConnection conn)
        {
            base.OnClientConnect(conn);
            OnClientConnected?.Invoke();
        }

        public override void OnClientDisconnect(NetworkConnection conn)
        {
            base.OnClientDisconnect(conn);
            OnClientDisconnected?.Invoke();
        }
        public override void OnServerConnect(NetworkConnection conn)
        {
            // Disconnect player if server is full
            if (numPlayers >= maxConnections)
            {
                conn.Disconnect();
                return;
            }
            // Stops players from joining if game is already in progress
            if (SceneManager.GetActiveScene().path != menuScene)
            {
                conn.Disconnect();
                return;
            }
        }
        // Instantiates player object and links it with its connection
        public override void OnServerAddPlayer(NetworkConnection conn)
        {
            if (SceneManager.GetActiveScene().path == menuScene)
            {
                bool isLeader = RoomPlayers.Count == 0;
                NetworkRoomPlayerLobby roomPlayerInstance = Instantiate(roomPlayerPrefab);
                roomPlayerInstance.IsLeader = isLeader;
                NetworkServer.AddPlayerForConnection(conn, roomPlayerInstance.gameObject);
            }
        }

        public override void OnServerDisconnect(NetworkConnection conn)
        {
            if (conn.identity != null)
            {
                var player = conn.identity.GetComponent<NetworkRoomPlayerLobby>();
                RoomPlayers.Remove(player);
                NotifyPlayersOfReadyState();
            }
            base.OnServerDisconnect(conn);
        }

        public void NotifyPlayersOfReadyState()
        {
            foreach (var player in RoomPlayers)
            {
                player.HandleReadyToStart(IsReadyToStart());
            }
        }

        public override void ServerChangeScene(string newSceneName)
        {
            if (SceneManager.GetActiveScene().path == menuScene && Path.GetFileName(newSceneName).StartsWith("Map_"))
            {
                for (int i = RoomPlayers.Count - 1; i >= 0; i--)
                {
                    var conn = RoomPlayers[i].connectionToClient;
                    var gameplayerInstance = Instantiate(gamePlayerPrefab);
                    gameplayerInstance.SetDisplayName(RoomPlayers[i].DisplayName);

                    NetworkServer.Destroy(conn.identity.gameObject);
                    NetworkServer.ReplacePlayerForConnection(conn, gameplayerInstance.gameObject);
                }

            }
            base.ServerChangeScene(newSceneName);
        }

        private bool IsReadyToStart()
        {
            if (numPlayers < minPlayers)
            {
                return false;
            }

            foreach (var player in RoomPlayers)
            {
                if (!player.IsReady)
                {
                    return false;
                }
            }
            return true;
        }

        public override void OnStopServer()
        {
            OnServerStopped?.Invoke();
            RoomPlayers.Clear();
            GamePlayers.Clear();
        }

        public void StartGame()
        {
            
            if (SceneManager.GetActiveScene().path == menuScene)
            {
                if (!IsReadyToStart())
                {
                    return;
                }

                mapHandler = new MapHandler(mapSet, numberOfRounds);
                ServerChangeScene(mapHandler.NextMap());
                Debug.Log("Changed Scene");

            }
        }

        public override void OnServerReady(NetworkConnection conn)
        {
            base.OnServerReady(conn);

            OnServerReadied?.Invoke(conn);
        }

        public override void OnServerSceneChanged(string sceneName)
        {
            if (Path.GetFileName(sceneName).StartsWith("Map_"));
            {
                GameObject playerSpawnSystemInstance = Instantiate(playerSpawnSystem);
                NetworkServer.Spawn(playerSpawnSystemInstance);

                GameObject roundSystemInstance = Instantiate(roundSystem);
                NetworkServer.Spawn(roundSystemInstance);
            }
        }
        // Saving values from dropdowns to public variables
        public void SaveMaxPlayers()
        {
            MaxNumOfPlayers = playerDropdown.value + 2;
            Debug.Log($"max players = {MaxNumOfPlayers}");
        }

        public void SaveMurderers()
        {
            NumOfMurderers = murdererDropdown.value + 1;
            Debug.Log($"murderers = {NumOfMurderers}");
        }

        public void SaveDetectives()
        {
            NumOfDetectives = detectiveDropdown.value;
            Debug.Log($"detectives = {NumOfDetectives}");
        }

    }
}
