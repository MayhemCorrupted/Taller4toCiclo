using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
public static class BinarySaveService
{
    private const string RUN_FILE_NAME = "rotbound_run.bin";
    private const string FALLEN_FILE_NAME = "rotbound_fallen.bin";
    private const string PENDING_FILE_NAME = "rotbound_pending.bin";

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
    private static string GetFullPath(string fileName)
    {
        if (!_isInitialized) Initialize();
        return Path.Combine(_baseDirectory, fileName);
    }
    public static bool HasActiveRun() => File.Exists(GetFullPath(RUN_FILE_NAME));

    public static async Task SaveRunAsync(GameSaveContainer container)
    {
        if (container == null) return;

        string path = GetFullPath(RUN_FILE_NAME);
        string plainJson = JsonUtility.ToJson(container, false);

        await Task.Run(() => WriteEncryptedBinary(path, plainJson));
        GameLogger.LogInfo("Partida encriptada y guardada en disco.");
    }
    public static async Task<GameSaveContainer> LoadRunAsync()
    {
        string path = GetFullPath(RUN_FILE_NAME);
        if (!File.Exists(path)) return null;

        try
        {
            string plainJson = await Task.Run(() => ReadEncryptedBinary(path));
            if (string.IsNullOrWhiteSpace(plainJson)) return null;

            return JsonUtility.FromJson<GameSaveContainer>(plainJson);
        }
        catch (Exception ex)
        {
            GameLogger.LogError($"Archivo de guardado corrupto o alterado: {ex.Message}");
            DeleteRunSave();
            return null;
        }
    }
    public static void DeleteRunSave()
    {
        try
        {
            string path = GetFullPath(RUN_FILE_NAME);
            if (File.Exists(path))
            {
                File.Delete(path);
                GameLogger.LogInfo("Permadeath/Fin: Archivo local de partida eliminado.");
            }
        }
        catch (Exception ex)
        {
            GameLogger.LogError($"Error al borrar archivo local: {ex.Message}");
        }
    }
    public static async Task SaveLocalFallenRemainAsync(FallenRemainRecord record)
    {
        string path = GetFullPath(FALLEN_FILE_NAME);
        FallenRemainsList list = await LoadLocalFallenRemainsAsync();
        list.records.Add(record);

        string json = JsonUtility.ToJson(list, false);
        await Task.Run(() => WriteEncryptedBinary(path, json));
    }
    public static async Task<FallenRemainsList> LoadLocalFallenRemainsAsync()
    {
        string path = GetFullPath(FALLEN_FILE_NAME);
        if (!File.Exists(path)) return new FallenRemainsList();

        try
        {
            string json = await Task.Run(() => ReadEncryptedBinary(path));
            return JsonUtility.FromJson<FallenRemainsList>(json) ?? new FallenRemainsList();
        }
        catch
        {
            return new FallenRemainsList();
        }
    }
    public static async Task EnqueuePendingSubmissionAsync(RunSubmissionPayload payload)
    {
        string path = GetFullPath(PENDING_FILE_NAME);
        PendingSubmissionsList list = await LoadPendingSubmissionsAsync();
        list.pendingRuns.Add(payload);

        string json = JsonUtility.ToJson(list, false);
        await Task.Run(() => WriteEncryptedBinary(path, json));
    }
    public static async Task<PendingSubmissionsList> LoadPendingSubmissionsAsync()
    {
        string path = GetFullPath(PENDING_FILE_NAME);
        if (!File.Exists(path)) return new PendingSubmissionsList();

        try
        {
            string json = await Task.Run(() => ReadEncryptedBinary(path));
            return JsonUtility.FromJson<PendingSubmissionsList>(json) ?? new PendingSubmissionsList();
        }
        catch
        {
            return new PendingSubmissionsList();
        }
    }
    public static void ClearPendingSubmissions()
    {
        string path = GetFullPath(PENDING_FILE_NAME);
        if (File.Exists(path)) File.Delete(path);
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