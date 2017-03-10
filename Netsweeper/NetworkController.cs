using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace NetSweeper {
    class Pool {
        public List<Species> species = new List<Species>();
        public int generation = 0;
        public int currentSpecies = 0;
        public int currentGenome = 0;
        public int currentFrame = 0;
        public int currentFitness;
        public int maxFitness = 0;
        public int innovation = NetworkController.OutputCount;
        
        public void Initialize() {
            for (int i = 0; i < NetworkController.Population; i++)
                AddToSpecies(Genome.Basic);

            new Network(species[currentSpecies].genomes[currentGenome]);
        }

        public int NewInnovation() {
            innovation++;
            return innovation;
        }

        public void AddToSpecies(Genome genome) {
            bool foundSpecies = false;
            foreach (Species s in species) {
                if (!foundSpecies && Genome.SameSpecies(genome, s.genomes[0])) {
                    foundSpecies = true;
                    s.genomes.Add(genome);
                }
            }

            if (!foundSpecies) {
                Species s = new Species();
                s.genomes.Add(genome);
                species.Add(s);
            }
        }

        public float TotalAverageFitness() {
            float total = 0;
            species.ForEach(s => total += s.averageFitness);
            return total;
        }
        
        public void CullSpecies(bool cutToOne) {
            foreach (Species s in species) {
                s.genomes = s.genomes.OrderByDescending(g => g.fitness).ToList();
                s.genomes = s.genomes.Take(cutToOne ? 1 : (int)Math.Ceiling(s.genomes.Count / 2f)).ToList();
            }
        }
        public void RemoveStaleSpecies() {
            List<Species> survived = new List<Species>();

            foreach (Species s in species) {
                s.genomes.OrderByDescending(g => g.fitness);

                if (s.genomes[0].fitness > s.topFitness) {
                    s.topFitness = s.genomes[0].fitness;
                    s.staleness = 0;
                } else
                    s.staleness++;

                if (s.staleness < NetworkController.StaleSpecies || s.topFitness >= maxFitness)
                    survived.Add(s);
            }

            species = survived;
        }
        public void RemoveWeakSpecies() {
            List<Species> survived = new List<Species>();

            float sum = TotalAverageFitness();
            foreach (Species s in species) {
                int breed = (int)Math.Floor(s.averageFitness / sum * NetworkController.Population);
                if (breed >= 1)
                    survived.Add(s);
            }

            species = survived;
        }
        
        public void RankGlobally() {
            List<Genome> global = new List<Genome>();

            foreach (Species s in species)
                global.AddRange(s.genomes);

            global.OrderBy(g => g.fitness);
            
            for (int i = 0; i < global.Count; i++)
                global[i].globalRank = i;
        }

        public void NewGeneration() {
            CullSpecies(false);
            RankGlobally();
            RemoveStaleSpecies();
            RankGlobally();

            foreach (Species s in species)
                s.CalculateAverageFitness();

            RemoveWeakSpecies();

            List<Genome> children = new List<Genome>();
            float sum = TotalAverageFitness();
            foreach (Species s in species) {
                int breed = (int)Math.Floor(s.averageFitness / sum * NetworkController.Population) - 1;
                for (int i = 0; i < breed; i++)
                    children.Add(s.BreedChild());
            }

            CullSpecies(true);

            while (children.Count + species.Count < NetworkController.Population)
                children.Add(species[NetworkController.random.Next(0, species.Count)].BreedChild());

            foreach (Genome genome in children)
                AddToSpecies(genome);

            generation++;
        }
        public void NextGenome() {
            currentGenome++;
            if (currentGenome >= species[currentSpecies].genomes.Count) {
                currentGenome = 0;
                currentSpecies++;
                if (currentSpecies >= species.Count) {
                    NewGeneration();
                    currentSpecies = 0;
                }
            }
        }

        public void SaveToFile(string file) {
            using (FileStream fs = File.Open(file, FileMode.OpenOrCreate)) {
                using (BinaryWriter bw = new BinaryWriter(fs)) {
                    bw.Write(generation);
                    bw.Write(maxFitness);
                    bw.Write(species.Count);
                    foreach (Species s in species) {
                        bw.Write(s.topFitness);
                        bw.Write(s.staleness);
                        bw.Write(s.genomes.Count);
                        foreach (Genome genome in s.genomes) {
                            bw.Write(genome.fitness);
                            bw.Write(genome.maxNeuron);
                            bw.Write(genome.connectionsRate);
                            bw.Write(genome.linkRate);
                            bw.Write(genome.biasRate);
                            bw.Write(genome.nodeRate);
                            bw.Write(genome.enableRate);
                            bw.Write(genome.disableRate);
                            bw.Write(genome.stepRate);
                            bw.Write(genome.genes.Count);
                            foreach (Gene gene in genome.genes) {
                                bw.Write(gene.into);
                                bw.Write(gene.@out);
                                bw.Write(gene.weight);
                                bw.Write(gene.enabled);
                                bw.Write(gene.innovation);
                            }
                        }
                    }
                }
            }
        }

        public void LoadFile(string file) {
            using (FileStream fs = File.Open(file, FileMode.Open)) {
                using (BinaryReader br = new BinaryReader(fs)) {
                    generation = br.ReadInt32();
                    maxFitness = br.ReadInt32();
                    int scount = br.ReadInt32();
                    for (int i = 0; i < scount; i++) {
                        Species s = new Species();
                        s.topFitness = br.ReadInt32();
                        s.staleness = br.ReadInt32();
                        int gcount = br.ReadInt32();
                        for (int j = 0; j < gcount; j++) {
                            Genome genome = new Genome();
                            genome.fitness = br.ReadInt32();
                            genome.maxNeuron = br.ReadInt32();
                            genome.connectionsRate = br.ReadSingle();
                            genome.linkRate = br.ReadSingle();
                            genome.biasRate = br.ReadSingle();
                            genome.nodeRate = br.ReadSingle();
                            genome.enableRate = br.ReadSingle();
                            genome.disableRate = br.ReadSingle();
                            genome.stepRate = br.ReadSingle();
                            int genecount = br.ReadInt32();
                            for (int k = 0; k < genecount; k++) {
                                Gene gene = new Gene();
                                gene.into = br.ReadInt32();
                                gene.@out = br.ReadInt32();
                                gene.weight = br.ReadSingle();
                                gene.enabled = br.ReadBoolean();
                                gene.innovation = br.ReadInt32();
                                genome.genes.Add(gene);
                            }
                            s.genomes.Add(genome);
                        }
                        species.Add(s);
                    }
                }
            }
        }
    }
    class NetworkController {
        public static Game game;

        public static int InputCount;
        public static int OutputCount;

        public static float PerturbChance = .9f;
        public static int MaxNodes = 1000000;
        public static int Population = 300;

        public static float CrossoverChance = .75f;

        public static float DeltaDisjoint = 2f;
        public static float DeltaWeights = .4f;
        public static float DeltaThreshold = 1f;

        public static int StaleSpecies = 15;

        public static Pool pool;

        public static Random random;

        public static void Initialize(Game game) {
            NetworkController.game = game;
            InputCount = game.gameSize * game.gameSize + 1;
            OutputCount = game.gameSize * game.gameSize;
            
            random = new Random();

            pool = new Pool();
            pool.Initialize();
        }

        public static float[] inputs, outputs;

        struct mv { public Point pt; public float w; }
        public static List<Point> EvaluateCurrent() {
            float[] @in = new float[InputCount];
            
            for (int x = 0; x < game.gameSize; x++)
                for (int y = 0; y < game.gameSize; y++) {
                    float s = -1f;
                    if (!game.tiles[x, y].isExposed) {
                        s = 1f;
                        for (int x2 = Math.Max(0, x - 1); x2 <= Math.Min(game.gameSize - 1, x + 1); x2++)
                            for (int y2 = Math.Max(0, y - 1); y2 <= Math.Min(game.gameSize - 1, y + 1); y2++)
                                if (game.tiles[x2, y2].isExposed)
                                    s += 1f / Math.Max(1f, game.tiles[x2, y2].neighborMineCount);
                    }
                    @in[x + y * game.gameSize] = s;
                }
            @in[InputCount - 1] = 1;
            
            float[] @out = pool.species[pool.currentSpecies].genomes[pool.currentGenome].network.Evaluate(@in);

            inputs = @in;
            outputs = @out;

            List<mv> mvs = new List<mv>();

            for (int i = 0; i < outputs.Length; i++)
                if (outputs[i] > 0)
                    mvs.Add(new mv() { pt = new Point(i % game.gameSize, i / game.gameSize), w = outputs[i] });
            mvs.OrderByDescending(m => m.w);

            List<Point> pts = new List<Point>();
            foreach (mv m in mvs) pts.Add(m.pt);
            return pts;
        }
        
        static int maxIdleTimeout = 3;
        static int idleTimeout = maxIdleTimeout;
        static int lastExposed = 0;
        public static List<Point> curMoves = new List<Point>();

        public static void Update() {
            List<Point> cur = EvaluateCurrent();
            foreach (Point p in cur) {
                game.Move(p.x, p.y);
                if (game.gameOver) break;
            }
            pool.currentFrame++;
            
            if (lastExposed == game.exposedCount)
                idleTimeout--;
            else
                idleTimeout = maxIdleTimeout;

            int fitness = game.moves + game.exposedCount;
            if (fitness == 0)
                fitness = -1;
            if (game.win)
                fitness += 1000;
            else
                fitness -= 500;

            pool.currentFitness = fitness;
            if (fitness > pool.maxFitness)
                pool.maxFitness = fitness;

            if (game.gameOver || idleTimeout < 0) {
                pool.species[pool.currentSpecies].genomes[pool.currentGenome].fitness = fitness;

                idleTimeout = maxIdleTimeout;
                pool.currentSpecies = 0;
                pool.currentGenome = 0;
                while (pool.species[pool.currentSpecies].genomes[pool.currentGenome].fitness != 0)
                    pool.NextGenome();

                new Network(pool.species[pool.currentSpecies].genomes[pool.currentGenome]);
                pool.currentFrame = 0;
                pool.currentFitness = 0;
                inputs = outputs = null;
                game.SetupBoard();
            }

            int measured = 0, total = 0;
            foreach (Species s in pool.species)
                foreach (Genome g in s.genomes) {
                    total++;
                    if (g.fitness != 0)
                        measured++;
                }

            lastExposed = game.exposedCount;
        }
        
        public static void Save(string name) {
            string file = AppDomain.CurrentDomain.BaseDirectory;
            pool.SaveToFile(file + name + ".pool");
        }
        public static void Load(string name) {
            string file = AppDomain.CurrentDomain.BaseDirectory + name + ".pool";
            if (File.Exists(file)) {
                pool = new Pool();
                pool.LoadFile(file);

                idleTimeout = maxIdleTimeout;
                pool.currentSpecies = 0;
                pool.currentGenome = 0;
                while (pool.species[pool.currentSpecies].genomes[pool.currentGenome].fitness != 0)
                    pool.NextGenome();

                new Network(pool.species[pool.currentSpecies].genomes[pool.currentGenome]);
                pool.currentFrame = 0;
                pool.currentFitness = 0;
                inputs = outputs = null;
                game.SetupBoard();
            }
        }

        public static void LoadTop() {
            int maxg = 0, maxs = 0;
            int maxfitness = 0;
            for (int s = 0; s < pool.species.Count; s++)
                for (int g = 0; g < pool.species[s].genomes.Count; g++) {
                    if (pool.species[s].genomes[g].fitness > maxfitness) {
                        maxfitness = pool.species[s].genomes[g].fitness;
                        maxg = g;
                        maxs = s;
                    }
                }

            pool.currentSpecies = maxs;
            pool.currentGenome = maxg;
            new Network(pool.species[pool.currentSpecies].genomes[pool.currentGenome]);
            pool.currentFitness = 0;
            pool.currentFrame = 0;
            inputs = outputs = null;
            game.SetupBoard();
        }
    }
}
