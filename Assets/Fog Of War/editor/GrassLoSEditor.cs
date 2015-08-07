using UnityEngine;
using System.Collections;
using UnityEditor;

[CustomEditor(typeof(GrassLoSBlock))]
public class GrassLoSEditor : Editor
{
    private BoxCollider _box;
    private GrassLoSBlock _target;

    void OnEnable()
    {
        _target = target as GrassLoSBlock;

        _box = _target.GetComponent<BoxCollider>();
    }

    void OnSceneGUI()
    {
        var posOfBox = _box.transform.position + _box.center;
        var size = _box.size;
        var center = _box.center;
        Vector3[] verts = new[]
        {
            new Vector3(posOfBox.x - size.x*.5f, 1, posOfBox.z),
            new Vector3(posOfBox.x,              1, posOfBox.z + size.z*.5f),
            new Vector3(posOfBox.x + size.x*.5f, 1, posOfBox.z),
            new Vector3(posOfBox.x,              1, posOfBox.z - size.z*.5f)
        };


        var x = DoResizeHandle(_box.size.x, verts[2], Vector3.right);
        if (x % 3 != 0)
            x = Mathf.FloorToInt(_box.size.x);

        center.x = x * .5f;

        var z = DoResizeHandle(_box.size.z, verts[1], Vector3.forward);
        if (z % 3 != 0)
            z = Mathf.FloorToInt(_box.size.z);
        center.z = z * .5f;

        verts[0].z -= _box.size.z * .5f;
        verts[1].x -= _box.size.x * .5f;
        verts[2].z += _box.size.z * .5f;
        verts[3].x += _box.size.x * .5f;

        _box.size = new Vector3(
            Mathf.Clamp(x, 3, x),
            1, Mathf.Clamp(z, 3, z));
        _box.center = center;

        Handles.DrawSolidRectangleWithOutline(verts, Color.green * .5f, Color.yellow);
    }

    int DoResizeHandle(float startVal, Vector3 edge, Vector3 direction)
    {
        return Mathf.FloorToInt(Handles.ScaleSlider(startVal, edge, direction, Quaternion.identity, 
                                        2, HandleUtility.GetHandleSize(_target.transform.position)));
    }
}
