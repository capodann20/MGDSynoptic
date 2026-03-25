using TMPro;
using UnityEngine.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;

public class LobbySceneUIController : MonoBehaviour
{
    [SerializeField] private TMP_Text lobbyNameText;
    [SerializeField] private TMP_Text lobbyCodeText;
    [SerializeField] private TMP_Text playerListText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private GameObject startGameButton;

    private float refreshTimer = 0f;
    private const float refreshInterval = 2f;
    private bool isJoiningRelay = false;
    private async void Start()
    {
        if (LobbyManager.Instance == null || LobbyManager.Instance.currentLobby == null)
        {
            statusText.text = "no active lobby found";
            return;
        }
        statusText.text = "lobby loaded";
        await RefreshLobby();

    }

    private void Update()
    {
        refreshTimer -= Time.deltaTime;

        if (refreshTimer <= 0f)
        {
            refreshTimer = refreshInterval;
            AutoRefreshLobby();
        }
    }

    private async void AutoRefreshLobby()
    {
        if (LobbyManager.Instance == null || LobbyManager.Instance.currentLobby == null)
            return;

        await LobbyManager.Instance.RefreshCurrentLobbyAsync();
        await RefreshLobby();

        TryJoinRelayAsClient();
        
    }

    public async void OnreadyPressed()
    {
        await LobbyManager.Instance.toggleReadyAsync(statusText);
        await RefreshLobby();
    }

    public async void OnRefreshPressed()
    {
        await LobbyManager.Instance.RefreshCurrentLobbyAsync();
        await RefreshLobby();
    }

    public async void OnLeaveLobbyPressed()
    {
        await LobbyManager.Instance.LeaveLobbyAsync();
    }

    public async void OnstartGamePressed()
    {
        if (LobbyManager.Instance == null || LobbyManager.Instance.currentLobby == null)
        {
            statusText.text = "No active lobby.";
            return;
        }

        if (!LobbyManager.Instance.IsHost())
        {
            statusText.text = "Only the host can start the game.";
            return;
        }

        if (!LobbyManager.Instance.AreAllPlayersReady())
        {
            statusText.text = "All players must be ready before starting.";
            return;
        }

        statusText.text = "Creating Relay allocation...";

        try
        {
            string relayJoinCode = await RelayManager.Instance.StartHosWithRelayAsync(1);

            if (string.IsNullOrEmpty(relayJoinCode))
            {
                statusText.text = "Failed to start host.";
                return;
            }

            await LobbyManager.Instance.SetRelayJoinCodeAsync(relayJoinCode);

            statusText.text = "Waiting for client connection...";

            float timeout = 10f;
            float timer = 0f;

            while (Unity.Netcode.NetworkManager.Singleton.ConnectedClientsList.Count < 2 && timer < timeout)
            {
                await Task.Delay(200);
                timer += 0.2f;
            }

            if (Unity.Netcode.NetworkManager.Singleton.ConnectedClientsList.Count < 2)
            {
                statusText.text = "Client did not connect in time.";
                return;
            }

            statusText.text = "Both players connected. Loading game...";
            Unity.Netcode.NetworkManager.Singleton.SceneManager.LoadScene(
                "Game",
                UnityEngine.SceneManagement.LoadSceneMode.Single
            );
        }
        catch (System.Exception e)
        {
            Debug.LogError("Start game failed: " + e.Message);
            statusText.text = "Start failed: " + e.Message;
        }
    }

    public async System.Threading.Tasks.Task RefreshLobby()
    {
        var lobby = LobbyManager.Instance.currentLobby;

        if (lobby == null)
        {
            statusText.text = "lobby no longer exist";
            return;
        }

        lobbyNameText.text = $"Lobby name: {lobby.Name}";
        lobbyCodeText.text = $"Lobby code: {lobby.LobbyCode}";

        string playerText = "";

        foreach (var player in lobby.Players)
        {
            string playerName = "Unknown";
            string ready = "0";

            if (player.Data != null)
            {
                if (player.Data.ContainsKey("PlayerName") && player.Data["PlayerName"] != null)
                    playerName = player.Data["PlayerName"].Value;

                if (player.Data.ContainsKey("Ready") && player.Data["Ready"] != null)
                    ready = player.Data["Ready"].Value;
            }

            playerText += $"{playerName} - {(ready == "1" ? "Ready" : "Not Ready")}\n";
        }

        playerListText.text = playerText;

        bool isHost = lobby.HostId == Unity.Services.Authentication.AuthenticationService.Instance.PlayerId;
        startGameButton.SetActive(isHost);
        var button = startGameButton.GetComponent<Button>();
        if (button != null)
        {
            button.interactable = isHost && LobbyManager.Instance.AreAllPlayersReady();
        }
    }

    private async void TryJoinRelayAsClient()
    {
        if (isJoiningRelay) return;
        if (LobbyManager.Instance.IsHost()) return;

        string relayJoinCode = LobbyManager.Instance.GetRelayJoinCodeFromLobby();

        if (string.IsNullOrEmpty(relayJoinCode)) 
            return;
        isJoiningRelay = true;
        statusText.text = "host started the game. joining relay";

        try
        {
            await RelayManager.Instance.StartClientWithRelayAsync(relayJoinCode);
            
        }
        catch (System.Exception e)
        {
            Debug.LogError(" client relay join failed:" + e.Message);
            statusText.text = " relay join failed" + e.Message;
            isJoiningRelay = false;
        }
    }
   
}
