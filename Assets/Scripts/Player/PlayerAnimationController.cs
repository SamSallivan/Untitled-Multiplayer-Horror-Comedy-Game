using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAnimationController : MonoBehaviour
{
    public Animator animator;
    public PlayerController playerController;
    public float velocityX;
    public float velocityZ;
    public float tempX;
    public float tempZ;
    public float transitionSpeed = 1;

    void Awake()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (playerController.NetworkObject.IsOwner)
        {   
            if (playerController.enableMovement)
            {   
                float inputX = (playerController.inputDir.x == 0) ? 0 : 1;
                float inputZ = (playerController.inputDir.z == 0) ? 0 : 1;
                float sprint = playerController.sprinting ? 1 : 0.5f;
                Vector3 velocity = playerController.headTransform.InverseTransformDirection(playerController.vel).normalized;

                tempX = velocity.x * inputX * sprint;
                tempZ = velocity.z * inputZ * sprint;

                if(tempZ < 0)
                {
                    tempX *= 0.5f;
                }

                velocityX = Mathf.Lerp(velocityX, tempX, Time.deltaTime * transitionSpeed);
                velocityZ = Mathf.Lerp(velocityZ, tempZ, Time.deltaTime * transitionSpeed);
                animator.SetFloat("VelocityX", velocityX);
                animator.SetFloat("VelocityZ", velocityZ);

                // if(velocity.sqrMagnitude > 0.75f)
                // {
                //     animator.SetBool("isMoving", true);
                // }
                // else
                // {
                //     animator.SetBool("isMoving", false);
                // }
            }
        }
    }

    void OnAnimatorIK() 
    {
        animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, animator.GetFloat("LeftFoot"));
        animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, animator.GetFloat("RightFoot"));
        animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, animator.GetFloat("LeftFoot"));
        animator.SetIKRotationWeight(AvatarIKGoal.RightFoot, animator.GetFloat("RightFoot"));
    }
}
