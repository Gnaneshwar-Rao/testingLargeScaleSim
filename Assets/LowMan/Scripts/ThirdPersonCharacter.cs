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
    //[RequireComponent(typeof(FieldOfView))]
    public class ThirdPersonCharacter : MonoBehaviour
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

        Rigidbody m_Rigidbody;
		Animator m_Animator;
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

        FieldOfView fieldOfView;
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
			fieldOfView = GetComponent<FieldOfView>();
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
			velocity = move;
			// desiredVelocity = desVelocity;
			if (move.magnitude > 1f || move.magnitude < 1f) move.Normalize();
			move = transform.InverseTransformDirection(move);
			CheckGroundStatus();
			move = Vector3.ProjectOnPlane(move, m_GroundNormal);
			m_TurnAmount = Mathf.Atan2(move.x, move.z);
			m_ForwardAmount = move.z;

			ApplyExtraTurnRotation();

			// control and velocity handling is different when grounded and airborne:
			if (m_IsGrounded)
			{
				HandleGroundedMovement(crouch, jump);
			}
			else
			{
				HandleAirborneMovement();
			}

			ScaleCapsuleForCrouching(crouch);
			PreventStandingInLowHeadroom();

            // New functions to change m_TurnAmount and m_ForwardAmount through Implicit Movement

            // send input and other state parameters to the animator
            UpdateAnimator(move);
		}

        public void updateTimeStepVelocity (Vector3 move)
		{
			velocityN = move;

			//StartCoroutine(velocity_function());
			velocity_function();

            /*try
			{
                LBFGSOptim();
            }
            catch (alglib.alglibexception alglib_exception)
            {
                System.Console.WriteLine("ALGLIB exception with message '{0}'", alglib_exception.msg);
            }*/

        }


        void ScaleCapsuleForCrouching(bool crouch)
		{
			if (m_IsGrounded && crouch)
			{	//never hits this branch even though it keeps crouching
				if (m_Crouching) return;
				m_Capsule.height = m_Capsule.height / 2f;
				m_Capsule.center = m_Capsule.center / 2f;
				m_Crouching = true;
			}
			else
			{
				if (CrouchCheck() && allowCrouching)
				{
					m_Crouching = true;
					return;
				}
				m_Capsule.height = m_CapsuleHeight;
				m_Capsule.center = m_CapsuleCenter;
				m_Crouching = false;
			}
		}

		bool CrouchCheck() {
            Ray crouchRay = new Ray(m_Rigidbody.position + Vector3.up * m_Capsule.radius * k_Half, Vector3.up);
            float crouchRayLength = m_CapsuleHeight - m_Capsule.radius * k_Half;
            if (Physics.SphereCast(crouchRay, m_Capsule.radius * k_Half, crouchRayLength, Physics.AllLayers, QueryTriggerInteraction.Ignore)) {
				return true;
			}
			else {
				return false;
			}

        }

		void PreventStandingInLowHeadroom()
		{
			// prevent standing up in crouch-only zones
			if (!m_Crouching)
			{
				if (CrouchCheck() && allowCrouching)
				{
					m_Crouching = true;
				}
			}
		}


		void UpdateAnimator(Vector3 move)
		{
			// update the animator parameters
			m_Animator.SetFloat("Forward", m_ForwardAmount/2, 0.1f, Time.deltaTime);
			m_Animator.SetFloat("Turn", m_TurnAmount, 0.1f, Time.deltaTime);
			m_Animator.SetBool("Crouch", m_Crouching);
			m_Animator.SetBool("OnGround", m_IsGrounded);
			if (!m_IsGrounded)
			{
				m_Animator.SetFloat("Jump", m_Rigidbody.velocity.y);
			}

			// calculate which leg is behind, so as to leave that leg trailing in the jump animation
			// (This code is reliant on the specific run cycle offset in our animations,
			// and assumes one leg passes the other at the normalized clip times of 0.0 and 0.5)
			
			/*float runCycle =
				Mathf.Repeat(
					m_Animator.GetCurrentAnimatorStateInfo(0).normalizedTime + m_RunCycleLegOffset, 1);
			float jumpLeg = (runCycle < k_Half ? 1 : -1) * m_ForwardAmount;
			if (m_IsGrounded)
			{
				m_Animator.SetFloat("JumpLeg", jumpLeg);
			}*/

			// the anim speed multiplier allows the overall speed of walking/running to be tweaked in the inspector,
			// which affects the movement speed because of the root motion.
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


		void HandleAirborneMovement()
		{
			// apply extra gravity from multiplier:
			Vector3 extraGravityForce = (Physics.gravity * m_GravityMultiplier) - Physics.gravity;
			m_Rigidbody.AddForce(extraGravityForce);

			m_GroundCheckDistance = m_Rigidbody.velocity.y < 0 ? m_OrigGroundCheckDistance : 0.01f;
		}


		void HandleGroundedMovement(bool crouch, bool jump)
		{
            // check whether conditions are right to allow a jump:
            if (jump && !crouch && m_Animator.GetCurrentAnimatorStateInfo(0).IsName("Grounded"))
			{
                // jump!
                m_Rigidbody.velocity = new Vector3(m_Rigidbody.velocity.x, m_JumpPower, m_Rigidbody.velocity.z);
				m_IsGrounded = false;
				m_Animator.applyRootMotion = false;
				m_GroundCheckDistance = 0.1f;
			}
		}

		void ApplyExtraTurnRotation()
		{
			// help the character turn faster (this is in addition to root rotation in the animation)
			float turnSpeed = Mathf.Lerp(m_StationaryTurnSpeed, m_MovingTurnSpeed, m_ForwardAmount);
			transform.Rotate(0, m_TurnAmount * turnSpeed * Time.deltaTime, 0);
		}


		public void OnAnimatorMove()
		{
			// we implement this function to override the default root motion.
			// this allows us to modify the positional speed before it's applied.
			if (m_IsGrounded && Time.deltaTime > 0)
			{
				Vector3 v = (m_Animator.deltaPosition * m_MoveSpeedMultiplier) / Time.deltaTime;

				// we preserve the existing y part of the current velocity.
				v.y = m_Rigidbody.velocity.y;
				//m_Rigidbody.velocity = v;
			}
		}

        public void velocity_function()
        {
            double x = 0, z = 0;
            (double RAnticipatroryPot_1, double RAnticipatroryPot_2) = RAnticipatoryPotentialFn();
			double UPotentialEnergy = UPotentialEnergyFn();
			double equationConstant = 0.5 * (Math.Pow(velocity.x, 2) + Math.Pow(velocity.z, 2)) + UPotentialEnergy + RAnticipatroryPot_1 + RAnticipatroryPot_2;

            var f = new QuadraticObjectiveFunction(() => 0.5 * x * x + 0.5 * z * z - velocityN.x * x - velocity.z * z + equationConstant);

			// Establishing constraints for velocity.x and velocity.z
            var constraints = new List<LinearConstraint>();
			constraints.Add(new LinearConstraint(f, () => x <= 4));
            constraints.Add(new LinearConstraint(f, () => x >= 0));
            constraints.Add(new LinearConstraint(f, () => z <= 4));
            constraints.Add(new LinearConstraint(f, () => z >= 0));

            // Create the Quadratic Programming solver
            GoldfarbIdnani solver = new GoldfarbIdnani(f, constraints);

            // Minimize the function
            bool success = solver.Minimize();
            //yield return new WaitUntil(() => success = solver.Minimize());

            double value = solver.Value;
            double[] solutions = solver.Solution;

            /*//change desiredVelocity to timebased velocity at n timestep?
            func = 0.5 * Math.Pow(v[0], 2) + 0.5 * Math.Pow(v[1], 2) - velocityN.x * v[0] - velocity.z * v[1] + 0.5*(Math.Pow(velocity.x, 2) + Math.Pow(velocity.z, 2))
				+ UPotentialEnergyFn() + RAnticipatroryPot_1 + RAnticipatroryPot_2;
            //func = 100 * System.Math.Pow(v[0] + 3, 4) + System.Math.Pow(v[1] - 3, 4);
            grad[0] = v[0] - velocity.x;
            grad[1] = v[1] - velocity.z;*/
        }

        private void LBFGSOptim()
        {
			velocity_function();
			
			/*double[] x = new double[] { 0, 0 };
            double[] s = new double[] { 1, 1 };
            double epsg = 0;
            double epsf = 0;
            double epsx = 0.0000000001;
            int maxits = 0;
            alglib.minlbfgsstate state;

			alglib.minlbfgscreate(1, x, out state);
            alglib.minlbfgssetcond(state, epsg, epsf, epsx, maxits);
            alglib.minlbfgssetscale(state, s);

            alglib.minlbfgsreport rep;
            alglib.minlbfgsoptimize(state, velocity_function, null, null);
            alglib.minlbfgsresults(state, out x, out rep);*/
        }

        private (double, double) RAnticipatoryPotentialFn()
        {
            float p = 2f;
			float k = 2f;
            float eps = 0.2f;
            float tauO = 3f;
			float strengthConst = 2f;

            double cumulativeGoalPotential = 0f;
            double goalPotential = 0f;

			double cumulativeTTCPotential = 0f;
			double TTCPotential = 0f;

            // change 5 to fieldOfView - MAXOBSERVABLECHARS
            for (int i = 0; i < fieldOfView.charactersNearList.Count; i++)
            {
				goalPotential = 0.5 * strengthConst * Math.Pow((velocityN - velocity).magnitude, 2);

                Vector3 x_i = transform.position;
                Vector3 x_j = fieldOfView.charactersNearList[i].transform.position;
                Vector3 v_i = velocity;
                Vector3 v_j = velocity;	//fieldOfView.charactersNearList[i].character.

                Vector3 relativePosition = x_j - x_i;
                Vector3 relativeVelocity = v_j - v_i;

				double powerValue = -1 / calculateSigma(relativePosition, relativeVelocity, 1f, eps) * tauO; // radius is hardcoded as 1f
				Debug.Log(powerValue);

                TTCPotential = k * Math.Pow(calculateSigma(relativePosition, relativeVelocity, 1f, eps), p) * Math.Pow(Math.E, powerValue); // radius is hardcoded as 1f

                cumulativeTTCPotential += TTCPotential;
                cumulativeGoalPotential += goalPotential;
				Debug.Log(TTCPotential);
				Debug.Log(goalPotential);
            }
			return (cumulativeGoalPotential, cumulativeTTCPotential);
        }

		private double calculateSigma(Vector3 xij, Vector3 vij, float radius, float eps)
		{
            double alpha = Math.Atan(vij.z/vij.x);
			double theta = Math.Atan(xij.z/xij.x);
			float vP_mag = vij.magnitude * (float)Math.Cos(alpha-theta);
			Vector3 vP = new Vector3(xij.normalized.x * vP_mag, vij.y, xij.normalized.z * vP_mag);

			float mag = radius / (float)(Math.Pow(xij.magnitude, 2) - Math.Pow(radius, 2));
			Vector3 vt_max = new Vector3(vP.x * mag, vP.y, vP.z * mag);

			mag = (float)Math.Sqrt(1 - Math.Pow(eps, 2));
            Vector3 v_t = new Vector3(vt_max.x * mag, vt_max.y, vt_max.z * mag);

			double sigma_lim = Math.Pow(Vector3.Dot(v_t, v_t), 2) / 
				(-1*Vector3.Dot(xij, v_t) - Math.Sqrt(Math.Pow(Vector3.Dot(xij, v_t), 2) - Math.Pow(v_t.magnitude, 2) * (Math.Pow(xij.magnitude, 2) - Math.Pow(radius, 2))));
			
			if (vij.magnitude < vt_max.magnitude)
			{
				Debug.Log(Math.Pow(Vector3.Dot(xij, vij), 2));
				Debug.Log(Math.Pow(Vector3.Dot(vij, vij), 2) * (Math.Pow(Vector3.Dot(xij, xij), 2) - Math.Pow(radius, 2)));
                double sigma = Math.Pow(Vector3.Dot(vij, vij), 2) /
                (-1 * Vector3.Dot(xij, vij) - Math.Sqrt(Math.Pow(Vector3.Dot(xij, vij), 2) - Math.Pow(vij.magnitude, 2) * (Math.Pow(xij.magnitude, 2) - Math.Pow(radius, 2))));

				return sigma;
            } else
			{
				return sigma_lim * vij.magnitude / v_t.magnitude;
			}
        }

        private double UPotentialEnergyFn()
        {
            float repulsionConst = 0.01f;
            float cumulativePotentialEnergy = 0f;
            float potentialEnergy = 0f;
            float value, minimum;
            Vector3 dMin, relativePosition, relativePositionN;

            // change 5 to fieldOfView - MAXOBSERVABLECHARS
            for (int i = 0; i < fieldOfView.charactersNearList.Count; i++)
            {
                Vector3 x_i = transform.position;
                Vector3 x_j = fieldOfView.charactersNearList[i].transform.position;
				Vector3 xn_i = x_i + velocityN*Time.deltaTime;
                Vector3 xn_j = x_j + velocityN * Time.deltaTime;	//fieldOfView.charactersNearList[i].character.

                relativePosition = x_j - x_i;
                relativePositionN = xn_j - xn_i;

                value = Vector3.Dot(relativePositionN, relativePositionN - relativePosition) / (float)Math.Pow((relativePosition - relativePositionN).magnitude, 2);
                minimum = Math.Clamp(value, 0, 1);

                dMin = (1 - minimum) * relativePositionN + minimum * relativePosition;
                // dMin = dMin.normalized;

                if (dMin.magnitude > 1f)										// radius for boundary for characters hardcoded
                {
                    potentialEnergy = repulsionConst / (dMin.magnitude - 1f);   // radius for boundary for characters hardcoded
					Debug.Log(potentialEnergy);
                }
                else
                {
                    potentialEnergy = 99999f;
                }
                cumulativePotentialEnergy += potentialEnergy;

            }
            return cumulativePotentialEnergy;
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
