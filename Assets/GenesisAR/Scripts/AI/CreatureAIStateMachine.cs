using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace GenesisAR.AI
{
    // ─────────────────────────────────────────────────────────────
    //  CreatureState – FSM states
    // ─────────────────────────────────────────────────────────────
    public enum CreatureState { Idle, Flocking, Foraging, Hunting, Fleeing, Resting }

    // ─────────────────────────────────────────────────────────────
    //  CreatureAIStateMachine – drives creature behaviour in AR world
    // ─────────────────────────────────────────────────────────────
    [RequireComponent(typeof(NavMeshAgent))]
    public class CreatureAIStateMachine : MonoBehaviour
    {
        [Header("Identity")]
        public string SpeciesID;
        public bool   IsPredator;

        [Header("Stats")]
        [Range(0f, 1f)] public float Hunger      = 0.5f;
        [Range(0f, 1f)] public float Fear        = 0.0f;
        [Range(0f, 1f)] public float Energy      = 1.0f;

        [Header("Sensing")]
        public float SightRadius   = 10f;
        public float FlockRadius   = 5f;
        public float AttackRadius  = 1.5f;

        [Header("Flocking (Boids)")]
        public float SeparationWeight  = 1.5f;
        public float AlignmentWeight   = 1.0f;
        public float CohesionWeight    = 1.0f;

        [Header("State")]
        [SerializeField] private CreatureState _currentState = CreatureState.Idle;
        public CreatureState CurrentState => _currentState;

        // Internal refs
        private NavMeshAgent _agent;
        private Transform    _target;      // prey or food source
        private Transform    _threat;      // predator
        private Vector3      _wanderTarget;
        private float        _stateTimer;
        private static readonly List<CreatureAIStateMachine> _allCreatures = new List<CreatureAIStateMachine>();

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _allCreatures.Add(this);
        }

        private void OnDestroy() => _allCreatures.Remove(this);

        private void Update()
        {
            UpdateSenses();
            RunStateMachine();
            DrainEnergy();
        }

        // ── Sensing ───────────────────────────────────────────────
        private void UpdateSenses()
        {
            _target  = null;
            _threat  = null;
            float closestThreat = float.MaxValue;
            float closestPrey   = float.MaxValue;

            foreach (CreatureAIStateMachine other in _allCreatures)
            {
                if (other == this) continue;
                float dist = Vector3.Distance(transform.position, other.transform.position);
                if (dist > SightRadius) continue;

                if (other.IsPredator && !IsPredator && dist < closestThreat)
                {
                    _threat = other.transform;
                    closestThreat = dist;
                }
                else if (!other.IsPredator && IsPredator && dist < closestPrey)
                {
                    _target = other.transform;
                    closestPrey = dist;
                }
            }

            Fear   = _threat != null ? Mathf.Clamp01(1f - closestThreat / SightRadius) : 0f;
        }

        // ── FSM ───────────────────────────────────────────────────
        private void RunStateMachine()
        {
            _stateTimer += Time.deltaTime;

            // Priority-based transition logic
            if (_threat != null && !IsPredator)         TransitionTo(CreatureState.Fleeing);
            else if (Hunger > 0.7f && IsPredator && _target != null) TransitionTo(CreatureState.Hunting);
            else if (Hunger > 0.6f)                     TransitionTo(CreatureState.Foraging);
            else if (Energy < 0.2f)                     TransitionTo(CreatureState.Resting);
            else if (NearbyFlockCount() > 1)            TransitionTo(CreatureState.Flocking);
            else if (_stateTimer > 5f)                  TransitionTo(CreatureState.Idle);

            switch (_currentState)
            {
                case CreatureState.Idle:      StateIdle();      break;
                case CreatureState.Flocking:  StateFlocking();  break;
                case CreatureState.Foraging:  StateForaging();  break;
                case CreatureState.Hunting:   StateHunting();   break;
                case CreatureState.Fleeing:   StateFleeing();   break;
                case CreatureState.Resting:   StateResting();   break;
            }
        }

        private void TransitionTo(CreatureState next)
        {
            if (_currentState == next) return;
            _currentState = next;
            _stateTimer   = 0f;
        }

        // ── State implementations ─────────────────────────────────
        private void StateIdle()
        {
            if (_stateTimer < 2f) return;
            _wanderTarget = RandomNavPoint(8f);
            _agent.SetDestination(_wanderTarget);
        }

        private void StateFlocking()
        {
            Vector3 separation = Vector3.zero, alignment = Vector3.zero, cohesion = Vector3.zero;
            int count = 0;

            foreach (CreatureAIStateMachine other in _allCreatures)
            {
                if (other == this || other.SpeciesID != SpeciesID) continue;
                float dist = Vector3.Distance(transform.position, other.transform.position);
                if (dist > FlockRadius) continue;

                separation += (transform.position - other.transform.position).normalized / Mathf.Max(dist, 0.1f);
                alignment  += other._agent.velocity;
                cohesion   += other.transform.position;
                count++;
            }

            if (count == 0) { StateIdle(); return; }
            cohesion /= count;

            Vector3 steer = separation * SeparationWeight
                          + alignment.normalized * AlignmentWeight
                          + (cohesion - transform.position).normalized * CohesionWeight;

            _agent.SetDestination(transform.position + steer);
        }

        private void StateForaging()
        {
            if (!_agent.hasPath || _agent.remainingDistance < 0.5f)
                _agent.SetDestination(RandomNavPoint(12f));

            // Simulate eating when arrived
            if (_agent.remainingDistance < 0.5f)
                Hunger = Mathf.Clamp01(Hunger - 0.2f * Time.deltaTime);
        }

        private void StateHunting()
        {
            if (_target == null) { TransitionTo(CreatureState.Idle); return; }
            _agent.SetDestination(_target.position);

            if (Vector3.Distance(transform.position, _target.position) < AttackRadius)
            {
                // Attack: reduce prey and feed self
                CreatureAIStateMachine prey = _target.GetComponent<CreatureAIStateMachine>();
                if (prey != null) Destroy(prey.gameObject);
                Hunger = Mathf.Clamp01(Hunger - 0.5f);
                Energy = Mathf.Clamp01(Energy + 0.3f);
                _target = null;
            }
        }

        private void StateFleeing()
        {
            if (_threat == null) { TransitionTo(CreatureState.Idle); return; }
            Vector3 fleeDir = (transform.position - _threat.position).normalized;
            _agent.SetDestination(transform.position + fleeDir * 10f);
        }

        private void StateResting()
        {
            _agent.ResetPath();
            Energy = Mathf.Clamp01(Energy + 0.1f * Time.deltaTime);
            Hunger = Mathf.Clamp01(Hunger + 0.01f * Time.deltaTime);
            if (Energy > 0.6f) TransitionTo(CreatureState.Idle);
        }

        // ── Helpers ───────────────────────────────────────────────
        private void DrainEnergy()
        {
            float speed = _agent.velocity.magnitude;
            Energy = Mathf.Clamp01(Energy - (0.005f + speed * 0.002f) * Time.deltaTime);
            Hunger = Mathf.Clamp01(Hunger + 0.003f * Time.deltaTime);
        }

        private int NearbyFlockCount()
        {
            int c = 0;
            foreach (CreatureAIStateMachine other in _allCreatures)
            {
                if (other == this || other.SpeciesID != SpeciesID) continue;
                if (Vector3.Distance(transform.position, other.transform.position) < FlockRadius) c++;
            }
            return c;
        }

        private Vector3 RandomNavPoint(float range)
        {
            Vector3 rand = Random.insideUnitSphere * range + transform.position;
            NavMeshHit hit;
            return NavMesh.SamplePosition(rand, out hit, range, NavMesh.AllAreas)
                ? hit.position
                : transform.position;
        }
    }
}
