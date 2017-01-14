using UnityEngine;
using System.Collections.Generic;

namespace BioIK {
	//----------------------------------------------------------------------------------------------------
	//====================================================================================================
	//Hybrid Genetic Swarm Algorithm
	//====================================================================================================
	//----------------------------------------------------------------------------------------------------
	public class Evolution {
		private Model Model;
		
		private float[] Solution;
		private float[] Storage;
		private float[] Probabilities;

		private Individual[] Population;
		private Individual[] Offspring;

		private int Size;
		private int Elites;
		private int Dimensionality;

		private JointMotion[] Motions;

		public Evolution(Model model, int size, int elites) {
			Model = model;
			Size = size;
			Elites = elites;
			Motions = Model.GetMotions();
			Dimensionality = Motions.Length;
			
			Population = new Individual[Size];
			Offspring = new Individual[Size];

			for(int i=0; i<Size; i++) {
				Population[i] = new Individual(Dimensionality);
				Offspring[i] = new Individual(Dimensionality);
			}

			Solution = Model.GetTargetConfiguration();
			Storage = new float[Dimensionality];
			Probabilities = new float[Size];

			Initialize();
			TryUpdateSolution();
		}

		//Returns whether the solution could be improved
		public bool Evolve() {
			//Let elites survive
			for(int i=0; i<Elites; i++) {
				Survive(i);
			}

			//Create mating pool
			List<Individual> pool = new List<Individual>(Population);

			//Evolve offspring
			for(int i=Elites; i<Size; i++) {
				if(pool.Count > 0) {
					Individual parentA = Select(pool);
					Individual parentB = Select(pool);
					Individual prototype = Select(pool);

					Reproduce(i, parentA, parentB, prototype);

					if(Offspring[i].Fitness < parentA.Fitness) {
						pool.Remove(parentA);
					}
					if(Offspring[i].Fitness < parentB.Fitness) {
						pool.Remove(parentB);
					}
				} else {
					Reroll(i);
				}
			}

			//Swap population and offspring
			Swap(ref Population, ref Offspring);

			//Finalize
			SortByFitness();
			ComputeExtinctions();

			//Check improvement and wipeout criterion
			if(!TryUpdateSolution()) {
				if(CheckWipeout()) {
					Initialize();
					return TryUpdateSolution();
				} else {
					return false;
				}
			} else {
				return true;
			}
		}

		public Model GetModel() {
			return Model;
		}

		public Individual[] GetPopulation() {
			return Population;
		}

		public float[] GetSolution() {
			return Solution;
		}

		public int GetDimensionality() {
			return Dimensionality;
		}

		public int GetSize() {
			return Size;
		}

		public int GetElites() {
			return Elites;
		}

		private void Initialize() {
			for(int i=0; i<Dimensionality; i++) {
				Population[0].Genes[i] = Solution[i];
				Population[0].Gradient[i] = 0f;
			}
			Population[0].Fitness = ComputeFitness(Population[0].Genes, false, false);

			for(int i=1; i<Size; i++) {
				for(int j=0; j<Dimensionality; j++) {
					Population[i].Genes[j] = Random.Range(Motions[j].GetLowerLimit(), Motions[j].GetUpperLimit());
					Population[i].Gradient[j] = 0f;
				}
				Population[i].Fitness = ComputeFitness(Population[i].Genes, false, false);
			}

			SortByFitness();
			ComputeExtinctions();
		}

		private void Survive(int index) {
			Individual survivor = Population[index];
			Individual offspring = Offspring[index];

			for(int i=0; i<Dimensionality; i++) {
				offspring.Genes[i] = survivor.Genes[i];
				offspring.Gradient[i] = survivor.Gradient[i];
			}

			Exploit(offspring);
		}

		private void Reproduce(int index, Individual parentA, Individual parentB, Individual prototype) {
			Individual offspring = Offspring[index];
			float weight;
			for(int i=0; i<Dimensionality; i++) {
				//Recombination
				weight = Random.value;
				offspring.Genes[i] = weight*parentA.Genes[i] + (1f-weight)*parentB.Genes[i] + Random.value*parentA.Gradient[i] + Random.value*parentB.Gradient[i];

				//Store
				Storage[i] = offspring.Genes[i];

				//Mutation
				if(Random.value < GetMutationProbability(parentA, parentB)) {
					offspring.Genes[i] += Random.Range(-1f, 1f) * GetMutationStrength(Motions[i], parentA, parentB);
				}

				//Adoption
				weight = Random.value;
				offspring.Genes[i] += 
					weight * Random.value * (0.5f * (parentA.Genes[i] + parentB.Genes[i]) - offspring.Genes[i])
					+ (1f-weight) * Random.value * (prototype.Genes[i] - offspring.Genes[i]);

				//Clip
				offspring.Genes[i] = Clip(offspring.Genes[i], Motions[i]);

				//Evolutionary Gradient
				offspring.Gradient[i] = Random.value*offspring.Gradient[i] + (offspring.Genes[i] - Storage[i]);
			}

			//Fitness
			offspring.Fitness = ComputeFitness(offspring.Genes, false, false);
		}

		private void Reroll(int index) {
			Individual offspring = Offspring[index];
			for(int i=0; i<Dimensionality; i++) {
				offspring.Genes[i] = Random.Range(Motions[i].GetLowerLimit(), Motions[i].GetUpperLimit());
				offspring.Gradient[i] = 0f;
			}
			offspring.Fitness = ComputeFitness(offspring.Genes, false, false);
		}

		private void Exploit(Individual individual) {
			float fitnessSum = 0f;
			for(int i=0; i<Dimensionality; i++) {
				float fitness = ComputeFitness(individual.Genes, false, true);
				float heuristicError = Model.Motions[i].Node.HeuristicError;

				float inc = Clip(individual.Genes[i] + Random.Range(0f, heuristicError), Motions[i]) - individual.Genes[i];
				individual.Genes[i] += inc;
				float incFitness = ComputeFitness(individual.Genes, false, false);
				individual.Genes[i] -= inc;

				float dec = Clip(individual.Genes[i] - Random.Range(0f, heuristicError), Motions[i]) - individual.Genes[i];
				individual.Genes[i] += dec;
				float decFitness = ComputeFitness(individual.Genes, false, false);
				individual.Genes[i] -= dec;

				if(incFitness <= decFitness && incFitness < fitness) {
					individual.Genes[i] += inc;
					individual.Gradient[i] = Random.value*individual.Gradient[i] + inc;
					fitnessSum += incFitness;
				} else if(decFitness <= incFitness && decFitness < fitness) {
					individual.Genes[i] += dec;
					individual.Gradient[i] = Random.value*individual.Gradient[i] + dec;
					fitnessSum += decFitness;
				} else {
					fitnessSum += fitness;
				}
			}
			individual.Fitness = fitnessSum/(float)Dimensionality;
		}

		private bool CheckWipeout() {
			for(int i=0; i<Dimensionality; i++) {
				Storage[i] = Population[0].Genes[i];
			}
			for(int i=0; i<Dimensionality; i++) {
				float fitness = ComputeFitness(Storage, true, true);
				float heuristicError = Model.Motions[i].Node.HeuristicError;

				float inc = Clip(Storage[i] + Random.Range(0f, heuristicError), Motions[i]) - Storage[i];
				Storage[i] += inc;
				float incFitness = ComputeFitness(Storage, true, false);
				Storage[i] -= inc;

				float dec = Clip(Storage[i] - Random.Range(0f, heuristicError), Motions[i]) - Storage[i];
				Storage[i] += dec;
				float decFitness = ComputeFitness(Storage, true, false);
				Storage[i] -= dec;

				if(incFitness < fitness || decFitness < fitness) {
					return false;
				}
			}
			return true;
		}

		private bool TryUpdateSolution() {
			float solutionFitness = ComputeFitness(Solution, true, false);
			float candidateFitness = ComputeFitness(Population[0].Genes, true, false);
			if(candidateFitness < solutionFitness) {
				for(int i=0; i<Dimensionality; i++) {
					Solution[i] = Population[0].Genes[i];
				}
				return true;
			} else {
				return false;
			}
		}

		private Individual Select(List<Individual> pool) {
			int count = pool.Count;
			float rankSum = count*(count+1f) / 2f;
			for(int i=0; i<count; i++) {
				Probabilities[i] = (count-i)/rankSum;
			}
			return pool[GetRandomWeightedIndex(Probabilities, count)];
		}
		
		private int GetRandomWeightedIndex(float[] probabilities, int count) {
			float weightSum = 0f;
			for(int i=0; i<count; i++) {
				weightSum += probabilities[i];
			}
			float rVal = Random.value*weightSum;
			for(int i=0; i<count; i++) {
				rVal -= probabilities[i];
				if(rVal <= 0f) {
					return i;
				}
			}
			return count-1;
		}

		private float GetMutationProbability(Individual parentA, Individual parentB) {
			float extinction = 0.5f * (parentA.Extinction + parentB.Extinction);
			float inverse = 1f/Dimensionality;
			return extinction * (1f-inverse) + inverse;
		}

		private float GetMutationStrength(JointMotion motion, Individual parentA, Individual parentB) {
			float extinction = 0.5f * (parentA.Extinction + parentB.Extinction);
			float span = motion.GetUpperLimit() - motion.GetLowerLimit();
			return span * extinction;
		}

		private void SortByFitness() {
			System.Array.Sort(Population,
				delegate(Individual a, Individual b) {
					return a.Fitness.CompareTo(b.Fitness);
				}
			);
		}

		private void ComputeExtinctions() {
			float min = Population[0].Fitness;
			float max = Population[Size-1].Fitness;
			for(int i=0; i<Size; i++) {
				float grading = (float)i/((float)Size-1);
				Population[i].Extinction = (Population[i].Fitness + min*(grading-1f)) / max;
			}
		}

		private float Clip(float gene, JointMotion motion) {
			if(motion.Joint.Type == JointType.Continuous) {
				/*
				while(gene > Mathf.PI) {
					gene -= Mathf.PI;
				}
				while(gene < -Mathf.PI) {
					gene += Mathf.PI;
				}
				*/
			} else {
				gene = Mathf.Clamp(gene, motion.GetLowerLimit(), motion.GetUpperLimit());
			}
			return gene;
			//return motion.ConstrainToLimits(gene);
		}

		private float ComputeFitness(float[] genes, bool balanced, bool backpropagate) {
			float fitnessSum = 0f;
			Model.ApplyConfiguration(genes);
			for(int i=0; i<Model.Tips.Length; i++) {
				float fitness = 0f;
				IKTip tip = Model.Tips[i].Tip;
				Model.Node node = Model.Tips[i].Node;
				switch(tip.Objective.Type) {
					case IKTip.ObjectiveType.Position:
					fitness = tip.Weight * node.ComputeTranslationalDistance(tip.TPX, tip.TPY, tip.TPZ) / node.ComputeAngularScale();
					break;

					case IKTip.ObjectiveType.Orientation:
					fitness = tip.Weight * node.ComputeRotationalDistance(tip.TRX, tip.TRY, tip.TRZ, tip.TRW);
					break;

					case IKTip.ObjectiveType.Pose:
					if(balanced) {
						fitness = 
						tip.Weight *
						(0.5f * node.ComputeTranslationalDistance(tip.TPX, tip.TPY, tip.TPZ) / node.ComputeAngularScale()
						+ 0.5f * node.ComputeRotationalDistance(tip.TRX, tip.TRY, tip.TRZ, tip.TRW));
					} else {
						float weight = Random.value;
						fitness =
						tip.Weight *
						(weight * node.ComputeTranslationalDistance(tip.TPX, tip.TPY, tip.TPZ) / node.ComputeAngularScale()
						+ (1f-weight) * node.ComputeRotationalDistance(tip.TRX, tip.TRY, tip.TRZ, tip.TRW));
					}
					break;

					case IKTip.ObjectiveType.LookAt:
					fitness = tip.Weight * node.ComputeDirectionalError(tip.TPX, tip.TPY, tip.TPZ, tip.Objective.Direction);
					break;
				}
				if(backpropagate) {
					node.BackpropagateHeuristicError(fitness);
				}
				fitnessSum += fitness;
			}
			return fitnessSum;
		}

		private void Swap(ref Individual[] a, ref Individual[] b) {
			Individual[] tmp = a;
			a = b;
			b = tmp;
		}

		public class Individual {
			public float[] Genes;
			public float[] Gradient;
			public float Extinction;
			public float Fitness;

			public Individual(int dimensionality) {
				Genes = new float[dimensionality];
				Gradient = new float[dimensionality];
			}
		}
	}
}