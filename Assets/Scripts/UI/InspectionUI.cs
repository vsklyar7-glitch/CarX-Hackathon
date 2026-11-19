using System;
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
    private CaseFileView caseFile;

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
        if (item.evidenceId == "case_file")
        {
            if (caseFile == null) caseFile = gameObject.AddComponent<CaseFileView>();
            caseFile.Show(title.font, close);
            return;
        }
        title.text = item.title;
        description.text = item.description;
        Sprite illustration = item.image != null ? item.image : Resources.Load<Sprite>("NotebookArt/" + item.evidenceId);
        RectTransform descriptionRect = description.rectTransform;
        descriptionRect.anchorMin = new Vector2(illustration != null ? 0.42f : 0.02f, 0.29f);
        descriptionRect.anchorMax = new Vector2(0.96f, 0.80f);
        title.rectTransform.anchorMax = new Vector2(0.96f, title.rectTransform.anchorMax.y);
        description.enableAutoSizing = true;
        description.fontSizeMin = 18;
        description.fontSizeMax = 25;
        status.text = (added ? "Улика добавлена в блокнот" : "Улика уже есть в блокноте") + "   ·   J — открыть блокнот";
        status.rectTransform.anchorMax = new Vector2(0.96f, status.rectTransform.anchorMax.y);
        image.sprite = illustration;
        image.preserveAspect = true;
        image.raycastTarget = false;
        image.rectTransform.anchorMin = new Vector2(0.03f, 0.30f);
        image.rectTransform.anchorMax = new Vector2(0.38f, 0.78f);
        image.rectTransform.offsetMin = Vector2.zero;
        image.rectTransform.offsetMax = Vector2.zero;
        image.gameObject.SetActive(illustration != null);
        // Ссылка сохранена для совместимости с уже размещёнными префабами.
        if (journal != null) journal.gameObject.SetActive(false);
        panel.SetActive(true);
    }

    public void Hide() { panel.SetActive(false); if (caseFile != null) caseFile.Hide(); onClose = null; }
    private void Close() { onClose?.Invoke(); }
    private void OnDestroy() { if (closeButton != null) closeButton.onClick.RemoveListener(Close); }
}
