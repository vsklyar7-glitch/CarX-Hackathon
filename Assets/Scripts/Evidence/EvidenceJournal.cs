using System;
using System.Collections.Generic;
using UnityEngine;

// Только прогресс текущего запуска. Храним данные, а не ссылки на объекты сцены.
public class EvidenceJournal : MonoBehaviour
{
    public static EvidenceJournal Instance { get; private set; }
    public event Action<string> EvidenceAdded;
    private readonly Dictionary<string, string> entries = new Dictionary<string, string>();
    public int Count => entries.Count;
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

    public bool Add(string id, string title)
    {
        if (string.IsNullOrWhiteSpace(id) || entries.ContainsKey(id)) return false;
        entries.Add(id, title);
        EvidenceAdded?.Invoke(id);
        return true;
    }

    public bool Contains(string id) => entries.ContainsKey(id);

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
