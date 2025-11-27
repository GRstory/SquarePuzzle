using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

[RequireComponent(typeof(CanvasRenderer))]
public class UILineRenderer : Graphic
{
    public Vector2[] Points;
    public float LineThickness = 2f;
    public bool relativeSize = false;

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        if (Points == null || Points.Length < 2)
            return;

        float angle = 0;

        for (int i = 0; i < Points.Length - 1; i++)
        {
            Vector2 point = Points[i];
            Vector2 point2 = Points[i + 1];

            if (i < Points.Length - 1)
                angle = GetAngle(point, point2) + 90f;

            DrawVerticesForPoint(point, point2, angle, vh);
        }

        for (int i = 0; i < Points.Length - 1; i++)
        {
            int index = i * 4;
            vh.AddTriangle(index + 0, index + 1, index + 2);
            vh.AddTriangle(index + 1, index + 2, index + 3);
        }
    }

    private void DrawVerticesForPoint(Vector2 point, Vector2 point2, float angle, VertexHelper vh)
    {
        UIVertex vertex = UIVertex.simpleVert;
        vertex.color = color;

        vertex.position = Quaternion.Euler(0, 0, angle) * new Vector3(-LineThickness / 2, 0);
        vertex.position += new Vector3(point.x, point.y);
        vh.AddVert(vertex);

        vertex.position = Quaternion.Euler(0, 0, angle) * new Vector3(LineThickness / 2, 0);
        vertex.position += new Vector3(point.x, point.y);
        vh.AddVert(vertex);

        vertex.position = Quaternion.Euler(0, 0, angle) * new Vector3(-LineThickness / 2, 0);
        vertex.position += new Vector3(point2.x, point2.y);
        vh.AddVert(vertex);

        vertex.position = Quaternion.Euler(0, 0, angle) * new Vector3(LineThickness / 2, 0);
        vertex.position += new Vector3(point2.x, point2.y);
        vh.AddVert(vertex);
    }

    private float GetAngle(Vector2 me, Vector2 target)
    {
        return (float)(Mathf.Atan2(target.y - me.y, target.x - me.x) * (180 / Mathf.PI));
    }
}
