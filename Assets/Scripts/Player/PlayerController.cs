using System.Collections;
using System.Collections.Generic;
using NWH.DWP2.WaterObjects;
using UnityEngine;
using Unity.Netcode;
using System.Linq;
using MyBox;
using Steamworks;
using System.ComponentModel;
using System.Text.RegularExpressions;
using TMPro;
using Unity.Netcode.Components;


//Added Function Die(), Damage(),
//And postprocessing effect when taken damage.

public class PlayerController : NetworkBehaviour //, Damagable//, Slappable
{
    public static PlayerController instance;

    [Foldout("Networks", true)]

    public string playerUsername = "Player";

    public ulong localClientId;

    public ulong localSteamId;

    public ulong localPlayerIndex;

    public bool isPlayerControlled;

    public bool isHostPlayerObject;

    public bool onAssignPlayerControllerToClient;

    public Vector3 playerServerPosition;

    [Foldout("References", true)]
    
    public Transform playerUsernameCanvasTransform;
    public TMP_Text playerUsernameText;

    public List<Camera> cameraList = new List<Camera>();

    public Transform t;

    public Transform tHead;

    public Transform equippedTransform;

    public Transform equippedTransformLeft;

    public Transform equippedTransformRight;

    public Transform equippedTransformCenter;

    public Rigidbody rb;

    public CapsuleCollider playerCollider;

    //public PlayerDecapitate playerDecapitate;

    private Grounder grounder;

    //public PlayerSlide slide;

    //public PlayerDash dash;

    public MouseLook mouseLookX;
    public MouseLook mouseLookY;

    public CameraBob bob;
    public HeadPosition headPosition;

    // public PlayerAudio playerAudio;

    // public InventoryAudio inventoryAudio;

    [Foldout("Inputs", true)]

    private float hTemp;

    private float vTemp;

    private float h;

    private float v;

    private Vector3 inputDir;

    [Foldout("Dynamic Movements", true)]

    public float dynamicSpeed = 1f;

    public float dynamicSpeedSprint = 1f;

    public Vector3 vel;

    private Vector3 gVel;

    private Vector3 gDir;

    private Vector3 gDirCross;

    private Vector3 gDirCrossProject;

    private Vector3 localVelo;

    private RaycastHit hit;

    private float airControl = 1f;

    private float airControlBlockTimer;

    public WaterObject waterObject;

    [Foldout("Kinematic Movements", true)]

    public bool isNonPhysics;
    public float acceleration;
    public float deaccerlation;

    public float distance;
    public float radius;
    public float collisionCoefficient;

    public LayerMask nonPhysicsCollisions;

    [Foldout("Settings", true)]
    public bool enableMovement = true;

    public bool enableJump = true;

    public Vector3 jumpForce = new Vector3(0f, 15f, 0f);

    public float gTimer;

    public float gravity = -40f;

    private int climbState;

    private float climbTimer;

    private Vector3 climbStartPos;

    private Vector3 climbStartDir;

    private Vector3 climbTargetPos;

    public AnimationCurve climbCurve;

    public GameObject poofVFX;

    public GameObject slamVFX;

    public bool extraUpForce;

    private float damageTimer;


    [Foldout("Interaction", true)]
    public bool enableInteraction = true;
    public Interactable targetInteractable;
    public Interactable exclusiveInteractable;
    public float interactDistance = 5;
    public LayerMask interactableLayer;

    [Foldout("Mist", true)]
    public bool inMist = false;
    public float mistTimer;
    public float deathTime = 8;
    public float replenishCoeffcient = 2;


    /*	public PostProcessVolume volume;
        public Bloom bloom;
        public ChromaticAberration ca;
        public ColorGrading cg;
        public Vignette vg;*/


    private void Awake()
    {

        instance = this;
        onAssignPlayerControllerToClient = true;
        t = base.transform;
        tHead = t.Find("Head Pivot").transform;

        rb = GetComponent<Rigidbody>();
        playerCollider = GetComponent<CapsuleCollider>();
        grounder = GetComponent<Grounder>();

        bob = tHead.GetComponentInChildren<CameraBob>();
        headPosition = tHead.GetComponentInChildren<HeadPosition>();
        waterObject = GetComponentInChildren<WaterObject>();

        mouseLookX = GetComponent<MouseLook>();
        mouseLookY = tHead.GetComponent<MouseLook>();

        //slide = GetComponent<PlayerSlide>();
        //dash = GetComponent<PlayerDash>();
        //playerDecapitate = GetComponentInChildren<PlayerDecapitate>(true);
        //equippedTransform = tHead.Find("Equip Pivot").transform;

        /*
		volume = FindObjectOfType<PostProcessVolume>();
		volume.profile.TryGetSettings(out bloom);
		volume.profile.TryGetSettings(out ca);
		volume.profile.TryGetSettings(out cg);
		volume.profile.TryGetSettings(out vg);

		poofVFX = Instantiate(poofVFX, Vector3.zero, Quaternion.identity);
		slamVFX = Instantiate(slamVFX, Vector3.zero, Quaternion.identity);
		*/
    }

    private void Update()
    {
        isHostPlayerObject = this == GameSessionManager.Instance.playerControllerList[0];

        if (base.IsOwner && isPlayerControlled && (!base.IsServer || isHostPlayerObject))
        {
            if (onAssignPlayerControllerToClient)
            {
                //GameSessionManager.Instance.spectateCamera.enabled = false;

                cameraList = GetComponentsInChildren<Camera>(true).ToList<Camera>();
                foreach (Camera cam in cameraList)
                {
                    //cam.gameObject.SetActive(true);
                    cam.enabled = true;
                }

                Debug.Log("Taking control of player " + base.gameObject.name + " and enabling camera!");
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                ConnectClientToPlayerObject();
                onAssignPlayerControllerToClient = false;
            }
            InputUpdate();

            if(mouseLookX.enabled)
                mouseLookX.UpdateCameraRotation();
            if (mouseLookY.enabled)
                mouseLookY.UpdateCameraRotation();

            WalkSoundUpdate();
            BobUpdate();
            headPosition.PositionUpdate();

            HandleInteractableCheck();
            HandleInteraction();

            // if (UIManager.instance.gameplayUI.activeInHierarchy && enableInteraction)
            // {
            //     HandleInteractableCheck();
            //     HandleInteraction();
            // }

            if (climbState > 0)
            {
                ClimbingUpdate();
            }

            /*
            if (slide.slideState > 0)
            {
                slide.SlidingUpdate();
            }

            if (dash.state > 0)
            {
                dash.DashingUpdate();
            }
            */

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

            if (gTimer > 0f)
            {
                gTimer -= Time.deltaTime;
            }

            if (damageTimer != 0f)
            {
                damageTimer = Mathf.MoveTowards(damageTimer, 0f, Time.deltaTime);
                /*			bloom.intensity.value = Mathf.Lerp(0, 10, damageTimer/3);
                            ca.intensity.value = Mathf.Lerp(0, 1, damageTimer/3);
                            cg.mixerGreenOutRedIn.value = Mathf.Lerp(0, -100, damageTimer/3);
                            vg.intensity.value = Mathf.Lerp(0, 0.3f, damageTimer/3);*/
            }
        }
        else
        {
            if (!onAssignPlayerControllerToClient)
            {
                onAssignPlayerControllerToClient = true;
                cameraList = GetComponentsInChildren<Camera>(true).ToList<Camera>();
                foreach (Camera cam in cameraList)
                {
                    //cam.gameObject.SetActive(true);
                    cam.enabled = true;
                }
            }
        }
    }

    public void ConnectClientToPlayerObject()
    {
        localClientId = NetworkManager.Singleton.LocalClientId;
        Debug.Log("localClientId:   " + NetworkManager.Singleton.LocalClientId + "   " + localClientId);
        if (GameNetworkManager.Instance != null)
        {
            GameNetworkManager.Instance.localPlayerController = this;
        }
        if (GameSessionManager.Instance != null)
        {
            GameSessionManager.Instance.localPlayerController = this;
        }

        foreach(PlayerController playerController in GameSessionManager.Instance.playerControllerList)
        {
            if (!playerController.isPlayerControlled)
            {
                //Teleport playerController to despawn position.
            }
            if (playerController != GameSessionManager.Instance.localPlayerController)
            {
                //Add playerController to nonlocal client list.
            }
        }

        if (!GameNetworkManager.Instance.disableSteam)
        {
            playerUsername = GameNetworkManager.Instance.username;
            SendNewPlayerValuesServerRpc(SteamClient.SteamId);
        }
    }

    [ServerRpc]
    private void SendNewPlayerValuesServerRpc(ulong steamId)
    {
        if (!GameNetworkManager.Instance.disableSteam && GameNetworkManager.Instance.currentSteamLobby.HasValue)
        {
            if (!GameNetworkManager.Instance.steamIdsInCurrentSteamLobby.Contains(steamId))
            {
                NetworkManager.Singleton.DisconnectClient(localClientId);
                return;
            }
        }

        List<ulong> steamIdList = new List<ulong>();
        for (int i = 0; i < 4; i++)
        {
            if (i == (int)localPlayerIndex)
            {
                steamIdList.Add(steamId);
            }
            else
            {
                steamIdList.Add(GameSessionManager.Instance.playerControllerList[i].localSteamId);
            }
        }
        SendNewPlayerValuesClientRpc(steamIdList.ToArray());
    }

    [ClientRpc]
    private void SendNewPlayerValuesClientRpc(ulong[] steamIdList)
    {
        NetworkManager networkManager = base.NetworkManager;
        if ((object)networkManager == null || !networkManager.IsListening)
        {
            return;
        }

        for (int i = 0; i < steamIdList.Length; i++)
        {
            if (GameSessionManager.Instance.playerControllerList[i].isPlayerControlled) // || GameSessionManager.Instance.playerControllerList[i].isPlayerSpectating)
            {
                GameSessionManager.Instance.playerControllerList[i].localSteamId = steamIdList[i];

                string playerName = new Friend(steamIdList[i]).Name;
                GameSessionManager.Instance.playerControllerList[i].playerUsername = playerName;
                GameSessionManager.Instance.playerControllerList[i].playerUsernameText.text = playerName;

            }
        }
        if (GameNetworkManager.Instance != null && GameNetworkManager.Instance.localPlayerController != null)
        {
            //UpdatePlayerPositionServerRpc. To be created.
            //UpdatePlayerPositionClientRpc. To be created.
        }
    }

    private void LateUpdate() 
    {
        if (NetworkManager.Singleton == null)
        {
            return;
        }
        if (!base.IsOwner && GameNetworkManager.Instance.localPlayerController != null)
        {
            playerUsernameCanvasTransform.LookAt(GameNetworkManager.Instance.localPlayerController.cameraList[0].transform);
        }
    }

    public void TeleportPlayer(Vector3 targetPosition)
    {
        playerServerPosition = targetPosition;
        base.transform.position = targetPosition;
    }

    public int GetClimbState()
    {
        return climbState;
    }

    //Turns on Postprocessing. 
    //Enables the camera that simulates the decapitated head.
    //Disables Player.
    public void Die(Vector3 dir)
    {
        /*		bloom.intensity.value = 10;
                ca.intensity.value = 10;
                cg.mixerGreenOutRedIn.value = -100;
                vg.intensity.value = 0.3f;*/

        //playerDecapitate.gameObject.SetActive(true);
        //playerDecapitate.Decapitate(tHead, dir);
        base.gameObject.SetActive(false);
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
            || gTimer > 0f
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

        //if not grounded, but there is prop or enemy below
        //super jump
        /*
		Collider[] array = new Collider[1];
		Physics.OverlapCapsuleNonAlloc(t.position, t.position + Vector3.down * 1.25f, 1f, array, 25600);
		if (array[0] != null)
		{
			switch (array[0].gameObject.layer)
			{
			case 10:
			if (array[0].attachedRigidbody.isKinematic)
				{
					Damage damage = new Damage();
					damage.amount = 10f;
					damage.dir = t.forward;
					array[0].GetComponent<Damagable>().Damage(damage);
				}
				else
				{
					array[0].GetComponent<Slappable>().Slap(Vector3.down);
				}
				Jump(1.6f);
				bob.Sway(new Vector4(Mathf.Clamp(vel.magnitude, 5f, 10f), 0f, 0f, 4f));
				
				slamVFX.transform.position = t.position;
				slamVFX.transform.rotation = Quaternion.LookRotation(Vector3.up);
				slamVFX.GetComponent<ParticleSystem>().Play();

				break;
			case 13:
				weapons.Drop(Vector3.up + tHead.forward);
				array[0].GetComponent<Weapon>().Interact(weapons);
				//QuickEffectsPool.Get("Enemy Jump", t.position, Quaternion.LookRotation(Vector3.up)).Play();
				//CameraController.shake.Shake(2);
				bob.Sway(new Vector4(Mathf.Clamp(vel.magnitude, 5f, 10f), 0f, 0f, 4f));
				Jump((rb.velocity.y > 1f) ? 1.75f : 1.5f);
				//midairActionPossible = true;
				break;
			case 14:
				if (!array[0].attachedRigidbody.isKinematic)
				{
					array[0].GetComponent<Slappable>().Slap(tHead.forward);
				}
				Jump((rb.velocity.y > 1f) ? 1.75f : 1.5f);
				bob.Sway(new Vector4(Mathf.Clamp(vel.magnitude, 5f, 10f), 0f, 0f, 4f));
				
				slamVFX.transform.position = t.position;
				slamVFX.transform.rotation = Quaternion.LookRotation(Vector3.up);
				slamVFX.GetComponent<ParticleSystem>().Play(); 	
				
				break;
			}
			array[0] = null;
		} 
		*/

        //if none, check if player can climb
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
        if ((bool)grounder.groundCollider && grounder.groundCollider.gameObject.layer == 14)
        {
            Rigidbody attachedRigidbody = grounder.groundCollider.attachedRigidbody;
            if ((bool)attachedRigidbody)
            {
                attachedRigidbody.AddForce(Vector3.up * (7f * attachedRigidbody.mass), ForceMode.Impulse);
                attachedRigidbody.AddTorque(tHead.forward * 90f, ForceMode.Impulse);
            }
        }

        //ungrounds and jumps
        if (isNonPhysics)
        {
            DetachFromBoat();
        }
        grounder.Unground();
        gTimer = 0f;
        rb.velocity = new Vector3(0, 0, 0);
        rb.AddForce(jumpForce * multiplier, ForceMode.Impulse);
        //AddForceServerRpc(jumpForce * multiplier);
        //playerAudio.PlayJumpSound();
    }

    private void Climb()
    {   //if climbing, or no surface to climb up to, or surface too low, or obsticle on top of landing spot, too close to ground
        //no climbing
        if (climbState > 0
            || !Physics.Raycast(t.position + Vector3.up * 3f + tHead.forward * 2f, Vector3.down, out hit, 4f, 1)
            || !(hit.point.y + 1f > t.position.y)
            || Physics.Raycast(new Vector3(t.position.x, hit.point.y + 1f, t.position.z), tHead.forward.normalized, 2f, 1)
            || Physics.Raycast(t.position, Vector3.down, 1.5f, 1)
            || Physics.Raycast(t.position, Vector3.up, 2.5f, 1))
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
                bob.Sway(new Vector4(10f, 0f, -5f, 2f));

                //poofVFX.transform.position = climbTargetPos;
                //ParticleSystem particle = poofVFX.GetComponent<ParticleSystem>();
                //particle.Play();

                climbState--;
                break;

            //lerps from start position to target position based on curve value at current time
            //finishes climbing when timer ends
            case 2:
                bob.Angle(Mathf.Sin(climbTimer * (float)Mathf.PI * 5f));
                climbTimer = Mathf.MoveTowards(climbTimer, 1f, Time.deltaTime * 3f);
                t.position = Vector3.LerpUnclamped(climbStartPos, climbTargetPos, climbCurve.Evaluate(climbTimer));
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
        vTemp += (Input.GetKey(KeyCode.W) ? 1 : 0);
        vTemp += (Input.GetKey(KeyCode.S) ? (-1) : 0);
        hTemp = 0f;
        hTemp += (Input.GetKey(KeyCode.A) ? (-1) : 0);
        hTemp += (Input.GetKey(KeyCode.D) ? 1 : 0);
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

            if (Input.GetKeyDown(KeyCode.LeftShift))
            {
                //slide.Slide();
            }
            if (Input.GetKeyDown(KeyCode.E))
            {
                //dash.Dash();
            }
        }
        else
        {
            inputDir = Vector3.zero;
        }

    }

    public void WalkSoundUpdate()
    {

        if (grounder.grounded && (inputDir != Vector3.zero))
        {
            //StartCoroutine(playerAudio.PlayWalkSound());
        }
        else
        {
            //StartCoroutine(playerAudio.StopWalkSound());
        }
    }

    private void BobUpdate()
    {

        //tilts camera based on horizontal input
        if (climbState == 0)
        //if (slide.slideState == 0 && climbState == 0)
        {

            bob.Angle(inputDir.x * -1f - damageTimer * 3f);
        }

        //applies camera bob when grounded, walking, and not sliding
        //or sets camera position back to 0
        if (grounder.grounded && inputDir.sqrMagnitude > 0.25f)
        //if (grounder.grounded && inputDir.sqrMagnitude > 0.25f && slide.slideState == 0)
        {
            if (gVel.sqrMagnitude > 1f)
            {
                bob.Bob(dynamicSpeed);
            }
            else
            {
                bob.Reset();
            }
        }
        else
        {
            bob.Reset();
        }

    }

    private void FixedUpdate()
    {
        
        if (base.IsOwner && isPlayerControlled && (!base.IsServer || isHostPlayerObject))
        {
            //recalculates the previous velocity based on new ground normals
            if (!isNonPhysics)
            {
                vel = rb.velocity;
            }

            gVel = Vector3.ProjectOnPlane(vel, grounder.groundNormal);

            //recalculates direction based on new ground normals
            //gDir = transform.TransformDirection(inputDir);
            gDir = tHead.TransformDirection(inputDir);
            gDirCross = Vector3.Cross(Vector3.up, gDir).normalized;
            gDirCrossProject = Vector3.ProjectOnPlane(grounder.groundNormal, gDirCross);
            gDir = Vector3.Cross(gDirCross, gDirCrossProject);

            //if (slide.slideState == 0)
            //{

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
                        //AddForceServerRpc(gDir * 100f - gVel * 10f * dynamicSpeed);
                    }
                    else if (airControl > 0f)
                    {
                        rb.AddForce((gDir * 100f - gVel * 10f * dynamicSpeed) * airControl);
                        //AddForceServerRpc((gDir * 100f - gVel * 10f * dynamicSpeed) * airControl);
                    }
                }
                //if not fast, accelerates the slowing down process
                else if (grounder.grounded && gVel.sqrMagnitude != 0f)
                {
                    rb.AddForce(-gVel * 10f);
                    //AddForceServerRpc(-gVel * 10f);
                }

                rb.AddForce(grounder.groundNormal * gravity);
                //AddForceServerRpc(grounder.groundNormal * gravity);

                // if (extraUpForce)
                // {
                //     rb.AddForce(Vector3.up * 12f);
                //     extraUpForce = false;
                // }
            }
            else if (isNonPhysics)
            {
                // Vector3 normalized = transform.TransformDirection(inputDir).normalized;
                // normalized -= Vector3.Dot(normalized, base.transform.up) * base.transform.up;

                // localVelo += BoatController.instance.transform.InverseTransformDirection(normalized) * acceleration * Time.fixedDeltaTime;
                // localVelo = Vector3.Lerp(localVelo, Vector3.zero, Time.fixedDeltaTime * deaccerlation);
                // if (localVelo != Vector3.zero)
                // {
                //     Vector3 direction = BoatController.instance.transform.TransformDirection(localVelo);
                //     if (Physics.SphereCast(base.transform.position + base.transform.up * 0.5f, radius, direction, out RaycastHit hitInfo, distance, nonPhysicsCollisions))
                //     {
                //         Vector3 vector = BoatController.instance.transform.InverseTransformDirection(hitInfo.normal);
                //         vector.y = 0f;
                //         localVelo += vector.normalized * (1f / Mathf.Max(0.1f, hitInfo.distance)) * collisionCoefficient *
                //                      Time.fixedDeltaTime;
                //     }

                //     if (Physics.SphereCast(base.transform.position + base.transform.up * (-0.5f), radius, direction, out RaycastHit hitInfo2, distance, nonPhysicsCollisions))
                //     {
                //         Vector3 vector = BoatController.instance.transform.InverseTransformDirection(hitInfo2.normal);
                //         vector.y = 0f;
                //         localVelo += vector.normalized * (1f / Mathf.Max(0.1f, hitInfo2.distance)) * collisionCoefficient *
                //                      Time.fixedDeltaTime;
                //     }

                //     localVelo = new Vector3(localVelo.x, 0, localVelo.z);
                //     transform.localPosition += localVelo * Time.fixedDeltaTime;

                // }
            }

            /*
            }
            else if (slide.slideState == 2)
            {
                //if sliding, modifies the direction according to horizontal inputs
                if (Mathf.Abs(h) > 0.1f)
                {
                    rb.AddForce(Vector3.Cross(slide.slideDir, grounder.groundNormal) * (15f * (0f - h)));
                }
                //slows down if player holds back
                if (v < -0.5f)
                {
                    rb.AddForce(-vel.normalized * 20f);
                }
            }
            */

            //applies gravity in the direction of ground normal
            //so player does not slide off within the tolerable angle
        }
    }

    [ServerRpc]
    public void AddForceServerRpc(Vector3 force){
        rb.AddForce(force);
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

    public void HandleInteractableCheck()
    {
        if (Physics.Raycast(cameraList[0].ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f)), out RaycastHit hitInfo, interactDistance, interactableLayer))// && hitInfo.collider.gameObject.layer == 7)
        {
            if (targetInteractable == null || targetInteractable.name != hitInfo.collider.name) // || targetInteractable.triggerZone)
            {
                if (targetInteractable != null && targetInteractable.name != hitInfo.collider.name)
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
    }

    private void HandleInteraction()
    {
        if (Input.GetKeyDown(KeyCode.E) && targetInteractable != null)
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

        /*foreach (MouseLook look in GetComponentsInChildren<MouseLook>())
        {
            look.enableLook = !state;
        }*/
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

}

