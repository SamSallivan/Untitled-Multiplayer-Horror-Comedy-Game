using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.Serialization;

public class PlayerAnimationController : MonoBehaviour
{
    [Header("References")]
    public Animator animator;
    public PlayerController playerController;
    
    public MultiRotationConstraint headRotationalConstraint;
    public MultiRotationConstraint shoulderRotationalConstraint;
    public MultiRotationConstraint chestRotationalConstraint;
    public ChainIKConstraint leftFootIKConstraint;
    public ChainIKConstraint rightFootIKConstraint;
    public Transform leftFootIKTarget;
    public Transform rightFootIKTarget;
        
    private Transform leftFootTransform;
    private Transform rightFootTransform;
    
    [Header("Settings")]
    public float walkAnimationInterpolationSpeed = 10f;
    
    public bool footStickToSurface = true;
    public Vector3 footIKTargetPositionOffset;
    public float surfaceDetectDistance = 1.0f;
    
    public bool turnAnimation = true;
    public float bodyRotationInterpolationSpeed = 5f;
    public float turnBodyAngleThreshold = 45f;
    
    public float fallAirTimeThreshold = 0.65f;
    
    [Header("Values")]
    private float velocityX;
    private float velocityZ;
    
    private Vector3 _leftFootIKTargetPos;
    private Quaternion _leftFootIKTargetRot;
    private Vector3 _rightFootIKTargetPos;
    private Quaternion _rightFootIKTargetRot;

    private Quaternion targetBodyRotation;
    private float turnBodyCooldown;

    void Awake()
    {
        leftFootTransform = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
        rightFootTransform = animator.GetBoneTransform(HumanBodyBones.RightFoot);
        targetBodyRotation = transform.rotation;
    }
    
    void Update()
    {
        if (playerController.NetworkObject.IsOwner && !playerController.isPlayerDead)
        {   
            if (playerController.enableMovement)
            {   
                UpdateWalkAnimation();
            }
            
            if(playerController.mouseLookX.enabled)
            {
                UpdateBodyRotation();
            }
            
            UpdateFallAnimation();
        }
    }

    void OnAnimatorIK()
    {
        if (playerController.NetworkObject.IsOwner)
        {
            if (footStickToSurface)
            {
                UpdateFootPlacement();
            }
        }
    }
    
    void UpdateWalkAnimation()
    {
        float inputX = (playerController.inputDir.x == 0) ? 0 : 1;
        float inputZ = (playerController.inputDir.z == 0) ? 0 : 1;
        float sprint = playerController.sprinting ? 1 : 0.5f;
        Vector3 velocity = playerController.transform.GetChild(0).InverseTransformDirection(playerController.vel).normalized;
        //velocity = new Vector3(velocity.x, 0, velocity.z);

        float tempX = velocity.x * inputX * sprint;
        float tempZ = velocity.z * inputZ * sprint;

        if(tempZ < 0)
        {
            tempX *= 0.5f;
        }

        if (velocity.sqrMagnitude >= 0.5f)
        {
            animator.SetBool("isMoving", true);
        }
        else
        {
            animator.SetBool("isMoving", false);
        }

        velocityX = Mathf.Lerp(velocityX, tempX, Time.deltaTime * walkAnimationInterpolationSpeed);
        velocityZ = Mathf.Lerp(velocityZ, tempZ, Time.deltaTime * walkAnimationInterpolationSpeed);
        animator.SetFloat("VelocityX", velocityX);
        animator.SetFloat("VelocityZ", velocityZ);
            
        //Add body leaning using additive layer:
        //Get rigidbody velocity horizontal, tilt body Z rotation
        float leanX = playerController.rb.angularVelocity.normalized.y;
        animator.SetFloat("LeanX", Mathf.Lerp(animator.GetFloat("LeanX"), leanX, Time.deltaTime * bodyRotationInterpolationSpeed));
        //Get ground normal z, tilt waist X rotation
    }
    
    void UpdateBodyRotation()
    {
        if (turnBodyCooldown > 0)
        {
            turnBodyCooldown -= Time.deltaTime;
        }
        
        Vector3 headTransformForward = playerController.transform.GetChild(0).forward;
        Vector3 bodyTransformForward = transform.forward;
        float angle = Vector3.Angle(bodyTransformForward, headTransformForward);
        Vector3 cross = Vector3.Cross(bodyTransformForward, headTransformForward);
        angle *= Mathf.Sign(cross.y);
        
        if (!animator.GetBool("isMoving") && turnAnimation & Mathf.Abs(angle) < 120f)
        {
            if (angle >= turnBodyAngleThreshold && turnBodyCooldown <= 0)
            {
                animator.SetTrigger("TurnRight");
                targetBodyRotation = playerController.transform.GetChild(0).rotation;
                turnBodyCooldown = 0.5f;
            }
            else if (angle <= -turnBodyAngleThreshold && turnBodyCooldown <= 0)
            {
                animator.SetTrigger("TurnLeft");
                targetBodyRotation = playerController.transform.GetChild(0).rotation;
                turnBodyCooldown = 0.5f;
            }
        }
        else
        {
            targetBodyRotation = playerController.transform.GetChild(0).rotation;
        }
        
        transform.rotation = Quaternion.Slerp(transform.rotation, targetBodyRotation, Time.deltaTime * bodyRotationInterpolationSpeed);
        
        //Upper Body Rotation Constraint Weight
        if (playerController.grounder.groundTime > 0.5f)
        {
            shoulderRotationalConstraint.weight = Mathf.Lerp(shoulderRotationalConstraint.weight, 0.66f,Time.deltaTime * bodyRotationInterpolationSpeed);
            chestRotationalConstraint.weight = Mathf.Lerp(chestRotationalConstraint.weight, 0.33f,Time.deltaTime * bodyRotationInterpolationSpeed);
        }
        else
        {
            shoulderRotationalConstraint.weight = 0;
            chestRotationalConstraint.weight = 0;
        }
        
    }
    
    void UpdateFallAnimation()
    {
        if (playerController.grounder.airTime > fallAirTimeThreshold && playerController.jumpCooldown <= 0)
        {
            animator.SetBool("isFalling", true);
        }
    }

    void UpdateFootPlacement()
    {
        animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, animator.GetFloat("LeftFoot"));
        animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, animator.GetFloat("RightFoot"));
        
        animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, animator.GetFloat("LeftFoot"));
        animator.SetIKRotationWeight(AvatarIKGoal.RightFoot, animator.GetFloat("RightFoot"));

        /*leftFootIKConstraint.weight = animator.GetFloat("LeftFoot");
        rightFootIKConstraint.weight = animator.GetFloat("RightFoot");*/
        
        FootToSurfaceRaycast(leftFootTransform, ref _leftFootIKTargetPos, ref _leftFootIKTargetRot);
        FootToSurfaceRaycast(rightFootTransform, ref _rightFootIKTargetPos, ref _rightFootIKTargetRot);
        
        animator.SetIKPosition(AvatarIKGoal.LeftFoot, _leftFootIKTargetPos + footIKTargetPositionOffset);
        animator.SetIKRotation(AvatarIKGoal.LeftFoot, _leftFootIKTargetRot);
        
        animator.SetIKPosition(AvatarIKGoal.RightFoot, _rightFootIKTargetPos + footIKTargetPositionOffset);
        animator.SetIKRotation(AvatarIKGoal.RightFoot, _rightFootIKTargetRot);

        /*leftFootIKTarget.position = Vector3.Lerp(leftFootIKTarget.position, _leftFootIKTargetPos, Time.deltaTime * velocityLerpSpeed);
        leftFootIKTarget.rotation = Quaternion.Lerp(leftFootIKTarget.rotation, _leftFootIKTargetRot, Time.deltaTime * velocityLerpSpeed);
        rightFootIKTarget.position = Vector3.Lerp(rightFootIKTarget.position, _rightFootIKTargetPos, Time.deltaTime * velocityLerpSpeed);
        rightFootIKTarget.rotation = Quaternion.Lerp(rightFootIKTarget.rotation, _rightFootIKTargetRot, Time.deltaTime * velocityLerpSpeed);*/


    }
    
    void FootToSurfaceRaycast(Transform footTransform, ref Vector3 targetPosition, ref Quaternion targetRotation)
    {
        // move the ray origin back a bit
        Vector3 origin = footTransform.position + Vector3.up * 0.3f;
        RaycastHit hit;
 
        // raycast in the given direction
        if (Physics.Raycast(origin, -Vector3.up, out hit, surfaceDetectDistance))
        {
            // the hit point is the position of the hand/foot
            targetPosition = hit.point;            
            // then rotate based on the hit normal
            Quaternion rot = Quaternion.LookRotation(transform.forward);          
            targetRotation = Quaternion.FromToRotation(Vector3.up, hit.normal) * rot;   
            Debug.DrawRay(origin, -Vector3.up, Color.green);       
        }
        else
        {
            Debug.DrawRay(origin, -Vector3.up, Color.red);
        }
    }
}
