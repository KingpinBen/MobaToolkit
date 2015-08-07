using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Reflection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Fog
{
    [CustomEditor(typeof(FogOfWarManager))]
    public class FogOfWarManagerEditor : Editor
    {
        private SerializedProperty _probeMaskProperty;
        private SerializedProperty _ignoreMaskProperty;
        private SerializedProperty _assetProperty;

        private int _groundLayer;
        private int _LoSBlockLayer;
        private static int _debugIndex;
        private static bool _showDebug;
        private FogOfWarManager _target;
        private GameplayArea _gameplayArea;
        private FogDataAsset _dataAsset;

        private const float cSphereCastRadius = .1f;
        private const float cMaxProbeRayDistance = 50.0f;

        private void OnEnable()
        {
            _target = target as FogOfWarManager;
            

            _probeMaskProperty = serializedObject.FindProperty("probeableLayers");
            _ignoreMaskProperty = serializedObject.FindProperty("ignoredLayers");
            
            _gameplayArea   = FindObjectOfType<GameplayArea>();

            _groundLayer    = LayerMask.NameToLayer("Ground");
            _LoSBlockLayer  = LayerMask.NameToLayer("SightBlock");

            TryFindFogDataAsset();

        }

        public override void OnInspectorGUI()
        {
            //base.OnInspectorGUI();

            EditorGUILayout.PropertyField(_probeMaskProperty);
            EditorGUILayout.PropertyField(_ignoreMaskProperty);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(_assetProperty);
            EditorGUI.EndChangeCheck();

            if (GUI.changed)
                TryFindFogDataAsset();

            if (_dataAsset)
            {
                DrawDebugGUI();

                if (GUILayout.Button("Generate Vision Probes"))
                {
                    TryFindFogDataAsset();
                    _dataAsset.SetData(GenerateProbeData());

                    ////  Grab the data field from the target and set the new probe data (if any, otherwise null)
                    //var type = _target.GetType();
                    //var dataField = type.GetField("_data", BindingFlags.NonPublic | BindingFlags.Instance);

                    //dataField.SetValue(_target, probeData);

                    //  Force a clean up
                    GC.Collect();
                }
            }
            

            serializedObject.ApplyModifiedProperties();
        }

        private void FindProbeNeighbours(FogOfWarProbeData[] probeData)
        {
            Vector3 temp = Vector3.zero;
            Vector3 temp2 = Vector3.zero;
            RaycastHit hit;
            Ray ray = new Ray();

            //  We use a List here as clearing a BetterList doesn't probably remove
            //  visible probes from previously checked probes
            List<int> visibleNodes = new List<int>();
            int j;

            var testCollider = GameObject.CreatePrimitive(PrimitiveType.Sphere).GetComponent<SphereCollider>();
            testCollider.radius = .2f;

            //  We add a rigidbody as we may check through some triggers 
            //  which need to block line of sight
            testCollider.gameObject.AddComponent<Rigidbody>();

            for (int i = 0; i < probeData.Length; i++)
            {
                temp = new Vector3(probeData[i].x, 20, probeData[i].z);
                

                visibleNodes.Clear();

                /*  
                //  This is basically to just hit the ground to see where we can see from. 
                //  It's a nice thing to have as we're able to get probes that can look down over edges
                //  but not be able to look up, sort of like someone being able to look over a cliff.

                //  If you don't want this effect just remove/comment-out this ray cast
                */
                if (Physics.Raycast(temp, Vector3.down, out hit))
                {
                    temp.y  = hit.point.y + .1f;
                    temp2.y = hit.point.y + .1f;

                    ray.origin = temp;

                    for (j = 0; j < probeData.Length; j++)
                    {
                        //  Skip if it's checking itself
                        if (probeData[i].x == probeData[j].x && 
                            probeData[i].z == probeData[j].z)
                            continue;

                        temp2.x = probeData[j].x;
                        temp2.z = probeData[j].z;

                        /*  
                        //  The reason we move the collider instead of setting the position to the probe is 
                        //  so that the cast works properly when we encounter grass. This way, we can't look in
                        //  (unless inside the grass) but are able to look out
                        */
                        testCollider.transform.position = temp2;
                        ray.direction = (testCollider.transform.position - temp).normalized;

                        
                        if (Physics.Raycast(ray, out hit, cMaxProbeRayDistance, ~_target.ignoredLayers.value))
                        {
                            if (hit.collider == testCollider)
                                visibleNodes.Add(j);
                        }
                    }

                    probeData[i].visibleProbesIndices = visibleNodes.ToArray();
                }
            }

            DestroyImmediate(testCollider.gameObject);
        }

        private FogOfWarProbeData[] GenerateProbeData()
        {
            if (!_gameplayArea)
                throw new UnityException("FOGOFWARMANAGER::GENERATEPROBEDATA::NO_GAMEPLAY_AREA_SET");

            if (_groundLayer == -1)
                throw new UnityException("FOGOFWARMANAGER::GENERATEPROBEDATA::NO_GROUND_LAYER_EXISTS");

            if (_LoSBlockLayer == -1)
                throw new UnityException("FOGOFWARMANAGER::GENERATEPROBEDATA::NO_SIGHTBLOCK_LAYER_EXISTS");

            float x;
            RaycastHit hit;
            Vector3 rayOrigin = Vector3.zero;
            Dictionary<Point, FogOfWarProbeData> wData = new Dictionary<Point, FogOfWarProbeData>();
            var bottomLeftGameplayArea  = _gameplayArea.transform.position - (new Vector3(_gameplayArea.Size, 0, _gameplayArea.Size) * .5f);
            var topRightGameplayArea    = _gameplayArea.transform.position + (new Vector3(_gameplayArea.Size, 0, _gameplayArea.Size) * .5f);

            rayOrigin.y = 20;

            for (float z = bottomLeftGameplayArea.z; z < topRightGameplayArea.z; z += FogOfWarManager.cProbeSpacing)
            {
                for (x = bottomLeftGameplayArea.x; x < topRightGameplayArea.x; x += FogOfWarManager.cProbeSpacing)
                {
                    rayOrigin.x = x;
                    rayOrigin.z = z;

                    Point p;
                    FogOfWarManager.FindNearestPointVector3(rayOrigin, out p);

                    if (wData.ContainsKey(p))
                        continue;

                    if (Physics.SphereCast(rayOrigin, cSphereCastRadius, Vector3.down, out hit, _target.probeableLayers.value))
                    {
                        if (((1 << hit.transform.gameObject.layer) & _target.probeableLayers.value) != 0)
                        {
                            wData.Add(p, new FogOfWarProbeData(p));
                        }
                    }
                }
            }

            FogOfWarProbeData[] result = new FogOfWarProbeData[wData.Keys.Count];
            wData.Values.CopyTo(result, 0);

            if (result == null || result.Length == 0)
            {
                Debug.Log("No probe data was created.");
                return null;
            }

            Debug.Log(string.Format("Created {0} probes", result.Length));
            FindProbeNeighbours(result);

            return result;

            //return workingData.ToArray();
        }

        private void DrawDebugGUI()
        {
            if (_dataAsset == null)
                return;

            var oldShowDebug = _showDebug;

            _showDebug = EditorGUILayout.BeginToggleGroup("Enable Debug", _showDebug);
            {
                EditorGUILayout.BeginHorizontal();
                _debugIndex = EditorGUILayout.IntSlider(_debugIndex, 0, _dataAsset.Data == null ? 0 : _dataAsset.Count);

                GUI.enabled = _dataAsset.Data != null && _showDebug ? _debugIndex > 0 : false;
                if (GUILayout.Button("<"))
                {
                    _debugIndex--;
                    SceneView.RepaintAll();
                }

                GUI.enabled = _dataAsset.Data != null && _showDebug ? _debugIndex < _dataAsset.Count - 1 : false;
                if (GUILayout.Button(">"))
                {
                    _debugIndex++;
                    SceneView.RepaintAll();
                }

                GUI.enabled = true;

                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndToggleGroup();
            
            if (_showDebug != oldShowDebug)
                SceneView.RepaintAll();
        }

        private void OnSceneGUI()
        {
            if (!_showDebug || _dataAsset == null)
                return;

            if (_debugIndex >= _dataAsset.Count)
                _debugIndex = 0;

            if (_dataAsset.Count == 0)
                return;

            Handles.color = Color.red;
            Vector3 v = Vector3.one;
            int i;

            var upQuat = Quaternion.LookRotation(Vector3.up);

            for (i = 0; i < _dataAsset.Data.Length; i++)
            {
                v.x = _dataAsset.Data[i].x;
                v.z = _dataAsset.Data[i].z;

                if (i == _debugIndex)
                {
                    Handles.color = new Color(1, .7f, 0);
                    v.y = 1.5f;
                }
                else
                {
                    Handles.color = Color.red;
                    v.y = 1.0f;
                }

                Handles.SphereCap(0, v, Quaternion.identity, 0.5f);
                Handles.RectangleCap(0, v, upQuat, FogOfWarManager.cProbeSpacing *.5f);
            }

            Vector3 indexed = new Vector3(_dataAsset.Data[_debugIndex].x, 1.5f, _dataAsset.Data[_debugIndex].z);

            Handles.DrawWireDisc(indexed, Vector3.up, 50);

            if (_dataAsset.Data[_debugIndex].visibleProbesIndices == null)
                return;

            Handles.color = new Color(1, .7f, 0);
            for (i = 0; i < _dataAsset.Data[_debugIndex].visibleProbesIndices.Length; i++)
            {
                var visibleProbe = _dataAsset.Data[_dataAsset.Data[_debugIndex].visibleProbesIndices[i]];
                var vector = new Vector3(visibleProbe.x, 1, visibleProbe.z);

                Handles.DrawLine(indexed, vector);
            }
        }

        private void TryFindFogDataAsset()
        {
            if (_assetProperty == null)
                _assetProperty = serializedObject.FindProperty("_asset");

            _dataAsset = null;

            if (!_assetProperty.objectReferenceValue)
                return;

            _dataAsset = _assetProperty.objectReferenceValue as FogDataAsset;
        }
    }
}