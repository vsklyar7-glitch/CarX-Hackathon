using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class EvidenceItem : MonoBehaviour, IInteractable
{
    public string evidenceId;
    public string title;
    [TextArea(3, 10)] public string description;
    [TextArea(2, 5)] public string notebookDescription;
    public Sprite image;

    public string Prompt => "E — Осмотреть: " + title;

    public void Interact(PlayerInteraction player)
    {
        player.Inspect(this);
    }
}
