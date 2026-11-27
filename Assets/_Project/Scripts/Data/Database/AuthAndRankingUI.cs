using TMPro;
using UnityEngine;
using System.Collections;
using UnityEngine.UI;
public class AuthAndRankingUI : MonoBehaviour
{
    [Header("Referencias Generales")]
    [SerializeField] private MainMenuSaveUI _mainMenuSaveUI;
    [SerializeField] private GameObject _authPanelContainer;
    [SerializeField] private GameObject _mainMenuContainer;

    [Header("Inputs de Registro")]
    [SerializeField] private TMP_InputField _usernameInput;
    [SerializeField] private TMP_InputField _passwordInput;
    [SerializeField] private Button _loginButton;
    [SerializeField] private Button _registerButton;
    [SerializeField] private TMP_Text _authStatusText;
    [SerializeField] private TMP_Text _loggedUserLabel;

    [Header("Panel de Rankings")]
    [SerializeField] private GameObject _rankingPanelContainer;
    [SerializeField] private Button _openRankingButton;
    [SerializeField] private Button _closeRankingButton;
    [SerializeField] private TMP_Text _conventionalRankingText;
    [SerializeField] private TMP_Text _testersRankingText;

    private bool _isBusy;

    private void Start()
    {
        if (_loginButton != null) _loginButton.onClick.AddListener(OnLoginClicked);
        if (_registerButton != null) _registerButton.onClick.AddListener(OnRegisterClicked);
        if (_openRankingButton != null) _openRankingButton.onClick.AddListener(OpenRankingPanel);
        if (_closeRankingButton != null) _closeRankingButton.onClick.AddListener(CloseRankingPanel);

        if (_rankingPanelContainer != null)
            _rankingPanelContainer.SetActive(false);

        if (DatabaseManager.Instance != null && DatabaseManager.Instance.IsLoggedIn())
        {
            StartCoroutine(LoadAccountAndShowMenu());
        }
        else
        {
            if (_authPanelContainer != null) _authPanelContainer.SetActive(true);
            if (_mainMenuContainer != null) _mainMenuContainer.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (_loginButton != null) _loginButton.onClick.RemoveAllListeners();
        if (_registerButton != null) _registerButton.onClick.RemoveAllListeners();
        if (_openRankingButton != null) _openRankingButton.onClick.RemoveAllListeners();
        if (_closeRankingButton != null) _closeRankingButton.onClick.RemoveAllListeners();
    }

    private void OnLoginClicked()
    {
        if (_isBusy || DatabaseManager.Instance == null) return;
        StartCoroutine(LoginProcess());
    }

    private IEnumerator LoginProcess()
    {
        string user = _usernameInput != null ? _usernameInput.text.Trim() : "";
        string pass = _passwordInput != null ? _passwordInput.text.Trim() : "";

        if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
        {
            SetStatus("ingresa usuario y contraseña.");
            yield break;
        }

        _isBusy = true;
        SetStatus("conectando...");

        yield return StartCoroutine(DatabaseManager.Instance.Auth.LoginCoroutine(user, pass));

        AuthResponse authRes = DatabaseManager.Instance.Auth.lastAuthResponse;
        if (authRes != null && authRes.success)
        {
            SetStatus("");
            yield return StartCoroutine(LoadAccountAndShowMenu());
        }
        else
        {
            string msg = authRes != null ? authRes.message : "error al iniciar sesion";
            SetStatus(msg);
        }

        _isBusy = false;
    }

    private void OnRegisterClicked()
    {
        if (_isBusy || DatabaseManager.Instance == null) return;
        StartCoroutine(RegisterProcess());
    }

    private IEnumerator RegisterProcess()
    {
        string user = _usernameInput != null ? _usernameInput.text.Trim() : "";
        string pass = _passwordInput != null ? _passwordInput.text.Trim() : "";

        if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
        {
            SetStatus("ingresa usuario y contraseña.");
            yield break;
        }

        _isBusy = true;
        SetStatus("registrando cuenta...");

        yield return StartCoroutine(DatabaseManager.Instance.Auth.RegisterCoroutine(user, pass));

        AuthResponse authRes = DatabaseManager.Instance.Auth.lastAuthResponse;
        if (authRes != null && authRes.success)
        {
            SetStatus("");
            yield return StartCoroutine(LoadAccountAndShowMenu());
        }
        else
        {
            string msg = authRes != null ? authRes.message : "error al registrar";
            SetStatus(msg);
        }

        _isBusy = false;
    }

    private IEnumerator LoadAccountAndShowMenu()
    {
        yield return StartCoroutine(DatabaseManager.Instance.Characters.GetCharactersCoroutine());

        if (_authPanelContainer != null) _authPanelContainer.SetActive(false);
        if (_mainMenuContainer != null) _mainMenuContainer.SetActive(true);

        if (_loggedUserLabel != null)
            _loggedUserLabel.text = "Cuenta: " + DatabaseManager.Instance.currentUsername;

        if (_mainMenuSaveUI != null)
            _ = _mainMenuSaveUI.RefreshSlotsDataAsync();
    }

    public void OpenRankingPanel()
    {
        if (_rankingPanelContainer != null)
            _rankingPanelContainer.SetActive(true);

        StartCoroutine(LoadRankingsProcess());
    }

    private IEnumerator LoadRankingsProcess()
    {
        if (_conventionalRankingText != null) _conventionalRankingText.text = "cargando...";
        if (_testersRankingText != null) _testersRankingText.text = "cargando...";

        if (DatabaseManager.Instance == null) yield break;

        yield return StartCoroutine(DatabaseManager.Instance.World.GetRankingsCoroutine());

        RankingsResponse data = DatabaseManager.Instance.World.lastRankings;

        if (_conventionalRankingText != null)
        {
            if (data == null || data.conventional == null || data.conventional.Length == 0)
            {
                _conventionalRankingText.text = "Sin partidas registradas aun.";
            }
            else
            {
                string lines = "";
                for (int i = 0; i < data.conventional.Length; i++)
                {
                    var item = data.conventional[i];
                    string timeFormatted = MainMenuSaveUI.FormatPlayTime(item.play_time);
                    string tag = item.result == "win" ? "[WIN]" : "[LOSE]";
                    lines += (i + 1) + ". " + item.nickname + " (" + item.username + ") " + tag + " - " + item.score + " pts | " + timeFormatted + "\n";
                }
                _conventionalRankingText.text = lines;
            }
        }

        if (_testersRankingText != null)
        {
            if (data == null || data.testers == null || data.testers.Length == 0)
            {
                _testersRankingText.text = "no hay testers.";
            }
            else
            {
                string lines = "";
                for (int i = 0; i < data.testers.Length; i++)
                {
                    var item = data.testers[i];
                    string timeFormatted = MainMenuSaveUI.FormatPlayTime(item.play_time);
                    lines += (i + 1) + ". " + item.nickname + " (" + item.username + ") - " + item.score + " pts | " + timeFormatted + "\n";
                }
                _testersRankingText.text = lines;
            }
        }
    }

    public void CloseRankingPanel()
    {
        if (_rankingPanelContainer != null)
            _rankingPanelContainer.SetActive(false);
    }

    private void SetStatus(string msg)
    {
        if (_authStatusText != null)
            _authStatusText.text = msg;
    }
}