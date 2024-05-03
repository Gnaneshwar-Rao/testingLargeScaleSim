using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NewPlayerBoundaryDetection : MonoBehaviour
{
    private Testing parent;
    public float range = 1.7f;
    public float tempSpeed;

    private void Awake()
    {
        parent = this.GetComponentInParent<Testing>();
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 dir = Vector3.forward;
        Ray theRay = new Ray(transform.position, transform.TransformDirection(dir * range));
        Debug.DrawRay(transform.position, transform.TransformDirection(dir * range), Color.black, 0.1f);

        /*if(Physics.Raycast(theRay, out RaycastHit hit, range))
        {
            if(hit.collider.tag == "Player")
            {
                print("Player in front");
                if (!parent.collisionDetect && hit.distance <= 1f)
                {
                    tempSpeed = parent.agent.speed;
                    parent.avoidCollision();
                } else if (parent.collisionDetect && hit.distance > 1.5f)
                {
                    parent.collisionDetect = false;
                    parent.agent.speed = tempSpeed;
                }
            } else if (hit.collider.tag != "Player")
            {
                if (parent.collisionDetect)
                {
                    parent.collisionDetect = false;
                    parent.agent.speed = tempSpeed;
                }
            }
        } else
        {
            if (parent.collisionDetect)
            {
                parent.collisionDetect = false;
                parent.agent.speed = tempSpeed;
            }
        }*/
    }
}
