using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

public class AuthDatabaseController : MonoBehaviour
{
    public AuthResponse lastAuthResponse;

    public IEnumerator LoginCoroutine(string username, string password)
    {
        WWWForm form = new ();
        form.AddField("username", username);
        form.AddField("password", password);

        string url = DatabaseManager.Instance.BaseUrl + "/login.php";

        using (UnityWebRequest www = UnityWebRequest.Post(url, form))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                lastAuthResponse = JsonUtility.FromJson<AuthResponse>(www.downloadHandler.text);
                if (lastAuthResponse.success)
                {
                    DatabaseManager.Instance.currentUserId = lastAuthResponse.user_id;
                    DatabaseManager.Instance.currentUsername = lastAuthResponse.username;
                }
            }
            else
            {
                lastAuthResponse = new AuthResponse { success = false, message = "error de conexion con el servidor" };
                Debug.LogError("error en login: " + www.error);
            }
        }
    }

    public IEnumerator RegisterCoroutine(string username, string password)
    {
        WWWForm form = new ();
        form.AddField("username", username);
        form.AddField("password", password);

        string url = DatabaseManager.Instance.BaseUrl + "/register.php";

        using (UnityWebRequest www = UnityWebRequest.Post(url, form))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                lastAuthResponse = JsonUtility.FromJson<AuthResponse>(www.downloadHandler.text);
                if (lastAuthResponse.success)
                {
                    DatabaseManager.Instance.currentUserId = lastAuthResponse.user_id;
                    DatabaseManager.Instance.currentUsername = lastAuthResponse.username;
                }
            }
            else
            {
                lastAuthResponse = new AuthResponse { success = false, message = "error de conexion con el servidor" };
                Debug.LogError("error en register: " + www.error);
            }
        }
    }
}