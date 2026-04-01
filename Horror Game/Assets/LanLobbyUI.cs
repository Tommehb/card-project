using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LanLobbyUI : MonoBehaviour
{
    [Header("Optional References")]
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text playerListText;
    [SerializeField] private TMP_Text flowSummaryText;
    [SerializeField] private TMP_InputField playerNameInput;
    [SerializeField] private Button readyButton;
    [SerializeField] private TMP_Text readyButtonText;
    [SerializeField] private Button startGameButton;
    [SerializeField] private Button leaveButton;

    private void Start()
    {
        if (playerNameInput != null)
        {
            playerNameInput.onEndEdit.AddListener(HandlePlayerNameSubmitted);
        }

        if (readyButton != null)
        {
            readyButton.onClick.AddListener(ToggleReady);
        }

        if (startGameButton != null)
        {
            startGameButton.onClick.AddListener(StartGame);
        }

        if (leaveButton != null)
        {
            leaveButton.onClick.AddListener(LeaveSession);
        }
    }

    private void Update()
    {
        Refresh();
    }

    public void ToggleReady()
    {
        var session = LanSessionManager.Instance;
        var localPlayer = session != null ? session.LocalPlayer : null;
        if (localPlayer != null)
        {
            localPlayer.ToggleReady();
        }
    }

    public void StartGame()
    {
        LanSessionManager.Instance?.TryStartGame();
    }

    public void LeaveSession()
    {
        LanSessionManager.Instance?.ShutdownAndReturnToMenu();
    }

    private void HandlePlayerNameSubmitted(string playerName)
    {
        LanSessionManager.Instance?.SetLocalPlayerName(playerName);
    }

    private void Refresh()
    {
        var session = LanSessionManager.Instance;
        if (session == null)
        {
            if (statusText != null)
            {
                statusText.text = "No LAN session manager was found.";
            }

            if (playerListText != null)
            {
                playerListText.text = "Add or keep a NetworkManager with PersistNetworkManager in this scene flow.";
            }

            return;
        }

        if (statusText != null)
        {
            statusText.text = session.StatusMessage;
        }

        if (playerListText != null)
        {
            playerListText.text = session.BuildLobbyRosterText();
        }

        if (flowSummaryText != null)
        {
            flowSummaryText.text = $"Lobby: {session.LobbySceneName}\nGameplay: {session.GameplaySceneName}";
        }

        if (playerNameInput != null && !playerNameInput.isFocused)
        {
            playerNameInput.SetTextWithoutNotify(session.LocalPlayerName);
        }

        var localPlayer = session.LocalPlayer;

        if (readyButton != null)
        {
            readyButton.interactable = session.IsSessionActive && session.IsInLobbyScene && localPlayer != null;
        }

        if (readyButtonText != null)
        {
            readyButtonText.text = localPlayer != null && localPlayer.IsReady ? "Unready" : "Ready";
        }

        if (startGameButton != null)
        {
            startGameButton.interactable = session.CanLocalPlayerStartGame();
        }

        if (leaveButton != null)
        {
            leaveButton.interactable = session.IsSessionActive;
        }
    }
}
