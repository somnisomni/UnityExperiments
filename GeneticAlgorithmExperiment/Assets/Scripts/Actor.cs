using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GeneticAlgorithmExperiment {
    [RequireComponent(typeof(Rigidbody))]
    public class Actor : MonoBehaviour {
        [SerializeField, TextArea]
        private string geneString;

        [SerializeField]
        public int score = 0;

        public Gene ActorGene { get; private set; }

        private float _startTime;
        private List<ScoreSphere> _passedScoreObjects = new();
        private Rigidbody _rigidbody;

        private void Awake() {
            _startTime = Time.time;
            _rigidbody = GetComponent<Rigidbody>();
        }

        public void Init(Gene gene) {
            ActorGene = gene;

            StartCoroutine(Act());
        }

        private IEnumerator Act() {
            for(int i = 0; i < ActorGene.velocity.Length; i++) {
                _rigidbody.linearVelocity = new Vector3(
                    x: ActorGene.velocity[i].x,
                    y: 0.0f,
                    z: ActorGene.velocity[i].y);
                yield return new WaitForSeconds(ActorGene.actTime[i]);
            }

            _rigidbody.linearVelocity = Vector3.zero;
        }

        private void OnGUI() {
            geneString = string.Join(",", ActorGene);
        }

        private void OnTriggerEnter(Collider other) {
            if(other.GetComponent<ScoreSphere>() is not { } scoreSphere) return;

            // Give point(s) for each entrance into score object
            score += scoreSphere.scoreForEachTrigger;

            // Give additional alive-time-based score if the object is not passed before
            if(!_passedScoreObjects.Contains(scoreSphere)) {
                if(scoreSphere.enableTimeBasedFirstTriggerScore) {
                    score += Mathf.FloorToInt(
                        Mathf.Lerp(
                            a: scoreSphere.maxTimeBasedFirstTriggerScore,
                            b: 0,
                            t: Mathf.Clamp01((Time.time - _startTime) / GeneticAlgoManager.MaxGenerationTime)));
                } else {
                    score += scoreSphere.scoreForFirstTrigger;
                }

                _passedScoreObjects.Add(scoreSphere);
            }
        }

        private void OnCollisionEnter(Collision other) {
            if(!other.gameObject.CompareTag("ScoreMinus")) return;

            // Reduce 1 point for each collision with scoreminus object
            score--;
        }
    }
}
