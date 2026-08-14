using UnityEngine;

namespace GenesisAR.Genetics
{
    /// <summary>
    /// Reads the creature's active genome and physically manifests genetic traits
    /// by adjusting scale, material color, emission, and other renderer properties.
    ///
    /// Attach this component to a creature GameObject that has a MeshRenderer or
    /// SkinnedMeshRenderer on itself or one of its children.
    /// </summary>
    public class CreaturePhenotype : MonoBehaviour
    {
        [Header("Genome")]
        [Tooltip("The genome whose genes will be expressed when ExpressGenome() is called.")]
        public Genome activeGenome = new Genome();

        [Header("Phenotype Limits")]
        [Tooltip("Minimum uniform scale applied by a BodyScale gene at value 0.")]
        public float minScale = 0.5f;

        [Tooltip("Maximum uniform scale applied by a BodyScale gene at value 1.")]
        public float maxScale = 3.0f;

        // Cached renderer so we don't search the hierarchy every frame.
        private Renderer _renderer;
        private MaterialPropertyBlock _propertyBlock;

        private void Awake()
        {
            CacheRenderer();
            _propertyBlock = new MaterialPropertyBlock();
        }

        /// <summary>
        /// Reads <see cref="activeGenome"/> and applies all expressed traits to
        /// this creature's transform and renderer.  Call this after modifying the
        /// genome sequence at runtime.
        /// </summary>
        public void ExpressGenome()
        {
            if (activeGenome == null)
            {
                Debug.LogWarning($"[CreaturePhenotype] No active genome on '{name}'.", this);
                return;
            }

            ApplyBodyScale();
            ApplyBodyColor();
            ApplyBioluminescence();
        }

        // ------------------------------------------------------------------ //
        //  Private helpers                                                      //
        // ------------------------------------------------------------------ //

        private void CacheRenderer()
        {
            _renderer = GetComponent<Renderer>();
            if (_renderer == null)
                _renderer = GetComponentInChildren<Renderer>();

            if (_renderer == null)
                Debug.LogWarning($"[CreaturePhenotype] No Renderer found on '{name}' or its children. " +
                                 "Color/emission traits will be skipped.", this);
        }

        private void ApplyBodyScale()
        {
            Gene scaleTrait = activeGenome.GetDominantTrait(TraitCategory.BodyScale);
            if (scaleTrait == null) return;

            float targetScale = Mathf.Lerp(minScale, maxScale, scaleTrait.value);
            transform.localScale = Vector3.one * targetScale;

            Debug.Log($"[CreaturePhenotype] '{name}' scale set to {targetScale:F2} " +
                      $"from gene '{scaleTrait.geneName}'.");
        }

        private void ApplyBodyColor()
        {
            if (_renderer == null) return;

            Gene colorTrait = activeGenome.GetDominantTrait(TraitCategory.BodyColor);
            if (colorTrait == null) return;

            _renderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor("_BaseColor", colorTrait.expressColor);
            // Fallback for legacy Standard shader.
            _propertyBlock.SetColor("_Color", colorTrait.expressColor);
            _renderer.SetPropertyBlock(_propertyBlock);

            Debug.Log($"[CreaturePhenotype] '{name}' body color set to {colorTrait.expressColor} " +
                      $"from gene '{colorTrait.geneName}'.");
        }

        private void ApplyBioluminescence()
        {
            if (_renderer == null) return;

            Gene bioTrait = activeGenome.GetDominantTrait(TraitCategory.Bioluminescence);
            if (bioTrait == null)
            {
                // Ensure emission is disabled when no bioluminescence gene is present.
                _renderer.GetPropertyBlock(_propertyBlock);
                _propertyBlock.SetColor("_EmissionColor", Color.black);
                _renderer.SetPropertyBlock(_propertyBlock);
                return;
            }

            Color emissionColor = bioTrait.expressColor * bioTrait.value;

            _renderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor("_EmissionColor", emissionColor);
            _renderer.SetPropertyBlock(_propertyBlock);

            Debug.Log($"[CreaturePhenotype] '{name}' bioluminescence set to {emissionColor} " +
                      $"from gene '{bioTrait.geneName}'.");
        }
    }
}
