using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class WorldDatabaseController : MonoBehaviour
{
    public RemainsData[] zoneRemains;
    public RankingsResponse lastRankings;

    public IEnumerator GetRemainsCoroutine(string zoneName)
    {
        zoneRemains = new RemainsData[0];

        WWWForm form = new WWWForm();
        form.AddField("zone_name", zoneName);

        string url = DatabaseManager.Instance.BaseUrl + "/get_remains.php";

        using (UnityWebRequest www = UnityWebRequest.Post(url, form))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                RemainsListResponse res = JsonUtility.FromJson<RemainsListResponse>(www.downloadHandler.text);
                if (res != null && res.remains != null)
                {
                    zoneRemains = res.remains;
                }
            }
            else
            {
                Debug.LogError("Error al cargar cadaveres: " + www.error);
            }
        }
    }

    public IEnumerator GetRankingsCoroutine()
    {
        lastRankings = new RankingsResponse();
        WWWForm form = new WWWForm();

        string url = DatabaseManager.Instance.BaseUrl + "/get_rankings.php";

        using (UnityWebRequest www = UnityWebRequest.Post(url, form))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                lastRankings = JsonUtility.FromJson<RankingsResponse>(www.downloadHandler.text);
            }
            else
            {
                Debug.LogError("Error al cargar rankings: " + www.error);
            }
        }
    }
}