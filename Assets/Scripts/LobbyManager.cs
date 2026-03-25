using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine.SceneManagement;


public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance { get; private set; }
    public Lobby currentLobby { get; private set; }

    private float heartbeattimer = 0f;
    private const float heartbeatInterval = 15f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        HandleLobbyHeartBeat();
    }

    private async void HandleLobbyHeartBeat()
    {
        if (currentLobby == null) return;
        if (currentLobby.HostId != AuthenticationService.Instance.PlayerId) return;

        heartbeattimer -= Time.deltaTime;
        if (heartbeattimer > 0f) return;

        heartbeattimer = heartbeatInterval;

        try
        {
            await LobbyService.Instance.SendHeartbeatPingAsync(currentLobby.Id);
            Debug.Log("lobby heartbeat sent.");
        }

        catch (LobbyServiceException e)
        {
            Debug.LogWarning("heart beat failed: " + e.Message);
        }
    }

    public async Task CreateLobbyAsync(string lobbyname, int maxPlayers, bool isPrivate, TMP_Text statusText)
    {
        try
        {
            var playerData = new Dictionary<string, PlayerDataObject>
            {
                {
                "PlayerName",
                new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, "player_" + AuthenticationService.Instance.PlayerId[..6])
                },
                {
                    "Ready",
                    new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, "0")
                }
            };

            var options = new CreateLobbyOptions
            {
                IsPrivate = isPrivate,
                Player = new Player(
                    id: AuthenticationService.Instance.PlayerId,
                    data: playerData
                ),
                Data = new Dictionary<string, DataObject>
                {
                    {
                        "GameMode",
                        new DataObject(DataObject.VisibilityOptions.Public, "CardBattle")
                    }
                }
            };

            currentLobby = 
                await LobbyService.Instance.CreateLobbyAsync(lobbyname, maxPlayers, options);

            Debug.Log($"Lobby created: {currentLobby.Name} | Code: {currentLobby.LobbyCode}");
            statusText.text = $"Lobby created!\nName: {currentLobby.Name}\nCode: {currentLobby.LobbyCode}";

            SceneManager.LoadScene("Lobby");
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError("Create lobby failed: " + e.Message);
            statusText.text = "Create lobby failed: " + e.Message;
        }
    }

    public async Task ListLobbiesAsync(TMP_Text lobbyListText, TMP_Text statusText)
    {
        try
        {
            var response = await LobbyService.Instance.QueryLobbiesAsync();

            if (response.Results.Count == 0)
            {
                lobbyListText.text = "No lobbies found.";
                statusText.text = "No lobbies available.";
                return;
            }

            string result = "";

            foreach (var lobby in response.Results)
            {
                result += $"Name: {lobby.Name} | Players: {lobby.Players.Count}/{lobby.MaxPlayers} | Code: {lobby.LobbyCode}\n";
            }

            lobbyListText.text = result;
            statusText.text = "Lobbies updated.";
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError("List lobbies failed: " + e.Message);
            statusText.text = "List failed: " + e.Message;
        }
    }

    public async Task JoinLobbyByCodeAsync(string code, TMP_Text statusText)
    {
        try
        {
            var playerData = new Dictionary<string, PlayerDataObject>
        {
            {
                "PlayerName",
                new PlayerDataObject(
                    PlayerDataObject.VisibilityOptions.Public,
                    "player_" + AuthenticationService.Instance.PlayerId[..6]
                )
            },
            {
                "Ready",
                new PlayerDataObject(
                    PlayerDataObject.VisibilityOptions.Member,
                    "0"
                )
            }
        };

            var options = new JoinLobbyByCodeOptions
            {
                Player = new Player(
                    id: AuthenticationService.Instance.PlayerId,
                    data: playerData
                )
            };

            currentLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(code, options);

            Debug.Log("Joined lobby: " + currentLobby.Name);
            statusText.text = "Joined lobby!";

            SceneManager.LoadScene("Lobby");
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError("Join by code failed: " + e.Message);
            statusText.text = "Join failed: " + e.Message;
        }
    }
    
    public async Task RefreshCurrentLobbyAsync()
    {
        if (currentLobby == null) return;
        
        try
        {
            currentLobby = await LobbyService.Instance.GetLobbyAsync(currentLobby.Id);
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError("refresh lobby failed: " + e.Message);
        }
    }

    public async Task toggleReadyAsync(TMPro.TMP_Text statusText)
    {
        if (currentLobby == null)
        {
            statusText.text = "no current lobby.";
            return;
        }

        try
        {
            var me = currentLobby.Players.Find(p => p.Id == AuthenticationService.Instance.PlayerId);

            if (me == null)
            {
                statusText.text = "player not found in lobby";
                return;
            }

            string currentReady = me.Data != null && me.Data.ContainsKey("Ready")
                ? me.Data["Ready"].Value
                : "0";
            string newReady = currentReady == "1" ? "0" : "1";

            var options = new UpdatePlayerOptions
            {
                Data = new Dictionary<string, PlayerDataObject>
                {
                    {
                        "Ready",
                        new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, newReady)
                    }


                }
            };

            await LobbyService.Instance.UpdatePlayerAsync(currentLobby.Id, AuthenticationService.Instance.PlayerId, options);

            statusText.text = newReady == "1" ? "you are ready" : "you are not ready";

            await RefreshCurrentLobbyAsync();
        }

        catch (LobbyServiceException e)
        {
            Debug.LogError(" toggle ready failed: " + e.Message);
            statusText.text = " ready update failed: " + e.Message;
        }
    }

    public async Task LeaveLobbyAsync()
    {
        if (currentLobby == null) return;

        try
        {
            await LobbyService.Instance.RemovePlayerAsync(currentLobby.Id, AuthenticationService.Instance.PlayerId);
            currentLobby = null;
            UnityEngine.SceneManagement.SceneManager.LoadScene("Menu");
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError(" leave lobby failed: " +e.Message);
        }
    }
    public bool IsHost()
    {
        if (currentLobby == null) return false;
        return currentLobby.HostId == Unity.Services.Authentication.AuthenticationService.Instance.PlayerId;
    }

    public bool CanStartGame()
    {
        if (currentLobby == null) return false;
        if (!IsHost()) return false;
        if (!AreAllPlayersReady()) return false;

        return true;
    }
    public bool AreAllPlayersReady()
    {
        if (currentLobby == null) return false;
        if (currentLobby.Players == null || currentLobby.Players.Count < currentLobby.MaxPlayers)
            return false;

        foreach (var player in currentLobby.Players)
        {
            if (player.Data == null) return false;
            if (!player.Data.ContainsKey("Ready")) return false;
            if (player.Data["Ready"] == null) return false;
            if (player.Data["Ready"].Value != "1") return false;

        }
        return true;
    }

    public async Task SetRelayJoinCodeAsync(string relayJoinCode)
    {
        if (currentLobby == null) return;

        try
        {
            var options = new UpdateLobbyOptions
            {
                Data = new Dictionary<string, DataObject>
                {
                    {
                        "RelayJoinCode",
                        new DataObject(DataObject.VisibilityOptions.Member, relayJoinCode)
                    }
                }
            };
            currentLobby = await LobbyService.Instance.UpdateLobbyAsync(currentLobby.Id, options);
            Debug.Log("Relay join code stored in lobby");
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError("failed to store relay code:" +  e.Message);
        }
    }

    public string GetRelayJoinCodeFromLobby()
    {
        if (currentLobby == null || currentLobby.Data == null)
            return null;

        if (!currentLobby.Data.ContainsKey("RelayJoinCode"))
            return null;

        return currentLobby.Data["RelayJoinCode"]?.Value;

    }
}
    

