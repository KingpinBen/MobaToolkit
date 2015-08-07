using UnityEngine;
using System.Collections;

namespace Fog
{
    public sealed class FogOfWarRevealer : MonoBehaviour
    {
        [SerializeField]
        private float _viewRadius = 10;

        private Point _pointInFog;

        void Start()
        {
            FogOfWarManager.Instance.AddTrackedRevealer(this);
        }

        public Point Point
        {
            get { return _pointInFog; }
            set { _pointInFog = value; }
            
        }

        public float Range
        {
            get { return _viewRadius; }
        }
    }
}

