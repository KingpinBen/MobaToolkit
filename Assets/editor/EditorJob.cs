using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Collections.Generic;

public class EditorJob<T> 
{
    protected readonly IEnumerator<T> _coroutine;
    protected bool _inProgress;

    protected EditorJob(IEnumerator<T> coroutine)
    {
        _coroutine = coroutine; 
        EditorApplication.update += Update;
        _inProgress = true;
    }

    public virtual void Stop()
    {
        _inProgress = false;
        EditorApplication.update -= Update;
    }

    protected virtual void Update()
    {
        if (!_coroutine.MoveNext())
            Stop();
    }

    public bool Finished
    {
        get { return !_inProgress; }
    }

    public static EditorJob<T> Start(IEnumerator<T> ienumerator)
    {
        return new EditorJob<T>(ienumerator);
    }
}

public sealed class EditorProgressJob : EditorJob<float>
{
    private float _progressResult;

    private EditorProgressJob(IEnumerator<float> coroutine)
        : base(coroutine)
    {
        _progressResult = 0;
    }

    public override void Stop()
    {
        base.Stop();
        _progressResult = 1.0f;
    }

    protected override void Update()
    {
        base.Update();

        if (_inProgress)
            _progressResult = _coroutine.Current;
    }

    public float Progress
    {
        get { return _progressResult; }
    }

    public static new EditorProgressJob Start(IEnumerator<float> ienumerator)
    {
        return new EditorProgressJob(ienumerator);
    }
}
