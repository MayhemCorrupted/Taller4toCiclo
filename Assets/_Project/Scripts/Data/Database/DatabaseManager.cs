using UnityEngine;

[RequireComponent(typeof(AuthDatabaseController))]
[RequireComponent(typeof(CharacterDatabaseController))]
[RequireComponent(typeof(WorldDatabaseController))]
public class DatabaseManager : MonoBehaviour
{
    public static DatabaseManager Instance;

    [SerializeField] private string baseUrl = "https://progra262chgrupo1.samidareno.com/rotbound_php";

    public int currentUserId = -1;
    public string currentUsername = "";

    public AuthDatabaseController Auth { get; private set; }
    public CharacterDatabaseController Characters { get; private set; }
    public WorldDatabaseController World { get; private set; }

    public string BaseUrl => baseUrl;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        Auth = GetComponent<AuthDatabaseController>();
        Characters = GetComponent<CharacterDatabaseController>();
        World = GetComponent<WorldDatabaseController>();
    }

    public bool IsLoggedIn()
    {
        return currentUserId > 0;
    }
}