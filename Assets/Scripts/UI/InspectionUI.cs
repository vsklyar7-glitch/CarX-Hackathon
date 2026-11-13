using System;
using System.Text;
using TMPro;
using UnityEngine;

public class InspectionUI : MonoBehaviour
{
    public TMP_Text prompt;
    public GameObject panel;
    public TMP_Text title;
    public TMP_Text description;
    public TMP_Text status;
    public TMP_Text journal;
    public UnityEngine.UI.Image image;
    public UnityEngine.UI.Button closeButton;
    private Action onClose;

    private void Awake()
    {
        closeButton.onClick.AddListener(Close);
        Hide();
    }

    public void SetPrompt(string text) { if (prompt != null) prompt.text = text; }

    public void Show(EvidenceItem item, EvidenceJournal evidenceJournal, bool added, Action close)
    {
        onClose = close;
        SetPrompt("");
        title.text = item.title;
        description.text = item.description;
        RectTransform descriptionRect = description.rectTransform;
        descriptionRect.anchorMax = new Vector2(item.image != null ? 0.46f : 0.66f, descriptionRect.anchorMax.y);
        status.text = added ? "Улика добавлена в блокнот" : "Улика уже есть в блокноте";
        image.sprite = item.image;
        image.gameObject.SetActive(item.image != null);
        StringBuilder entries = new StringBuilder("БЛОКНОТ\n\n");
        foreach (string entry in evidenceJournal.Titles) entries.Append("• ").AppendLine(entry);
        journal.text = entries.ToString();
        panel.SetActive(true);
    }

    public void Hide() { panel.SetActive(false); onClose = null; }
    private void Close() { onClose?.Invoke(); }
    private void OnDestroy() { if (closeButton != null) closeButton.onClick.RemoveListener(Close); }
}
