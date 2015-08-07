using UnityEngine;
using System.Collections;
using UnityEditor;
using Fog;

namespace Fog
{
    [CustomEditor(typeof(FogOfWarRevealer))]
    public class FogOfWarRevealerEditor : Editor
    {
        private SerializedProperty _range;
        private FogOfWarRevealer _target;

    void OnEnable()
        {
            _target = target as FogOfWarRevealer;

            _range = serializedObject.FindProperty("_viewRadius");
        }

        void OnSceneGUI()
        { 
            HelperMethods.DrawCircleWithOutline(_target.transform.position, Vector3.up, _range.floatValue, Color.white, Color.blue);
        }
    }

}
