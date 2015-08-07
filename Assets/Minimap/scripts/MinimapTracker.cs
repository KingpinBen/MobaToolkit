using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

namespace Minimap
{
    public class MinimapTracker : MonoBehaviour
    {
        [SerializeField]
        private MinimapTrackedObject trackedObjectPrefab;

        private List<MinimapTrackedObject> _tracked;
        private RectTransform _minimapUiPanel;
        private GameplayArea _gameplayArea;
        private Vector3[] _topRightBottomLeft = new Vector3[2];
        private RectTransform _transform;

        private void Awake()
        {
            _tracked = new List<MinimapTrackedObject>(20);

            _transform = GetComponent<RectTransform>();

            StartCoroutine(UpdatePositions());
        }

        private void Start()
        {
            var minimapGo = GameObject.FindGameObjectWithTag("Minimap");
            if (!minimapGo)
                throw new NullReferenceException("no object was found with the 'Minimap' tag. Check it exists and is written correctly");

            _minimapUiPanel = minimapGo.GetComponent<RectTransform>();
            _gameplayArea = FindObjectOfType<GameplayArea>();

            _minimapUiPanel = transform.parent.transform as RectTransform;

            _topRightBottomLeft[0] = _gameplayArea.transform.position + new Vector3(_gameplayArea.Size, 0, _gameplayArea.Size) * .5f;
            _topRightBottomLeft[1] = _gameplayArea.transform.position - new Vector3(_gameplayArea.Size, 0, _gameplayArea.Size) * .5f;
        }

        private IEnumerator UpdatePositions()
        {
            var i = 0;

            while(true)
            {
                for (i = 0; i < _tracked.Count; i++)
                {
                    var rectTransform = _tracked[i].transform as RectTransform;
                    rectTransform.anchoredPosition = CalculateMinimapPosition(_tracked[i].TrackedTransform.position);
                }

                yield return null;
            }
        }

        private Vector2 CalculateMinimapPosition(Vector3 position)
        {
            var result = new Vector2(
                FindPercentageBetween(position.x, _topRightBottomLeft[1].x, _topRightBottomLeft[0].x),
                FindPercentageBetween(position.z, _topRightBottomLeft[1].z, _topRightBottomLeft[0].z));

            result = ((result * 2) - new Vector2(1, 1)) * (_minimapUiPanel.sizeDelta.y * .5f);

            return result;
        }

        private float FindPercentageBetween(float value, float min, float max)
        {
            //  range = max - min
            //  valFromMin = val-min
            //  valFromMin / range

            value = Mathf.Clamp(value, min, max);
            return (value - min) / (max - min);
        }

        public void AddTrackable(MinimapTrackable trackable)
        {
            if (!trackable)
                throw new ArgumentNullException("MINIMAPTRACKER::ADDTRACKER::TRACKABLE_IS_NULL");

            var obj = Instantiate(trackedObjectPrefab.gameObject);
            obj.transform.SetParent(transform);

            var newTracked = obj.GetComponent<MinimapTrackedObject>();
            newTracked.Setup(0, trackable, trackable is CharacterMinimapTrackable);

            _tracked.Add(newTracked);
        }

        private static MinimapTracker _trackerInstance;
        internal static MinimapTracker Instance
        {
            get
            {
                if (!_trackerInstance)
                {
                    _trackerInstance = FindObjectOfType<MinimapTracker>();
                    if (!_trackerInstance)
                        throw new MissingReferenceException("No MinimapTracker object was found.");
                }

                return _trackerInstance;
            }
        }
    }
}

