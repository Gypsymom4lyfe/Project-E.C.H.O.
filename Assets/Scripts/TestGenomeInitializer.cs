using UnityEngine;
using GenesisAR.Genetics;

/// <summary>
/// Quick initializer that builds a small test genome and calls
/// <see cref="CreaturePhenotype.ExpressGenome"/> on Start so you can verify
/// that scale and bioluminescence traits are applied correctly in the editor.
///
/// Attach this to the same GameObject as <see cref="CreaturePhenotype"/>.
/// </summary>
public class TestGenomeInitializer : MonoBehaviour
{
    private void Start()
    {
        CreaturePhenotype phenotype = GetComponent<CreaturePhenotype>();
        if (phenotype != null)
        {
            // Build a test genome sequence
            phenotype.activeGenome.sequence.Add(new Gene {
                geneName = "Giant Scale",
                category = TraitCategory.BodyScale,
                value = 0.8f,
                isDominant = true
            });

            phenotype.activeGenome.sequence.Add(new Gene {
                geneName = "Bioluminescent Blue",
                category = TraitCategory.Bioluminescence,
                value = 1.0f,
                expressColor = Color.cyan
            });

            // Trigger physical manifestation
            phenotype.ExpressGenome();
        }
        else
        {
            Debug.LogError($"[TestGenomeInitializer] No CreaturePhenotype found on '{name}'. " +
                           "Please add the component and try again.");
        }
    }
}
