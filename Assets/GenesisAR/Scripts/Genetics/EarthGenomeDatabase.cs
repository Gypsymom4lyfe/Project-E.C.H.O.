using System.Collections.Generic;
using UnityEngine;

namespace GenesisAR.Genetics
{
    // ─────────────────────────────────────────────────────────────
    //  GenomeTemplate – a named species blueprint used to seed Genomes
    // ─────────────────────────────────────────────────────────────
    [System.Serializable]
    public class GenomeTemplate
    {
        public string SpeciesID;
        public string CommonName;
        public string Kingdom;   // Animalia, Plantae, Fungi, Protista, Monera
        public string Phylum;
        public string Class;
        public List<Gene> BaseGenes = new List<Gene>();

        public Genome InstantiateGenome()
        {
            Genome g = new Genome(SpeciesID);
            foreach (Gene gene in BaseGenes)
            {
                // Slight variance per individual so no two specimens are identical
                float variance = Random.Range(-0.05f, 0.05f);
                g.MaternalChromosome.Add(new Gene(gene.Name, Mathf.Clamp01(gene.Value + variance), gene.IsDominant));
                variance = Random.Range(-0.05f, 0.05f);
                g.PaternalChromosome.Add(new Gene(gene.Name, Mathf.Clamp01(gene.Value + variance), gene.IsDominant));
            }
            return g;
        }
    }

    // ─────────────────────────────────────────────────────────────
    //  EarthGenomeDatabase – master library of Earth life
    // ─────────────────────────────────────────────────────────────
    public static class EarthGenomeDatabase
    {
        private static readonly Dictionary<string, GenomeTemplate> _library =
            new Dictionary<string, GenomeTemplate>();

        static EarthGenomeDatabase()
        {
            RegisterAnimals();
            RegisterPlants();
            RegisterBugs();
            RegisterMicroorganisms();
        }

        public static bool TryGet(string speciesID, out GenomeTemplate template) =>
            _library.TryGetValue(speciesID, out template);

        public static IEnumerable<GenomeTemplate> All => _library.Values;

        // ── Helper ───────────────────────────────────────────────
        private static Gene G(string name, float val, bool dom = false) => new Gene(name, val, dom);

        private static List<Gene> BaseGeneSet(float size, float speed, float aggression,
                                              float fertility, float longevity,
                                              float metabolic, float camouflage,
                                              float r, float g, float b)
        {
            return new List<Gene>
            {
                G("BodySize",        size),
                G("SpeedMultiplier", speed),
                G("Aggression",      aggression),
                G("Fertility",       fertility),
                G("Longevity",       longevity),
                G("MetabolicRate",   metabolic),
                G("Camouflage",      camouflage),
                G("ColorR",          r),
                G("ColorG",          g),
                G("ColorB",          b),
            };
        }

        private static void Add(GenomeTemplate t) => _library[t.SpeciesID] = t;

        // ─────────────────────────────────────────────────────────
        //  ANIMALS
        // ─────────────────────────────────────────────────────────
        private static void RegisterAnimals()
        {
            Add(new GenomeTemplate
            {
                SpeciesID = "wolf_grey",     CommonName = "Grey Wolf",
                Kingdom = "Animalia",        Phylum = "Chordata",    Class = "Mammalia",
                BaseGenes = BaseGeneSet(0.70f, 0.75f, 0.80f, 0.55f, 0.65f, 0.60f, 0.40f, 0.55f, 0.50f, 0.45f)
            });
            Add(new GenomeTemplate
            {
                SpeciesID = "deer_whitetail", CommonName = "White-tailed Deer",
                Kingdom = "Animalia",         Phylum = "Chordata",    Class = "Mammalia",
                BaseGenes = BaseGeneSet(0.60f, 0.80f, 0.10f, 0.70f, 0.55f, 0.55f, 0.50f, 0.70f, 0.55f, 0.30f)
            });
            Add(new GenomeTemplate
            {
                SpeciesID = "rabbit_cottontail", CommonName = "Cottontail Rabbit",
                Kingdom = "Animalia",            Phylum = "Chordata",  Class = "Mammalia",
                BaseGenes = BaseGeneSet(0.20f, 0.85f, 0.05f, 0.90f, 0.35f, 0.70f, 0.45f, 0.85f, 0.80f, 0.75f)
            });
            Add(new GenomeTemplate
            {
                SpeciesID = "eagle_bald",  CommonName = "Bald Eagle",
                Kingdom = "Animalia",      Phylum = "Chordata",   Class = "Aves",
                BaseGenes = BaseGeneSet(0.55f, 0.90f, 0.75f, 0.30f, 0.70f, 0.65f, 0.20f, 0.20f, 0.20f, 0.20f)
            });
            Add(new GenomeTemplate
            {
                SpeciesID = "salmon_atlantic", CommonName = "Atlantic Salmon",
                Kingdom = "Animalia",          Phylum = "Chordata",   Class = "Actinopterygii",
                BaseGenes = BaseGeneSet(0.45f, 0.70f, 0.20f, 0.80f, 0.40f, 0.75f, 0.35f, 0.80f, 0.40f, 0.30f)
            });
            Add(new GenomeTemplate
            {
                SpeciesID = "frog_tree",   CommonName = "Green Tree Frog",
                Kingdom = "Animalia",      Phylum = "Chordata",   Class = "Amphibia",
                BaseGenes = BaseGeneSet(0.10f, 0.65f, 0.10f, 0.85f, 0.25f, 0.80f, 0.80f, 0.20f, 0.75f, 0.20f)
            });
            Add(new GenomeTemplate
            {
                SpeciesID = "lion",        CommonName = "African Lion",
                Kingdom = "Animalia",      Phylum = "Chordata",   Class = "Mammalia",
                BaseGenes = BaseGeneSet(0.85f, 0.80f, 0.90f, 0.50f, 0.70f, 0.65f, 0.35f, 0.80f, 0.65f, 0.20f)
            });
            Add(new GenomeTemplate
            {
                SpeciesID = "elephant_african", CommonName = "African Elephant",
                Kingdom = "Animalia",           Phylum = "Chordata",   Class = "Mammalia",
                BaseGenes = BaseGeneSet(1.00f, 0.25f, 0.50f, 0.25f, 0.90f, 0.40f, 0.10f, 0.50f, 0.45f, 0.40f)
            });
        }

        // ─────────────────────────────────────────────────────────
        //  PLANTS
        // ─────────────────────────────────────────────────────────
        private static void RegisterPlants()
        {
            Add(new GenomeTemplate
            {
                SpeciesID = "oak_white",  CommonName = "White Oak",
                Kingdom = "Plantae",      Phylum = "Tracheophyta",  Class = "Magnoliopsida",
                BaseGenes = BaseGeneSet(0.85f, 0.00f, 0.00f, 0.40f, 0.95f, 0.20f, 0.10f, 0.40f, 0.55f, 0.15f)
            });
            Add(new GenomeTemplate
            {
                SpeciesID = "fern_sword", CommonName = "Sword Fern",
                Kingdom = "Plantae",      Phylum = "Tracheophyta",  Class = "Polypodiopsida",
                BaseGenes = BaseGeneSet(0.30f, 0.00f, 0.00f, 0.60f, 0.50f, 0.35f, 0.20f, 0.20f, 0.65f, 0.15f)
            });
            Add(new GenomeTemplate
            {
                SpeciesID = "grass_blue", CommonName = "Kentucky Bluegrass",
                Kingdom = "Plantae",      Phylum = "Tracheophyta",  Class = "Liliopsida",
                BaseGenes = BaseGeneSet(0.05f, 0.00f, 0.00f, 0.90f, 0.30f, 0.50f, 0.05f, 0.25f, 0.70f, 0.20f)
            });
            Add(new GenomeTemplate
            {
                SpeciesID = "cactus_saguaro", CommonName = "Saguaro Cactus",
                Kingdom = "Plantae",          Phylum = "Tracheophyta",  Class = "Magnoliopsida",
                BaseGenes = BaseGeneSet(0.75f, 0.00f, 0.05f, 0.20f, 0.95f, 0.15f, 0.05f, 0.55f, 0.70f, 0.30f)
            });
        }

        // ─────────────────────────────────────────────────────────
        //  BUGS / INSECTS
        // ─────────────────────────────────────────────────────────
        private static void RegisterBugs()
        {
            Add(new GenomeTemplate
            {
                SpeciesID = "butterfly_monarch", CommonName = "Monarch Butterfly",
                Kingdom = "Animalia",            Phylum = "Arthropoda",  Class = "Insecta",
                BaseGenes = BaseGeneSet(0.05f, 0.60f, 0.05f, 0.75f, 0.20f, 0.80f, 0.10f, 0.95f, 0.55f, 0.05f)
            });
            Add(new GenomeTemplate
            {
                SpeciesID = "bee_honey", CommonName = "Honey Bee",
                Kingdom = "Animalia",    Phylum = "Arthropoda",  Class = "Insecta",
                BaseGenes = BaseGeneSet(0.03f, 0.70f, 0.40f, 0.85f, 0.15f, 0.90f, 0.10f, 0.90f, 0.70f, 0.10f)
            });
            Add(new GenomeTemplate
            {
                SpeciesID = "ant_fire", CommonName = "Fire Ant",
                Kingdom = "Animalia",   Phylum = "Arthropoda",  Class = "Insecta",
                BaseGenes = BaseGeneSet(0.02f, 0.65f, 0.70f, 0.95f, 0.10f, 0.95f, 0.20f, 0.80f, 0.15f, 0.10f)
            });
            Add(new GenomeTemplate
            {
                SpeciesID = "beetle_dung", CommonName = "Dung Beetle",
                Kingdom = "Animalia",      Phylum = "Arthropoda",  Class = "Insecta",
                BaseGenes = BaseGeneSet(0.04f, 0.40f, 0.10f, 0.70f, 0.25f, 0.70f, 0.50f, 0.15f, 0.15f, 0.10f)
            });
        }

        // ─────────────────────────────────────────────────────────
        //  MICROORGANISMS
        // ─────────────────────────────────────────────────────────
        private static void RegisterMicroorganisms()
        {
            Add(new GenomeTemplate
            {
                SpeciesID = "ecoli",    CommonName = "E. coli",
                Kingdom = "Monera",     Phylum = "Proteobacteria",  Class = "Gammaproteobacteria",
                BaseGenes = BaseGeneSet(0.01f, 0.30f, 0.20f, 1.00f, 0.05f, 1.00f, 0.00f, 0.10f, 0.50f, 0.10f)
            });
            Add(new GenomeTemplate
            {
                SpeciesID = "paramecium", CommonName = "Paramecium",
                Kingdom = "Protista",    Phylum = "Ciliophora",  Class = "Oligohymenophorea",
                BaseGenes = BaseGeneSet(0.01f, 0.50f, 0.10f, 0.90f, 0.05f, 0.90f, 0.00f, 0.30f, 0.70f, 0.50f)
            });
            Add(new GenomeTemplate
            {
                SpeciesID = "amoeba", CommonName = "Amoeba proteus",
                Kingdom = "Protista", Phylum = "Amoebozoa",   Class = "Tubulinea",
                BaseGenes = BaseGeneSet(0.01f, 0.20f, 0.30f, 0.80f, 0.05f, 0.85f, 0.00f, 0.50f, 0.50f, 0.30f)
            });
        }
    }
}
