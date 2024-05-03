using System;
using System.Collections;
using UnityEngine;
using static UnityEngine.UI.Image;
using static alglib;
using UnityEngine.AI;
using UnityEngine.TextCore.Text;
using static Accord.Math.Optimization.QuadraticObjectiveFunction;
using Accord.Math.Optimization;
using System.Collections.Generic;
using static TMPro.SpriteAssetUtilities.TexturePacker_JsonArray;
using System.Linq;

namespace UnityStandardAssets.Characters.ThirdPerson
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CapsuleCollider))]
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(NavMeshAgent))]
    //[RequireComponent(typeof(FieldOfView))]
    public class NavThirdPersonCharacter : MonoBehaviour
    {
        [SerializeField] float m_MovingTurnSpeed = 360;
        [SerializeField] float m_StationaryTurnSpeed = 180;
        [SerializeField] float m_JumpPower = 6f;
        [Range(1f, 4f)][SerializeField] float m_GravityMultiplier = 2f;
        [SerializeField] float m_RunCycleLegOffset = 0.2f; //specific to the character in sample assets, will need to be modified to work with others
        public float m_MoveSpeedMultiplier = 1f;
        [SerializeField] float m_AnimSpeedMultiplier = 1f;
        [SerializeField] float m_GroundCheckDistance = 0.2f;

        private bool allowCrouching;
        Vector3 desiredVelocity;
        Vector3 velocity;
        Vector3 velocityN = Vector3.zero;

        public Rigidbody m_Rigidbody;
        Animator m_Animator;
        public NavMeshAgent m_agent;
        bool m_IsGrounded;
        float m_OrigGroundCheckDistance;
        const float k_Half = 0.5f;
        float m_TurnAmount;
        float m_ForwardAmount;
        float m_BackwardAmount;
        Vector3 m_GroundNormal;
        float m_CapsuleHeight;
        Vector3 m_CapsuleCenter;
        CapsuleCollider m_Capsule;
        bool m_Crouching;

        //FieldOfView fieldOfView;

        // private alglib.minlbfgsstate state;
        /*double[] x = new double[] { 0, 0 };
        double[] s = new double[] { 1, 1 };
        double epsg = 0;
        double epsf = 0;
        double epsx = 0.0000000001;
        int maxits = 0;*/

        void Start()
        {
            allowCrouching = false;

            m_Animator = GetComponent<Animator>();
            m_Rigidbody = GetComponent<Rigidbody>();
            m_Capsule = GetComponent<CapsuleCollider>();
            m_agent = GetComponent<NavMeshAgent>();
            // fieldOfView = GetComponent<FieldOfView>();
            m_CapsuleHeight = m_Capsule.height;
            m_CapsuleCenter = m_Capsule.center;

            m_Rigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ;
            m_OrigGroundCheckDistance = m_GroundCheckDistance;
        }

        public void Move(Vector3 move, bool crouch, bool jump)
        {

            // convert the world relative moveInput vector into a local-relative
            // turn amount and forward amount required to head in the desired
            // direction.
            

            move = transform.InverseTransformDirection(move);
            if (move.magnitude > 1f || move.magnitude < 1f) move.Normalize();
            CheckGroundStatus();
            velocity = move;
            move = Vector3.ProjectOnPlane(move, m_GroundNormal);
            m_TurnAmount = Mathf.Atan2(move.x, move.z);

            m_ForwardAmount = move.z;

            ApplyExtraTurnRotation();

            // send input and other state parameters to the animator
            UpdateAnimator(move);
        }

        public void Move(Vector3 move, bool crouch, bool jump, float turnAngle, float tangentialVelFactor)
        {

            // convert the world relative moveInput vector into a local-relative
            // turn amount and forward amount required to head in the desired
            // direction.
            move = transform.InverseTransformDirection(move);
            move.x = move.x * tangentialVelFactor;
            if (move.magnitude > 1f || move.magnitude < 1f) move.Normalize();
            //move = move * tangentialVelFactor;
            CheckGroundStatus();
            velocity = move;
            
            m_TurnAmount = turnAngle;
            move = Vector3.ProjectOnPlane(move, m_GroundNormal);
            m_ForwardAmount = move.z;

            ApplyExtraTurnRotation();

            // send input and other state parameters to the animator
            UpdateAnimator(move);
        }


        void UpdateAnimator(Vector3 move)
        {
            // update the animator parameters
            m_Animator.SetFloat("Forward", m_ForwardAmount / 2, 0.1f, Time.deltaTime);
            m_Animator.SetFloat("Turn", m_TurnAmount, 0.1f, Time.deltaTime);
            m_Animator.SetBool("Crouch", m_Crouching);
            m_Animator.SetBool("OnGround", m_IsGrounded);
            if (!m_IsGrounded)
            {
                m_Animator.SetFloat("Jump", m_Rigidbody.velocity.y);
            }

            if (m_IsGrounded && move.magnitude > 0)
            {
                m_Animator.speed = m_AnimSpeedMultiplier;
            }
            else
            {
                // don't use that while airborne
                m_Animator.speed = 1;
            }
        }

        void ApplyExtraTurnRotation()
        {
            // help the character turn faster (this is in addition to root rotation in the animation)
            float turnSpeed = Mathf.Lerp(m_StationaryTurnSpeed, m_MovingTurnSpeed, m_ForwardAmount);
            transform.Rotate(0, m_TurnAmount * turnSpeed * Time.deltaTime, 0);
            velocity = Quaternion.AngleAxis(m_TurnAmount * turnSpeed * Time.deltaTime, Vector3.up) * velocity;
        }

        public void OnAnimatorMove()
        {
            // we implement this function to override the default root motion.
            // this allows us to modify the positional speed before it's applied.
            if (m_IsGrounded && Time.deltaTime > 0)
            {
                /*Vector3 v = (m_Animator.deltaPosition * m_MoveSpeedMultiplier) / Time.deltaTime;

                v.y = m_agent.velocity.y;
                m_agent.velocity = v;*/
                velocity = transform.TransformDirection(velocity) * m_MoveSpeedMultiplier;
                m_agent.velocity = velocity;
            }
        }

        void CheckGroundStatus()
        {
            RaycastHit hitInfo;
#if UNITY_EDITOR
            // helper to visualise the ground check ray in the scene view
            Debug.DrawLine(transform.position + (Vector3.up * 0.1f), transform.position + (Vector3.up * 0.1f) + (Vector3.down * m_GroundCheckDistance));
#endif
            // 0.1f is a small offset to start the ray from inside the character
            // it is also good to note that the transform position in the sample assets is at the base of the character
            if (Physics.Raycast(transform.position + (Vector3.up * 0.1f), Vector3.down, out hitInfo, m_GroundCheckDistance))
            {
                m_GroundNormal = hitInfo.normal;
                m_IsGrounded = true;
                m_Animator.applyRootMotion = true;
            }
            else
            {

                m_IsGrounded = false;
                m_GroundNormal = Vector3.up;
                m_Animator.applyRootMotion = false;
            }
        }

    }
}
