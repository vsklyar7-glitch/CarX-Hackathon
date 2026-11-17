using UnityEngine;
using UnityEngine.EventSystems;

// UV совпадают по рваным краям соседних фрагментов.
[RequireComponent(typeof(CanvasRenderer))]
public class PhotoPuzzlePiece : UnityEngine.UI.MaskableGraphic, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public int Index { get; private set; }
    public bool IsPlaced { get; private set; }
    private Sprite photo;
    private PhotoPuzzleView owner;
    private Vector2[] outline;
    private Vector2 grabOffset;
    public override Texture mainTexture => photo != null ? photo.texture : Texture2D.whiteTexture;

    public void Initialize(int index, Sprite sprite, PhotoPuzzleView view)
    {
        Index = index;
        photo = sprite;
        owner = view;
        outline = PhotoPuzzleLayout.Outline(index);
        raycastTarget = true;
        SetAllDirty();
    }

    public static Vector2[] Outline(int index) => PhotoPuzzleLayout.Outline(index);

    public static Vector2 Center(int index) => PhotoPuzzleLayout.Center(index);

    // Прозрачные участки вокруг неровного фрагмента пропускают мышь к нижним частям.
    public override bool Raycast(Vector2 screenPoint, Camera eventCamera)
    {
        if (!base.Raycast(screenPoint, eventCamera) || outline == null) return false;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPoint, eventCamera, out var local);
        Vector2 uv = Center(Index) + local / owner.PhotoSize;
        bool inside = false;
        for (int i = 0, j = outline.Length - 1; i < outline.Length; j = i++)
        {
            Vector2 a = outline[i], b = outline[j];
            if ((a.y > uv.y) != (b.y > uv.y) && uv.x < (b.x - a.x) * (uv.y - a.y) / (b.y - a.y) + a.x) inside = !inside;
        }
        return inside;
    }

    protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper mesh)
    {
        mesh.Clear();
        if (outline == null || photo == null) return;
        Vector2 center = Center(Index);
        Rect textureRect = photo.textureRect;
        Vector2 textureSize = new Vector2(photo.texture.width, photo.texture.height);
        for (int i = -1; i < outline.Length; i++)
        {
            Vector2 uv = i < 0 ? center : outline[i];
            Vector2 local = Vector2.Scale(uv - center, owner.PhotoSize);
            Vector2 textureUV = (textureRect.position + Vector2.Scale(uv, textureRect.size)) / textureSize;
            mesh.AddVert(local, color, textureUV);
        }
        for (int i = 0; i < outline.Length; i++) mesh.AddTriangle(0, (i + 1) % outline.Length + 1, i + 1);
    }

    public void OnBeginDrag(PointerEventData data)
    {
        if (IsPlaced || data.button != PointerEventData.InputButton.Left) return;
        transform.SetAsLastSibling();
        rectTransform.localRotation = Quaternion.identity;
        // BeginDrag вызывается уже после смещения мыши: берём место первоначального нажатия.
        RectTransformUtility.ScreenPointToLocalPointInRectangle(owner.Surface, data.pressPosition, data.pressEventCamera, out var point);
        grabOffset = rectTransform.anchoredPosition - point;
    }

    public void OnDrag(PointerEventData data)
    {
        if (IsPlaced || data.button != PointerEventData.InputButton.Left) return;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(owner.Surface, data.position, data.pressEventCamera, out var point);
        Vector2 half = owner.Surface.rect.size * .5f - rectTransform.sizeDelta * .5f;
        Vector2 position = point + grabOffset;
        rectTransform.anchoredPosition = new Vector2(Mathf.Clamp(position.x, -half.x, half.x), Mathf.Clamp(position.y, -half.y, half.y));
    }

    public void OnEndDrag(PointerEventData data)
    {
        if (!IsPlaced && data.button == PointerEventData.InputButton.Left) owner.TryPlace(this);
    }

    public void Place(Vector2 position)
    {
        rectTransform.anchoredPosition = position;
        rectTransform.localRotation = Quaternion.identity;
        IsPlaced = true;
        raycastTarget = false;
        transform.SetAsFirstSibling();
    }
}
