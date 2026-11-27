using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class CorpseSpawner : MonoBehaviour
{
    [SerializeField] private GameObject _corpsePrefab;
    [SerializeField] private float _yOffset = 0f;

    private IEnumerator Start()
    {
        if (DatabaseManager.Instance == null || _corpsePrefab == null) yield break;

        string currentScene = SceneManager.GetActiveScene().name;
        yield return StartCoroutine(DatabaseManager.Instance.World.GetRemainsCoroutine(currentScene));

        RemainsData[] corpses = DatabaseManager.Instance.World.zoneRemains;
        for (int i = 0; i < corpses.Length; i++)
        {
            Vector3 spawnPos = new Vector3(corpses[i].pos_x, corpses[i].pos_y + _yOffset, corpses[i].pos_z);
            GameObject corpseObj = Instantiate(_corpsePrefab, spawnPos, Quaternion.identity);
            corpseObj.name = "Corpse_" + corpses[i].nickname;
        }
    }
}