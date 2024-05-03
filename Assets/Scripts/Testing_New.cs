using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityStandardAssets.Characters.ThirdPerson;
using static UnityEditor.Experimental.GraphView.GraphView;

public class Testing_New : MonoBehaviour
{
    [HideInInspector] public NavThirdPersonCharacter character;
    private Animator animator; //animator
    private NavMeshAgent agent; //agent
    private Vector3 newTarget;
    private bool isMoving;

    public float searchRadius = 5f;
    public float comfortVelocity = 0.3f; // Default comfortable walking speed -> 1.0
    public float maxVelocity = 0.5f; // Maximum allowed velocity -> 2.0
    public float minDecelerationTime = 3.0f; // Minimum time to start decelerating before collision
    public float maxAngularVelocity = 90.0f; // Maximum angular velocity for turning
    public float maxAngularAcceleration = 360.0f; // Maximum angular acceleration
    public float maxTangentialAcceleration = 2.0f; // Maximum tangential acceleration -> 2.0
    public LayerMask obstacleLayerMask;

    public List<PerceivedObstacle> perceivedObstacles = new List<PerceivedObstacle>();
    private bool collisionDetected = false;
    private float minTimeToCollision;

    private Vector3 lastPosition;
    private float angularVelocity;

    private bool firstFrame = true;

    private void Awake()
    {
        agent = this.GetComponent<NavMeshAgent>(); //gets the navmesh agent
        character = this.GetComponent<NavThirdPersonCharacter>();
        animator = this.GetComponent<Animator>();

        lastPosition = transform.position;
    }

    private void Start()
    {
        StartCoroutine(Delay());
        agent.SetDestination(newTarget);
    }

    void Update()
    {
        // Perform perception, collision prediction, and reaction strategy
        PerceptionModel();
        CollisionPrediction();
        float decelerationFactor = ReactionStrategy();

        // Update walker's position and orientation
        UpdatePositionAndOrientation(decelerationFactor);

        // Visualization
        Visualize();

        // character.Move(newDesiredVelocity, false, false, turnAngle, tangentialVel);
    }

    public IEnumerator Delay()
    {
        Vector3 currentTarget = InitializeMovement();

        agent.SetDestination(currentTarget); //sets the destination
        yield return new WaitUntil(() => agent.desiredVelocity.magnitude != 0); //waits until character starts to move

        isMoving = true;
        yield return new WaitUntil(() => agent.remainingDistance < 0.3f);
        //  waits until character reaches it destination
        //  sensitivity

        Finish();
    }

    private Vector3 InitializeMovement()
    {
        //newTarget = new Vector3(-character.transform.position.x, character.transform.position.y, -character.transform.position.z);
        newTarget = new Vector3(-transform.position.x, transform.position.y, -transform.position.z);

        return newTarget;
    }

    private void Finish()
    {
        agent.speed = 0f;
    }

    void PerceptionModel()
    {
        /*// Simulate vision perception
        // For simplicity, let's assume the walker has a cone of vision and can detect obstacles within a certain range
        // We'll use Physics.Raycast to simulate vision perception
        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.forward, out hit, 10f))
        {
            Debug.DrawLine(transform.position, hit.point, Color.red); // Visualize perceived obstacle
            
            // Compute bearing angle, time derivative of the bearing angle, and time-to-collision
            Vector3 obstacleDirection = hit.point - transform.position;
            float bearingAngle = Vector3.SignedAngle(transform.forward, obstacleDirection, Vector3.up);
            float timeToCollision = Vector3.Distance(transform.position, hit.point) / agent.velocity.magnitude;
            float bearingAngleDerivative = Vector3.Angle(agent.velocity, obstacleDirection) / timeToCollision;

            // For simplicity, we'll store these values for each perceived obstacle in a data structure
            // You might want to use a list or array to store multiple perceived obstacles
        }*/

        // Clear the list of perceived obstacles
        perceivedObstacles.Clear();

        // Cast a sphere around the walker to find obstacles within a 5f radius
        int maxColliders = 10;
        Collider[] hitColliders = new Collider[maxColliders];
        int numColliders = Physics.OverlapSphereNonAlloc(transform.position, searchRadius, hitColliders, obstacleLayerMask);

        // Loop through all obstacles within the radius
        for (int i = 0; i < numColliders; i++)
        {
            if (hitColliders[i].gameObject != this.gameObject)
            {
                GameObject obstacle = hitColliders[i].gameObject;

                // Calculate the relative position and velocity of the obstacle
                Vector3 relativePosition = obstacle.transform.position - transform.position;
                Vector3 relativeVelocity = obstacle.GetComponent<NavMeshAgent>().desiredVelocity;

                // Calculate the bearing angle (α) of the obstacle relative to the walker
                float bearingAngle = Vector3.SignedAngle(transform.forward, relativePosition, Vector3.up);

                // Calculate the time derivative of the bearing angle (˙α)
                float bearingAngleRate = Vector3.Dot(transform.right, relativeVelocity.normalized);

                // Calculate the time-to-collision (ttc) with the obstacle
                float distanceToObstacle = relativePosition.magnitude;
                //float timeToCollision = distanceToObstacle / Vector3.Dot(relativeVelocity, relativePosition.normalized);

                // Store the perceived obstacle data in the list
                perceivedObstacles.Add(new PerceivedObstacle()
                {
                    bearingAngle = bearingAngle,
                    bearingAngleRate = bearingAngleRate,
                    //timeToCollision = timeToCollision,
                    obstacleVelocity = relativeVelocity,
                    position = obstacle.transform.position
                });
            }
        }

        /*// Loop through all obstacles within the radius
        foreach (Collider collider in hitColliders)
        {
        }*/
    }

    void CollisionPrediction()
    {
        // Determine if a collision is imminent
        // For simplicity, we'll check if any perceived obstacles are within a certain distance threshold
        // and if the time-to-collision is less than a certain value
        // You might want to refine this by considering additional factors such as obstacle velocity and size

        // Iterate through perceived obstacles
        minTimeToCollision = Mathf.Infinity;

        foreach (var obstacle in perceivedObstacles)
        {
            // Calculate relative position and velocity of the obstacle
            Vector3 relativePosition = obstacle.position - transform.position;
            Vector3 relativeVelocity = obstacle.obstacleVelocity - agent.desiredVelocity;

            Vector3 convergingVelocity = Vector3.Dot(relativeVelocity, transform.forward.normalized) * transform.forward.normalized;
            Vector3 orthogonalVelocity = relativeVelocity - convergingVelocity;

            // Calculate time-to-collision (ttc)
            // float timeToCollision = Vector3.Dot(relativePosition, relativeVelocity) / Mathf.Pow(relativeVelocity.magnitude, 2);
            float timeToCollision = Vector3.Dot(transform.position - obstacle.position, convergingVelocity) / Mathf.Pow(convergingVelocity.magnitude, 2);

            // Calculate distance to obstacle
            float distanceToObstacle = relativePosition.magnitude;

            // If time-to-collision is less than 3 seconds and distance is less than threshold
            if (timeToCollision < 3f && distanceToObstacle < 5f)
            {
                // Imminent collision detected
                collisionDetected = true;

                // Store the minimum time-to-collision
                if (timeToCollision < minTimeToCollision)
                {
                    minTimeToCollision = timeToCollision;
                }
            }
        }
    }

    float ReactionStrategy()
    {
        // Implement twofold reaction strategy: reorientation and deceleration
        // For simplicity, we'll assume a basic reaction strategy based on collision prediction results
        // Reorientation strategy: Adjust walker's orientation to avoid future collisions
        // Deceleration strategy: Slow down or stop the walker to avoid imminent collisions
        // You might want to implement more sophisticated strategies based on the specific requirements of your simulation

        // Implement twofold reaction strategy: reorientation and deceleration
        // For simplicity, we'll assume a basic reaction strategy based on collision prediction results

        // Reorientation strategy: Adjust walker's orientation to avoid future collisions
        if (collisionDetected) // Assuming collisionDetected is a boolean indicating if a collision is imminent
        {
            // Calculate the direction to steer away from the obstacle
            Vector3 desiredDirection = agent.desiredVelocity.normalized;
            Vector3 steerDirection = Vector3.Cross(Vector3.up, desiredDirection);

            // Smoothly adjust the angular velocity for smoother steering
            float angle = Vector3.Angle(transform.forward, steerDirection);

            // Apply proportional control to adjust the angular velocity smoothly
            float angularAcceleration = maxAngularAcceleration * Mathf.Sign(angle);
            angularVelocity += angularAcceleration * Time.deltaTime;
            angularVelocity = Mathf.Clamp(angularVelocity, -maxAngularVelocity, maxAngularVelocity);
        }
        else
        {
            angularVelocity = 0f; // No need for reorientation, so set angular velocity to zero
        }

        // Deceleration strategy: Slow down or stop the walker to avoid imminent collisions
        if (minTimeToCollision < minDecelerationTime && collisionDetected) // Assuming timeToCollision is the time until the collision
        {
            // Slow down the walker to avoid collision
            float decelerationFactor = Mathf.Clamp01(minTimeToCollision / minDecelerationTime); // Linear deceleration
            // agent.speed *= decelerationFactor;
            //agent.desiredVelocity *= decelerationFactor;
            //return decelerationFactor;
        }

        return 1f;
    }

    void UpdatePositionAndOrientation(float decelFactor)
    {
        // Update walker's position and orientation based on calculated velocities
        Vector3 newPosition = transform.position;
        Vector3 velocity = (newPosition - lastPosition) / Time.deltaTime;
        Vector3 direction = (newTarget - newPosition).normalized;

        // Apply tangential acceleration
        velocity += direction * maxTangentialAcceleration * Time.deltaTime;

        // Clamp velocity
        velocity = Vector3.ClampMagnitude(velocity, maxVelocity);

        // Apply collision avoidance
        if (collisionDetected)
        {
            // Calculate the direction to avoid collision
            Vector3 avoidDirection = CalculateAvoidanceDirection();

            // Adjust the direction based on collision avoidance
            direction += avoidDirection;
        }

        // Update position
        transform.position += velocity * Time.deltaTime * decelFactor;

        // Update orientation
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, maxAngularAcceleration * Time.deltaTime);

        // Update last position
        lastPosition = newPosition;
    }

    Vector3 CalculateAvoidanceDirection()
    {
        // Calculate the direction to avoid collision based on perceived obstacles
        Vector3 avoidDirection = Vector3.zero;

        foreach (var obstacle in perceivedObstacles)
        {
            // Adjust the avoid direction based on the bearing angle of the obstacle
            float bearingAngle = obstacle.bearingAngle;
            Vector3 obstacleDirection = Quaternion.AngleAxis(bearingAngle, Vector3.up) * transform.forward;

            // Weight the avoidance direction based on the inverse of the time-to-interaction
            float weight = 1f / obstacle.timeToCollision;
            avoidDirection += obstacleDirection * weight;
        }

        // Normalize the avoid direction
        avoidDirection.Normalize();

        return avoidDirection;
    }
    void Visualize()
    {
        // Visualize the crowd simulation
        // This could involve drawing debug lines, gizmos, or using Unity's built-in visualization tools
    }
}
