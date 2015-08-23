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
        private string _errorMessage;
        private State _currentState = State.Idle;
        private EditorProgressJob _currentJob;
        private FogOfWarProbeData[] _tempProbeData;

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
            _currentState = State.Idle;

            TryFindFogDataAsset();
        }

        private void OnDisable()
        {
            EditorApplication.update -= UpdateState;
            _tempProbeData = null;
            _currentJob = null;
            _currentState = State.Idle;
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.PropertyField(_probeMaskProperty);
            EditorGUILayout.PropertyField(_ignoreMaskProperty);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(_assetProperty);
            EditorGUI.EndChangeCheck();

            if (GUI.changed)
                TryFindFogDataAsset();

            GUI.enabled = !EditorApplication.isPlaying && _dataAsset && _currentState == State.Idle;
            DrawDebugGUI();

            if (GUILayout.Button("Generate Vision Probes"))
            {
                if (EditorApplication.isPlaying)
                {
                    _errorMessage = "Cannot generate probes while in playmode.";
                }
                else
                {
                    var result = EditorUtility.DisplayDialog("Probe Generation", 
                        "Are you sure you want to generate probes? This may take some time.", "Yes", "No");

                    if (result)
                    {
                        _errorMessage = null;
                        TryFindFogDataAsset();

                        _currentJob = EditorProgressJob.Start(GenerateProbeData());

                        //  Force a clean up
                        GC.Collect();
                    }
                }
            }

            GUI.enabled = true;

            VisionProbeGenerationProgressBar();

            if (!string.IsNullOrEmpty(_errorMessage))
                EditorGUILayout.HelpBox(_errorMessage, MessageType.Error);

            serializedObject.ApplyModifiedProperties();
        }

        private IEnumerator<float> GenerateProbeData()
        {
            if (!_gameplayArea)
                throw new UnityException("FOGOFWARMANAGER::GENERATEPROBEDATA::NO_GAMEPLAY_AREA_SET");

            if (_groundLayer == -1)
                throw new UnityException("FOGOFWARMANAGER::GENERATEPROBEDATA::NO_GROUND_LAYER_EXISTS");

            if (_LoSBlockLayer == -1)
                throw new UnityException("FOGOFWARMANAGER::GENERATEPROBEDATA::NO_SIGHTBLOCK_LAYER_EXISTS");

            if (_currentState != State.Idle)
                yield break;

            float x;
            RaycastHit hit;
            var rayOrigin = Vector3.zero;
            var wData = new Dictionary<Point, FogOfWarProbeData>();
            var bottomLeftGameplayArea  = _gameplayArea.transform.position - (new Vector3(_gameplayArea.Size, 0, _gameplayArea.Size) * .5f);
            var topRightGameplayArea    = _gameplayArea.transform.position + (new Vector3(_gameplayArea.Size, 0, _gameplayArea.Size) * .5f);

            _tempProbeData = null;
            rayOrigin.y = 20;
            _currentState = State.GeneratingProbes;
            EditorApplication.update += UpdateState;

            for (float z = bottomLeftGameplayArea.z; z < topRightGameplayArea.z; z += FogOfWarManager.cProbeSpacing)
            {
                for (x = bottomLeftGameplayArea.x; x < topRightGameplayArea.x; x += FogOfWarManager.cProbeSpacing)
                {
                    rayOrigin.x = x;
                    rayOrigin.z = z;

                    Point p;
                    FogOfWarManager.FindNearestPointVector3(rayOrigin, out p);

                    rayOrigin.x = p.x;
                    rayOrigin.z = p.y;

                    if (wData.ContainsKey(p))
                        continue;

                    //if (Physics.SphereCast(rayOrigin, cSphereCastRadius, Vector3.down, out hit, 50))
                    if (Physics.Raycast(rayOrigin, Vector3.down, out hit, 50))
                    {
                        if (((1 << hit.transform.gameObject.layer) & _target.probeableLayers.value) != 0)
                        {
                            wData.Add(p, new FogOfWarProbeData(p));
                        }
                    }
                }

                //  Repaint for the progress bar
                Repaint();
                yield return ((z - bottomLeftGameplayArea.z) / ((topRightGameplayArea.z - bottomLeftGameplayArea.z) + (topRightGameplayArea.x - bottomLeftGameplayArea.x)));
            }

            _tempProbeData = new FogOfWarProbeData[wData.Keys.Count];
            wData.Values.CopyTo(_tempProbeData, 0);

            if (_tempProbeData == null || _tempProbeData.Length == 0)
                Debug.Log("No probe data was created.");
            else
                Debug.Log(string.Format("Created {0} probes", _tempProbeData.Length));

            FindProbeNeighbours();
        }

        private IEnumerator<float> FindProbeNeighbours()
        {
            if (_currentState != State.GeneratingProbes)
                yield break;

            _currentState = State.LinkingNeighbours;

            Vector3 temp = Vector3.zero;
            Vector3 temp2 = Vector3.zero;
            RaycastHit hit;
            Ray ray = new Ray();

            //  We use a List here as clearing a BetterList doesn't probably remove
            //  visible probes from previously checked probes
            List<int> visibleNodes = new List<int>();
            int j;

            var testCollider = GameObject.CreatePrimitive(PrimitiveType.Sphere).GetComponent<SphereCollider>();
            testCollider.gameObject.hideFlags = HideFlags.HideAndDontSave;
            testCollider.radius = .2f;

            var renderer = testCollider.GetComponent<MeshRenderer>();
            renderer.enabled = false;

            //  We add a rigidbody as we may check through some triggers 
            //  which need to block line of sight
            testCollider.gameObject.AddComponent<Rigidbody>();

            for (int i = 0; i < _tempProbeData.Length; i++)
            {
                temp = new Vector3(_tempProbeData[i].x, 20, _tempProbeData[i].z);

                visibleNodes.Clear();

                /*  
                //  This is basically to just hit the ground to see where we can see from. 
                //  It's a nice thing to have as we're able to get probes that can look down over edges
                //  but not be able to look up, sort of like someone being able to look over a cliff.

                //  If you don't want this effect just remove/comment-out this ray cast
                */
                if (Physics.Raycast(temp, Vector3.down, out hit))
                {
                    temp.y = hit.point.y + .1f;
                    temp2.y = hit.point.y + .1f;

                    ray.origin = temp;

                    for (j = 0; j < _tempProbeData.Length; j++)
                    {
                        //  Skip if it's checking itself
                        if (_tempProbeData[i].x == _tempProbeData[j].x &&
                            _tempProbeData[i].z == _tempProbeData[j].z)
                            continue;

                        temp2.x = _tempProbeData[j].x;
                        temp2.z = _tempProbeData[j].z;

                        if ((temp2 - temp).sqrMagnitude > cMaxProbeRayDistance * cMaxProbeRayDistance)
                            continue;

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

                    _tempProbeData[i].visibleProbesIndices = visibleNodes.ToArray();
                }

                //  Repaint for the progress bar
                Repaint();
                yield return ((i * 1.0f) / _tempProbeData.Length);
            }

            DestroyImmediate(testCollider.gameObject);
        }

        private void VisionProbeGenerationProgressBar()
        {
            if (_currentState == State.Idle || _currentJob == null)
                return;

            var percent = (_currentJob.Progress*100).ToString("0.0");

            var title = string.Format("{0}%: {1}", percent, _currentState == State.GeneratingProbes ?
                "Generating Probes" : "Finding Neighbours");

            var info = _currentState == State.GeneratingProbes ?
                "Probes are current being placed around the gameplay area." :
                "Depending on the ProbeScaling, this may take some time..";

            var result = EditorUtility.DisplayCancelableProgressBar(title, info, _currentJob.Progress);

            if (result)
                CancelCurrentJob();
        }

        private void CancelCurrentJob()
        {
            _currentJob.Stop();
            _currentJob = null;
            _currentState = State.Idle;
            EditorUtility.ClearProgressBar();
            EditorApplication.update -= UpdateState;
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

                //Handles.SphereCap(0, v, Quaternion.identity, 0.5f);
                Handles.RectangleCap(0, v, upQuat, 0.5f);
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

        private void UpdateState()
        {
            if (_currentState == State.GeneratingProbes)
            {
                if (_currentJob.Finished)
                {
                    _currentJob = EditorProgressJob.Start(FindProbeNeighbours());
                }
            }
            else
            {
                if (_currentState == State.LinkingNeighbours)
                {
                    if (_currentJob.Finished)
                    {
                        _dataAsset.SetData(_tempProbeData);
                        EditorUtility.SetDirty(_dataAsset);

                        AssetDatabase.SaveAssets();
                        AssetDatabase.Refresh();
                        CancelCurrentJob();
                        
                        Repaint();
                    }
                }
            }
        }

        private enum State
        {
            Idle,
            GeneratingProbes,
            LinkingNeighbours
        }
    }
}