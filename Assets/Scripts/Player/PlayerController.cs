using System;
using System.Collections;
using System.Collections.Generic;
using NWH.DWP2.WaterObjects;
using UnityEngine;
using Unity.Netcode;
using System.Linq;
using Steamworks;
using TMPro;
using Dissonance;
using Sirenix.OdinInspector;
using Enviro;

public class PlayerController : NetworkBehaviour, IDamagable
{
    public static PlayerController instance;

    // [FoldoutGroup("Inventory")]
    // public List<I_InventoryItem> inventoryItemList= new List<I_InventoryItem>();

    // [FoldoutGroup("Inventory")]
    // public List<I_InventoryItem> storageItemList= new List<I_InventoryItem>();

    [FoldoutGroup("Voice Chat")]
    public VoicePlayerState voicePlayerState;

    [FoldoutGroup("Voice Chat")]
    public AudioSource currentVoiceChatAudioSource;

    [FoldoutGroup("Voice Chat")]
    public PlayerVoiceIngameSettings currentVoiceChatIngameSettings;

    [FoldoutGroup("Networks")]
    public string playerUsername = "Player";

    [FoldoutGroup("Networks")]
    public ulong localPlayerId;

    [FoldoutGroup("Networks")]
    public ulong localClientId;

    [FoldoutGroup("Networks")]
	public ulong localSteamId;

    [FoldoutGroup("Networks")]
    public bool controlledByClient;

    [FoldoutGroup("Networks")]
    public bool isPlayerDead;

    [FoldoutGroup("Networks")]
    public bool awaitInitialization;

    // [FoldoutGroup("Networks")]
    // public Vector3 playerServerPosition;

    [FoldoutGroup("References")]
    public Transform playerUsernameCanvasTransform;

    [FoldoutGroup("References")]
    public TMP_Text playerUsernameText;

    [FoldoutGroup("References")]
    public List<Camera> cameraList = new List<Camera>();

    [FoldoutGroup("References")]
    public Transform headTransform;

    [FoldoutGroup("References")]
    public Transform equippedTransform;

    [FoldoutGroup("References")]
    public Rigidbody rb;

    [FoldoutGroup("References")]
    public CapsuleCollider playerCollider;

    [FoldoutGroup("References")]
    private Grounder grounder;

    [FoldoutGroup("References")]
    public MouseLook mouseLookX;

    [FoldoutGroup("References")]
    public MouseLook mouseLookY;

    [FoldoutGroup("References")]
    public CameraBob cameraBob;
    
    [FoldoutGroup("References")]
    public HeadPosition headPosition;

    [FoldoutGroup("Inputs")]
    private float hTemp;

    [FoldoutGroup("Inputs")]
    private float vTemp;

    [FoldoutGroup("Inputs")]
    private float h;

    [FoldoutGroup("Inputs")]
    private float v;

    [FoldoutGroup("Inputs")]
    private Vector3 inputDir;

    [FoldoutGroup("Settings")]
    public bool enableMovement = true;

    [FoldoutGroup("Settings")]
    public bool isNonPhysics;

    [FoldoutGroup("Physics Based Movements")]
    public float dynamicSpeed = 1f;

    [FoldoutGroup("Physics Based Movements")]
    public float dynamicSpeedSprint = 1f;

    [FoldoutGroup("Physics Based Movements")]
    public Vector3 vel;

    [FoldoutGroup("Physics Based Movements")]
    private Vector3 gVel;

    [FoldoutGroup("Physics Based Movements")]
    private Vector3 gDir;

    [FoldoutGroup("Physics Based Movements")]
    private Vector3 gDirCross;

    [FoldoutGroup("Physics Based Movements")]
    private Vector3 gDirCrossProject;

    [FoldoutGroup("Physics Based Movements")]
    private RaycastHit hit;

    [FoldoutGroup("Physics Based Movements")]
    private float airControl = 1f;

    [FoldoutGroup("Physics Based Movements")]
    private float airControlBlockTimer;

    [FoldoutGroup("Physics Based Movements")]
    public WaterObject waterObject;

    [FoldoutGroup("Physics Based Movements")]
    public bool enableJump = true;

    [FoldoutGroup("Physics Based Movements")]
    public Vector3 jumpForce = new Vector3(0f, 15f, 0f);

    [FoldoutGroup("Physics Based Movements")]
    public float ungroundedJumpGraceTimer;

    [FoldoutGroup("Physics Based Movements")]
    public float gravity = -40f;

    [FoldoutGroup("Physics Based Movements")]
    private int climbState;

    [FoldoutGroup("Physics Based Movements")]
    private float climbTimer;

    [FoldoutGroup("Physics Based Movements")]
    private Vector3 climbStartPos;

    [FoldoutGroup("Physics Based Movements")]
    private Vector3 climbStartDir;

    [FoldoutGroup("Physics Based Movements")]
    private Vector3 climbTargetPos;

    [FoldoutGroup("Physics Based Movements")]
    public AnimationCurve climbCurve;

    [FoldoutGroup("Kinematic Movements")]
    public float acceleration;

    [FoldoutGroup("Kinematic Movements")]
    public float deaccerlation;

    [FoldoutGroup("Kinematic Movements")]
    public float distance;

    [FoldoutGroup("Kinematic Movements")]
    public float radius;

    [FoldoutGroup("Kinematic Movements")]
    public float collisionCoefficient;

    [FoldoutGroup("Kinematic Movements")]
    public LayerMask nonPhysicsCollisions;

    private float damageTimer;

    [FoldoutGroup("Interaction")]
    public bool enableInteraction = true;
    
    [FoldoutGroup("Interaction")]
    public Interactable targetInteractable;
    
    [FoldoutGroup("Interaction")]
    public Interactable exclusiveInteractable;
    
    [FoldoutGroup("Interaction")]
    public float interactDistance = 5;
    
    [FoldoutGroup("Interaction")]
    public LayerMask interactableLayer;

    [FoldoutGroup("Health")] 
    public float maxHp = 100f;

    [FoldoutGroup("Health")] 
    public NetworkVariable<float> currentHp = new NetworkVariable<float>(100, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);


    private void Awake()
    {
        instance = this;
        awaitInitialization = true;

        headTransform = transform.Find("Head Pivot").transform;
        rb = GetComponent<Rigidbody>();
        playerCollider = GetComponent<CapsuleCollider>();
        grounder = GetComponent<Grounder>();
        cameraBob = headTransform.GetComponentInChildren<CameraBob>();
        headPosition = headTransform.GetComponentInChildren<HeadPosition>();
        waterObject = GetComponentInChildren<WaterObject>();
        mouseLookX = GetComponent<MouseLook>();
        mouseLookY = headTransform.GetComponent<MouseLook>();

        playerUsername = $"Player #{localPlayerId}";
        playerUsernameText.text = playerUsername;

    }

    private void Update()
    {

        if (base.IsOwner && controlledByClient)
        {
            if (awaitInitialization)
            {
                ConnectClientToPlayerObject();
                awaitInitialization = false;
            }

            InputUpdate();

            if(mouseLookX.enabled)
                mouseLookX.UpdateCameraRotation();
            if (mouseLookY.enabled)
                mouseLookY.UpdateCameraRotation();

            //WalkSoundUpdate();
            BobUpdate();
            headPosition.PositionUpdate();

            InteractionUpdate();

            if (climbState > 0)
            {
                ClimbingUpdate();
            }

            //counts down the timer that restricts air control 
            if (airControlBlockTimer > 0f)
            {
                airControlBlockTimer -= Time.deltaTime;
                airControl = 0f;
            }

            //sets air control back to 1 over time
            else if (airControl != 1f)
            {
                airControl = Mathf.MoveTowards(airControl, 1f, Time.deltaTime);
            }

            if (ungroundedJumpGraceTimer > 0f)
            {
                ungroundedJumpGraceTimer -= Time.deltaTime;
            }
        }
        else
        {
            if (!awaitInitialization)
            {
                awaitInitialization = true;
                DisconnectClientFromPlayerObject();
            }
        }
    }

    private void FixedUpdate()
    {
        if (base.IsOwner)
        {
            MovementUpdate();
        }
    }

    private void LateUpdate() 
    {
        //Rotate Username Canvas facing local player's camera
        if (!base.IsOwner && GameSessionManager.Instance.localPlayerController != null)
        {
            playerUsernameCanvasTransform.LookAt(GameSessionManager.Instance.localPlayerController.cameraList[0].transform);
        }
    }

    public void ConnectClientToPlayerObject()
    {
        localClientId = NetworkManager.Singleton.LocalClientId;

        if (GameSessionManager.Instance != null)
        {
            GameSessionManager.Instance.localPlayerController = this;
        }

        if (!GameNetworkManager.Instance.steamDisabled)
        {
            GameNetworkManager.Instance.localSteamClientUsername = SteamClient.Name.ToString();
            playerUsername = GameNetworkManager.Instance.localSteamClientUsername;
            UpdatePlayerSteamIdServerRpc(SteamClient.SteamId);
        }

        //GameSessionManager.Instance.spectateCamera.enabled = false;

        cameraList = GetComponentsInChildren<Camera>(true).ToList<Camera>();
        cameraList[0].tag = "MainCamera";
        EnviroManager.instance.Camera = cameraList[0];
        foreach (Camera cam in cameraList)
        {
            cam.enabled = true;
        }

        GameSessionManager.Instance.audioListener = GetComponentInChildren<AudioListener>(true);
        GameSessionManager.Instance.audioListener.enabled = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        currentHp.Value = maxHp;
    }

    public void DisconnectClientFromPlayerObject()
    {
        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = true;
    }

    [ServerRpc]
    private void UpdatePlayerSteamIdServerRpc(ulong steamId)
    {
        if (!GameNetworkManager.Instance.steamDisabled && GameNetworkManager.Instance.currentSteamLobby.HasValue)
        {
            if (!GameNetworkManager.Instance.steamIdsInCurrentSteamLobby.Contains(steamId))
            {
                NetworkManager.Singleton.DisconnectClient(localPlayerId);
                return;
            }
        }

        List<ulong> steamIdList = new List<ulong>();
        for (int i = 0; i < 4; i++)
        {
            if (i == (int)localPlayerId)
            {
                steamIdList.Add(steamId);
            }
            else
            {
                steamIdList.Add(GameSessionManager.Instance.playerControllerList[i].localSteamId);
            }
        }

        UpdatePlayerSteamIdClientRpc(steamIdList.ToArray());

    }

    [ClientRpc]
    private void UpdatePlayerSteamIdClientRpc(ulong[] steamIdList)
    {
        for (int i = 0; i < steamIdList.Length; i++)
        {
            if (GameSessionManager.Instance.playerControllerList[i].controlledByClient) // || GameSessionManager.Instance.playerControllerList[i].isPlayerSpectating)
            {
                GameSessionManager.Instance.playerControllerList[i].localSteamId = steamIdList[i];

                string playerName = new Friend(steamIdList[i]).Name;
                GameSessionManager.Instance.playerControllerList[i].playerUsername = playerName;
                GameSessionManager.Instance.playerControllerList[i].playerUsernameText.text = playerName;

            }
        }

    }

    [Button]
    public void TeleportPlayer(Vector3 targetPosition)
    {
        // bool flag = rb.isKinematic;
        // rb.isKinematic = true;
        // playerServerPosition = targetPosition;
        // base.transform.position = targetPosition;
        // rb.isKinematic = flag;
        rb.position = targetPosition;
    }

    public int GetClimbState()
    {
        return climbState;
    }

    private void JumpOrClimb()
    {
        //if is climbing, return
        if (climbState != 0)
        {
            return;
        }

        //if grounded, or just ungrouned, or just finished climbing
        //jump
        if (grounder.grounded
            || ungroundedJumpGraceTimer > 0f
            || (climbState == 2 && climbTimer > 0.8f)
            || GetComponentInChildren<WaterObject>().IsTouchingWater())
        {
            if (climbState == 2)
            {
                rb.isKinematic = false;
                climbState = 0;
            }
            Jump();
            return;
        }

        //else check if player can climb
        else
        {
            Climb();
        }
    }

    public void Jump(float multiplier = 1f)
    {
        if (isNonPhysics)
        {
            //return;
        }
        if (!enableJump)
        {
            return;
        }

        //if jumping on top of props, push props away
        // if ((bool)grounder.groundCollider && grounder.groundCollider.gameObject.layer == 14)
        // {
        //     Rigidbody attachedRigidbody = grounder.groundCollider.attachedRigidbody;
        //     if ((bool)attachedRigidbody)
        //     {
        //         attachedRigidbody.AddForce(Vector3.up * (7f * attachedRigidbody.mass), ForceMode.Impulse);
        //         attachedRigidbody.AddTorque(tHead.forward * 90f, ForceMode.Impulse);
        //     }
        // }

        //ungrounds and jumps
        grounder.Unground();
        ungroundedJumpGraceTimer = 0f;
        rb.velocity = new Vector3(0, 0, 0);
        rb.AddForce(jumpForce * multiplier, ForceMode.Impulse);
        //playerAudio.PlayJumpSound();
    }

    private void Climb()
    {   //if climbing, or no surface to climb up to, or surface too low, or obsticle on top of landing spot, too close to ground
        //no climbing
        if (climbState > 0
            || !Physics.Raycast(transform.position + Vector3.up * 3f + headTransform.forward * 1f, Vector3.down, out hit, 4f, 1)
            || !(hit.point.y + 1f > transform.position.y)
            || Physics.Raycast(new Vector3(transform.position.x, hit.point.y + 1f, transform.position.z), headTransform.forward.normalized, 2f, 1)
            || Physics.Raycast(transform.position, Vector3.down, 1.5f, 1)
            || Physics.Raycast(transform.position, Vector3.up, 2.5f, 1))
        {
            return;
        }

        //else sets target position and start climbing
        climbTargetPos = hit.point + hit.normal;
        climbState = 3;
    }

    private void ClimbingUpdate()
    {
        switch (climbState)
        {
            //sets player rb to kinematic to directly modify position
            case 3:
                rb.isKinematic = true;
                rb.velocity = Vector3.zero;
                climbTimer = 0f;
                climbStartPos = rb.position;
                climbStartDir = climbStartPos;
                climbStartDir.y += 2f;
                cameraBob.Sway(new Vector4(10f, 0f, -5f, 2f));

                //poofVFX.transform.position = climbTargetPos;
                //ParticleSystem particle = poofVFX.GetComponent<ParticleSystem>();
                //particle.Play();

                climbState--;
                break;

            //lerps from start position to target position based on curve value at current time
            //finishes climbing when timer ends
            case 2:
                cameraBob.Angle(Mathf.Sin(climbTimer * (float)Mathf.PI * 5f));
                climbTimer = Mathf.MoveTowards(climbTimer, 1f, Time.deltaTime * 3f);
                transform.position = Vector3.LerpUnclamped(climbStartPos, climbTargetPos, climbCurve.Evaluate(climbTimer));
                if (climbTimer == 1f)
                {
                    climbState--;
                }
                break;

            //sets player rb back to not kinematic
            case 1:
                rb.isKinematic = false;
                climbState--;
                break;
        }
    }


    private void InputUpdate()
    {
        vTemp = 0f;
        vTemp += Input.GetKey(KeyCode.W) ? 1 : 0;
        vTemp += Input.GetKey(KeyCode.S) ? (-1) : 0;
        hTemp = 0f;
        hTemp += Input.GetKey(KeyCode.A) ? (-1) : 0;
        hTemp += Input.GetKey(KeyCode.D) ? 1 : 0;
        v = vTemp;
        h = hTemp;

        if (enableMovement)
        {
            inputDir.x = h;
            inputDir.y = 0f;
            inputDir.z = v;
            inputDir = inputDir.normalized;

            if (Input.GetKeyDown(KeyCode.Space))
            {
                JumpOrClimb();
            }

            if (inputDir != Vector3.zero)
            {
                if (Input.GetKeyDown(KeyCode.LeftShift))
                {
                    if (dynamicSpeed == 1.5f)
                    {
                        dynamicSpeed = 2.5f;
                    }
                    else if (dynamicSpeed == 2.5f)
                    {
                        dynamicSpeed = 1.5f;
                    }
                }
            }
            else
            {
                dynamicSpeed = 2.5f;
            }
        }
        else
        {
            inputDir = Vector3.zero;
        }

    }

    // public void WalkSoundUpdate()
    // {
    //     if (grounder.grounded && (inputDir != Vector3.zero))
    //     {
    //         StartCoroutine(playerAudio.PlayWalkSound());
    //     }
    //     else
    //     {
    //         StartCoroutine(playerAudio.StopWalkSound());
    //     }
    // }

    private void BobUpdate()
    {

        //tilts camera based on horizontal input
        if (climbState == 0)
        {

            cameraBob.Angle(inputDir.x * -1f - damageTimer * 3f);
        }

        //applies camera bob when grounded, walking, and not sliding
        //or sets camera position back to 0
        if (grounder.grounded && inputDir.sqrMagnitude > 0.25f)
        {
            if (gVel.sqrMagnitude > 1f)
            {
                cameraBob.Bob(dynamicSpeed);
            }
            else
            {
                cameraBob.Reset();
            }
        }
        else
        {
            cameraBob.Reset();
        }

    }

    public void MovementUpdate()
    {
        //recalculates the previous velocity based on new ground normals
        if (!isNonPhysics)
        {
            vel = rb.velocity;
        }

        gVel = Vector3.ProjectOnPlane(vel, grounder.groundNormal);

        //recalculates direction based on new ground normals
        gDir = headTransform.TransformDirection(inputDir);
        gDirCross = Vector3.Cross(Vector3.up, gDir).normalized;
        gDirCrossProject = Vector3.ProjectOnPlane(grounder.groundNormal, gDirCross);
        gDir = Vector3.Cross(gDirCross, gDirCrossProject);

        if (!isNonPhysics)
        {
            //if moving fast, apply the calculated movement.
            //based on new input subtracted by previous velocity
            //so that player accelerates faster when start moving.
            if (inputDir.sqrMagnitude > 0.25f)
            {
                if (grounder.grounded)
                {
                    rb.AddForce(gDir * 100f - gVel * 10f * dynamicSpeed);
                }
                else if (airControl > 0f)
                {
                    rb.AddForce((gDir * 100f - gVel * 10f * dynamicSpeed) * airControl);
                }
            }
            //if not fast, accelerates the slowing down process
            else if (grounder.grounded && gVel.sqrMagnitude != 0f)
            {
                rb.AddForce(-gVel * 10f);
            }

            //applies gravity in the direction of ground normal
            //so player does not slide off within the tolerable angle
            rb.AddForce(grounder.groundNormal * gravity);

        }
        // else if (isNonPhysics)
        // {
        //     Vector3 normalized = transform.TransformDirection(inputDir).normalized;
        //     normalized -= Vector3.Dot(normalized, base.transform.up) * base.transform.up;

        //     localVelo += BoatController.instance.transform.InverseTransformDirection(normalized) * acceleration * Time.fixedDeltaTime;
        //     localVelo = Vector3.Lerp(localVelo, Vector3.zero, Time.fixedDeltaTime * deaccerlation);
        //     if (localVelo != Vector3.zero)
        //     {
        //         Vector3 direction = BoatController.instance.transform.TransformDirection(localVelo);
        //         if (Physics.SphereCast(base.transform.position + base.transform.up * 0.5f, radius, direction, out RaycastHit hitInfo, distance, nonPhysicsCollisions))
        //         {
        //             Vector3 vector = BoatController.instance.transform.InverseTransformDirection(hitInfo.normal);
        //             vector.y = 0f;
        //             localVelo += vector.normalized * (1f / Mathf.Max(0.1f, hitInfo.distance)) * collisionCoefficient *
        //                          Time.fixedDeltaTime;
        //         }

        //         if (Physics.SphereCast(base.transform.position + base.transform.up * (-0.5f), radius, direction, out RaycastHit hitInfo2, distance, nonPhysicsCollisions))
        //         {
        //             Vector3 vector = BoatController.instance.transform.InverseTransformDirection(hitInfo2.normal);
        //             vector.y = 0f;
        //             localVelo += vector.normalized * (1f / Mathf.Max(0.1f, hitInfo2.distance)) * collisionCoefficient *
        //                          Time.fixedDeltaTime;
        //         }

        //         localVelo = new Vector3(localVelo.x, 0, localVelo.z);
        //         transform.localPosition += localVelo * Time.fixedDeltaTime;

        //     }
        // }
    }
    //Executes when taken damage from a source.
    /*
	public void Damage(Damage damage)
	{
		slamVFX.transform.position = transform.position;
		slamVFX.transform.rotation = Quaternion.LookRotation(transform.forward);
		slamVFX.GetComponent<ParticleSystem>().Play();

		//When blocking, knocks off the current weapon.
		if (weapons.IsBlocking())
		{
			weapons.weapons[weapons.currentWeapon].Block();
			rb.AddForce(damage.dir * 20f, ForceMode.Impulse);
			bob.Sway(new Vector4(-20f, 20f, 0f, 5f));
		}
		//if player hasnt taken damage in 3 seconds, knocks player back and upwwards.
		//and next attck in 3 seconds will kill player.
		else if (damageTimer <= 0f && damage.amount < 100f)
		{
			if (grounder.grounded)
			{
				grounder.Unground();
				airControlBlockTimer = 0.2f;
				rb.velocity = Vector3.zero;
				rb.AddForce((Vector3.up + (Vector3)damage.dir).normalized * 10f, ForceMode.Impulse);
			}
			bob.Sway(new Vector4(5f, 0f, 30f, 3f));
			damageTimer = 3f;
			//QuickEffectsPool.Get("Damage", tHead.position, Quaternion.LookRotation(tHead.forward)).Play();
		}
		//else kill player.
		else
		{
			Die(damage.dir);
			TimeManager.instance.SlowMotion(0.1f, 1f, 0.2f);
		}
	}
	*/

    public void InteractionUpdate()
    {
        if (Physics.Raycast(cameraList[0].ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f)), out RaycastHit hitInfo, interactDistance) && (interactableLayer.value & 1 << hitInfo.collider.gameObject.layer) > 0)
        {
            if (targetInteractable == null || targetInteractable != hitInfo.collider.GetComponent<Interactable>()) // || targetInteractable.triggerZone)
            {
                if (targetInteractable != null && targetInteractable != hitInfo.collider.GetComponent<Interactable>())
                {
                    targetInteractable.UnTarget();
                }

                targetInteractable = hitInfo.collider.GetComponent<Interactable>();
                if (targetInteractable != null)
                {
                    if (exclusiveInteractable != null && exclusiveInteractable != targetInteractable)
                    {
                        targetInteractable.UnTarget();
                        targetInteractable = null;
                        return;
                    }
                    // if (targetInteractable.triggerZone != null && !targetInteractable.triggerZone.triggered)
                    // {
                    //     targetInteractable.UnTarget();
                    //     targetInteractable = null;
                    //     return;
                    // }
                    targetInteractable.Target();
                }
            }
        }
        else if (targetInteractable != null)
        {
            targetInteractable.UnTarget();
            targetInteractable = null;
        }
        
        if (enableMovement && Input.GetKeyDown(KeyCode.E) && targetInteractable != null)
        {
            targetInteractable.Interact();
        }
    }

    public void LockMovement(bool state)
    {
        enableMovement = !state;
    }

    public void LockCamera(bool state)
    {
        mouseLookX.enabled = !state;
        mouseLookY.enabled = !state;
    }

    public void SetCameraClamp(float x1, float x2, float y1, float y2)
    {
        GetComponent<MouseLook>().SetClamp(x1, x2, y1, y2);

        foreach (MouseLook look in GetComponentsInChildren<MouseLook>())
        {
            look.SetClamp(x1, x2, y1, y2);
        }
    }

    public void AttachToBoat(Transform playerParent)
    {
        // transform.SetParent(playerParent.gameObject.transform, true);
        // isNonPhysics = true;
        // waterObject.enabled = false;
        // Destroy(PlayerController.instance.rb);

        // Vector3 temp = PlayerController.instance.transform.localPosition;
        // //PlayerController.instance.transform.localPosition = new Vector3(temp.x, playerHeight.localPosition.y, temp.z);

        // PlayerController.instance.transform.localEulerAngles = new Vector3(0, PlayerController.instance.transform.localEulerAngles.y, 0);
        // //GetComponentInChildren<PlayerSway>().enabled = true;
        // GetComponentInChildren<PlayerSway>().lastRotation = transform.rotation;
        // GetComponent<MouseLook>().Reset();

        // UIManager.instance.boatUI.SetActive(true);
    }

    public void DetachFromBoat()
    {
        // if (rb == null)
        // {
        //     Rigidbody temp = transform.AddComponent<Rigidbody>();
        //     temp.isKinematic = false;
        //     temp.useGravity = false;
        //     temp.angularDrag = 0;
        //     temp.constraints = RigidbodyConstraints.FreezeRotation;

        //     rb = temp;
        //     grounder.rb = temp;
        //     waterObject.targetRigidbody = temp;

        //     gameObject.transform.SetParent(null, true);
        //     isNonPhysics = false;
        //     waterObject.enabled = true;

        //     //transform.eulerAngles = new Vector3(0, transform.eulerAngles.y, 0);
        //     PlayerController.instance.transform.localEulerAngles = new Vector3(0, PlayerController.instance.transform.localEulerAngles.y, 0);
        //     GetComponent<MouseLook>().Reset();
        //     //GetComponentInChildren<PlayerSway>().enabled = false;

        //     //BoatController.instance.helm.ShutDown();

        //     UIManager.instance.boatUI.SetActive(false);
        // }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.1f, 0.1f, 0.9f, 0.8f);
        Gizmos.DrawSphere(base.transform.position + base.transform.up * 0.5f, radius);
        Gizmos.DrawSphere(base.transform.position + base.transform.up * -0.5f, radius);
    }
    
    public void TakeDamage(float damage)
    {
        TakeDamageClientRpc(damage);
    }

    [ClientRpc]
    public void TakeDamageClientRpc(float damage)
    {
        if (IsOwner)
        {
            currentHp.Value -= damage;

            if (currentHp.Value <= 0)
            {
                Die();
            }

            Debug.Log(playerUsername + " took " + damage + " damage.");
        }
    }

    [Button]
    public void Die()
    {
        if (IsOwner && !isPlayerDead)
        {
            isPlayerDead = true;
            LockMovement(true);
            LockCamera(true);
            rb.constraints = RigidbodyConstraints.None;
            rb.AddTorque(base.transform.right);
            InventoryManager.instance.DropAllItemsFromInventory();
            DieServerRPC();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void DieServerRPC()
    {
        DieClientRpc();
    }

    [ClientRpc]
    public void DieClientRpc()
    {
        isPlayerDead = true;
    }

    [Button]
    public void Respawn()
    {
        if (IsOwner && isPlayerDead)
        {
            isPlayerDead = false;
            LockMovement(false);
            LockCamera(false);
            rb.constraints = RigidbodyConstraints.FreezeRotation;
            RespawnServerRPC();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void RespawnServerRPC()
    {
        RespawnClientRpc();
    }
    
    [ClientRpc]
    public void RespawnClientRpc()
    {
        isPlayerDead = false;
    }
}

