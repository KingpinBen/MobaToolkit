using UnityEngine;
using System.Collections;

namespace Minimap
{
    public class MinimapTrackable : MonoBehaviour
    {
        [SerializeField]
        private Sprite _sprite;

        public Sprite Sprite
        {
            get { return _sprite; }
        }

        private void Start()
        {
            MinimapTracker.Instance.AddTrackable(this);
        }
    }
}
