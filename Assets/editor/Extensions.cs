using UnityEngine;
using System.Collections;

public static class Extensions
{
    public static Vector2 Vector3ToVec2(this Vector3 vec3)
    {
        return new Vector2(vec3.x, vec3.z);
    }
}
