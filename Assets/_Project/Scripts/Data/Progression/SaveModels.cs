using System;
using System.Collections.Generic;
[Serializable] public class PlayerSaveData
{
    public int slotIndex = 0;
    public string nickname = "Faulvalier";
    public string currentScene = "Stage1_Bosque";

    public bool isCheckpointActivated = false;
    public int remainingLives = 0;
    public int baseVesania = 0;

    public int totalDeaths = 0;
    public float playTimeSeconds = 0f;

    public int statVida = 1;
    public int statStamina = 1;
    public int statFuerza = 1;
    public int statAgilidad = 1;
}
[Serializable] public struct StateKillRecord
{
    public int state;
    public string tag;
    public int count;
}
[Serializable] public class StageSaveData
{
    public string stageSceneName;
    public int currentState = 0;
    public List<string> unlockedFlags = new();
    public List<string> triggeredEventIDs = new();
    public List<StateKillRecord> stateKillRecords = new();
}
[Serializable] public class WorldSaveData
{
    public StageSaveData stage1 = new() { stageSceneName = "Stage1_Bosque" };
    public StageSaveData stage2 = new() { stageSceneName = "Stage2_Catedral" };

    public StageSaveData GetStageData(string sceneName, string stage1Name, string stage2Name)
    {
        if (string.Equals(sceneName, stage2Name, StringComparison.OrdinalIgnoreCase))
        {
            stage2 ??= new StageSaveData { stageSceneName = stage2Name };
            return stage2;
        }

        stage1 ??= new StageSaveData { stageSceneName = stage1Name };
        return stage1;
    }
}
[Serializable] public struct SerializedInventorySlot
{
    public int slotIndex;
    public string itemID;
    public int quantity;
}
[Serializable] public class InventorySaveData
{
    public string equippedArmorID = string.Empty;
    public string equippedConsumableID = string.Empty;
    public int equippedConsumableCount = 0;
    public List<SerializedInventorySlot> generalSlots = new();
    public List<SerializedInventorySlot> armorSlots = new();
}
[Serializable] public class GameSaveContainer
{
    public PlayerSaveData player = new();
    public WorldSaveData world = new();
    public InventorySaveData inventory = new();
}
public struct SaveSlotSummary
{
    public int slotIndex;
    public bool isOccupied;
    public string nickname;
    public float playTimeSeconds;
    public string currentScene;
}