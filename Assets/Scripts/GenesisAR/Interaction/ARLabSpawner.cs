using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.InputSystem;

namespace GenesisAR.Interaction
{
    [RequireComponent(typeof(ARRaycastManager))]
    public class ARLabSpawner : MonoBehaviour
    {
        [Header("AR Prefabs")]
        [Tooltip("Prefab for the AR Bio-Lab Workstation / Incubator")]
        public GameObject arLabPrefab;

        private GameObject spawnedLabInstance;
        private ARRaycastManager arRaycastManager;
        private static List<ARRaycastHit> hits = new List<ARRaycastHit>();

        private void Awake()
        {
            arRaycastManager = GetComponent<ARRaycastManager>();
        }

        private void Update()
        {
            // Check for tap/touch input on mobile or AR headsets
            if (!TryGetTouchPosition(out Vector2 touchPosition)) return;

            // Perform AR Raycast against detected real-world horizontal surfaces
            if (arRaycastManager.Raycast(touchPosition, hits, TrackableType.PlaneWithinPolygon))
            {
                Pose hitPose = hits[0].pose;

                if (spawnedLabInstance == null)
                {
                    // Instantiate the AR Lab Workstation at the raycast hit location
                    spawnedLabInstance = Instantiate(arLabPrefab, hitPose.position, hitPose.rotation);
                    Debug.Log("[Genesis AR] AR Bio-Lab placed successfully in physical space.");
                }
                else
                {
                    // Relocate existing lab workstation on surface tap
                    spawnedLabInstance.transform.position = hitPose.position;
                    spawnedLabInstance.transform.rotation = hitPose.rotation;
                }
            }
        }

        private bool TryGetTouchPosition(out Vector2 touchPosition)
        {
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
            {
                touchPosition = Touchscreen.current.primaryTouch.position.ReadValue();
                return true;
            }

            touchPosition = default;
            return false;
        }
    }
}
