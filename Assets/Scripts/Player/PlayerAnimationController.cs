using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class PlayerAnimationController : MonoBehaviour
{
    public Animator animator;
    public PlayerController playerController;
    public float velocityX;
    public float velocityZ;
    public float transitionSpeed = 1;
    Vector3 _leftFootPos;
    Quaternion _leftFootRot;
    Vector3 _rightFootPos;
    Quaternion _rightFootRot;
    public Vector3 footIKOffset;

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
                UpdateWalkAnimation();
            }
        }
    }
    
    void UpdateWalkAnimation()
    {
        float inputX = (playerController.inputDir.x == 0) ? 0 : 1;
        float inputZ = (playerController.inputDir.z == 0) ? 0 : 1;
        float sprint = playerController.sprinting ? 1 : 0.5f;
        Vector3 velocity = playerController.headTransform.InverseTransformDirection(playerController.vel).normalized;

        float tempX = velocity.x * inputX * sprint;
        float tempZ = velocity.z * inputZ * sprint;

        if(tempZ < 0)
        {
            tempX *= 0.5f;
        }

        velocityX = Mathf.Lerp(velocityX, tempX, Time.deltaTime * transitionSpeed);
        velocityZ = Mathf.Lerp(velocityZ, tempZ, Time.deltaTime * transitionSpeed);
        animator.SetFloat("VelocityX", velocityX);
        animator.SetFloat("VelocityZ", velocityZ);
    }

    void OnAnimatorIK()
    {
        UpdateFootPlacement();
    }


    void UpdateFootPlacement()
    {
        animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, animator.GetFloat("LeftFoot"));
        animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, animator.GetFloat("RightFoot"));
        
        animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, animator.GetFloat("LeftFoot"));
        animator.SetIKRotationWeight(AvatarIKGoal.RightFoot, animator.GetFloat("RightFoot"));
        
        Transform leftFoot = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
        Transform rightFoot = animator.GetBoneTransform(HumanBodyBones.RightFoot);
        
        FindFloorPositions(leftFoot, ref _leftFootPos, ref _leftFootRot, Vector3.up);
        FindFloorPositions(rightFoot, ref _rightFootPos, ref _rightFootRot, Vector3.up);
        
        animator.SetIKPosition(AvatarIKGoal.LeftFoot, _leftFootPos + footIKOffset);
        animator.SetIKRotation(AvatarIKGoal.LeftFoot, _leftFootRot);
        
        animator.SetIKPosition(AvatarIKGoal.RightFoot, _rightFootPos + footIKOffset);
        animator.SetIKRotation(AvatarIKGoal.RightFoot, _rightFootRot);
    }
    
    void FindFloorPositions(Transform t, ref Vector3 targetPosition, ref Quaternion targetRotation, Vector3 direction)
    {
        RaycastHit hit;
        Vector3 rayOrigin = t.position;
        // move the ray origin back a bit
        rayOrigin += direction * 0.3f;
 
        // raycast in the given direction
        Debug.DrawRay(rayOrigin, -direction, Color.green);
        if (Physics.Raycast(rayOrigin, -direction, out hit, 3))
        {
            // the hit point is the position of the hand/foot
            targetPosition = hit.point;            
            // then rotate based on the hit normal
            Quaternion rot = Quaternion.LookRotation(transform.forward);          
            targetRotation = Quaternion.FromToRotation(Vector3.up, hit.normal) * rot;          
        }
    }
}
