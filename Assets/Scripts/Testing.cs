using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityStandardAssets.Characters.ThirdPerson;

public class Testing : MonoBehaviour
{
    [HideInInspector] public NavThirdPersonCharacter character;
    private Animator animator; //animator
    public NavMeshAgent agent; //agent
    [HideInInspector] public int currentPosition;
    [HideInInspector] public int totalDistributedPoints;
    private Vector3 newTarget;
    private bool isMoving;
    //private FieldOfView fieldOfView;
    [SerializeField] List<Testing_0> fieldOfViewChars;

    private float turnAngle;
    private Vector3 newDesiredVelocity;
    private float tangentialVel;

    private void Awake()
    {
        agent = this.GetComponent<NavMeshAgent>(); //gets the navmesh agent
        character = this.GetComponent<NavThirdPersonCharacter>();
        animator = this.GetComponent<Animator>();

        StartCoroutine(Delay());
    }

    void Update()
    {
        (turnAngle, newDesiredVelocity, tangentialVel) = evaluateNewVel();

        if (agent.remainingDistance > agent.stoppingDistance)
        {
            //character.Move(agent.desiredVelocity, false, false, 0f);
            character.Move(newDesiredVelocity, false, false, turnAngle, tangentialVel);
        }
        else
        {
            character.Move(Vector3.zero, false, false, 0f, 0f);
            agent.velocity = Vector3.zero;
            isMoving = false;
        }
    }

    public IEnumerator Delay()
    {
        Vector3 currentTarget = InitializeMovement();

        agent.SetDestination(currentTarget); //sets the destination
        yield return new WaitUntil(() => agent.speed != 0); //waits until character starts to move

        isMoving = true;
        yield return new WaitUntil(() => agent.remainingDistance < 0.3f);
        //  waits until character reaches it destination
        //  sensitivity

        Finish();
    }

    private Vector3 InitializeMovement()
    {
        newTarget = new Vector3(-character.transform.position.x, character.transform.position.y, -character.transform.position.z);

        return newTarget;
    }

    private void Finish()
    {
        agent.speed = 0;
    }

    private (float, Vector3, float) evaluateNewVel()
    {
        float neg_Phi = float.MinValue; 
        float pos_Phi = float.MaxValue;
        float threshold_bearingAngle;
        // List<Testing> positive_neighbour = new List<Testing>();
        // List<Testing> negative_neighbour = new List<Testing>();

        bool neighbourExist = false;

        Vector3 relativeGoalVelocity = -1 * agent.velocity.normalized;
        //Vector3 relativeGoalVelocity = -1 * transform.forward.normalized;
        Vector3 relativeGoalVelocity_conv = Vector3.Dot(relativeGoalVelocity, (agent.destination - transform.position).normalized) * (agent.destination - transform.position).normalized;
        Vector3 relativeGoalVelocity_orth = relativeGoalVelocity - relativeGoalVelocity_conv;
        float changeInBearingAngle_Goal = Mathf.Atan2(relativeGoalVelocity_orth.magnitude , (Vector3.Distance(agent.destination, transform.position) - relativeGoalVelocity_conv.magnitude));

        //Vector3 forwardDir = transform.forward;
        //float bearingAngleGoal = Vector3.Angle(forwardDir, agent.destination - transform.position);
        
        float tti_min = float.MaxValue;

        float angularVelocity;
        Vector3 newDesiredVelocity = agent.desiredVelocity;
        float tangentialVelocity = 0f;

        bool Once = false;

        foreach (Testing_0 neighbour in this.fieldOfViewChars)
        {
            if(neighbour != null)
            {
                float bearingAngle = Vector3.Angle(this.agent.velocity.normalized, this.agent.velocity.normalized - neighbour.agent.velocity.normalized);

                float bearingAngleRad = bearingAngle * Mathf.Deg2Rad;

                Vector3 relativeVelocity = neighbour.agent.velocity.normalized - this.agent.velocity.normalized;

                //float bearingAngle = Vector3.Angle(neighbour.character.m_Rigidbody.velocity - this.character.m_Rigidbody.velocity, this.character.m_Rigidbody.velocity);
                //Vector3 relativeVelocity = neighbour.character.m_Rigidbody.velocity - this.character.m_Rigidbody.velocity;

                Vector3 relativeVelocity_conv = Vector3.Dot(relativeVelocity, (neighbour.transform.position - transform.position).normalized) * (neighbour.transform.position - transform.position).normalized;
                Vector3 relativeVelocity_orth = relativeVelocity - relativeVelocity_conv;
                float tti = Vector3.Distance(neighbour.transform.position, transform.position) / (relativeVelocity_conv.normalized.magnitude);
                
                if(Vector3.Angle((neighbour.transform.position - transform.position).normalized, relativeVelocity) <= 90f)
                {
                    tti = -1 * tti;
                }
                
                float changeInBearingAngle = Mathf.Atan2(relativeVelocity_orth.magnitude , (Vector3.Distance(neighbour.transform.position, transform.position) - relativeVelocity_conv.magnitude));


                if (changeInBearingAngle < 0)
                {
                    /*Model’s parameters (a, b, c) (cf. Equation (1)) can be adapted for each walker to individualize avoidance
                    behavior with negligible computational overhead.The impact of
                    parameters change on simulations is illustrated in the companion
                    video.An intuitive link exists between avoidance behavior and the
                    shape of ?1 which is completely controlled by(a, b, c).The higher
                    the peak of ?1, the earlier the anticipation. The wider the peak,
                    the stronger the adaptation.Finally, the curvature of ?1 controls a
                    trade - off between anticipation time and reaction strength: when the
                    maximum curvature is higher, early anticipated reactions remain
                    low whilst they get stronger when tti decreases. The automatic
                    adaptation of parameters with respect to external factors, such as
                    local density of population, may open interesting perspectives.*/



                    threshold_bearingAngle = -1f * 0.6f / (tti * Mathf.Sqrt(tti));
                    if (tti > 0 && Math.Abs(changeInBearingAngle) < Math.Abs(threshold_bearingAngle))
                    {
                        if (!Once)
                        {
                            // neg_Phi = changeInBearingAngle - threshold_bearingAngle;
                            neg_Phi = threshold_bearingAngle - changeInBearingAngle;
                        } else
                        {
                            // neg_Phi = Mathf.Max(neg_Phi, changeInBearingAngle - threshold_bearingAngle);
                            neg_Phi = Mathf.Max(neg_Phi, threshold_bearingAngle - changeInBearingAngle);
                        }

                        // negative_neighbour.Add(neighbour);             
                        tti_min = Mathf.Min(tti_min, tti);
                        neighbourExist = true;

                        Once = true;
                    }
                }
                else
                {
                    threshold_bearingAngle = 0.6f / (tti * Mathf.Sqrt(tti));
                    if (tti > 0 && Math.Abs(changeInBearingAngle) < Math.Abs(threshold_bearingAngle))
                    {
                        if (!Once)
                        {
                            pos_Phi = changeInBearingAngle - threshold_bearingAngle;
                            //pos_Phi = threshold_bearingAngle - changeInBearingAngle;
                        }
                        else
                        {
                            pos_Phi = Mathf.Min(pos_Phi, changeInBearingAngle - threshold_bearingAngle);
                            //pos_Phi = Mathf.Min(pos_Phi, threshold_bearingAngle - changeInBearingAngle);
                        }

                        // positive_neighbour.Add(neighbour);
                        tti_min = Mathf.Min(tti_min, tti);
                        neighbourExist = true;

                        Once = true;
                    }
                }
            }      
        }

        if(tti_min > 0 && !neighbourExist)
        {

        }
        
        if (Math.Abs(changeInBearingAngle_Goal) < 0.1f)
        {
            if (Math.Abs(pos_Phi) < Math.Abs(neg_Phi))
            {
                if (pos_Phi == float.MaxValue)
                {
                    angularVelocity = 0f;
                } else
                {
                    angularVelocity = pos_Phi;
                }
            }
            else
            {
                if (neg_Phi == float.MinValue)
                {
                    angularVelocity = 0f;
                }
                else
                {
                    angularVelocity = neg_Phi;
                }
            }
        } else if ((changeInBearingAngle_Goal > neg_Phi && changeInBearingAngle_Goal < pos_Phi) && neighbourExist)
        {
            if (Math.Abs(pos_Phi - changeInBearingAngle_Goal) < Math.Abs(neg_Phi - changeInBearingAngle_Goal))
            {
                angularVelocity = pos_Phi;
            } else
            {
                angularVelocity = neg_Phi;
            }
        } else
        {
            angularVelocity = changeInBearingAngle_Goal;
        }

        if(Math.Abs(angularVelocity) > Math.PI / 2) {
            angularVelocity = (float)Math.PI / 2;
        }

        if (neighbourExist && tti_min < 3f)
        {
            tangentialVelocity = agent.desiredVelocity.x * (float)(1 - Math.Pow(Math.E, (double)(-1 * 0.5 * tti_min * tti_min)));
        }

        return (angularVelocity, newDesiredVelocity, tangentialVelocity);
    }

}
