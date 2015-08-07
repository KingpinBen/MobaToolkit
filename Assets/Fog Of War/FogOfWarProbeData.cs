using UnityEngine;
using System.Collections;
using System;

namespace Fog
{
    [Serializable]
    public sealed class FogOfWarProbeData
    {
        public int x;
        public int z;

        public int[] visibleProbesIndices;

        [NonSerialized]
        public bool isHidden = true;

        public FogOfWarProbeData(int xx, int zz)
        {
            x = xx;
            z = zz;
        }

        public FogOfWarProbeData(Point p)
        {
            x = p.x;
            z = p.y;
        }

        public Point Point
        {
            get { return new Point(x, z); }
        }
    }
}