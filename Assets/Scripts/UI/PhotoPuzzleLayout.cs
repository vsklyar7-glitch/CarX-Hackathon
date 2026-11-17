using System.Collections.Generic;
using UnityEngine;

// Семь разных многоугольников. Общие края используют одинаковую линию разрыва.
public static class PhotoPuzzleLayout
{
    public const int Count = 7;
    private static readonly Vector2 A = new Vector2(0, 0), B = new Vector2(.38f, 0), C = new Vector2(.74f, 0), D = new Vector2(1, 0);
    private static readonly Vector2 E = new Vector2(1, .5f), F = new Vector2(1, 1), G = new Vector2(.64f, 1), H = new Vector2(.25f, 1), I = new Vector2(0, 1), J = new Vector2(0, .48f);
    private static readonly Vector2 P = new Vector2(.28f, .35f), Q = new Vector2(.66f, .42f), R = new Vector2(.52f, .73f);
    private static readonly Vector2[][] corners = {
        new[] { A, B, P, J }, new[] { B, C, Q, P }, new[] { C, D, E, Q },
        new[] { J, P, Q, R }, new[] { Q, E, F, G, R }, new[] { R, G, H }, new[] { J, R, H, I }
    };

    public static Vector2 Center(int index)
    {
        Vector2 sum = Vector2.zero;
        foreach (Vector2 corner in corners[index]) sum += corner;
        return sum / corners[index].Length;
    }

    public static Vector2[] Outline(int index)
    {
        var points = new List<Vector2>();
        var polygon = corners[index];
        for (int edge = 0; edge < polygon.Length; edge++)
        {
            Vector2 a = polygon[edge], b = polygon[(edge + 1) % polygon.Length];
            bool outer = a.x == b.x && (a.x == 0 || a.x == 1) || a.y == b.y && (a.y == 0 || a.y == 1);
            if (outer) { points.Add(a); continue; }
            bool reverse = a.x > b.x || a.x == b.x && a.y > b.y;
            Vector2 start = reverse ? b : a, end = reverse ? a : b;
            Vector2 direction = end - start;
            Vector2 normal = new Vector2(-direction.y, direction.x).normalized;
            int seed = Mathf.RoundToInt(start.x * 1009 + start.y * 2027 + end.x * 4001 + end.y * 8009);
            var random = new System.Random(seed);
            var seam = new Vector2[7];
            seam[0] = start; seam[6] = end;
            for (int k = 1; k < 6; k++)
            {
                float t = k / 6f;
                float bend = Mathf.Sin(t * Mathf.PI) * .012f;
                float roughness = ((float)random.NextDouble() - .5f) * .022f;
                seam[k] = Vector2.Lerp(start, end, t) + normal * (bend + roughness);
            }
            for (int k = 0; k < 6; k++) points.Add(seam[reverse ? 6 - k : k]);
        }
        return points.ToArray();
    }

    public static Vector2 Size(int index, Vector2 photoSize)
    {
        Vector2 center = Center(index), half = Vector2.zero;
        foreach (Vector2 uv in Outline(index))
        {
            Vector2 delta = Vector2.Scale(uv - center, photoSize);
            half = Vector2.Max(half, new Vector2(Mathf.Abs(delta.x), Mathf.Abs(delta.y)));
        }
        return half * 2 + Vector2.one * 4;
    }
}
