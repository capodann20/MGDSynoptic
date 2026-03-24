using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class MenuUIController : MonoBehaviour
{

    [SerializeField] private TMP_InputField LobbyNameInput;
    [SerializeField] private Toggle privateToggle;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_InputField joinCodeInput;
    [SerializeField] private TMP_Text lobbyListText;

    private void Start()
    {
        statusText.text = "ready.";
    }

    public async void OnCreatedLobbyPressed()
    {
        string lobbyName = LobbyNameInput.text.Trim();

        if (string.IsNullOrEmpty(lobbyName) )
        {
            lobbyName = "My lobby";
        }
        statusText.text = "creating lobby...";
        await LobbyManager.Instance.CreateLobbyAsync(lobbyName, 2, privateToggle.isOn, statusText);
    }

    public async void OnrefreshLobbiesPressed()
    {
        statusText.text = "Refreshing..";
        await LobbyManager.Instance.ListLobbiesAsync(lobbyListText, statusText);
    }

    public async void OnJoinBycodePressed()
    {
        string code = joinCodeInput.text.Trim();

        if (string.IsNullOrEmpty(code))
        {
            statusText.text = "enter a code.";
            return;
        }
        statusText.text = "joining lobby.";
        await LobbyManager.Instance.JoinLobbyByCodeAsync(code, statusText);
    }

}
