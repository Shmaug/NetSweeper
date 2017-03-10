using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetSweeper {
    class Gene {
        public int into;
        public int @out;
        public float weight;
        public bool enabled;
        public int innovation;
        
        public static Gene Default {
            get {
                return new Gene() {
                    into = 0,
                    @out = 0,
                    weight = 0f,
                    enabled = true,
                    innovation = 0
                };
            }
        }

        public static Gene Copy(Gene from) {
            Gene gene = new Gene();
            return new Gene() {
                into = from.into,
                @out = from.@out,
                weight = from.weight,
                enabled = from.enabled,
                innovation = from.innovation
            };
        }
    }
    class Genome {
        public int fitness = 0;
        public Network network;
        public int maxNeuron = 0;
        public int globalRank = 0;
        public List<Gene> genes = new List<Gene>();

        // Mutation rates
        public float connectionsRate = .25f;
        public float linkRate = 2f;
        public float biasRate = .4f;
        public float nodeRate = .5f;
        public float enableRate = .5f;
        public float disableRate = .3f;
        public float stepRate = .1f;

        public static Genome Basic {
            get {
                Genome g = new Genome();
                g.maxNeuron = NetworkController.InputCount;
                Mutate(g);
                return g;
            }
        }

        public static Genome Copy(Genome from) {
            Genome genome = new Genome() {
                maxNeuron = from.maxNeuron,
                connectionsRate = from.connectionsRate,
                linkRate = from.linkRate,
                biasRate = from.biasRate,
                nodeRate = from.nodeRate,
                enableRate = from.enableRate,
                disableRate = from.disableRate,
                stepRate = from.stepRate
            };
            from.genes.ForEach(gene => genome.genes.Add(Gene.Copy(gene)));
            return genome;
        }

        static bool containsLink(Genome genome, Gene link) {
            foreach (Gene gene in genome.genes)
                if (gene.into == link.into && gene.@out == link.@out)
                    return true;
            return false;
        }

        static void LinkMutate(Genome genome, bool forceBias) {
            int neuron1 = Neuron.RandomNeuron(genome.genes, false, NetworkController.random);
            int neuron2 = Neuron.RandomNeuron(genome.genes, true, NetworkController.random);

            if (neuron1 < NetworkController.InputCount && neuron2 <= NetworkController.InputCount)
                // both input nodes
                return;

            if (neuron2 < NetworkController.InputCount) {
                // swap input and output
                int tmp = neuron1;
                neuron1 = neuron2;
                neuron2 = tmp;
            }

            Gene newLink = Gene.Default;
            newLink.into = forceBias ? NetworkController.InputCount - 1 : neuron1;
            newLink.@out = neuron2;

            if (containsLink(genome, newLink)) return;
            newLink.innovation = NetworkController.pool.NewInnovation();
            newLink.weight = (float)NetworkController.random.NextDouble() * 4 - 2;

            genome.genes.Add(newLink);
        }
        static void NodeMutate(Genome genome) {
            if (genome.genes.Count == 0) return;

            genome.maxNeuron++;

            Gene gene = genome.genes[NetworkController.random.Next(0, genome.genes.Count)];
            if (!gene.enabled) return;
            gene.enabled = false;

            Gene gene1 = Gene.Copy(gene);
            gene1.@out = genome.maxNeuron;
            gene1.weight = 1f;
            gene1.innovation = NetworkController.pool.NewInnovation();
            gene1.enabled = true;
            genome.genes.Add(gene1);

            Gene gene2 = Gene.Copy(gene);
            gene2.@out = genome.maxNeuron;
            gene2.innovation = NetworkController.pool.NewInnovation();
            gene2.enabled = true;
            genome.genes.Add(gene2);
        }
        static void EnableDisableMutate(Genome genome, bool enable) {
            List<Gene> candidates = new List<Gene>();
            foreach (Gene gene in genome.genes)
                if (gene.enabled == !enable)
                    candidates.Add(gene);

            if (candidates.Count == 0) return;

            Gene g = candidates[NetworkController.random.Next(0, candidates.Count)];
            g.enabled = !g.enabled;
        }

        public static void Mutate(Genome genome) {
            Random r = NetworkController.random;

            genome.connectionsRate *= r.NextDouble() > .5 ? .95f : 1.05263f;
            genome.linkRate *= r.NextDouble() > .5 ? .95f : 1.05263f;
            genome.biasRate *= r.NextDouble() > .5 ? .95f : 1.05263f;
            genome.nodeRate *= r.NextDouble() > .5 ? .95f : 1.05263f;
            genome.enableRate *= r.NextDouble() > .5 ? .95f : 1.05263f;
            genome.disableRate *= r.NextDouble() > .5 ? .95f : 1.05263f;
            genome.stepRate *= r.NextDouble() > .5 ? .95f : 1.05263f;

            #region Point Mutation
            if (r.NextDouble() < genome.connectionsRate) {
                for (int i = 0; i < genome.genes.Count; i++) {
                    if (r.NextDouble() < NetworkController.PerturbChance)
                        genome.genes[i].weight += (float)r.NextDouble() * genome.stepRate * 2 - genome.stepRate;
                    else
                        genome.genes[i].weight = (float)r.NextDouble() * 4 - 2;
                }
            }
            #endregion

            float p;

            #region Link Mutation
            p = genome.linkRate;
            while (p > 0f) {
                if (r.NextDouble() < p)
                    LinkMutate(genome, false);
                p--;
            }
            #endregion

            #region Bias Mutation
            p = genome.biasRate;
            while (p > 0f) {
                if (r.NextDouble() < p)
                    LinkMutate(genome, true);
                p--;
            }
            #endregion

            #region Node Mutation
            p = genome.nodeRate;
            while (p > 0f) {
                if (r.NextDouble() < p)
                    NodeMutate(genome);
                p--;
            }
            #endregion

            #region Enable Mutation
            p = genome.enableRate;
            while (p > 0f) {
                if (r.NextDouble() < p)
                    EnableDisableMutate(genome, true);
                p--;
            }
            #endregion

            #region Disable Mutation
            p = genome.disableRate;
            while (p > 0f) {
                if (r.NextDouble() < p)
                    EnableDisableMutate(genome, false);
                p--;
            }
            #endregion
        }

        public static Genome Crossover(Genome genome1, Genome genome2) {
            if (genome2.fitness > genome1.fitness) {
                Genome tmp = genome1;
                genome1 = genome2;
                genome2 = tmp;
            }

            Genome child = new Genome();

            Dictionary<int, Gene> innovations2 = new Dictionary<int, Gene>();
            foreach (Gene g in genome2.genes)
                innovations2[g.innovation] = g;

            foreach (Gene g in genome1.genes) {
                Gene g2;
                if (innovations2.TryGetValue(g.innovation, out g2) && NetworkController.random.NextDouble() > .5 && g2.enabled)
                    child.genes.Add(Gene.Copy(g2));
                else
                    child.genes.Add(Gene.Copy(g));
            }

            child.maxNeuron = Math.Max(genome1.maxNeuron, genome2.maxNeuron);

            child.connectionsRate = genome1.connectionsRate;
            child.linkRate = genome1.linkRate;
            child.biasRate = genome1.biasRate;
            child.nodeRate = genome1.nodeRate;
            child.enableRate = genome1.enableRate;
            child.disableRate = genome1.disableRate;
            child.stepRate = genome1.stepRate;

            return child;
        }

        public static float Disjoint(List<Gene> genes1, List<Gene> genes2) {
            List<int> i1 = new List<int>();
            List<int> i2 = new List<int>();

            foreach (Gene gene in genes1)
                i1.Add(gene.innovation);
            foreach (Gene gene in genes2)
                i2.Add(gene.innovation);

            int disjoint = 0;
            foreach (Gene g in genes1)
                if (!i2.Contains(g.innovation))
                    disjoint++;
            foreach (Gene g in genes2)
                if (!i1.Contains(g.innovation))
                    disjoint++;

            return (float)disjoint / Math.Max(genes1.Count, genes2.Count);
        }

        public static float Weights(List<Gene> genes1, List<Gene> genes2) {
            Dictionary<int, Gene> i2 = new Dictionary<int, Gene>();
            foreach (Gene gene in genes2)
                i2[gene.innovation] = gene;

            float sum = 0;
            int coincident = 0;
            foreach (Gene gene in genes1) {
                if (i2.ContainsKey(gene.innovation)) {
                    sum += Math.Abs(gene.weight - i2[gene.innovation].weight);
                    coincident++;
                }
            }

            return sum / coincident;
        }

        public static bool SameSpecies(Genome g1, Genome g2) {
            float dd = NetworkController.DeltaDisjoint * Disjoint(g1.genes, g2.genes);
            float dw = NetworkController.DeltaWeights * Weights(g1.genes, g2.genes);
            return dd + dw < NetworkController.DeltaThreshold;
        }
    }
    class Species {
        public int topFitness = 0;
        public int staleness = 0;
        public float averageFitness = 0f;
        public List<Genome> genomes = new List<Genome>();

        public void CalculateAverageFitness() {
            int total = 0;

            foreach (Genome g in genomes)
                total += g.globalRank;

            averageFitness = (float)total / genomes.Count;
        }

        public Genome BreedChild() {
            Genome genome;

            if (NetworkController.random.NextDouble() < NetworkController.CrossoverChance)
                genome = Genome.Crossover(
                        genomes[NetworkController.random.Next(0, genomes.Count)],
                        genomes[NetworkController.random.Next(0, genomes.Count)]
                    );
            else
                genome = Genome.Copy(genomes[NetworkController.random.Next(0, genomes.Count)]);

            Genome.Mutate(genome);
            return genome;
        }
    }

    class Neuron {
        public List<Gene> incoming = new List<Gene>();
        public float value = 0f;

        public static int RandomNeuron(List<Gene> genes, bool nonInput, Random r) {
            List<int> neurons = new List<int>();
            if (!nonInput)
                for (int i = 0; i < NetworkController.InputCount; i++)
                    neurons.Add(i);

            for (int i = 0; i < NetworkController.OutputCount; i++)
                neurons.Add(NetworkController.MaxNodes + i);

            for (int i = 0; i < genes.Count; i++) {
                if (!nonInput || genes[i].into > NetworkController.InputCount)
                    neurons.Add(genes[i].into);
                if (!nonInput || genes[i].@out > NetworkController.InputCount)
                    neurons.Add(genes[i].@out);
            }
            neurons = neurons.Distinct().ToList();

            return neurons[r.Next(0, neurons.Count)];
        }
    }
    class Network {
        public Dictionary<int, Neuron> neurons;

        public Network(Genome genome) {
            neurons = new Dictionary<int, Neuron>();

            for (int i = 0; i < NetworkController.InputCount; i++)
                neurons[i] = new Neuron();

            for (int i = 0; i < NetworkController.OutputCount; i++)
                neurons[NetworkController.MaxNodes + i] = new Neuron();

            genome.genes = genome.genes.OrderBy(x => x.@out).ToList();

            foreach (Gene gene in genome.genes) {
                if (gene.enabled) {
                    if (!neurons.ContainsKey(gene.@out))
                        neurons[gene.@out] = new Neuron();
                    Neuron neuron = neurons[gene.@out];
                    neuron.incoming.Add(gene);
                    if (!neurons.ContainsKey(gene.into))
                        neurons[gene.into] = new Neuron();
                }
            }

            genome.network = this;
        }

        static float Sigmoid(float x) {
            return 2f / (1f + (float)Math.Exp(-4.9f * x)) - 1f;
        }
        
        public float[] Evaluate(float[] inputs) {
            for (int i = 0; i < inputs.Length; i++)
                neurons[i].value = inputs[i];

            foreach (KeyValuePair<int, Neuron> kp in neurons) {
                Neuron neuron = kp.Value;

                float sum = 0;
                foreach (Gene incoming in neuron.incoming)
                    sum += incoming.weight * neurons[incoming.into].value;

                if (neuron.incoming.Count > 0)
                    neuron.value = Sigmoid(sum);
            }

            float[] o = new float[NetworkController.OutputCount];
            for (int i = 0; i < NetworkController.OutputCount; i++)
                o[i] = neurons[NetworkController.MaxNodes + i].value;
            
            return o;
        }
    }
}
