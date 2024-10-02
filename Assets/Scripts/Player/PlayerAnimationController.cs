using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.Serialization;
using Sirenix.OdinInspector;

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
    public TwoBoneIKConstraint leftArmIKConstraint;
    public TwoBoneIKConstraint rightArmIKConstraint;
    public List<ChainIKConstraint> rightFingerIKConstraints = new List<ChainIKConstraint>();

    public Transform leftArmIKTarget;
    public Transform rightArmIKTarget;

    [SerializeField]
    private Transform leftArmTransform;
    [SerializeField]
    private Transform rightArmTransform;
    [SerializeField]
    private Transform leftFootTransform;
    [SerializeField]
    private Transform rightFootTransform;

    [Header("Settings")] 
    public float walkAnimationInterpolationSpeed = 10f;

    public bool footStickToSurface = true;
    public LayerMask footSurfaceLayerMask;
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
    private Vector3 previousPosition;

    [Header("Emotes")] 
    public EmoteData emoteData;
    [SerializeField]
    private bool lockLookRotation;
    [SerializeField]
    private bool lockBodyRotation;
    [SerializeField]
    private bool overrideArmAnimation;
    [SerializeField]
    private bool leftArmAnimation;
    [SerializeField]
    private bool rightArmAnimation;
    
    
    [Header("Models")] 
    public List<GameObject> modelList = new List<GameObject>();
    public List<Avatar> avatarList = new List<Avatar>();

    void Awake()
    {
        leftArmTransform = bodyAnimator.GetBoneTransform(HumanBodyBones.LeftHand);
        rightArmTransform = bodyAnimator.GetBoneTransform(HumanBodyBones.RightHand);
        leftFootTransform = bodyAnimator.GetBoneTransform(HumanBodyBones.LeftFoot);
        rightFootTransform = bodyAnimator.GetBoneTransform(HumanBodyBones.RightFoot);
        targetBodyRotation = transform.rotation;
    }

    void Update()
    {
        if (playerController.controlledByClient.Value && !playerController.isPlayerDead.Value)
        {

            UpdateWalkAnimation();
            UpdateCrouchAnimation();
            
            UpdateEmoteAnimation();
            
            UpdateBodyRotation();
            UpdateArmAnimationWeight();
            UpdateLookAnimationWeight();
        }
    }

    void OnAnimatorIK()
    {
        if (footStickToSurface)
        {
            UpdateFootPlacement();
        }
    }

    void UpdateArmAnimationWeight()
    {
        if (overrideArmAnimation)
        {
            if (leftArmAnimation)
            {
                leftArmIKConstraint.weight = Mathf.Lerp(leftArmIKConstraint.weight, 1f, Time.deltaTime * ArmIKWeightInterpolationSpeed);
            }
            else
            {
                leftArmIKConstraint.weight = Mathf.Lerp(leftArmIKConstraint.weight, 0f, Time.deltaTime * ArmIKWeightInterpolationSpeed);
            }

            if (rightArmAnimation)
            {
                rightArmIKConstraint.weight = Mathf.Lerp(rightArmIKConstraint.weight, 1f, Time.deltaTime * ArmIKWeightInterpolationSpeed);

                foreach (ChainIKConstraint constraint in rightFingerIKConstraints)
                {
                    constraint.weight =
                        Mathf.Lerp(constraint.weight, 1f, Time.deltaTime * ArmIKWeightInterpolationSpeed);
                }
            }
            else
            {
                
                rightArmIKConstraint.weight = Mathf.Lerp(rightArmIKConstraint.weight, 0f, Time.deltaTime * ArmIKWeightInterpolationSpeed);

                foreach (ChainIKConstraint constraint in rightFingerIKConstraints)
                {
                    constraint.weight =
                        Mathf.Lerp(constraint.weight, 0f, Time.deltaTime * ArmIKWeightInterpolationSpeed);
                }
            }

            return;
        }
        
        if (playerController.currentEquippedItem != null)
        {
            if (playerController.currentEquippedItem.itemData.leftHandAnimation)
            {
                leftArmIKConstraint.weight = Mathf.Lerp(leftArmIKConstraint.weight, 1f, Time.deltaTime * ArmIKWeightInterpolationSpeed);
            }
            else
            {
                leftArmIKConstraint.weight = Mathf.Lerp(leftArmIKConstraint.weight, 0f, Time.deltaTime * ArmIKWeightInterpolationSpeed);
            }

            if (playerController.currentEquippedItem.itemData.rightHandAnimation)
            {
                rightArmIKConstraint.weight = Mathf.Lerp(rightArmIKConstraint.weight, 1f, Time.deltaTime * ArmIKWeightInterpolationSpeed);
            }
            else
            {
                rightArmIKConstraint.weight = Mathf.Lerp(rightArmIKConstraint.weight, 0f, Time.deltaTime * ArmIKWeightInterpolationSpeed);
            }

            foreach (ChainIKConstraint constraint in rightFingerIKConstraints)
            {
                constraint.weight = Mathf.Lerp(constraint.weight, 1f, Time.deltaTime * ArmIKWeightInterpolationSpeed);
            }
        }
        else
        {
            leftArmIKConstraint.weight = Mathf.Lerp(leftArmIKConstraint.weight, 0f, Time.deltaTime * ArmIKWeightInterpolationSpeed);
            rightArmIKConstraint.weight = Mathf.Lerp(rightArmIKConstraint.weight, 0f, Time.deltaTime * ArmIKWeightInterpolationSpeed);
            
            //left fingers
            foreach (ChainIKConstraint constraint in rightFingerIKConstraints)
            {
                constraint.weight = Mathf.Lerp(constraint.weight, 0f, Time.deltaTime * ArmIKWeightInterpolationSpeed);
            }
        }
    }

    void UpdateCrouchAnimation()
    {
        if (playerController.crouchingNetworkVariable.Value)
        {
            bodyAnimator.SetFloat("Crouch",
                Mathf.Lerp(bodyAnimator.GetFloat("Crouch"), 1f, Time.deltaTime * walkAnimationInterpolationSpeed));
        }
        else
        {
            bodyAnimator.SetFloat("Crouch",
                Mathf.Lerp(bodyAnimator.GetFloat("Crouch"), 0f, Time.deltaTime * walkAnimationInterpolationSpeed));
        }
    }

    void UpdateWalkAnimation()
    {
        float inputX = (playerController.inputDirNetworkVariable.Value.x == 0) ? 0 : 1;
        float inputZ = (playerController.inputDirNetworkVariable.Value.z == 0) ? 0 : 1;
        float sprint = playerController.sprintingNetworkVariable.Value ? 1 : 0.5f;
        Vector3 velocity = (playerController.transform.position - previousPosition) / Time.deltaTime;
        velocity = playerController.transform.GetChild(0).InverseTransformDirection(velocity);
        Vector3 velocityNormalized = velocity.normalized;
        previousPosition = playerController.transform.position;

        float tempX = velocityNormalized.x * inputX * sprint;
        float tempZ = velocityNormalized.z * inputZ * sprint;

        if (tempZ < 0)
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
        if (bodyAnimator.GetBool("isMoving") && playerController.grounder.grounded.Value)
        {
            Vector2 mouseInput = playerController.playerInputActions.FindAction("Look").ReadValue<Vector2>();
            float leanX = mouseInput.x * playerController.mouseLookX.sensitivityX;
            leanX *= velocity.z / 50;
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

        if (lockBodyRotation)
        {
            return;
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

    void UpdateLookAnimationWeight()
    {
        if (!lockLookRotation)
        {
            //Upper Body Rotation Constraint Weight
            //if (playerController.grounder.groundTime.Value > 0.5f)
            if (playerController.grounder.groundTime >= 0.5f)
            {
                //headVerticalRotationalConstraint.weight = Mathf.Lerp(headVerticalRotationalConstraint.weight, 0.5f, Time.deltaTime * 5f);
                headHorizontalRotationalConstraint.weight = Mathf.Lerp(headHorizontalRotationalConstraint.weight, 0.5f, Time.deltaTime * 5f);
                headZRotationalConstraint.weight = Mathf.Lerp(headZRotationalConstraint.weight, 0.5f, Time.deltaTime * 5f);
                shoulderRotationalConstraint.weight = Mathf.Lerp(shoulderRotationalConstraint.weight, 0.25f, Time.deltaTime * 5f);
                chestRotationalConstraint.weight = Mathf.Lerp(chestRotationalConstraint.weight, 0.25f, Time.deltaTime * 5f);
            }
            else
            {
                //headVerticalRotationalConstraint.weight = Mathf.Lerp(headVerticalRotationalConstraint.weight, 0f, Time.deltaTime * 5f);
                headHorizontalRotationalConstraint.weight = Mathf.Lerp(headHorizontalRotationalConstraint.weight, 0f, Time.deltaTime * 5f);
                headZRotationalConstraint.weight = Mathf.Lerp(headZRotationalConstraint.weight, 0f, Time.deltaTime * 5f);
                shoulderRotationalConstraint.weight = Mathf.Lerp(shoulderRotationalConstraint.weight, 0f, Time.deltaTime * 5f);
                chestRotationalConstraint.weight = Mathf.Lerp(chestRotationalConstraint.weight, 0f, Time.deltaTime * 5f);
            }

            headVerticalRotationalConstraint.weight = Mathf.Lerp(headVerticalRotationalConstraint.weight, 0.5f, Time.deltaTime * 5f);
        }
        else
        {
            headHorizontalRotationalConstraint.weight = Mathf.Lerp(headHorizontalRotationalConstraint.weight, 0f, Time.deltaTime * 5f);
            headZRotationalConstraint.weight = Mathf.Lerp(headZRotationalConstraint.weight, 0f, Time.deltaTime * 5f);
            shoulderRotationalConstraint.weight = Mathf.Lerp(shoulderRotationalConstraint.weight, 0f, Time.deltaTime * 5f);
            chestRotationalConstraint.weight = Mathf.Lerp(chestRotationalConstraint.weight, 0f, Time.deltaTime * 5f);
            headVerticalRotationalConstraint.weight = Mathf.Lerp(headVerticalRotationalConstraint.weight, 0f, Time.deltaTime * 5f);
        }
        
    }

    void UpdateFootPlacement()
    {
        if (emoteData && emoteData.fullBodyAnimation)
        {
            return;
        }
        
        if (playerController.grounder.grounded.Value)
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
        if (Physics.Raycast(origin, -Vector3.up, out hit, surfaceDetectDistance, footSurfaceLayerMask))
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

    public void StartEmoteAnimation(int index)
    {
        emoteData = playerController.emoteDataList[index];
        if (emoteData.fullBodyAnimation)
        {
            bodyAnimator.SetTrigger(emoteData.animatorTrigger);
            bodyAnimator.applyRootMotion = true;
            lockLookRotation = emoteData.lockLookRotation;
            lockBodyRotation = emoteData.lockBodyRotation;
            overrideArmAnimation = emoteData.overrideArmAnimation;
            leftArmAnimation = !overrideArmAnimation;
            rightArmAnimation = !overrideArmAnimation;
        }
        else
        {
            armAnimator.SetTrigger(emoteData.animatorTrigger);
            lockLookRotation = false;
            lockBodyRotation = false;
            overrideArmAnimation = true;
            leftArmAnimation = emoteData.leftArmAnimation;
            rightArmAnimation = emoteData.rightArmAnimation;
        }
    }
    
    public void StopEmoteAnimation()
    {
        if (!emoteData)
        {
            return;
        }
        
        if (emoteData.fullBodyAnimation)
        {
            bodyAnimator.ResetTrigger(emoteData.animatorTrigger);
            bodyAnimator.SetTrigger("Stop Emote");
        }
        else
        {
            armAnimator.ResetTrigger(emoteData.animatorTrigger);
            armAnimator.SetTrigger("Stop Emote");
        }
        transform.localPosition = new Vector3(0, -1, 0);
        bodyAnimator.applyRootMotion = false;
        armAnimator.enabled = true;
        lockLookRotation = false;
        lockBodyRotation = false;
        overrideArmAnimation = false;
        leftArmAnimation = false;
        rightArmAnimation = false;
        emoteData = null;
        playerController.StopEmote();
    }

    void UpdateEmoteAnimation()
    {
        if (emoteData)
        {
            if (emoteData.fullBodyAnimation)
            {
                if (overrideArmAnimation)
                {
                    armAnimator.enabled = false;
                    leftArmIKTarget.position = leftArmTransform.position;
                    leftArmIKTarget.rotation = leftArmTransform.rotation;
                    rightArmIKTarget.position = rightArmTransform.position;
                    rightArmIKTarget.rotation = rightArmTransform.rotation;
                }
                
                if (bodyAnimator.GetBool("isMoving") || armAnimator.GetBool("Held") || playerController.crouchingNetworkVariable.Value || playerController.isPlayerDead.Value)
                {
                    if (playerController.IsOwner)
                    {
                        playerController.StopEmoteRpc();
                    }
                }
            }
            else
            {
                if (armAnimator.GetBool("Held") || playerController.isPlayerDead.Value)
                {
                    if (playerController.IsOwner)
                    {
                        playerController.StopEmoteRpc();
                    }
                }
            }
        }

        if (GameSessionManager.Instance.localPlayerController == playerController)
        {
            if (emoteData && emoteData.overrideCameraPosition)
            {
                Camera.main.transform.localPosition = Vector3.Lerp(Camera.main.transform.localPosition, emoteData.targetCameraPosition, Time.deltaTime * 10f);
            }
            else if (!SpectateManager.Instance.isSpectating)
            {
                Camera.main.transform.localPosition = Vector3.Lerp(Camera.main.transform.localPosition, new Vector3(0, 0, 0), Time.deltaTime * 10f);
            }
        }
    }
}
