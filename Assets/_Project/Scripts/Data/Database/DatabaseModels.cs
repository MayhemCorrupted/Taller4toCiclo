using System;

[Serializable]
public class AuthResponse
{
    public bool success;
    public int user_id;
    public string username;
    public string message;
}

[Serializable]
public class CloudCharacterData
{
    public int character_id;
    public string nickname;
    public string save_json;
    public string result;
    public int play_time;
}

[Serializable]
public class CharacterListResponse
{
    public CloudCharacterData[] characters;
}

[Serializable]
public class CreateCharacterResponse
{
    public bool success;
    public int character_id;
    public string message;
}

[Serializable]
public class RemainsData
{
    public string nickname;
    public float pos_x;
    public float pos_y;
    public float pos_z;
}

[Serializable]
public class RemainsListResponse
{
    public RemainsData[] remains;
}

[Serializable]
public class RankingEntry
{
    public string username;
    public string nickname;
    public string result;
    public int score;
    public int play_time;
}

[Serializable]
public class RankingsResponse
{
    public RankingEntry[] conventional;
    public RankingEntry[] testers;
}