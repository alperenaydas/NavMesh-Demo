using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

public class Agent : MonoBehaviour
{
    [SerializeField] private float m_DestinationDistanceOffset;
    
    private NavMeshAgent m_NavMeshAgent;
    private Action WanderTargetChanged;
    private Action<AgentState> AgentStateChanged;

    private Vector3 m_OrderDestinationPos;

    private AgentState m_AgentState;
    private Vector3? m_WanderTarget;
    
    public AgentState AgentState
    {
        get => m_AgentState;
        set
        {
            m_AgentState = value;
            AgentStateChanged?.Invoke(AgentState);
        }
    }
    public Vector3? WanderTarget
    {
        get => m_WanderTarget;
        set
        {
            if (WanderTarget == value) return;
            m_WanderTarget = value;
            WanderTargetChanged?.Invoke();
        }
    }

    private bool m_CanWander => AgentState != AgentState.Idle;
    
    

    private void Awake()
    {
        m_NavMeshAgent = GetComponent<NavMeshAgent>();
        WanderTargetChanged += OnWanderTargetChanged;
        AgentStateChanged += OnAgentStateChanged;
    }

    private void Start()
    {
        Invoke(nameof(SpawnOrder), Random.Range(15, 25));
        AgentState = AgentState.Wandering;
    }

    private void Update()
    {
        if (WanderTarget.HasValue && AgentState != AgentState.Idle)
        {
            if (!m_NavMeshAgent.pathPending)
            {
                if (m_NavMeshAgent.remainingDistance <= m_NavMeshAgent.stoppingDistance)
                {
                    if (!m_NavMeshAgent.hasPath || m_NavMeshAgent.velocity.sqrMagnitude == 0f)
                    {
                        WanderTarget = null;
                    }
                }
            }
            
        }
    }

    private void OnWanderTargetChanged()
    {
        if (WanderTarget.HasValue) m_NavMeshAgent.SetDestination(WanderTarget.Value);
        else
        {
            switch (AgentState)
            {
                case AgentState.Wandering:
                    SetRandomWanderTarget();
                    break;
                case AgentState.OrderFound:
                    AgentState = AgentState.OrderTaken;
                    break;
                case AgentState.OrderTaken:
                    AgentState = AgentState.OrderComplete;
                    break;
            }
        }
    }

    private void OnAgentStateChanged(AgentState agentState)
    {
        switch (agentState)
        {
            case AgentState.Idle:
                WanderTarget = null;
                break;
            case AgentState.Wandering:
                SetRandomWanderTarget();
                break;
            case AgentState.OrderFound:
                RandomNavMeshPos(Vector3.zero, out var orderPos, 20);
                RandomNavMeshPos(orderPos, out m_OrderDestinationPos, 10);
                WanderTarget = orderPos;
                break;
            case AgentState.OrderTaken:
                WanderTarget = m_OrderDestinationPos;
                Debug.Log("Money earning start");
                break;
            case AgentState.OrderComplete:
                Debug.Log("Money earned!");
                AgentState = AgentState.Wandering;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(agentState), agentState, null);
        }
    }

    private void SpawnOrder()
    {
        Debug.Log("An order found");
        if (AgentState != AgentState.Wandering)
        {
            Debug.Log("No courier found for this order, cancelling order...");
            Invoke(nameof(SpawnOrder), Random.Range(15, 25));
            return;
        }
        AgentState = AgentState.OrderFound;
        Invoke(nameof(SpawnOrder), Random.Range(15, 25));
    }

    private void RandomNavMeshPos(Vector3 startPos, out Vector3 foundPos, float offset)
    {
        NavMeshHit hit;
        bool sample;
        var dir = Random.insideUnitCircle.normalized;
        var offsetVector = new Vector3(dir.x, 0f, dir.y) * offset;
        do
        {
            sample = NavMesh.SamplePosition(startPos + offsetVector, out hit, offset, NavMesh.AllAreas);
            offsetVector = Quaternion.Euler(Vector3.up * 18f) * offsetVector;
        } while (!sample);

        foundPos = hit.position;
    }

    private void SetRandomWanderTarget()
    {
        RandomNavMeshPos(transform.position, out var newDestination, m_DestinationDistanceOffset);
        WanderTarget = newDestination;
    }
}

public enum AgentState
{
    Idle,
    Wandering,
    OrderFound,
    OrderTaken,
    OrderComplete
}