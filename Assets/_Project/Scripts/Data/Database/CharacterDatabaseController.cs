using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

public class CharacterDatabaseController : MonoBehaviour
{
    public CloudCharacterData[] myCharacters;

    public IEnumerator GetCharactersCoroutine()
    {
        myCharacters = new CloudCharacterData[0];
        if (!DatabaseManager.Instance.IsLoggedIn()) yield break;

        WWWForm form = new WWWForm();
        form.AddField("user_id", DatabaseManager.Instance.currentUserId);

        string url = DatabaseManager.Instance.BaseUrl + "/get_characters.php";

        using (UnityWebRequest www = UnityWebRequest.Post(url, form))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                CharacterListResponse res = JsonUtility.FromJson<CharacterListResponse>(www.downloadHandler.text);
                if (res != null && res.characters != null)
                {
                    myCharacters = res.characters;
                }
            }
            else
            {
                Debug.LogError("Error al obtener personajes: " + www.error);
            }
        }
    }

    public void CreateCharacter(string nickname, GameSaveContainer container)
    {
        if (!DatabaseManager.Instance.IsLoggedIn()) return;
        StartCoroutine(CreateCharacterCoroutine(nickname, container));
    }

    private IEnumerator CreateCharacterCoroutine(string nickname, GameSaveContainer container)
    {
        WWWForm form = new WWWForm();
        form.AddField("user_id", DatabaseManager.Instance.currentUserId);
        form.AddField("nickname", nickname);
        form.AddField("save_json", JsonUtility.ToJson(container, false));

        string url = DatabaseManager.Instance.BaseUrl + "/create_character.php";

        using (UnityWebRequest www = UnityWebRequest.Post(url, form))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                CreateCharacterResponse res = JsonUtility.FromJson<CreateCharacterResponse>(www.downloadHandler.text);
                if (res != null && res.success && container != null)
                {
                    container.player.characterId = res.character_id;
                }
            }
            else
            {
                Debug.LogError("Error al crear personaje: " + www.error);
            }
        }
    }

    public void SaveProgress(GameSaveContainer container)
    {
        if (!DatabaseManager.Instance.IsLoggedIn() || container == null || container.player.characterId <= 0) return;
        StartCoroutine(SaveProgressCoroutine(container));
    }

    private IEnumerator SaveProgressCoroutine(GameSaveContainer container)
    {
        WWWForm form = new ();
        form.AddField("character_id", container.player.characterId);
        form.AddField("play_time", Mathf.RoundToInt(container.player.playTimeSeconds));
        form.AddField("save_json", JsonUtility.ToJson(container, false));

        string url = DatabaseManager.Instance.BaseUrl + "/save_progress.php";

        using (UnityWebRequest www = UnityWebRequest.Post(url, form))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error al guardar progreso: " + www.error);
            }
        }
    }

    public void FinishRunLose(int characterId, int score, float playTimeSeconds, string sceneName, Vector3 deathPos)
    {
        if (!DatabaseManager.Instance.IsLoggedIn() || characterId <= 0) return;
        StartCoroutine(FinishRunLoseCoroutine(characterId, score, Mathf.RoundToInt(playTimeSeconds), sceneName, deathPos));
    }

    private IEnumerator FinishRunLoseCoroutine(int characterId, int score, int playTime, string sceneName, Vector3 deathPos)
    {
        string posX = deathPos.x.ToString().Replace(',', '.');
        string posY = deathPos.y.ToString().Replace(',', '.');
        string posZ = deathPos.z.ToString().Replace(',', '.');

        WWWForm form = new WWWForm();
        form.AddField("character_id", characterId);
        form.AddField("result", "lose");
        form.AddField("score", score);
        form.AddField("play_time", playTime);
        form.AddField("zone_name", sceneName);
        form.AddField("pos_x", posX);
        form.AddField("pos_y", posY);
        form.AddField("pos_z", posZ);

        string url = DatabaseManager.Instance.BaseUrl + "/finish_run.php";

        using (UnityWebRequest www = UnityWebRequest.Post(url, form))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error al guardar derrota: " + www.error);
            }
        }
    }

    public void FinishRunWin(int characterId, int score, float playTimeSeconds)
    {
        if (!DatabaseManager.Instance.IsLoggedIn() || characterId <= 0) return;
        StartCoroutine(FinishRunWinCoroutine(characterId, score, Mathf.RoundToInt(playTimeSeconds)));
    }

    private IEnumerator FinishRunWinCoroutine(int characterId, int score, int playTime)
    {
        WWWForm form = new WWWForm();
        form.AddField("character_id", characterId);
        form.AddField("result", "win");
        form.AddField("score", score);
        form.AddField("play_time", playTime);

        string url = DatabaseManager.Instance.BaseUrl + "/finish_run.php";

        using (UnityWebRequest www = UnityWebRequest.Post(url, form))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error al guardar victoria: " + www.error);
            }
        }
    }

    public void DeleteCharacter(int characterId)
    {
        if (!DatabaseManager.Instance.IsLoggedIn() || characterId <= 0) return;
        StartCoroutine(DeleteCharacterCoroutine(characterId));
    }

    private IEnumerator DeleteCharacterCoroutine(int characterId)
    {
        WWWForm form = new WWWForm();
        form.AddField("character_id", characterId);
        form.AddField("user_id", DatabaseManager.Instance.currentUserId);

        string url = DatabaseManager.Instance.BaseUrl + "/delete_character.php";

        using (UnityWebRequest www = UnityWebRequest.Post(url, form))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error al borrar personaje: " + www.error);
            }
        }
    }
}