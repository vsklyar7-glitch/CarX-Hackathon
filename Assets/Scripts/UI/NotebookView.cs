using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class NotebookView : MonoBehaviour
{
    public GameObject panel;
    public TMP_Text heading;
    public TMP_Text body;
    public TMP_Text pageNumber;
    public TMP_Text evidenceCount;
    public UnityEngine.UI.Image illustration;
    public UnityEngine.UI.Button previous;
    public UnityEngine.UI.Button next;
    public UnityEngine.UI.Button close;
    public RectTransform turningLeaf;
    public RectTransform leftPaper;
    public RectTransform rightPaper;
    public RectTransform[] leftPageContent;
    public RectTransform[] rightPageContent;
    [Range(0.15f, 1f)] public float turnDuration = 0.38f;
    public int PageIndex { get; private set; }
    public bool IsTurning { get; private set; }
    private EvidenceJournal journal;
    private Action onClose;
    private Coroutine animationRoutine;
    private readonly List<GameObject> pageSnapshots = new List<GameObject>();

    public void Initialize(Action closeAction)
    {
        onClose = closeAction;
        previous.onClick.AddListener(Previous);
        next.onClick.AddListener(Next);
        close.onClick.AddListener(RequestClose);
        panel.SetActive(false);
        turningLeaf.gameObject.SetActive(false);
    }

    public void Open(EvidenceJournal evidenceJournal)
    {
        journal = evidenceJournal;
        journal.EvidenceAdded += RefreshForNewEvidence;
        PageIndex = Mathf.Clamp(PageIndex, 0, Mathf.Max(0, journal.Count - 1));
        panel.SetActive(true);
        RefreshPage();
    }

    public void Close()
    {
        if (journal != null) journal.EvidenceAdded -= RefreshForNewEvidence;
        if (animationRoutine != null) StopCoroutine(animationRoutine);
        animationRoutine = null;
        ClearPageSnapshots();
        IsTurning = false;
        turningLeaf.gameObject.SetActive(false);
        panel.SetActive(false);
    }

    public void Turn(int direction)
    {
        if (!panel.activeSelf || IsTurning || journal == null || direction == 0) return;
        int target = PageIndex + (direction > 0 ? 1 : -1);
        if (target < 0 || target >= journal.Count) return;
        animationRoutine = StartCoroutine(AnimateTurn(target, direction));
    }

    private IEnumerator AnimateTurn(int target, int direction)
    {
        IsTurning = true;
        UpdateButtons();
        Canvas.ForceUpdateCanvases();
        bool forward = direction > 0;
        // Сохраняем обе старые страницы, затем готовим новый разворот под ними.
        RectTransform front = CapturePage(forward ? rightPaper : leftPaper,
            forward ? rightPageContent : leftPageContent, "TurningFront");
        RectTransform stationary = CapturePage(forward ? leftPaper : rightPaper,
            forward ? leftPageContent : rightPageContent, "OutgoingStationary");
        PageIndex = target;
        RefreshPage();
        Canvas.ForceUpdateCanvases();
        RectTransform back = CapturePage(forward ? leftPaper : rightPaper,
            forward ? leftPageContent : rightPageContent, "TurningBack");
        RectTransform paper = forward ? rightPaper : leftPaper;
        turningLeaf.anchorMin = paper.anchorMin;
        turningLeaf.anchorMax = paper.anchorMax;
        // Ось вращения всегда у корешка, с нужной стороны листа.
        turningLeaf.pivot = new Vector2(forward ? 0 : 1, 0.5f);
        turningLeaf.offsetMin = Vector2.zero;
        turningLeaf.offsetMax = Vector2.zero;
        turningLeaf.localRotation = Quaternion.identity;
        PutOnLeaf(front, false);
        PutOnLeaf(back, true);
        back.gameObject.SetActive(false);
        turningLeaf.SetAsLastSibling();
        turningLeaf.gameObject.SetActive(true);
        float elapsed = 0;
        while (elapsed < turnDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / turnDuration);
            float smooth = Mathf.SmoothStep(0, 1, progress);
            turningLeaf.localRotation = Quaternion.Euler(0, (forward ? -180 : 180) * smooth, 0);
            bool frontVisible = progress < 0.5f;
            front.gameObject.SetActive(frontVisible);
            back.gameObject.SetActive(!frontVisible);
            // Противоположная старая страница остаётся под листом до конца,
            // чтобы новая картинка не дублировалась под собственной копией.
            yield return null;
        }
        PageIndex = target;
        turningLeaf.gameObject.SetActive(false);
        ClearPageSnapshots();
        IsTurning = false;
        animationRoutine = null;
        RefreshPage();
    }

    // Копируются UI-элементы, а не только фон листа: текст и спрайты остаются
    // частью страницы. Кнопки не копируются, поэтому обработчики не дублируются.
    private RectTransform CapturePage(RectTransform paper, RectTransform[] content, string name)
    {
        RectTransform book = (RectTransform)turningLeaf.parent;
        RectTransform snapshot = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image)).GetComponent<RectTransform>();
        snapshot.SetParent(book, false);
        snapshot.anchorMin = paper.anchorMin;
        snapshot.anchorMax = paper.anchorMax;
        snapshot.offsetMin = paper.offsetMin;
        snapshot.offsetMax = paper.offsetMax;
        var background = snapshot.GetComponent<UnityEngine.UI.Image>();
        background.color = paper.GetComponent<UnityEngine.UI.Image>().color;
        background.raycastTarget = false;
        pageSnapshots.Add(snapshot.gameObject);
        Vector2 span = paper.anchorMax - paper.anchorMin;
        foreach (RectTransform source in content)
        {
            if (source == null) continue;
            RectTransform copy = Instantiate(source.gameObject, snapshot, false).GetComponent<RectTransform>();
            copy.anchorMin = new Vector2((source.anchorMin.x - paper.anchorMin.x) / span.x,
                (source.anchorMin.y - paper.anchorMin.y) / span.y);
            copy.anchorMax = new Vector2((source.anchorMax.x - paper.anchorMin.x) / span.x,
                (source.anchorMax.y - paper.anchorMin.y) / span.y);
            copy.offsetMin = source.offsetMin;
            copy.offsetMax = source.offsetMax;
            foreach (var graphic in copy.GetComponentsInChildren<UnityEngine.UI.Graphic>(true)) graphic.raycastTarget = false;
        }
        return snapshot;
    }

    private void PutOnLeaf(RectTransform page, bool reverse)
    {
        page.SetParent(turningLeaf, false);
        page.anchorMin = Vector2.zero;
        page.anchorMax = Vector2.one;
        page.offsetMin = Vector2.zero;
        page.offsetMax = Vector2.zero;
        // Компенсация поворота задней стороны предотвращает зеркальный текст.
        page.localRotation = Quaternion.Euler(0, reverse ? 180 : 0, 0);
    }

    private void ClearPageSnapshots()
    {
        foreach (GameObject snapshot in pageSnapshots)
        {
            if (snapshot == null) continue;
            snapshot.SetActive(false);
            Destroy(snapshot);
        }
        pageSnapshots.Clear();
        if (turningLeaf != null) turningLeaf.localRotation = Quaternion.identity;
    }

    private void RefreshForNewEvidence(string id) { if (!IsTurning) RefreshPage(); }
    private void RefreshPage()
    {
        int count = journal.Count;
        evidenceCount.text = "Найдено улик: " + count;
        if (count == 0)
        {
            heading.text = "Дело ещё не раскрыто";
            body.text = "Пока здесь нет записей.\n\nОсматривайте предметы клавишей E. Найденные улики появятся в блокноте вместе с краткими заметками.";
            pageNumber.text = "Нет записей";
            illustration.gameObject.SetActive(false);
        }
        else
        {
            PageIndex = Mathf.Clamp(PageIndex, 0, count - 1);
            EvidenceJournal.Entry entry = journal.Pages[PageIndex];
            heading.text = entry.title;
            body.text = string.IsNullOrWhiteSpace(entry.description) ? "Улика найдена. Описание пока не заполнено." : entry.description;
            pageNumber.text = "Запись " + (PageIndex + 1) + " / " + count;
            illustration.sprite = entry.image != null ? entry.image : Resources.Load<Sprite>("NotebookArt/" + entry.id);
            illustration.gameObject.SetActive(illustration.sprite != null);
        }
        UpdateButtons();
    }

    private void UpdateButtons()
    {
        previous.interactable = !IsTurning && journal != null && PageIndex > 0;
        next.interactable = !IsTurning && journal != null && PageIndex + 1 < journal.Count;
    }
    private void Previous() { Turn(-1); }
    private void Next() { Turn(1); }
    private void RequestClose() { onClose?.Invoke(); }
    private void OnDestroy()
    {
        ClearPageSnapshots();
        if (journal != null) journal.EvidenceAdded -= RefreshForNewEvidence;
        previous.onClick.RemoveListener(Previous);
        next.onClick.RemoveListener(Next);
        close.onClick.RemoveListener(RequestClose);
    }
}
