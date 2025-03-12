using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VelocityGeneAlgo {
    [RequireComponent(typeof(Rigidbody))]
    public class Actor : MonoBehaviour {
        [SerializeField, TextArea]
        private string geneString;

        [SerializeField]
        public int score = 0;

        public Gene ActorGene { get; private set; }

        private const int MaxTimeScore = 100;
        private float _startTime;
        private List<GameObject> _passedScoreObjects = new();
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
            if(!other.CompareTag("Score")) return;

            // Give 1 point for each entrance into score object
            score++;

            // Give additional alive-time-based score if the object is not passed before
            if(!_passedScoreObjects.Contains(other.gameObject)) {
                score += Mathf.FloorToInt(Mathf.Lerp(MaxTimeScore, 0, Mathf.Clamp01((Time.time - _startTime) / GeneticAlgoManager.MaxGenerationTime)));
                _passedScoreObjects.Add(other.gameObject);
            }
        }
    }
}
