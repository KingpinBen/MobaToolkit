using UnityEngine;
using System.Collections;

public sealed class FogDataAsset : ScriptableObject
{
    [SerializeField]
    private Fog.FogOfWarProbeData[] _probeData;

    public Fog.FogOfWarProbeData[] Data
    {
        get { return _probeData; }
    }

    public int Count
    {
        get { return _probeData.Length; }
    }

#if UNITY_EDITOR
    public void SetData(Fog.FogOfWarProbeData[] newData)
    {
        Debug.Assert(newData != null);
        
        _probeData = newData;
    }
#endif
}
