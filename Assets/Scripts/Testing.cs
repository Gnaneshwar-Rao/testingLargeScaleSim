using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityStandardAssets.Characters.ThirdPerson;

public class Testing : MonoBehaviour
{
    [HideInInspector] public ThirdPersonCharacter character;
    private Animator animator; //animator
    private NavMeshAgent agent; //agent
    [HideInInspector] public int currentPosition;
    [HideInInspector] public int totalDistributedPoints;
    private Vector3 newTarget;
    private bool isMoving;
    private FieldOfView fieldOfView;

    private void Awake()
    {
        agent = this.GetComponent<NavMeshAgent>(); //gets the navmesh agent
        character = this.GetComponent<ThirdPersonCharacter>();
        animator = this.GetComponent<Animator>();

        StartCoroutine(Delay());
    }

    void Update()
    {

        if (agent.remainingDistance > agent.stoppingDistance)
        {
            character.Move(agent.desiredVelocity, false, false);
        }
        else
        {
            character.Move(Vector3.zero, false, false);
            isMoving = false;
        }
    }

    public IEnumerator Delay()
    {
        Vector3 currentTarget = InitializeMovement();

        agent.SetDestination(currentTarget); //sets the destination
        // yield return new WaitUntil(() => agent.speed != 0); //waits until character starts to move

        isMoving = true;
        yield return new WaitUntil(() => agent.remainingDistance < 0.3f);
        //  waits until character reaches it destination
        //  sensitivity

        Finish();
    }

    private Vector3 InitializeMovement()
    {
        newTarget = new Vector3(30, 0, 0);
        //newTarget.x = newTarget.x + 25f;

        return newTarget;
    }

    private void Finish()
    {
        agent.speed = 0;
    }

}
