using UnityEngine;
using System;
using UnityEngine.Localization;
[Serializable] public struct DialogueLine
{
    public LocalizedString SpeakerName;
    public LocalizedString DialogueText;
}
[CreateAssetMenu(fileName = "New Dialogue Sequence", menuName = "Rotbound/Dialogue Sequence")]
public class DialogueSequence : ScriptableObject
{
    public DialogueLine[] Lines;
}
