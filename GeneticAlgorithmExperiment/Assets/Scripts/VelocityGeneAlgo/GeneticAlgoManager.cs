using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;

namespace VelocityGeneAlgo {
    [Serializable]
    public record Gene {
        [SerializeField]
        public Vector2[] velocity;

        [SerializeField]
        public float[] actTime;

        public override string ToString() {
            string str = "";

            for(int i = 0; i < velocity.Length; i++) {
                str += $"v[{velocity[i].x},{velocity[i].y}],t[{actTime[i]}s]\n";
            }

            return str;
        }
    }
    
    public class GeneticAlgoManager : MonoBehaviour {
        [SerializeField]
        private GameObject actorPrefab;

        [SerializeField]
        private TMP_Text generationText;

        [SerializeField]
        private TMP_Text topGeneText;
        
        [SerializeField]
        private TMP_Text topGeneScoreText;

        [SerializeField]
        private TMP_Text scoreRankText;
    
        public const int GeneLength = 32;
        public const int ActorCountInGeneration = 200;
        public const int TopRankCount = 10;
        public const float MutationRate = 0.075f;
        public const float MaxGenerationTime = 10.0f;
        public const float RandomVelocityRange = 10.0f;
        public const float RandomActTimeRange = 1.0f;

        public int CurrentGeneration { get; private set; } = 1;
        public Gene CurrentTopGene { get; private set; }
        public int CurrentTopGeneScore { get; private set; }
        public Gene[] CurrentGenes { get; private set; } = new Gene[ActorCountInGeneration];

        private readonly List<Actor> _geneActors = new();

        private void Awake() {
            for(int i = 0; i < ActorCountInGeneration; i++) {
                CurrentGenes[i] = GenerateRandomGene();

                var actor = Instantiate(actorPrefab, new Vector3(0.0f, 0.5f, 0.0f), Quaternion.identity).GetComponent<Actor>();
                actor.Init(CurrentGenes[i]);
                _geneActors.Add(actor);
            }

            CurrentTopGene = CurrentGenes[0];
            StartCoroutine(DoGeneration());

            Time.timeScale = 3.0f;
        }

        private void Update() {
            generationText.text = CurrentGeneration.ToString();
            topGeneText.text = CurrentTopGene.ToString();
            topGeneScoreText.text = CurrentTopGeneScore.ToString();
            scoreRankText.text = string.Join(
                separator: "\n",
                values: _geneActors.Take(20)
                    .OrderByDescending(actor => actor.score)
                    .Select((actor, index) => $"#{index + 1}. {actor.score}  (Idx #{_geneActors.IndexOf(actor)})"));
        }

        private IEnumerator DoGeneration() {
            while(true) {
                yield return new WaitForSeconds(MaxGenerationTime);
                StartNewGeneration();
            }
        }

        private void StartNewGeneration() {
            // Sort actors by score and take genes
            Actor[] orderedActors = _geneActors
                .OrderByDescending(actor => actor.score)
                .ToArray();
            Gene[] orderedGenes = orderedActors
                .Select(actor => actor.ActorGene)
                .ToArray();
            CurrentTopGene = orderedGenes[0];
            CurrentTopGeneScore = orderedActors[0].score;
            
            // Prepare for crossover
            var crossoversList = new List<Gene>();
            
            // Top genes will be survived to the next generation
            for(int i = 0; i < TopRankCount; i++) {
                crossoversList.Add(orderedGenes[i]);
            }
            
            // Remaining of the genes (including top genes) will be crossed over
            for(int i = 0; i < orderedGenes.Length - 1; i++) {
                Gene[] co = CrossoverGeneHalfChanceSwap(orderedGenes[i], orderedGenes[i + 1]);
                crossoversList.AddRange(co);
            }
            
            // Cut crossover list to the same length as the original gene list
            // This assumes almost half of the gene will be cut, as `crossoversList` would be almost twice much as the original gene list
            Gene[] crossovers = crossoversList.Take(ActorCountInGeneration).ToArray();
            
            // Replace last 10% of the genes with random genes
            for(int i = (int)(crossovers.Length * 0.9f); i < crossovers.Length; i++) {
                crossovers[i] = GenerateRandomGene();
            }
            
            // Random mutation
            for(int i = 0; i < crossovers.Length; i++) {
                crossovers[i] = MutateGene(crossovers[i]);
            }
            
            // Update current genes
            CurrentGenes = crossovers.ToArray();
            
            // Update current generation
            CurrentGeneration += 1;
            
            // Destroy existing actors
            foreach(Actor existingActor in _geneActors) {
                Destroy(existingActor.gameObject);
            }
            _geneActors.Clear();

            // Create new actors with new genes
            foreach(Gene gene in CurrentGenes) {
                var actor = Instantiate(actorPrefab, new Vector3(0.0f, 0.5f, 0.0f), Quaternion.identity).GetComponent<Actor>();
                actor.Init(gene);
                _geneActors.Add(actor);
            }
        }

        private Gene[] PickTopRankGenes() {
            var topGenes = new Gene[TopRankCount];
            Actor[] actorSorted = _geneActors.OrderByDescending(actor => actor.score).ToArray();

            for(int i = 0; i < TopRankCount; i++) {
                topGenes[i] = actorSorted[i].ActorGene;
            }
            
            return topGenes;
        }

        private static Gene[] CrossoverGeneHalfChanceSwap(Gene a, Gene b) {
            // When crossover, each single gene will be swapped between two genes, by 50% chance
            var newGenes = new Gene[2] {
                new() {
                    velocity = new Vector2[GeneLength],
                    actTime = new float[GeneLength],
                },
                new() {
                    velocity = new Vector2[GeneLength],
                    actTime = new float[GeneLength],
                },
            };
            
            for(int i = 0; i < GeneLength; i++) {
                Vector2 aVel = a.velocity[i];
                float aTime = a.actTime[i];
                Vector2 bVel = b.velocity[i];
                float bTime = b.actTime[i];
                
                if(Random.value < 0.5f) {
                    newGenes[0].velocity[i] = aVel;
                    newGenes[0].actTime[i] = aTime;
                    newGenes[1].velocity[i] = bVel;
                    newGenes[1].actTime[i] = bTime;
                } else {
                    newGenes[0].velocity[i] = bVel;
                    newGenes[0].actTime[i] = bTime;
                    newGenes[1].velocity[i] = aVel;
                    newGenes[1].actTime[i] = aTime;
                }
            }
            
            return newGenes;
        }
        
        private static Gene[] CrossoverGeneSinglePointSwap(Gene a, Gene b) {
            // When crossover, the first half of the gene is from a, and the second half is from b.
            var newGenes = new Gene[2] {
                new() {
                    velocity = new Vector2[GeneLength],
                    actTime = new float[GeneLength],
                },
                new() {
                    velocity = new Vector2[GeneLength],
                    actTime = new float[GeneLength],
                },
            };
            
            for(int i = 0; i < GeneLength; i++) {
                if(i < GeneLength / 2) {
                    newGenes[0].velocity[i] = a.velocity[i];
                    newGenes[0].actTime[i] = a.actTime[i];
                    newGenes[1].velocity[i] = b.velocity[i];
                    newGenes[1].actTime[i] = b.actTime[i];
                } else {
                    newGenes[0].velocity[i] = b.velocity[i];
                    newGenes[0].actTime[i] = b.actTime[i];
                    newGenes[1].velocity[i] = a.velocity[i];
                    newGenes[1].actTime[i] = a.actTime[i];
                }
            }
            
            return newGenes;
        }


        private static Gene MutateGene(Gene original) {
            if(original == null) {
                Debug.Log(original);
                return null;
            }
            
            var velocity = new Vector2[GeneLength];
            var actTime = new float[GeneLength];

            for(int i = 0; i < GeneLength; i++) {
                velocity[i] = original.velocity[i];
                actTime[i] = original.actTime[i];
                
                if(!(Random.Range(0.0f, 1.0f) < MutationRate)) continue;
                
                velocity[i] = GenerateRandomVelocity();
                actTime[i] = GenerateRandomActTime();
            }

            return new Gene {
                velocity = velocity,
                actTime = actTime,
            };
        }
    
        private static Gene GenerateRandomGene() {
            var velocity = new Vector2[GeneLength];
            var actTime = new float[GeneLength];
            
            for(int i = 0; i < GeneLength; i++) {
                velocity[i] = GenerateRandomVelocity();
                actTime[i] = GenerateRandomActTime();
            }
        
            return new Gene {
                velocity = velocity,
                actTime = actTime,
            };
        }

        private static Vector2 GenerateRandomVelocity() {
            return new Vector2(
                x: Random.Range(-RandomVelocityRange, RandomVelocityRange),
                y: Random.Range(-RandomVelocityRange, RandomVelocityRange));
        }
        
        private static float GenerateRandomActTime() {
            return Random.Range(0.0f, RandomActTimeRange);
        }
    }
}