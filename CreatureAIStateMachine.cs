using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using GenesisAR.Genetics;

namespace GenesisAR.AI
{
    public enum AIState
    {
        IdleFlocking,
        Foraging,
        Hunting,
        Fleeing
    }

    [RequireComponent(typeof(CreaturePhenotype))]
    [RequireComponent(typeof(NavMeshAgent))]
    public class CreatureAIStateMachine : MonoBehaviour
    {
        [Header("State Machine Status")]
        public AIState currentState = AIState.IdleFlocking;

        [Header("Internal Vitals")]
        [Range(0f, 100f)] public float hunger = 50f;
        [Range(0f, 100f)] public float health = 100f;
        public bool isPredator = false;

        [Header("Perception & Flocking Settings")]
        public float perceptionRadius = 12f;
        public float attackRange = 1.8f;
        public LayerMask creatureLayerMask;
        public LayerMask foodSourceLayerMask;

        [Header("Flocking Weights")]
        public float neighborDistance = 5f;
        public float separationWeight = 1.5f;
        public float cohesionWeight = 1.0f;

        private NavMeshAgent navAgent;
        private CreaturePhenotype phenotype;
        private Transform currentTarget;

        private void Awake()
        {
            navAgent = GetComponent<NavMeshAgent>();
            phenotype = GetComponent<CreaturePhenotype>();
        }

        private void Start()
        {
            if (phenotype != null)
            {
                navAgent.speed = phenotype.moveSpeed;
            }

            StartCoroutine(BehaviorUpdateRoutine());
        }

        private void Update()
        {
            hunger = Mathf.Clamp(hunger + (Time.deltaTime * 1.5f), 0f, 100f);

            switch (currentState)
            {
                case AIState.IdleFlocking:
                    ExecuteFlockingBehavior();
                    break;
                case AIState.Foraging:
                    ExecuteForagingBehavior();
                    break;
                case AIState.Hunting:
                    ExecuteHuntingBehavior();
                    break;
                case AIState.Fleeing:
                    ExecuteFleeingBehavior();
                    break;
            }
        }

        private IEnumerator BehaviorUpdateRoutine()
        {
            while (true)
            {
                EvaluateStateTransitions();
                yield return new WaitForSeconds(0.5f);
            }
        }

        private void EvaluateStateTransitions()
        {
            Collider[] neighbors = Physics.OverlapSphere(transform.position, perceptionRadius, creatureLayerMask);

            if (!isPredator)
            {
                foreach (var col in neighbors)
                {
                    CreatureAIStateMachine otherAI = col.GetComponent<CreatureAIStateMachine>();
                    if (otherAI != null && otherAI.isPredator)
                    {
                        currentTarget = otherAI.transform;
                        currentState = AIState.Fleeing;
                        return;
                    }
                }
            }

            if (hunger > 40f)
            {
                if (isPredator)
                {
                    Transform nearestPrey = FindNearestTarget(neighbors, other => !other.isPredator);
                    if (nearestPrey != null)
                    {
                        currentTarget = nearestPrey;
                        currentState = AIState.Hunting;
                        return;
                    }
                }
                else
                {
                    Collider[] foodSources = Physics.OverlapSphere(transform.position, perceptionRadius, foodSourceLayerMask);
                    if (foodSources.Length > 0)
                    {
                        currentTarget = foodSources[0].transform;
                        currentState = AIState.Foraging;
                        return;
                    }
                }
            }

            currentTarget = null;
            currentState = AIState.IdleFlocking;
        }

        private void ExecuteFlockingBehavior()
        {
            Collider[] neighbors = Physics.OverlapSphere(transform.position, neighborDistance, creatureLayerMask);
            Vector3 separation = Vector3.zero;
            Vector3 cohesion = Vector3.zero;
            int count = 0;

            foreach (var col in neighbors)
            {
                if (col.gameObject != gameObject)
                {
                    Vector3 diff = transform.position - col.transform.position;
                    separation += diff.normalized / Mathf.Max(diff.magnitude, 0.1f);
                    cohesion += col.transform.position;
                    count++;
                }
            }

            if (count > 0)
            {
                cohesion = (cohesion / count) - transform.position;
                Vector3 flockingDirection = (separation * separationWeight) + (cohesion * cohesionWeight);
                Vector3 targetPos = transform.position + flockingDirection.normalized * 3f;

                if (NavMesh.SamplePosition(targetPos, out NavMeshHit hit, 4f, NavMesh.AllAreas))
                {
                    navAgent.SetDestination(hit.position);
                }
            }
        }

        private void ExecuteForagingBehavior()
        {
            if (currentTarget == null)
            {
                return;
            }

            navAgent.SetDestination(currentTarget.position);

            if (Vector3.Distance(transform.position, currentTarget.position) <= attackRange)
            {
                hunger = Mathf.Max(0f, hunger - 30f);
                currentTarget = null;
                currentState = AIState.IdleFlocking;
                Debug.Log($"[Genesis AR AI] {gameObject.name} foraged food source.");
            }
        }

        private void ExecuteHuntingBehavior()
        {
            if (currentTarget == null)
            {
                return;
            }

            navAgent.SetDestination(currentTarget.position);

            if (Vector3.Distance(transform.position, currentTarget.position) <= attackRange)
            {
                CreatureAIStateMachine prey = currentTarget.GetComponent<CreatureAIStateMachine>();
                if (prey != null)
                {
                    prey.health -= 50f;
                    hunger = Mathf.Max(0f, hunger - 50f);
                    if (prey.health <= 0f)
                    {
                        currentTarget = null;
                        currentState = AIState.IdleFlocking;
                    }

                    Debug.Log($"[Genesis AR AI] {gameObject.name} hunted prey {prey.gameObject.name}.");
                }
            }
        }

        private void ExecuteFleeingBehavior()
        {
            if (currentTarget == null)
            {
                return;
            }

            Vector3 runDirection = (transform.position - currentTarget.position).normalized;
            Vector3 fleePos = transform.position + runDirection * 6f;

            if (NavMesh.SamplePosition(fleePos, out NavMeshHit hit, 6f, NavMesh.AllAreas))
            {
                navAgent.SetDestination(hit.position);
            }
        }

        private Transform FindNearestTarget(Collider[] candidates, System.Predicate<CreatureAIStateMachine> condition)
        {
            Transform nearest = null;
            float minDistance = float.MaxValue;

            foreach (var col in candidates)
            {
                CreatureAIStateMachine ai = col.GetComponent<CreatureAIStateMachine>();
                if (ai != null && ai.gameObject != gameObject && condition(ai))
                {
                    float dist = Vector3.Distance(transform.position, col.transform.position);
                    if (dist < minDistance)
                    {
                        minDistance = dist;
                        nearest = col.transform;
                    }
                }
            }

            return nearest;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = isPredator ? Color.red : Color.green;
            Gizmos.DrawWireSphere(transform.position, perceptionRadius);
        }
    }
}
