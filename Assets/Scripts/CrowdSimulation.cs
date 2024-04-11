using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class CrowdSimulation : MonoBehaviour
{
    public float maxSpeed = 3.5f; // Max speed of agents
    public float avoidanceRadius = 1.0f; // Radius for detecting nearby agents

    private NavMeshAgent navMeshAgent;
    private List<Transform> nearbyAgents;

    void Start()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
        nearbyAgents = new List<Transform>();
    }

    void Update()
    {
        // Update nearby agents list
        UpdateNearbyAgents();

        // Calculate new velocity with collision avoidance
        Vector3 newVelocity = CalculateAvoidanceVelocity();

        // Apply velocity to NavMeshAgent
        navMeshAgent.velocity = newVelocity;

        // Limit speed to maxSpeed
        if (navMeshAgent.velocity.magnitude > maxSpeed)
        {
            navMeshAgent.velocity = navMeshAgent.velocity.normalized * maxSpeed;
        }
    }

    void UpdateNearbyAgents()
    {
        nearbyAgents.Clear();

        Collider[] colliders = Physics.OverlapSphere(transform.position, avoidanceRadius);
        foreach (Collider collider in colliders)
        {
            if (collider != gameObject.GetComponent<Collider>())
            {
                nearbyAgents.Add(collider.transform);
            }
        }
    }

    Vector3 CalculateAvoidanceVelocity()
    {
        Vector3 avoidanceVelocity = Vector3.zero;

        foreach (Transform agentTransform in nearbyAgents)
        {
            Vector3 direction = agentTransform.position - transform.position;
            float distance = direction.magnitude;

            // Calculate bearing angle
            float bearingAngle = Vector3.SignedAngle(transform.forward, direction, Vector3.up);

            // Apply avoidance force based on bearing angle
            if (bearingAngle < 90f && bearingAngle > -90f)
            {
                float avoidanceFactor = 1.0f - Mathf.Abs(bearingAngle) / 90f;
                avoidanceVelocity -= direction.normalized * avoidanceFactor / distance;
            }
        }

        return avoidanceVelocity.normalized * maxSpeed;
    }
}
