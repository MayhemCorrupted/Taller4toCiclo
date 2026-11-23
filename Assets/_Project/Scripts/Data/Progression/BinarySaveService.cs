using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
public static class BinarySaveService
{
    public const int MAX_SLOTS = 3;
    private const string SLOT_FILE_PREFIX = "rotbound_slot_";
    private const string SLOT_FILE_EXT = ".bin";

    private static readonly byte[] AES_KEY = Encoding.UTF8.GetBytes("Rotbound_Pandemoniac_Key_2026_32");
    private static readonly byte[] AES_IV = Encoding.UTF8.GetBytes("Rotbound_IV_16By");

    private static string _baseDirectory;
    private static bool _isInitialized;
    public static void Initialize()
    {
        if (_isInitialized) return;
        _baseDirectory = Application.persistentDataPath;
        _isInitialized = true;
    }
    private static string GetSlotPath(int slotIndex)
    {
        if (!_isInitialized) Initialize();
        int clampedSlot = Mathf.Clamp(slotIndex, 0, MAX_SLOTS - 1);
        return Path.Combine(_baseDirectory, $"{SLOT_FILE_PREFIX}{clampedSlot}{SLOT_FILE_EXT}");
    }
    public static bool HasSlotSave(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= MAX_SLOTS) return false;
        return File.Exists(GetSlotPath(slotIndex));
    }
    public static bool HasAnySave()
    {
        for (int i = 0; i < MAX_SLOTS; i++)
        {
            if (HasSlotSave(i)) return true;
        }
        return false;
    }
    public static async Task SaveSlotAsync(int slotIndex, GameSaveContainer container)
    {
        if (container == null || slotIndex < 0 || slotIndex >= MAX_SLOTS) return;

        container.player.slotIndex = slotIndex;
        string path = GetSlotPath(slotIndex);
        string plainJson = JsonUtility.ToJson(container, false);

        await Task.Run(() => WriteEncryptedBinary(path, plainJson));
        GameLogger.LogInfo($"Partida guardada en Slot {slotIndex + 1}.");
    }
    public static async Task<GameSaveContainer> LoadSlotAsync(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= MAX_SLOTS) return null;

        string path = GetSlotPath(slotIndex);
        if (!File.Exists(path)) return null;

        try
        {
            string plainJson = await Task.Run(() => ReadEncryptedBinary(path));
            if (string.IsNullOrWhiteSpace(plainJson)) return null;

            return JsonUtility.FromJson<GameSaveContainer>(plainJson);
        }
        catch (Exception ex)
        {
            GameLogger.LogError($"Archivo de guardado corrupto en Slot {slotIndex + 1}: {ex.Message}");
            DeleteSlotSave(slotIndex);
            return null;
        }
    }
    public static async Task<SaveSlotSummary[]> GetAllSlotSummariesAsync()
    {
        SaveSlotSummary[] summaries = new SaveSlotSummary[MAX_SLOTS];

        for (int i = 0; i < MAX_SLOTS; i++)
        {
            if (!HasSlotSave(i))
            {
                summaries[i] = new SaveSlotSummary
                {
                    slotIndex = i,
                    isOccupied = false,
                    nickname = string.Empty,
                    playTimeSeconds = 0f,
                    currentScene = string.Empty
                };
                continue;
            }

            GameSaveContainer data = await LoadSlotAsync(i);
            if (data != null && data.player != null)
            {
                summaries[i] = new SaveSlotSummary
                {
                    slotIndex = i,
                    isOccupied = true,
                    nickname = data.player.nickname,
                    playTimeSeconds = data.player.playTimeSeconds,
                    currentScene = data.player.currentScene
                };
            }
            else
            {
                summaries[i] = new SaveSlotSummary { slotIndex = i, isOccupied = false };
            }
        }

        return summaries;
    }
    public static void DeleteSlotSave(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= MAX_SLOTS) return;

        try
        {
            string path = GetSlotPath(slotIndex);
            if (File.Exists(path))
            {
                File.Delete(path);
                GameLogger.LogInfo($"Slot {slotIndex + 1} eliminado del disco.");
            }
        }
        catch (Exception ex)
        {
            GameLogger.LogError($"Error al borrar Slot {slotIndex + 1}: {ex.Message}");
        }
    }
    private static void WriteEncryptedBinary(string filePath, string plainJson)
    {
        try
        {
            byte[] encryptedBytes;
            using (Aes aes = Aes.Create())
            {
                aes.Key = AES_KEY;
                aes.IV = AES_IV;

                using MemoryStream ms = new();
                using (CryptoStream cs = new(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                using (StreamWriter sw = new(cs, Encoding.UTF8))
                {
                    sw.Write(plainJson);
                }
                encryptedBytes = ms.ToArray();
            }

            using FileStream fs = new(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
            using BinaryWriter writer = new(fs);
            writer.Write(encryptedBytes.Length);
            writer.Write(encryptedBytes);
        }
        catch (Exception ex)
        {
            GameLogger.LogError($"Fallo de escritura binaria AES: {ex.Message}");
        }
    }
    private static string ReadEncryptedBinary(string filePath)
    {
        byte[] encryptedBytes;
        using (FileStream fs = new(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
        using (BinaryReader reader = new(fs))
        {
            int length = reader.ReadInt32();
            encryptedBytes = reader.ReadBytes(length);
        }

        using Aes aes = Aes.Create();
        aes.Key = AES_KEY;
        aes.IV = AES_IV;

        using MemoryStream ms = new(encryptedBytes);
        using CryptoStream cs = new(ms, aes.CreateDecryptor(), CryptoStreamMode.Read);
        using StreamReader sr = new(cs, Encoding.UTF8);
        return sr.ReadToEnd();
    }
}