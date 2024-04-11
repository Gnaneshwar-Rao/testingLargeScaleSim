using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FieldOfView : MonoBehaviour
{
    public float radius;
    [Range(0,360)]
    public float angle;

    public LayerMask targetMask;
    public LayerMask obstructionMask;

    public GameObject playerRef;

    public bool canSeePlayer;
    public static int MAXOBSERVABLECHARS = 5;
    public List<Testing> charactersNearList;

    private void Awake()
    {
        //for (int i = 0; i < MAXOBSERVABLECHARS; i++) charactersNearList.Add(null);
        CheckforColliders();
        StartCoroutine(FOVRoutine());
    }

    /*private void Start()
    {
        //playerRef = GameObject.FindGameObjectWithTag("Player");
        //StartCoroutine(FOVRoutine());
    }*/

    private IEnumerator FOVRoutine()
    {
        WaitForSeconds wait = new WaitForSeconds(0.1f);

        while (true)
        {
            yield return wait;
            FieldOfViewCheck();
        }
    }

    private void FieldOfViewCheck()
    {
        CheckforColliders();
        Collider[] rangeChecks = Physics.OverlapSphere(transform.position, radius, targetMask);

        if (rangeChecks.Length != 0)
        {
            Transform target = rangeChecks[0].transform;
            Vector3 directionToTarget = (target.position - transform.position).normalized;

            if (Vector3.Angle(transform.forward, directionToTarget) < angle / 2)
            {
                float distanceToTarget = Vector3.Distance(transform.position, target.position);

                if (!Physics.Raycast(transform.position, directionToTarget, distanceToTarget, obstructionMask))
                    canSeePlayer = true;
                else
                    canSeePlayer = false;
            }
            else
                canSeePlayer = false;
        }
        else if (canSeePlayer)
            canSeePlayer = false;
    }

    private void CheckforColliders()
    {
        Ray ray1 = new Ray(transform.position, transform.TransformDirection(new Vector3(0, 0, 1) * radius));
        Ray ray2 = new Ray(transform.position, transform.TransformDirection(new Vector3(1, 0, 1).normalized * radius));
        Ray ray3 = new Ray(transform.position, transform.TransformDirection(new Vector3(-1, 0, 1).normalized * radius));
        //Ray ray4 = new Ray(transform.position, transform.TransformDirection(new Vector3(1, 0, 0) * radius));
        //Ray ray5 = new Ray(transform.position, transform.TransformDirection(new Vector3(-1, 0, 0) * radius));



        if (Physics.Raycast(ray1, out RaycastHit hitObject, radius))
        {
            Testing characMovScript = hitObject.collider.GetComponentInParent<Testing>();
            if (characMovScript != null)
            {
                AddToNearList(characMovScript);
            }
        }

        if (Physics.Raycast(ray2, out RaycastHit hitObject2, radius))
        {
            Testing characMovScript = hitObject.collider.GetComponentInParent<Testing>();
            if (characMovScript != null)
            {
                AddToNearList(characMovScript);
            }
        }

        if (Physics.Raycast(ray3, out RaycastHit hitObject3, radius, targetMask))
        {
            Testing characMovScript = hitObject.collider.GetComponentInParent<Testing>();
            if (characMovScript != null)
            {
                AddToNearList(characMovScript);
            }
        }

        /*if (Physics.Raycast(ray4, out RaycastHit hitObject4, radius, targetMask))
        {
            Testing characMovScript = hitObject.collider.GetComponentInParent<Testing>();
            if (characMovScript != null)
            {
                AddToNearList(characMovScript);
            }
        }

        if (Physics.Raycast(ray5, out RaycastHit hitObject5, radius, targetMask))
        {
            Testing characMovScript = hitObject.collider.GetComponentInParent<Testing>();
            if (characMovScript != null)
            {
                AddToNearList(characMovScript);
            }
        }*/

    }

    private void AddToNearList(Testing characMovScript)
    {
        if(charactersNearList.Count < MAXOBSERVABLECHARS && !charactersNearList.Contains(characMovScript))
        {
            charactersNearList.Add(characMovScript);
        }
    }
}
