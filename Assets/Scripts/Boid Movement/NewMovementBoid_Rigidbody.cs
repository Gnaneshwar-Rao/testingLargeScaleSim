using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//handles movement of the boid using a rigidbody
[RequireComponent(typeof(Rigidbody))]
public class NewMovementBoid_Rigidbody : BoidMovement
{
    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public override void MoveBoid(Vector3 vel)
    {
        vel = LimitVelocity(vel, maxSpeed);
        rb.AddForce(vel);
        rb.velocity = LimitVelocity(rb.velocity, maxSpeed);
        if (rb.velocity != Vector3.zero) transform.forward = rb.velocity;
    }

    public override Vector3 GetVelocity()
    {
        return rb.velocity;
    }

    //Limit a vector's magnitude to a certain limit if it is over that limit
    private Vector3 LimitVelocity(Vector3 velocity, float velocityLimit)
    {
        float velMagnitude = velocity.magnitude;

        if (velMagnitude > velocityLimit)
        {
            return (velocity / velMagnitude) * velocityLimit;
        }
        else
        {
            return velocity;
        }
    }
}
