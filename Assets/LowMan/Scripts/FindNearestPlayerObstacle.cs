using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FindNearestPlayerObstacle : MonoBehaviour
{
    public float searchRadius = 10f;
    public List<Testing> nearestPlayers = new List<Testing>();
    [SerializeField] public int layer;
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
        Collider[] hitColliders = new Collider[maxColliders];
        int numColliders = Physics.OverlapSphereNonAlloc(transform.position, searchRadius, hitColliders, 1<<layer);
        
        for(int i = 0; i < numColliders; i++)
        {
            Vector3 directionToCollider = hitColliders[i].transform.position - transform.position;
            directionToCollider.y = 0;

            float angle = Vector3.Angle(transform.forward, directionToCollider);

            if (hitColliders[i].tag == "Player" && hitColliders[i].transform.position != transform.position && angle <= fieldOfViewAngle * 0.5f)
            {
                nearestPlayers.Add(hitColliders[i].GetComponent<Testing>());
            }
        }

    }

}
