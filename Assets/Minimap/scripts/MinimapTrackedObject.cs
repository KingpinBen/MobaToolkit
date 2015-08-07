using UnityEngine;
using System.Collections;
using UnityEngine.UI;

namespace Minimap
{
    [RequireComponent(typeof(Image))]
    public class MinimapTrackedObject : MonoBehaviour
    {
        private readonly int cPlayerIconSize = 26;
        private readonly int cWorldObjectIconSize = 16;

        [SerializeField]
        private Image _characterProfileImage;

        private MinimapTrackable _trackedObject;

        internal void Setup(byte teamID, MinimapTrackable obj, bool isCharacterProfile)
        {
            _trackedObject = obj;

            //  Set the correct colours
            var imageComp = GetComponent<Image>();
            var trans = GetComponent<RectTransform>();

            if (teamID > 0)
                imageComp.color = teamID == 1 ? Color.blue : Color.red;

            /*  Character profile trackables should be drawn differently from other
            //  trackables.
            //  Characters should be drawn ontop
            */

            if (isCharacterProfile)
            {
                trans.sizeDelta = new Vector2(cPlayerIconSize, cPlayerIconSize);
                _characterProfileImage.sprite = obj.Sprite;
            }
            else
            {
                //  Delete the mask to also get rid of characterProfile image
                Destroy(transform.GetChild(0).gameObject);

                if (obj.Sprite)
                {
                    imageComp.sprite = obj.Sprite;
                    trans.sizeDelta = new Vector2(cWorldObjectIconSize, cWorldObjectIconSize);
                }
            }
        }

        public Transform TrackedTransform
        {
            get
            {
                return _trackedObject.transform;
            }
        }
    }
}
