using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshVertex
{
    public int Id;
    public Vector3 Position;
    public Vector2 UV;
    public Vector2 UV2;

    public MeshVertex(Vector3 position, Vector2 uv, Vector2? uv2 = null)
    {
        Position = position;
        UV = uv;
        UV2 = uv2 == null ? new Vector2(0f, 0f) : new Vector2(uv2.Value.x, uv2.Value.y);
    }
}
