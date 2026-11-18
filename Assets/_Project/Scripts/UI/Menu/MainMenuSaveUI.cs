using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class MainMenuSaveUI : MonoBehaviour
{
    [Header("Controles de Partida")]
    [SerializeField] private Button _continueButton;
    [SerializeField] private TMP_InputField _nicknameInput;

    [Header("Indicador Online / Offline")]
    [SerializeField] private TMP_Text _connectionStatusText;

    [Header("UI de Ranking")]
    [SerializeField] private TMP_Text _rankingListText;
    private async void Start()
    {
        BinarySaveService.Initialize();

        if (_continueButton != null)
        {
            _continueButton.interactable = BinarySaveService.HasActiveRun();
        }

        if (CloudSyncService.Instance != null)
        {
            CloudSyncService.Instance.OnConnectionStatusChanged += UpdateConnectionLabel;
            bool isOnline = await CloudSyncService.Instance.CheckServerConnectionAsync();
            UpdateConnectionLabel(isOnline);

            if (isOnline)
            {
                await RefreshLeaderboardAsync();
            }
            else if (_rankingListText != null)
            {
                _rankingListText.text = "Modo Offline: Conéctate a internet para ver el Ranking.";
            }
        }
    }
    private void OnDestroy()
    {
        if (CloudSyncService.Instance != null)
        {
            CloudSyncService.Instance.OnConnectionStatusChanged -= UpdateConnectionLabel;
        }
    }
    private void UpdateConnectionLabel(bool isOnline)
    {
        if (_connectionStatusText != null)
        {
            _connectionStatusText.text = isOnline ? "ONLINE" : "OFFLINE";
        }
    }
    public void OnClickNewGame()
    {
        string nick = _nicknameInput != null ? _nicknameInput.text : "Faulvalier";
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.StartNewGame(nick);
        }
    }
    public void OnClickContinueGame()
    {
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.ContinueSavedGame();
        }
    }
    public async System.Threading.Tasks.Task RefreshLeaderboardAsync()
    {
        if (CloudSyncService.Instance == null || _rankingListText == null) return;

        List<LeaderboardEntry> entries = await CloudSyncService.Instance.FetchLeaderboardAsync();
        if (entries.Count == 0)
        {
            _rankingListText.text = "Aún no hay victorias registradas.";
            return;
        }

        System.Text.StringBuilder sb = new();
        for (int i = 0; i < entries.Count; i++)
        {
            int minutes = Mathf.FloorToInt(entries[i].playTime / 60f);
            int seconds = Mathf.FloorToInt(entries[i].playTime % 60f);
            sb.AppendLine($"#{i + 1}  {entries[i].nickname}  -  {minutes:00}:{seconds:00}  ({entries[i].deaths} muertes)");
        }
        _rankingListText.text = sb.ToString();
    }
}