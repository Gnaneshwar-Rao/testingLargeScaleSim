using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObstacleColliderData/* : MonoBehaviour*/
{
    public Rigidbody rb;
    public Vector3 hitLocation;

    public ObstacleColliderData(Collider col, Vector3 hitpoint)
    {
        rb = col.GetComponent<Rigidbody>();
        hitLocation = hitpoint;
    }
}
