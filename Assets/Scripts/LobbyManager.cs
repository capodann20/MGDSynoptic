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
            currentLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(code);

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
}
    

