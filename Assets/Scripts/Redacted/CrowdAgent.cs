using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class CrowdAgent : MonoBehaviour
{
    private NavMeshAgent agent;
    private Rigidbody rb;
    private List<Vector3> destinations = new List<Vector3>();
    private Vector3 currentDestination;

    // Parameters for steering behaviors
    public float maxSpeed = 5f;
    public float neighborRadius = 5f;
    public float separationWeight = 0.5f;
    public float avoidanceWeight = 0.5f;
    public float cohesionWeight = 0.5f;
    public float alignmentWeight = 1f;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();
        agent.speed = maxSpeed;
        SetDestinations(); // Initialize destinations for this agent
        SetNewDestination(); // Choose a random destination to start

        agent.velocity = agent.destination.normalized;
    }

    void SetDestinations()
    {
        // Add your destinations here or generate them dynamically
        destinations.Add(new Vector3(5f, 0f, 5f));
        destinations.Add(new Vector3(-5f, 0f, 5f));
        destinations.Add(new Vector3(5f, 0f, -5f));
        destinations.Add(new Vector3(-5f, 0f, -5f));
    }

    void SetNewDestination()
    {
        // Choose a random destination from the list
        currentDestination = destinations[Random.Range(0, destinations.Count)];
        agent.SetDestination(currentDestination);
    }

    void Update()
    {
        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            SetNewDestination(); // Choose a new destination when reached the current one
        }

        // Perform crowd steering behaviors
        PerformSteeringBehaviors();

        //rb.position = agent.nextPosition;
        //rb.rotation = agent.transform.rotation;
    }

    void PerformSteeringBehaviors()
    {
        Vector3 separationForce = Vector3.zero;
        Vector3 avoidanceForce = Vector3.zero;
        Vector3 cohesionForce = Vector3.zero;
        Vector3 alignmentForce = Vector3.zero;

        Collider[] nearbyColliders = Physics.OverlapSphere(transform.position, neighborRadius);
        foreach (var collider in nearbyColliders)
        {
            if (collider.CompareTag("Player") && collider.transform != transform)
            {
                Vector3 toOther = collider.transform.position - transform.position;
                float distance = toOther.magnitude;

                // Calculate steering forces
                separationForce -= toOther.normalized / distance;
                if (distance < neighborRadius * 0.5f)
                    avoidanceForce -= toOther.normalized / distance;
                cohesionForce += toOther;
                alignmentForce += collider.GetComponent<NavMeshAgent>().velocity;
            }
        }

        // Apply weights to steering forces
        separationForce *= separationWeight;
        avoidanceForce *= avoidanceWeight;
        cohesionForce *= cohesionWeight;
        alignmentForce *= alignmentWeight;

        // Calculate total steering force
        Vector3 steeringForce = separationForce + avoidanceForce + cohesionForce + alignmentForce;

        // Apply steering force to agent's velocity
        agent.velocity += steeringForce * Time.deltaTime;

        // Limit velocity to max speed
        if (agent.velocity.magnitude > maxSpeed)
        {
            agent.velocity = agent.velocity.normalized * maxSpeed;
        }
    }
}