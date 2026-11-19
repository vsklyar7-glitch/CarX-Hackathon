using TMPro;
using UnityEngine;

public class StationMailComputer : MonoBehaviour, IInteractable
{
    [SerializeField] private GameObject mailPanel;
    [SerializeField] private TMP_Text monitorStatus;
    private EvidenceJournal journal;

    public string Prompt => "E — открыть почту";
    public bool CanOpen => isActiveAndEnabled && mailPanel != null;

    private void Start()
    {
        journal = EvidenceJournal.GetOrCreate();
        journal.StationMailRead += RefreshMonitor;
        RefreshMonitor();
    }

    public void Interact(PlayerInteraction player) => player.InspectMail(this);

    public void Show()
    {
        mailPanel.SetActive(true);
        EvidenceJournal.GetOrCreate().ReadStationMail();
    }

    public void Hide()
    {
        if (mailPanel != null) mailPanel.SetActive(false);
    }

    private void RefreshMonitor()
    {
        if (monitorStatus == null) return;
        monitorStatus.text = journal.HasReadStationMail ? "INBOX  /  0 NEW" : "NEW MAIL  /  1 NEW";
        monitorStatus.color = journal.HasReadStationMail ? new Color32(149, 170, 184, 255) : new Color32(255, 204, 94, 255);
    }

    private void OnDestroy()
    {
        if (journal != null) journal.StationMailRead -= RefreshMonitor;
    }
}
