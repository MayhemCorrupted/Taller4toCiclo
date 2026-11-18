using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
public class FallenRemainsSpawner : MonoBehaviour
{
    [Header("Prefab del Esqueleto / Armadura Caída")]
    [SerializeField] private GameObject _fallenPlayerPrefab;
    [SerializeField] private int _maxRemainsInScene = 20;

    private readonly HashSet<Vector3> _spawnedPositions = new();
    private async void Start()
    {
        if (_fallenPlayerPrefab == null) return;

        string activeScene = SceneManager.GetActiveScene().name;

        FallenRemainsList localRemains = await BinarySaveService.LoadLocalFallenRemainsAsync();
        SpawnRemains(localRemains.records, activeScene);

        if (CloudSyncService.Instance != null)
        {
            List<FallenRemainRecord> onlineRemains = await CloudSyncService.Instance.FetchGlobalRemainsAsync(activeScene);
            SpawnRemains(onlineRemains, activeScene);
        }
    }
    private void SpawnRemains(List<FallenRemainRecord> records, string activeScene)
    {
        if (records == null) return;

        for (int i = 0; i < records.Count; i++)
        {
            if (_spawnedPositions.Count >= _maxRemainsInScene) break;

            FallenRemainRecord rec = records[i];
            if (!string.Equals(rec.sceneName, activeScene, StringComparison.OrdinalIgnoreCase)) continue;

            Vector3 roundedPos = new(
                Mathf.Round(rec.posX * 10f) / 10f,
                Mathf.Round(rec.posY * 10f) / 10f,
                Mathf.Round(rec.posZ * 10f) / 10f
            );
            if (_spawnedPositions.Contains(roundedPos)) continue;

            _spawnedPositions.Add(roundedPos);
            Instantiate(_fallenPlayerPrefab, new Vector3(rec.posX, rec.posY, rec.posZ), Quaternion.identity);
        }
    }
}