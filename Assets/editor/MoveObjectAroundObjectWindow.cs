using UnityEngine;
using System.Collections;
using UnityEditor;

public sealed class MoveObjectAroundObjectWindow : EditorWindow 
{
    private static readonly GUIContent _titleContent = new GUIContent("Rotation Helper");

    private Transform _movingObject;
    private Transform _objectToRotateAround;
    private int _delta;
    private float _distanceFromRotationObject;
    private bool _liveMovement;
    private RotationHandling _lookAtRotation;

    [MenuItem("MOBA/Rotation Helper Window")]
    static void Initialize()
    {
        MoveObjectAroundObjectWindow window = GetWindow<MoveObjectAroundObjectWindow>();
        window.titleContent = _titleContent;
        window.Show();
    }

    void OnGUI()
    {
        _liveMovement = EditorGUILayout.Toggle("Live Preview",_liveMovement);
        EditorGUILayout.Space();

        _movingObject = EditorGUILayout.ObjectField("Object To Move", _movingObject, typeof(Transform), true) as Transform;
        _objectToRotateAround = EditorGUILayout.ObjectField("Object To Rotate Around", _objectToRotateAround, typeof(Transform), true) as Transform;

        int initialDelta = _delta;
        float initialDistance = _distanceFromRotationObject;
            
        _delta = (int)EditorGUILayout.Slider(_delta, 0, 359);
        _distanceFromRotationObject = EditorGUILayout.FloatField("Distance From Object", _distanceFromRotationObject);
        _lookAtRotation = (RotationHandling)EditorGUILayout.EnumPopup("Rotate", _lookAtRotation);
        
        if (_liveMovement)
        {
            if (initialDelta != _delta ||
                initialDistance != _distanceFromRotationObject)
                UpdateMovingObjectPosition();
        }
        else
        {
            if (GUILayout.Button("Update"))
                UpdateMovingObjectPosition();
        }
    }

    void UpdateMovingObjectPosition()
    {
        Vector3 newPosition = Vector3.zero;

        newPosition.x = Mathf.Sin(_delta * Mathf.Deg2Rad) * _distanceFromRotationObject;
        newPosition.z = Mathf.Cos(_delta * Mathf.Deg2Rad) * _distanceFromRotationObject;

        newPosition += _objectToRotateAround.position;

        _movingObject.position = newPosition;

        switch (_lookAtRotation)
        {
            default:
                break;
            case RotationHandling.LookAtOrigin:
                Quaternion lookAt = Quaternion.LookRotation(_objectToRotateAround.position - newPosition);
                _movingObject.rotation = lookAt;
                break;
            case RotationHandling.LookAwayFromOrigin:
                Quaternion lookAway = Quaternion.LookRotation(newPosition - _objectToRotateAround.position);
                _movingObject.rotation = lookAway;
                break;
        }
    }

    private enum RotationHandling
    {
        Ignore,
        LookAtOrigin,
        LookAwayFromOrigin
    }
}
