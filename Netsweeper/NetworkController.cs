using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetSweeper {
    class Pool {
        public List<Species> species = new List<Species>();
        public int generation = 0;
        public int currentSpecies = 0;
        public int currentGenome = 0;
        public int currentMove = 0;
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

        public static Point EvaluateCurrent() {
            List<int> inputs = new List<int>();
            for (int x = 0; x < game.gameSize; x++)
                for (int y = 0; y < game.gameSize; y++)
                    inputs.Add(game.tiles[x, y].isMine ? -1 : game.tiles[x, y].neighborMineCount);

            return pool.species[pool.currentSpecies].genomes[pool.currentGenome].network.Evaluate(inputs);
        }

        static Point lastPoint;
        static int pointPenalty = 3;
        public static void Update() {
            Point pt = EvaluateCurrent();
            pool.currentMove++;
            game.Move(pt.x, pt.y);

            if (pool.currentMove == 0 || pt.x != lastPoint.x || pt.y != lastPoint.y)
                pointPenalty = 3;
            else
                pointPenalty--;

            if (game.gameOver || (pool.currentMove > 0 && pt.x == lastPoint.x && pt.y == lastPoint.y && pointPenalty <= 0)) {
                int fitness = game.moves;
                if (game.win)
                    fitness += 10;
                if (fitness == 0)
                    fitness = -1;
                pool.species[pool.currentSpecies].genomes[pool.currentGenome].fitness = fitness;
                if (fitness > pool.maxFitness)
                    pool.maxFitness = fitness;

                pool.currentSpecies = 0;
                pool.currentGenome = 0;
                while (pool.species[pool.currentSpecies].genomes[pool.currentGenome].fitness != 0)
                    pool.NextGenome();

                new Network(pool.species[pool.currentSpecies].genomes[pool.currentGenome]);
                pool.currentMove = 0;
                game.SetupBoard();
            }

            int measured = 0, total = 0;
            foreach (Species s in pool.species)
                foreach (Genome g in s.genomes) {
                    total++;
                    if (g.fitness != 0)
                        measured++;
                }

            lastPoint = pt;
        }
    }
}
