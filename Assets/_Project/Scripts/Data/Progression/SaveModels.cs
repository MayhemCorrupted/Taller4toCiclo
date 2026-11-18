using System;
using System.Collections.Generic;

[Serializable]
public class PlayerSaveData
{
    public string nickname = "Faulvalier";
    public string currentScene = "Stage1_Bosque";

    // Estado de Checkpoint, Vidas y Vesania (sincronizado con GameManager)
    public bool isCheckpointActivated = false;
    public int remainingLives = 0;
    public int baseVesania = 0;

    // Métricas de Ranking
    public int totalDeaths = 0;
    public float playTimeSeconds = 0f;

    // Estadísticas Base del Jugador (Nivel 1 a 10)
    public int statVida = 1;
    public int statStamina = 1;
    public int statFuerza = 1;
    public int statAgilidad = 1;
}

[Serializable]
public struct StateKillRecord
{
    public int state;
    public string tag;
    public int count;
}

[Serializable]
public class WorldSaveData
{
    public int currentState = 0;
    public List<string> unlockedFlags = new();
    public List<string> triggeredEventIDs = new();
    public List<StateKillRecord> stateKillRecords = new();
}

[Serializable]
public class InventorySaveData
{
    public string equippedArmorID = "Harapos_NoMuerto";
    public bool hasAnathema = false;
    public int lagrimasRedomaCount = 0;
    public int aguaMemoriaCount = 0;
    public int oleoCarronaCount = 0;
}

[Serializable]
public class GameSaveContainer
{
    public PlayerSaveData player = new();
    public WorldSaveData world = new();
    public InventorySaveData inventory = new();
}

[Serializable]
public struct FallenRemainRecord
{
    public string nickname;
    public string sceneName;
    public float posX;
    public float posY;
    public float posZ;
}

[Serializable]
public class FallenRemainsList
{
    public List<FallenRemainRecord> records = new();
}

[Serializable]
public class RunSubmissionPayload
{
    public string nickname;
    public float playTime;
    public int won; // 1 = Victoria, 0 = Permadeath
    public int deaths;
    public string deathScene;
    public float posX;
    public float posY;
    public float posZ;
}

[Serializable]
public class PendingSubmissionsList
{
    public List<RunSubmissionPayload> pendingRuns = new();
}