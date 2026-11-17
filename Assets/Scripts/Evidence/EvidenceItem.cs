using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class EvidenceItem : MonoBehaviour, IInteractable
{
    public string evidenceId;
    public string title;
    [TextArea(3, 10)] public string description;
    [TextArea(2, 5)] public string notebookDescription;
    public Sprite image;
    public bool requiresPhotoAssembly;
    public GameObject tornPhotoVisual;
    public GameObject intactPhotoVisual;

    public bool NeedsAssembly => requiresPhotoAssembly && !EvidenceJournal.GetOrCreate().FamilyPhotoRestored;

    private void OnEnable()
    {
        if (!requiresPhotoAssembly) return;
        EvidenceJournal.GetOrCreate().PhotoRestored += RefreshPhoto;
        RefreshPhoto();
    }

    private void OnDisable()
    {
        if (EvidenceJournal.Instance != null) EvidenceJournal.Instance.PhotoRestored -= RefreshPhoto;
    }

    private void RefreshPhoto()
    {
        bool restored = EvidenceJournal.GetOrCreate().FamilyPhotoRestored;
        if (tornPhotoVisual != null) tornPhotoVisual.SetActive(!restored);
        if (intactPhotoVisual != null) intactPhotoVisual.SetActive(restored);
    }

    public string Prompt => NeedsAssembly ? "E — Собрать порванную фотографию" : "E — Осмотреть: " + title;

    public void Interact(PlayerInteraction player)
    {
        player.Inspect(this);
    }
}
