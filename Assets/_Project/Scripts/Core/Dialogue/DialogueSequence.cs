using System;
using UnityEngine;
using UnityEngine.Localization;
[Serializable] public struct DialogueLine
{
    public LocalizedString SpeakerName;
    public LocalizedString DialogueText;
    public Sprite SpeakerSprite;
    [Range(0, 0.5f)] public float CustomTextSpeed;
    [Header("Eventos de Línea")]
    public string LineEndEventID;
}
[Serializable] public struct DialogueOption
{
    public LocalizedString OptionText;
    public DialogueSequence NextSequence;
}
[CreateAssetMenu(fileName = "New Dialogue Sequence", menuName = "Rotbound/Dialogue Sequence")]
public class DialogueSequence : ScriptableObject
{
    [Header("Estilo de UI (Opcional)")]
    [Tooltip("Dejar vacío para la UI por defecto, o escribir el ID de DialogueUI (ej: 'Pensamiento', 'Cartel').")]
    public string id;
    [Header("Contenido")]
    public DialogueLine[] Lines;
    public DialogueOption[] Options;
    public string SequenceEndEventID;
}