using UnityEngine;
using UnityEngine.Localization;

public abstract class ItemData : ScriptableObject
{
    public string ItemID;
    public Sprite Icon;
    public LocalizedString ItemName;
    public LocalizedString ItemDescription;
    public LocalizedString ItemEffect;
}
