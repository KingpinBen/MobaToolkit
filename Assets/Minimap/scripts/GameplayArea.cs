using UnityEngine;
using System.Collections;

public sealed class GameplayArea : MonoBehaviour
{
    [SerializeField]
    private float _size = 10.0f;

    [SerializeField]
    private Color _backgroundColor;

    private void Awake()
    {
        Shader.SetGlobalFloat("_FogAreaSize", _size);
        Shader.SetGlobalVector("_FogObjectWorldPosition", transform.position);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Vector3 pos = transform.position;

        Gizmos.DrawWireCube(new Vector3(pos.x, 2.5f, pos.z), new Vector3(_size, 5, _size));
    }

    public float Size
    {
        get { return _size; }
    }
}
