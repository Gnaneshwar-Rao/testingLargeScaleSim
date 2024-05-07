using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FindNearestPlayerObstacle : MonoBehaviour
{
    public float searchRadius = 10f;
    public List<Testing> nearestPlayers = new List<Testing>();
    public List<ObstacleColliderData> nearestObstacles = new List<ObstacleColliderData>();
    [SerializeField] public LayerMask playerLayer;
    [SerializeField] public LayerMask obstacleLayer;
    public float fieldOfViewAngle = 150f;

    // Update is called once per frame
    void Update()
    {  
        FindInArea();
    }

    private void FindInArea()
    {
        int maxColliders = 10;
        nearestPlayers.Clear();
        nearestObstacles.Clear();
        Collider[] hitColliders = new Collider[maxColliders];
        int layerMask = playerLayer | obstacleLayer;
        int numColliders = Physics.OverlapSphereNonAlloc(transform.position, searchRadius, hitColliders, layerMask);
        
        for(int i = 0; i < numColliders; i++)
        {
            Vector3 directionToCollider = hitColliders[i].transform.position - transform.position;
            directionToCollider.y = transform.position.y;

            float angle = Vector3.Angle(transform.forward, directionToCollider);

            if (hitColliders[i].tag == "Player" && hitColliders[i].transform.position != transform.position && angle <= fieldOfViewAngle * 0.5f)
            {
                nearestPlayers.Add(hitColliders[i].GetComponent<Testing>());
            } else if (hitColliders[i].tag == "Obstacle" && angle <= fieldOfViewAngle * 0.5f)
            {
                /*Vector3 furthestHitPoint = Vector3.zero; // Initialize nearest hit point variable
                float furthestDistance = 0f; // Initialize nearest distance variable

                for (int j = -90; j < 90; j += 15) // Adjust the angle increment to suit your needs
                {
                    Vector3 direction = Quaternion.Euler(0, j, 0) * transform.forward; // Get direction in a circular pattern
                    RaycastHit[] hits;
                    hits = Physics.RaycastAll(transform.position, direction, searchRadius, obstacleLayer);

                    foreach (RaycastHit hit in hits)
                    {
                        // Calculate distance from transform position to hit point
                        float distanceToHit = Vector3.Distance(transform.position, hit.point);

                        // If the new hit point is closer, update nearestHitPoint and nearestDistance
                        if (distanceToHit > furthestDistance)
                        {
                            furthestHitPoint = hit.point;
                            furthestDistance = distanceToHit;
                        }
                    }
                }

                nearestObstacles.Add(new ObstacleColliderData(hitColliders[i], furthestHitPoint));*/

                RaycastHit hit;
                if (Physics.Raycast(transform.position, /*(hitColliders[i].transform.position - transform.position)*/ transform.forward.normalized, out hit, searchRadius, obstacleLayer))
                {
                    nearestObstacles.Add(new ObstacleColliderData(hitColliders[i], hit.point));
                }
            }
        }

    }

}
