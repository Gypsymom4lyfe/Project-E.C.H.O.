using System.Collections;
using UnityEngine;

namespace GenesisAR.Environment
{
    /// <summary>
    /// Holographic AR Portal System — attach to the Portal Entrance Frame in Unity.
    /// Detects when a teleportable character crosses the portal threshold and switches
    /// their renderers to the inside-AR-world stencil shader.
    /// </summary>
    public class ARPortalTeleporter : MonoBehaviour
    {
        [Header("Portal Settings")]
        [Tooltip("Transform representing the plane/threshold of the portal ring")]
        public Transform portalThreshold;

        [Tooltip("Layer mask assigned to character objects/players eligible for teleportation")]
        public LayerMask teleportableLayer;

        [Header("Visual Effects")]
        public ParticleSystem portalPassVFX;
        public AudioSource portalSoundEffect;

        private Vector3 lastPosition;

        private void OnTriggerStay(Collider other)
        {
            // Check if the object entering the trigger is in the target layer
            if (((1 << other.gameObject.layer) & teleportableLayer) != 0)
            {
                Vector3 currentPosition = other.transform.position;

                // Calculate position relative to portal threshold
                Vector3 portalToCharacter = currentPosition - portalThreshold.position;
                float dotProduct = Vector3.Dot(portalThreshold.forward, portalToCharacter);

                // If character crosses from in front of the portal frame to behind it
                if (dotProduct < 0)
                {
                    ExecuteTeleport(other.gameObject);
                }

                lastPosition = currentPosition;
            }
        }

        private void ExecuteTeleport(GameObject subject)
        {
            // 1. Play AR Particle and Audio Feedback
            if (portalPassVFX != null) portalPassVFX.Play();
            if (portalSoundEffect != null) portalSoundEffect.Play();

            // 2. Toggle Stencil Mask / Material Shaders on the subject
            Renderer[] characterRenderers = subject.GetComponentsInChildren<Renderer>();
            foreach (Renderer rend in characterRenderers)
            {
                // Switch material to rendered-inside-AR-world shader
                rend.material.SetInt("_StencilComp", (int)UnityEngine.Rendering.CompareFunction.Equal);
            }

            Debug.Log($"[Genesis AR Portal] {subject.name} passed through the portal threshold into the AR planet environment!");
        }

        private void OnDrawGizmos()
        {
            if (portalThreshold != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawRay(portalThreshold.position, portalThreshold.forward * 1.5f);
            }
        }
    }
}
