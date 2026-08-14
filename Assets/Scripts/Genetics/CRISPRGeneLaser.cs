using System;
using UnityEngine;

namespace GenesisAR.Genetics
{
    public enum NucleotideBase
    {
        Adenine,
        Thymine,
        Cytosine,
        Guanine
    }

    [Serializable]
    public class NucleotidePair
    {
        public int SequenceIndex;
        public NucleotideBase BaseLeft;  // A, T, C, or G
        public NucleotideBase BaseRight; // Complementary pair: A-T, C-G
        public bool IsMutated;
        public string LinkedTrait;      // e.g., "Thermal Tolerance", "Respiration Efficiency"
    }

    public class CRISPRGeneLaser : MonoBehaviour
    {
        [Header("CRISPR Laser Settings")]
        [SerializeField] private LineRenderer laserBeam;
        [SerializeField] private ParticleSystem targetSparks;
        [SerializeField] private float beamDuration = 0.4f;

        [Header("Targeting State")]
        public NucleotidePair CurrentlyTargetedPair { get; private set; }
        private float _beamTimer;

        private void Awake()
        {
            if (laserBeam != null)
            {
                laserBeam.enabled = false;
            }
        }

        private void Update()
        {
            if (_beamTimer > 0f)
            {
                _beamTimer -= Time.deltaTime;
                if (_beamTimer <= 0f && laserBeam != null)
                {
                    laserBeam.enabled = false;
                }
            }
        }

        public bool ExecuteBasePairEdit(NucleotidePair targetPair, NucleotideBase newBase, Vector3 originPos, Vector3 targetPos)
        {
            if (targetPair == null)
            {
                return false;
            }

            CurrentlyTargetedPair = targetPair;

            // Enforce valid nucleotide pairing rules (A-T, C-G)
            NucleotideBase complementaryBase = GetComplementaryBase(newBase);

            targetPair.BaseLeft = newBase;
            targetPair.BaseRight = complementaryBase;
            targetPair.IsMutated = false; // Point mutation successfully repaired

            // Trigger Visual & Particle Effects
            FireLaserEffect(originPos, targetPos);

            Debug.Log($"[CRISPR Engine] Base pair {targetPair.SequenceIndex} edited to {newBase}-{complementaryBase}. Trait updated: {targetPair.LinkedTrait}");
            return true;
        }

        private NucleotideBase GetComplementaryBase(NucleotideBase inputBase)
        {
            return inputBase switch
            {
                NucleotideBase.Adenine => NucleotideBase.Thymine,
                NucleotideBase.Thymine => NucleotideBase.Adenine,
                NucleotideBase.Cytosine => NucleotideBase.Guanine,
                NucleotideBase.Guanine => NucleotideBase.Cytosine,
                _ => NucleotideBase.Thymine
            };
        }

        private void FireLaserEffect(Vector3 origin, Vector3 target)
        {
            if (laserBeam != null)
            {
                if (laserBeam.positionCount < 2)
                {
                    laserBeam.positionCount = 2;
                }
                laserBeam.SetPosition(0, origin);
                laserBeam.SetPosition(1, target);
                laserBeam.enabled = true;
                _beamTimer = beamDuration;
            }

            if (targetSparks != null)
            {
                targetSparks.transform.position = target;
                targetSparks.Play();
            }
        }
    }
}
