using UnityEngine;
using System.Collections;

[RequireComponent(typeof(BoxCollider))]
public class LineOfSightBlocker : MonoBehaviour
{

	void Start ()
    {
        var s = GetComponent<BoxCollider>();
        s.isTrigger = true;
	}

    void OnDrawGizmos()
    {
        BoxCollider col = GetComponent<BoxCollider>();

        Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
        Gizmos.color = Color.yellow * .5f;
        Gizmos.DrawCube(col.center, col.size);
    }
}
