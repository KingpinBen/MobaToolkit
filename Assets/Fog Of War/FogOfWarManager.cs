using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Fog
{
    public class FogOfWarManager : MonoBehaviour
    {
        [SerializeField]
        private FogDataAsset _asset;
        [SerializeField]
        private Material _fogBlurMaterial;

        private float _probeScaledSize;
        private int _fogShaderVariableName;
        private Vector2 _minimapScaleOffset;
        private RenderTexture _fogRT;
        private Rect _probeAreaRect;
        private Rect _fullTextureRect;
        private Dictionary<Point, FogOfWarProbeData> _probeLookup;
        private HashSet<int> _shownProbes;
        private BetterList<FogOfWarRevealer> _trackedRevealers;
        private FogOfWarProbeData[] _probeDataReference;
        
        public const int cProbeSpacing = 2;
        private const int cRenderTextureDimension = 512;

#if UNITY_EDITOR
        [HideInInspector]
        public LayerMask probeableLayers;
        [HideInInspector]
        public LayerMask ignoredLayers;

        public RenderTexture FogTexture
        {
            get { return _fogRT; }
        }
#endif

        private void Awake()
        {
            if (_instance)
                throw new UnityException("FOGOFWARMANAGER::SINGLETON::ALREADY_EXISTS");

            Debug.Assert(_asset != null);

            _instance = this;

            _probeLookup        = new Dictionary<Point, FogOfWarProbeData>();
            _trackedRevealers   = new BetterList<FogOfWarRevealer>();
            _shownProbes        = new HashSet<int>();

            var zone = GetComponent<GameplayArea>();

            _probeScaledSize = cRenderTextureDimension * (cProbeSpacing / zone.Size);

            GenerateLookUp();

            _fullTextureRect = new Rect();
            _fullTextureRect.xMin = 0;
            _fullTextureRect.yMin = 0;
            _fullTextureRect.xMax = cRenderTextureDimension;
            _fullTextureRect.yMax = cRenderTextureDimension;

            var minimapObj = FindObjectOfType<Minimap.MinimapTracker>();
            if (!minimapObj)
                throw new UnityException("FOGOFWARMANAGER::AWAKE::NO_MINIMAP_TAGGED_OBJ_FOUND");

            var fogRawImageComp = minimapObj.GetComponentInChildren<UnityEngine.UI.RawImage>();
            if (!fogRawImageComp)
                throw new UnityException("FOGOFWARMANAGER::AWAKE::RAW_IMAGE_COMPONENT_NOT_FOUND_IN_CHILD");

            _fogRT = new RenderTexture(cRenderTextureDimension, cRenderTextureDimension, 16);
            _fogRT.generateMips = false;

            fogRawImageComp.texture = _fogRT;

            _fogShaderVariableName = Shader.PropertyToID("_Fog_Texture");

            _minimapScaleOffset = new Vector2(transform.position.x - (zone.Size * .5f),
                transform.position.z - (zone.Size * .5f));
        }

        private void Start()
        {
            StartCoroutine(UpdateRevealedGround());
        }

        private void GenerateLookUp()
        {
            if (_probeLookup == null)
                throw new UnityException("FOGOFWARMANAGER::GENERATELOOKUP::DICTIONARY_NULL");

            _probeDataReference = _asset.Data;

            var p = Point.Zero;
            for(int i = 0; i < _probeDataReference.Length; i++)
            {
                p.x = _probeDataReference[i].x;
                p.y = _probeDataReference[i].z;

                if (_probeLookup.ContainsKey(p))
                    continue;

                _probeLookup.Add(p, _probeDataReference[i]);
            }
        }

        public void AddTrackedRevealer(FogOfWarRevealer revealer)
        {
            if (!revealer)
                throw new UnityException("FOGOFWARMANAGER::ADDTRACKEDREVEALER::PARAMETER_IS_NULL");

            if (_trackedRevealers.Contains(revealer))
                throw new UnityException("FOGOFWARMANAGER::ADDTRACKEDREVEALER::ALREADY_IN_LIST");

            _trackedRevealers.Add(revealer);

            //  Find and assign the nearest point on the manager that the revealer is
            Point closestPoint;
            FindNearestPointVector3(revealer.transform.position, out closestPoint);
            revealer.Point = closestPoint;
        }

        private IEnumerator UpdateRevealedGround()
        {
            int i, j;

            float sqrdRevealerViewDistance;
            FogOfWarRevealer current;
            Point closestPointToPlayer = Point.Zero;
            Point testProbePoint;

            while(true)
            {
                //  First we'll clear and start again
                _shownProbes.Clear();

                for (i = 0; i < _trackedRevealers.size; i++)
                {
                    if (!_trackedRevealers[i].gameObject.activeSelf)
                        continue;

                    current = _trackedRevealers[i];

                    sqrdRevealerViewDistance = current.Range * current.Range;

                    FindNearestPointVector3(current.transform.position, out closestPointToPlayer);

                    FogOfWarProbeData data;
                    if (_probeLookup.TryGetValue(closestPointToPlayer, out data))
                    {
                        current.Point = closestPointToPlayer;
                        
                        for (j = 0; j < data.visibleProbesIndices.Length; j++)
                        {
                            if (_shownProbes.Contains(data.visibleProbesIndices[j]))
                                continue;

                            testProbePoint = _probeDataReference[data.visibleProbesIndices[j]].Point;

                            if ((testProbePoint - closestPointToPlayer).SqrMagnitude > sqrdRevealerViewDistance)
                                continue;

                            _shownProbes.Add(data.visibleProbesIndices[j]);
                        }
                    }
                    else
                    {
                        if (current.Point != Point.Zero && _probeLookup.TryGetValue(current.Point, out data))
                        {
                            for (j = 0; j < data.visibleProbesIndices.Length; j++)
                            {
                                if (_shownProbes.Contains(data.visibleProbesIndices[j]))
                                    continue;

                                testProbePoint = _probeDataReference[data.visibleProbesIndices[j]].Point;

                                if ((testProbePoint - closestPointToPlayer).SqrMagnitude > sqrdRevealerViewDistance)
                                    continue;

                                _shownProbes.Add(data.visibleProbesIndices[j]);
                            }
                        }
                    }

                    
                }
                yield return null;
                Render();
            }
        }

        public static void FindNearestPointVector3(Vector3 position, out Point result)
        {
            float remainder;
            
            //  Casting instead of rounding to skip over the even-rounding issue
            remainder = position.x % cProbeSpacing;
            result.x = remainder < cProbeSpacing * .5f
                ? (int)(position.x - remainder)
                : (int)(position.x + (cProbeSpacing - remainder));

            remainder = position.z % cProbeSpacing;
            result.y = remainder < cProbeSpacing * .5f
                ? (int)(position.z - remainder)
                : (int)(position.z + (cProbeSpacing - remainder));
        }

        private void Render()
        {
            var e = _shownProbes.GetEnumerator();

            RenderTexture.active = _fogRT;
            GL.PushMatrix();                                
            GL.LoadPixelMatrix(0, cRenderTextureDimension, cRenderTextureDimension, 0);

            Graphics.DrawTexture(_fullTextureRect, Texture2D.whiteTexture, new Rect(0,0, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height), 0,0,0,0, Color.black * .5f);

            while (e.MoveNext())
            {
                _probeAreaRect.xMin = ((_probeDataReference[e.Current].x - _minimapScaleOffset.x) / cProbeSpacing) * _probeScaledSize;
                _probeAreaRect.yMin = cRenderTextureDimension - (((_probeDataReference[e.Current].z - _minimapScaleOffset.y) / cProbeSpacing) * _probeScaledSize);

                _probeAreaRect.xMax = _probeAreaRect.xMin + _probeScaledSize;
                _probeAreaRect.yMax = _probeAreaRect.yMin + _probeScaledSize;

                Graphics.DrawTexture(_probeAreaRect, Texture2D.whiteTexture);
            }

            GL.PopMatrix();
            RenderTexture.active = null;

            //  -----------------------------------------------
            //   Begin blurring the renderTarget
            //  -----------------------------------------------


            //  Downsample the width and height
            int rtW = cRenderTextureDimension >> 2;
            int rtH = cRenderTextureDimension >> 2;
            float widthMod = 1.0f / (1.0f * (1 << 2));

            var tempRT = RenderTexture.GetTemporary(rtW, rtH);
            tempRT.filterMode = FilterMode.Bilinear;
            Graphics.Blit(_fogRT, tempRT, _fogBlurMaterial, 0);

            for (int i = 0; i < 2; i++)
            { 
                _fogBlurMaterial.SetVector("_Parameter", new Vector4(2 * widthMod + i, -2 * widthMod - i, 0.0f, 0.0f));

                // vertical blur
                RenderTexture rt2 = RenderTexture.GetTemporary(rtW, rtH, 0, _fogRT.format);
                rt2.filterMode = FilterMode.Bilinear;
                Graphics.Blit(tempRT, rt2, _fogBlurMaterial, 1);
                RenderTexture.ReleaseTemporary(tempRT);
                tempRT = rt2;

                // horizontal blur
                rt2 = RenderTexture.GetTemporary(rtW, rtH, 0, _fogRT.format);
                rt2.filterMode = FilterMode.Bilinear;
                Graphics.Blit(tempRT, rt2, _fogBlurMaterial, 2);
                RenderTexture.ReleaseTemporary(tempRT);
                tempRT = rt2;
            }

            
            //  Copy the now blurred temporary RT onto our object RT
            //  so the minimap (and others) can use it
            Graphics.Blit(tempRT, _fogRT);

            Shader.SetGlobalTexture(_fogShaderVariableName, _fogRT);
            RenderTexture.ReleaseTemporary(tempRT);
        }

        private static FogOfWarManager _instance;
        public static FogOfWarManager Instance
        {
            get
            {
                if (!_instance)
                    throw new UnityException("FOGOFWARMANAGER::SINGLETON::NULL");

                return _instance;
            }
        }
    }
}