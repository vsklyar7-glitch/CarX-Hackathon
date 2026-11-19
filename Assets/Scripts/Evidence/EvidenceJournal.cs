using System;
using System.Collections.Generic;
using UnityEngine;

// Только прогресс текущего запуска. Храним данные, а не ссылки на объекты сцены.
public class EvidenceJournal : MonoBehaviour
{
    public static EvidenceJournal Instance { get; private set; }
    public event Action<string> EvidenceAdded;
    public event Action PhotoRestored;
    public event Action StationMailRead;
    public bool FamilyPhotoRestored { get; private set; }
    public bool HasReadStationMail { get; private set; }

    public void ReadStationMail()
    {
        if (HasReadStationMail) return;
        HasReadStationMail = true;
        StationMailRead?.Invoke();
    }

    public void RestoreFamilyPhoto()
    {
        if (FamilyPhotoRestored) return;
        FamilyPhotoRestored = true;
        PhotoRestored?.Invoke();
    }
    private readonly Dictionary<string, string> entries = new Dictionary<string, string>();
    // Снимки данных остаются доступны после выгрузки сцены с предметом.
    private readonly List<Entry> pages = new List<Entry>();
    public IReadOnlyList<Entry> Pages => pages;
    [Serializable]
    public class Entry
    {
        public string id;
        public string title;
        public string description;
        public Sprite image;
    }
    public int Count => entries.Count;
    public int ApartmentDay { get; private set; } = 1;
    public IEnumerable<string> Titles => entries.Values;

    public static EvidenceJournal GetOrCreate()
    {
        if (Instance == null) new GameObject("EvidenceJournal").AddComponent<EvidenceJournal>();
        return Instance;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public bool Add(string id, string title, string description = "", Sprite image = null)
    {
        if (string.IsNullOrWhiteSpace(id) || entries.ContainsKey(id)) return false;
        entries.Add(id, title);
        pages.Add(new Entry { id = id, title = title, description = description, image = image });
        EvidenceAdded?.Invoke(id);
        return true;
    }

    public bool Contains(string id) => entries.ContainsKey(id);

    public void SetApartmentDay(int day) => ApartmentDay = Mathf.Clamp(day, 1, 3);

    public bool Add(EvidenceItem item)
    {
        return Add(item.evidenceId, item.title,
            string.IsNullOrWhiteSpace(item.notebookDescription) ? item.description : item.notebookDescription, item.image);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
