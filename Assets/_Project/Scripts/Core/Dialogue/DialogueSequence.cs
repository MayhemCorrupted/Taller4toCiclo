using UnityEngine;
using System;
using UnityEngine.Localization;
[Serializable] public struct DialogueLine
{
    public LocalizedString SpeakerName;
    public LocalizedString DialogueText;
    public Sprite SpeakerSprite;
    [Range(0, 0.1f)] public float CustomTextSpeed;
}
[Serializable] public struct DialogueOption
{
    public LocalizedString OptionText;
    public DialogueSequence NextSequence;
}
[CreateAssetMenu(fileName = "New Dialogue Sequence", menuName = "Rotbound/Dialogue Sequence")]
public class DialogueSequence : ScriptableObject
{
    public DialogueLine[] Lines;
    public DialogueOption[] Options;
}