using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
[Serializable] public class LeaderboardEntry
{
    public string nickname;
    public float playTime;
    public int deaths;
}
[Serializable] public class LeaderboardResponse
{
    public List<LeaderboardEntry> entries;
}
[Serializable] public class GlobalRemainsResponse
{
    public List<FallenRemainRecord> remains;
}
public class CloudSyncService : MonoBehaviour
{
    public static CloudSyncService Instance { get; private set; }

    [Header("Configuración del Backend")]
    [SerializeField] private string _baseApiUrl = "https://tudominio.com/rotbound/api";
    [SerializeField] private int _requestTimeoutSeconds = 5;

    public event Action<bool> OnConnectionStatusChanged;

    private bool _isOnline = false;
    public bool IsOnline => _isOnline;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        BinarySaveService.Initialize();
    }
    private async void Start()
    {
        await CheckServerConnectionAsync();
    }
    /// <summary>
    /// Verifica si hay internet y si el servidor de Rotbound responde.
    /// Si está online, sincroniza automáticamente partidas pendientes jugadas offline.
    /// </summary>
    public async Task<bool> CheckServerConnectionAsync()
    {
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            SetOnlineStatus(false);
            return false;
        }

        string pingUrl = $"{_baseApiUrl}/ping.php";
        string response = await SendGetAsync(pingUrl);

        bool connected = !string.IsNullOrEmpty(response);
        SetOnlineStatus(connected);

        if (connected)
        {
            await FlushPendingOfflineRunsAsync();
        }

        return connected;
    }
    private void SetOnlineStatus(bool status)
    {
        if (_isOnline == status) return;
        _isOnline = status;
        OnConnectionStatusChanged?.Invoke(_isOnline);
        GameLogger.LogInfo(_isOnline ? "Modo ONLINE activo (Ranking conectado)." : "Modo OFFLINE activo.");
    }
    /// <summary>
    /// Envía el resultado final (Victoria o Permadeath).
    /// Si no hay conexión, lo guarda en la cola local encriptada para subirlo después.
    /// </summary>
    public async Task SubmitRunResultAsync(RunSubmissionPayload payload)
    {
        if (payload == null) return;

        if (payload.won == 0)
        {
            await BinarySaveService.SaveLocalFallenRemainAsync(new FallenRemainRecord
            {
                nickname = payload.nickname,
                sceneName = payload.deathScene,
                posX = payload.posX,
                posY = payload.posY,
                posZ = payload.posZ
            });
        }

        if (!_isOnline)
        {
            await CheckServerConnectionAsync();
        }

        if (_isOnline)
        {
            bool uploaded = await PostPayloadAsync(payload);
            if (uploaded)
            {
                GameLogger.LogInfo("Resultado enviado exitosamente al Ranking online.");
                return;
            }
        }

        // Respaldo Offline: encolar para cuando regrese la conexión
        await BinarySaveService.EnqueuePendingSubmissionAsync(payload);
        GameLogger.LogWarning("Sin conexión: resultado encolado localmente para sincronizar luego.");
    }
    private async Task FlushPendingOfflineRunsAsync()
    {
        PendingSubmissionsList pending = await BinarySaveService.LoadPendingSubmissionsAsync();
        if (pending.pendingRuns == null || pending.pendingRuns.Count == 0) return;

        List<RunSubmissionPayload> stillPending = new();

        foreach (var run in pending.pendingRuns)
        {
            bool success = await PostPayloadAsync(run);
            if (!success) stillPending.Add(run);
        }

        if (stillPending.Count == 0)
        {
            BinarySaveService.ClearPendingSubmissions();
            GameLogger.LogInfo("Partidas offline sincronizadas con el Ranking.");
        }
    }
    private async Task<bool> PostPayloadAsync(RunSubmissionPayload payload)
    {
        string url = $"{_baseApiUrl}/submit_run.php";
        string json = JsonUtility.ToJson(payload);

        try
        {
            using UnityWebRequest request = new(url, "POST");
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = _requestTimeoutSeconds;

            await SendWebRequestAsync(request);

            if (request.result == UnityWebRequest.Result.Success)
                return true;

            SetOnlineStatus(false);
            return false;
        }
        catch (Exception ex)
        {
            GameLogger.LogWarning($"Fallo de red silencioso (Offline): {ex.Message}");
            SetOnlineStatus(false);
            return false;
        }
    }
    /// <summary>
    /// Descarga el Top Ranking. Si está offline devuelve una lista vacía sin bloquear.
    /// </summary>
    public async Task<List<LeaderboardEntry>> FetchLeaderboardAsync()
    {
        if (!_isOnline && !await CheckServerConnectionAsync())
            return new List<LeaderboardEntry>();

        string url = $"{_baseApiUrl}/get_ranking.php";
        string json = await SendGetAsync(url);

        if (string.IsNullOrEmpty(json)) return new List<LeaderboardEntry>();

        try
        {
            LeaderboardResponse res = JsonUtility.FromJson<LeaderboardResponse>(json);
            return res?.entries ?? new List<LeaderboardEntry>();
        }
        catch
        {
            return new List<LeaderboardEntry>();
        }
    }
    /// <summary>
    /// Descarga las coordenadas de Global_Remains para la escena actual.
    /// </summary>
    public async Task<List<FallenRemainRecord>> FetchGlobalRemainsAsync(string sceneName)
    {
        if (!_isOnline && !await CheckServerConnectionAsync())
            return new List<FallenRemainRecord>();

        string url = $"{_baseApiUrl}/get_remains.php?scene={UnityWebRequest.EscapeURL(sceneName)}";
        string json = await SendGetAsync(url);

        if (string.IsNullOrEmpty(json)) return new List<FallenRemainRecord>();

        try
        {
            GlobalRemainsResponse res = JsonUtility.FromJson<GlobalRemainsResponse>(json);
            return res?.remains ?? new List<FallenRemainRecord>();
        }
        catch
        {
            return new List<FallenRemainRecord>();
        }
    }
    private async Task<string> SendGetAsync(string url)
    {
        try
        {
            using UnityWebRequest request = UnityWebRequest.Get(url);
            request.timeout = _requestTimeoutSeconds;
            await SendWebRequestAsync(request);

            if (request.result == UnityWebRequest.Result.Success)
            {
                SetOnlineStatus(true);
                return request.downloadHandler.text;
            }

            SetOnlineStatus(false);
            return null;
        }
        catch
        {
            SetOnlineStatus(false);
            return null;
        }
    }
    private static Task SendWebRequestAsync(UnityWebRequest request)
    {
        TaskCompletionSource<bool> tcs = new();
        UnityWebRequestAsyncOperation op = request.SendWebRequest();
        op.completed += _ => tcs.TrySetResult(true);
        return tcs.Task;
    }
}