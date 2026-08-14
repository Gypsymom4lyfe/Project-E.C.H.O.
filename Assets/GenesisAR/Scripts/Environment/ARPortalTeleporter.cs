using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace GenesisAR.Environment
{
    // ─────────────────────────────────────────────────────────────
    //  ARPortalTeleporter – detects when the camera crosses a portal
    //  threshold and toggles the stencil-mask VR world on/off.
    //
    //  Works in tandem with ARPortalWindow.shader which writes a
    //  stencil value so only geometry behind the portal window is
    //  visible through it.
    // ─────────────────────────────────────────────────────────────
    public class ARPortalTeleporter : MonoBehaviour
    {
        [Header("Portal Geometry")]
        [Tooltip("The portal window quad (uses ARPortalWindow.shader)")]
        public Transform PortalWindow;

        [Tooltip("Root of the world visible through the portal")]
        public GameObject PortalWorld;

        [Header("Threshold")]
        [Tooltip("How far past the portal plane we detect crossing (metres)")]
        public float CrossThreshold = 0.02f;

        [Header("Camera")]
        public Camera MainCamera;

        // Stencil trigger events
        public System.Action OnEnterPortal;
        public System.Action OnExitPortal;

        private bool  _insidePortal = false;
        private float _lastDot;

        private void Awake()
        {
            if (MainCamera == null)
                MainCamera = Camera.main;

            // Ensure portal world starts hidden
            if (PortalWorld != null)
                PortalWorld.SetActive(false);
        }

        private void Update()
        {
            if (PortalWindow == null || MainCamera == null) return;
            CheckThresholdCrossing();
        }

        // ── Dot-product threshold detection ───────────────────────
        // Sign change of the dot product between portal normal and
        // (camera - portal) vector indicates a crossing.
        private void CheckThresholdCrossing()
        {
            Vector3 portalToCamera = MainCamera.transform.position - PortalWindow.position;
            float   dot            = Vector3.Dot(PortalWindow.forward, portalToCamera);

            bool crossed = (_lastDot > CrossThreshold && dot < -CrossThreshold) ||
                           (_lastDot < -CrossThreshold && dot > CrossThreshold);

            if (crossed)
            {
                _insidePortal = !_insidePortal;
                ApplyPortalState();
            }

            _lastDot = dot;
        }

        private void ApplyPortalState()
        {
            if (PortalWorld != null)
                PortalWorld.SetActive(_insidePortal);

            if (_insidePortal)
            {
                Debug.Log("[ARPortalTeleporter] Entered portal world.");
                OnEnterPortal?.Invoke();
            }
            else
            {
                Debug.Log("[ARPortalTeleporter] Exited portal world.");
                OnExitPortal?.Invoke();
            }
        }

        // ── Stencil trigger via collider (alternative path) ───────
        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("MainCamera")) return;
            if (_insidePortal) return;
            _insidePortal = true;
            ApplyPortalState();
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag("MainCamera")) return;
            if (!_insidePortal) return;
            _insidePortal = false;
            ApplyPortalState();
        }

        // ── Public API ────────────────────────────────────────────
        public bool IsInsidePortal => _insidePortal;

        public void ForceEnter()
        {
            _insidePortal = true;
            ApplyPortalState();
        }

        public void ForceExit()
        {
            _insidePortal = false;
            ApplyPortalState();
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (PortalWindow == null) return;
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(PortalWindow.position, new Vector3(1f, 2f, 0.05f));
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(PortalWindow.position, PortalWindow.forward * 0.5f);
        }
#endif
    }
}
