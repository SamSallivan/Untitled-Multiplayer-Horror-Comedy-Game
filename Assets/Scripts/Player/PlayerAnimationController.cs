using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.Serialization;

public class PlayerAnimationController : MonoBehaviour
{
    [Header("References")]
    public Animator bodyAnimator;
    public Animator armAnimator;
    public PlayerController playerController;
    
    public MultiRotationConstraint headHorizontalRotationalConstraint;
    public MultiRotationConstraint headVerticalRotationalConstraint;
    public MultiRotationConstraint headZRotationalConstraint;
    public MultiRotationConstraint shoulderRotationalConstraint;
    public MultiRotationConstraint chestRotationalConstraint;
    public ChainIKConstraint leftFootIKConstraint;
    public ChainIKConstraint rightFootIKConstraint;
    /*public ChainIKConstraint leftArmIKConstraint;
    public ChainIKConstraint rightArmIKConstraint;*/
    public TwoBoneIKConstraint leftArmIKConstraint;
    public TwoBoneIKConstraint rightArmIKConstraint;
    public List<ChainIKConstraint> rightFingerIKConstraints = new List<ChainIKConstraint>();
    public Transform leftFootIKTarget;
    public Transform rightFootIKTarget;
        
    private Transform leftFootTransform;
    private Transform rightFootTransform;
    
    [Header("Settings")]
    public float walkAnimationInterpolationSpeed = 10f;
    
    public bool footStickToSurface = true;
    public Vector3 footIKTargetPositionOffset;
    public Vector3 footIKTargetRotationOffset;
    public float surfaceDetectDistance = 1.0f;
    
    public bool turnAnimation = true;
    public float bodyRotationInterpolationSpeed = 5f;
    public float turnBodyAngleThreshold = 45f;
    public float ArmIKWeightInterpolationSpeed = 15f;
    
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
        leftFootTransform = bodyAnimator.GetBoneTransform(HumanBodyBones.LeftFoot);
        rightFootTransform = bodyAnimator.GetBoneTransform(HumanBodyBones.RightFoot);
        targetBodyRotation = transform.rotation;
    }
    
    void Update()
    {
        if (playerController.controlledByClient && !playerController.isPlayerDead.Value)
        {
            if (playerController.NetworkObject.IsOwner)
            {
                //UpdateWalkAnimation();
                //UpdateBodyRotation();
                //UpdateFallAnimation();
            }

            UpdateWalkAnimation();
            UpdateBodyRotation();
            UpdateArmAnimation();
            UpdateCrouchAnimation();
            UpdateLookRotationConstraint();
        }
    }

    void OnAnimatorIK()
    {
        if (footStickToSurface)
        {
            UpdateFootPlacement();
        }
    }

    void UpdateArmAnimation()
    {
        if (playerController.currentEquippedItem != null)
        {
            if (playerController.currentEquippedItem.itemData.twoHandAnimation)
            {
                leftArmIKConstraint.weight = Mathf.Lerp(leftArmIKConstraint.weight, 1f,Time.deltaTime * ArmIKWeightInterpolationSpeed);
            }
            else
            {
                leftArmIKConstraint.weight = Mathf.Lerp(leftArmIKConstraint.weight, 0f,Time.deltaTime * ArmIKWeightInterpolationSpeed);
            }

            rightArmIKConstraint.weight = Mathf.Lerp(rightArmIKConstraint.weight, 1f,Time.deltaTime * ArmIKWeightInterpolationSpeed);
            
            foreach (ChainIKConstraint constraint in rightFingerIKConstraints)
            {
                constraint.weight = Mathf.Lerp(constraint.weight, 1f,Time.deltaTime * ArmIKWeightInterpolationSpeed);
            }
        }
        else
        {
            leftArmIKConstraint.weight = Mathf.Lerp(leftArmIKConstraint.weight, 0f,Time.deltaTime * ArmIKWeightInterpolationSpeed);
            rightArmIKConstraint.weight = Mathf.Lerp(rightArmIKConstraint.weight, 0f,Time.deltaTime * ArmIKWeightInterpolationSpeed);
            
            foreach (ChainIKConstraint constraint in rightFingerIKConstraints)
            {
                constraint.weight = Mathf.Lerp(constraint.weight, 0f,Time.deltaTime * ArmIKWeightInterpolationSpeed);
            }
        }
    }

    void UpdateCrouchAnimation()
    {
        if (playerController.crouchingNetworkVariable.Value)
        {
            bodyAnimator.SetFloat("Crouch", Mathf.Lerp(bodyAnimator.GetFloat("Crouch"), 1f, Time.deltaTime * walkAnimationInterpolationSpeed)) ;
        }
        else
        {
            bodyAnimator.SetFloat("Crouch", Mathf.Lerp(bodyAnimator.GetFloat("Crouch"), 0f, Time.deltaTime * walkAnimationInterpolationSpeed)) ;
        }
    }
    
    void UpdateWalkAnimation()
    {
        float inputX = (playerController.inputDirNetworkVariable.Value.x == 0) ? 0 : 1;
        float inputZ = (playerController.inputDirNetworkVariable.Value.z == 0) ? 0 : 1;
        float sprint = playerController.sprintingNetworkVariable.Value ? 1 : 0.5f;
        Vector3 velocity = playerController.transform.GetChild(0).InverseTransformDirection(playerController.velNetworkVariable.Value).normalized;
        //velocity = new Vector3(velocity.x, 0, velocity.z);

        float tempX = velocity.x * inputX * sprint;
        float tempZ = velocity.z * inputZ * sprint;

        if(tempZ < 0)
        {
            tempX *= 0.5f;
        }

        if (velocity.sqrMagnitude >= 0.5f)
        {
            bodyAnimator.SetBool("isMoving", true);
        }
        else
        {
            bodyAnimator.SetBool("isMoving", false);
        }

        velocityX = Mathf.Lerp(velocityX, tempX, Time.deltaTime * walkAnimationInterpolationSpeed);
        velocityZ = Mathf.Lerp(velocityZ, tempZ, Time.deltaTime * walkAnimationInterpolationSpeed);
        bodyAnimator.SetFloat("VelocityX", velocityX);
        bodyAnimator.SetFloat("VelocityZ", velocityZ);
            
        //Add body leaning using additive layer:
        //Get rigidbody velocity horizontal, tilt body Z rotation
        if (bodyAnimator.GetBool("isMoving") && playerController.grounder.grounded)
        {
            Vector2 mouseInput = playerController.playerInputActions.FindAction("Look").ReadValue<Vector2>();
            float leanX = mouseInput.x * playerController.mouseLookX.sensitivityX;
            leanX *= playerController.transform.GetChild(0).InverseTransformDirection(playerController.velNetworkVariable.Value).z / 50;
            bodyAnimator.SetFloat("LeanX", Mathf.Lerp(bodyAnimator.GetFloat("LeanX"), leanX, Time.deltaTime * 2f));
        }
        else
        {
            bodyAnimator.SetFloat("LeanX", Mathf.Lerp(bodyAnimator.GetFloat("LeanX"), 0, Time.deltaTime * 2f));
        }
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
        
        var turnSpeed = bodyRotationInterpolationSpeed;

        if (!bodyAnimator.GetBool("isMoving") && turnAnimation)
        {
            if (angle >= turnBodyAngleThreshold && turnBodyCooldown <= 0)
            {
                bodyAnimator.SetTrigger("TurnRight");
                turnBodyCooldown = 0.5f;
                targetBodyRotation = playerController.transform.GetChild(0).rotation;
            }
            else if (angle <= -turnBodyAngleThreshold && turnBodyCooldown <= 0)
            {
                bodyAnimator.SetTrigger("TurnLeft");
                turnBodyCooldown = 0.5f;
                targetBodyRotation = playerController.transform.GetChild(0).rotation;
            }
            else if (Mathf.Abs(angle) > 120f)
            {
                targetBodyRotation = playerController.transform.GetChild(0).rotation;
                turnSpeed *= 3.5f;
            }
        }
        else
        {
            targetBodyRotation = playerController.transform.GetChild(0).rotation;
            if (Mathf.Abs(angle) > 120f)
            {
                turnSpeed *= 3.5f;
            }
        }
        
        transform.rotation = Quaternion.Slerp(transform.rotation, targetBodyRotation, Time.deltaTime * turnSpeed);
    }

    void UpdateLookRotationConstraint()
    {
        //Upper Body Rotation Constraint Weight
        if (playerController.grounder.groundTime > 0.5f)
        {
            headHorizontalRotationalConstraint.weight = Mathf.Lerp(headHorizontalRotationalConstraint.weight, 0.5f,Time.deltaTime * 5f);
            headZRotationalConstraint.weight = Mathf.Lerp(headZRotationalConstraint.weight, 0.5f,Time.deltaTime * 5f);
            shoulderRotationalConstraint.weight = Mathf.Lerp(shoulderRotationalConstraint.weight, 0.25f,Time.deltaTime * 5f);
            chestRotationalConstraint.weight = Mathf.Lerp(chestRotationalConstraint.weight, 0.25f,Time.deltaTime * 5f);
        }
        else
        {
            headHorizontalRotationalConstraint.weight = Mathf.Lerp(headHorizontalRotationalConstraint.weight, 0f,Time.deltaTime * 5f);
            headZRotationalConstraint.weight = Mathf.Lerp(headZRotationalConstraint.weight, 0f,Time.deltaTime * 5f);
            shoulderRotationalConstraint.weight = Mathf.Lerp(shoulderRotationalConstraint.weight, 0f,Time.deltaTime * 5f);
            chestRotationalConstraint.weight = Mathf.Lerp(chestRotationalConstraint.weight, 0f,Time.deltaTime * 5f);
        }
    }

    void UpdateFootPlacement()
    {
        if (playerController.grounder.grounded)
        {
            bodyAnimator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, bodyAnimator.GetFloat("LeftFoot"));
            bodyAnimator.SetIKPositionWeight(AvatarIKGoal.RightFoot, bodyAnimator.GetFloat("RightFoot"));

            bodyAnimator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, bodyAnimator.GetFloat("LeftFoot"));
            bodyAnimator.SetIKRotationWeight(AvatarIKGoal.RightFoot, bodyAnimator.GetFloat("RightFoot"));
        }
        else
        {
            bodyAnimator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 0);
            bodyAnimator.SetIKPositionWeight(AvatarIKGoal.RightFoot, 0);

            bodyAnimator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, 0);
            bodyAnimator.SetIKRotationWeight(AvatarIKGoal.RightFoot, 0);
        }


        FootToSurfaceRaycast(leftFootTransform, ref _leftFootIKTargetPos, ref _leftFootIKTargetRot);
        FootToSurfaceRaycast(rightFootTransform, ref _rightFootIKTargetPos, ref _rightFootIKTargetRot);

        /*_leftFootIKTargetRot.eulerAngles = new Vector3(_leftFootIKTargetRot.eulerAngles.x, leftFootTransform.rotation.eulerAngles.y, _leftFootIKTargetRot.eulerAngles.z);
        _rightFootIKTargetRot.eulerAngles = new Vector3(_rightFootIKTargetRot.eulerAngles.x, rightFootTransform.rotation.eulerAngles.y, _rightFootIKTargetRot.eulerAngles.z);
        */

        bodyAnimator.SetIKPosition(AvatarIKGoal.LeftFoot, _leftFootIKTargetPos + footIKTargetPositionOffset);
        bodyAnimator.SetIKRotation(AvatarIKGoal.LeftFoot, _leftFootIKTargetRot * Quaternion.Euler(footIKTargetRotationOffset));
        
        bodyAnimator.SetIKPosition(AvatarIKGoal.RightFoot, _rightFootIKTargetPos + footIKTargetPositionOffset);
        bodyAnimator.SetIKRotation(AvatarIKGoal.RightFoot, _rightFootIKTargetRot * Quaternion.Euler(footIKTargetRotationOffset));

        /*leftFootIKConstraint.weight = animator.GetFloat("LeftFoot");
        rightFootIKConstraint.weight = animator.GetFloat("RightFoot");
        
        leftFootIKTarget.position = _leftFootIKTargetPos + footIKTargetPositionOffset;
        leftFootIKTarget.rotation = _leftFootIKTargetRot * Quaternion.Euler(footIKTargetRotationOffset);
        rightFootIKTarget.position = _rightFootIKTargetPos + footIKTargetPositionOffset;
        rightFootIKTarget.rotation = _rightFootIKTargetRot * Quaternion.Euler(footIKTargetRotationOffset);*/
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
