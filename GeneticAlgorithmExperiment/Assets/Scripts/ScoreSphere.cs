using UnityEngine;

namespace GeneticAlgorithmExperiment {
    public class ScoreSphere : MonoBehaviour {
        [SerializeField]
        public int scoreForEachTrigger = 1;

        [SerializeField]
        public int scoreForFirstTrigger = 100;

        [SerializeField]
        public bool enableTimeBasedFirstTriggerScore = true;

        [SerializeField]
        public int maxTimeBasedFirstTriggerScore = 100;

        private void Awake() {
            gameObject.tag = "Score";

            if(GetComponent<Collider>() is { } col) {
                col.isTrigger = true;
            }
        }
    }
}
