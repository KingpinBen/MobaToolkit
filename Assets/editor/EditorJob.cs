using UnityEngine;
using System.Collections;
using UnityEditor;

public sealed class EditorJob
{
    private readonly IEnumerator _coroutine;
    private bool _inProgress;

    private EditorJob(IEnumerator coroutine)
    {
        _coroutine = coroutine; 
        EditorApplication.update += Update;
        _inProgress = true;
    }

    private void Stop()
    {
        EditorApplication.update -= Update;
        _inProgress = false;
    }

    private void Update()
    {
        if (!_coroutine.MoveNext())
            Stop();
    }

    public bool Finished
    {
        get { return !_inProgress; }
    }

    public static EditorJob Start(IEnumerator ienumerator)
    {
        return new EditorJob(ienumerator);
    }
}
