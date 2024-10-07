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
using Steamworks.Data;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Serialization;
using System.Threading.Tasks;
using RootMotion.FinalIK;
using Image = UnityEngine.UIElements.Image;


public class PlayerController : NetworkBehaviour, IDamagable
{
   // [FoldoutGroup("Inventory")]
   // public List<I_InventoryItem> inventoryItemList = new List<I_InventoryItem>();


   // [FoldoutGroup("Inventory")]
   // public List<I_InventoryItem> storageItemList = new List<I_InventoryItem>();
  
   
   [FoldoutGroup("Inventory")]
   public I_InventoryItem currentEquippedItem;


   [FoldoutGroup("Voice Chat")]
   public PlayerVoicePlaybackObject playerVoiceChatPlaybackObject;


   [FoldoutGroup("Voice Chat")]
   public VoicePlayerState voicePlayerState;


   [FoldoutGroup("Voice Chat")]
   public AudioSource playerVoiceChatAudioSource;


   [FoldoutGroup("Networks")] 
   public string playerUsername = "Player";

   
   [FoldoutGroup("Networks")]
   public int localPlayerId;


   [FoldoutGroup("Networks")]
   public NetworkVariable<ulong>  localClientId = new (writePerm: NetworkVariableWritePermission.Server);


   [FoldoutGroup("Networks")]
   public NetworkVariable<ulong> localSteamId = new (writePerm: NetworkVariableWritePermission.Owner);


   [FoldoutGroup("Networks")]
   public NetworkVariable<bool> controlledByClient = new (writePerm: NetworkVariableWritePermission.Server);


   [FoldoutGroup("Networks")]
   public Texture2D steamAvatar;


   [FoldoutGroup("Networks")]
   public bool playerControllerInitialized;


   [FoldoutGroup("References")]
   public Transform playerUsernameCanvasTransform;


   [FoldoutGroup("References")]
   public TMP_Text playerUsernameText;


   [FoldoutGroup("References")]
   public Camera gameplayCamera;


   [FoldoutGroup("References")]
   public Camera UICamera;


   [FoldoutGroup("References")]
   public Transform headTransform;


   [FoldoutGroup("References")]
   public Transform equippedTransform;


   [FoldoutGroup("References")]
   public Rigidbody rb;
  
   
   [FoldoutGroup("References")]
   public Animator animator;
  
   
   [FoldoutGroup("References")]
   public PlayerAnimationController playerAnimationController;


   [FoldoutGroup("References")]
   public CapsuleCollider playerCollider;


   [FoldoutGroup("References")]
   public Grounder grounder;


   [FoldoutGroup("References")]
   public MouseLook mouseLookX;


   [FoldoutGroup("References")]
   public MouseLook mouseLookY;


   [FoldoutGroup("References")]
   public CameraBob cameraBob;
  
   
   [FoldoutGroup("References")]
   public HeadPosition headPosition;
  
   
   [FoldoutGroup("References")]
   public List<SkinnedMeshRenderer> playerMeshRendererList = new List<SkinnedMeshRenderer>();


   [FoldoutGroup("References")]
   public WaterObject waterObject;


   [FoldoutGroup("Inputs")]
   public PlayerInputActions playerInputActions;


   [FoldoutGroup("Inputs")]
   public PlayerInput playerInput;


   [FoldoutGroup("Inputs")]
   private float hTemp;


   [FoldoutGroup("Inputs")]
   private float vTemp;


   [FoldoutGroup("Inputs")]
   private float h;


   [FoldoutGroup("Inputs")]
   private float v;


   [FoldoutGroup("Inputs")]
   public Vector3 inputDir;


   [FoldoutGroup("Inputs")]
   public Vector2 mouseInput;
  
   
   [FoldoutGroup("Inputs")]
   public NetworkVariable<float> inputDirX = new (writePerm: NetworkVariableWritePermission.Owner);
  
   
   [FoldoutGroup("Inputs")]
   public NetworkVariable<float> inputDirZ = new (writePerm: NetworkVariableWritePermission.Owner);


   [FoldoutGroup("Settings")]
   public NetworkVariable<bool> isPlayerDead = new (writePerm: NetworkVariableWritePermission.Owner);
  
   
   [FoldoutGroup("Settings")]
   public NetworkVariable<bool> isPlayerExtracted = new (writePerm: NetworkVariableWritePermission.Owner);

   
   [FoldoutGroup("Settings")] 
   public NetworkVariable<bool> isPlayerGrabbed = new NetworkVariable<bool>(false);


   [FoldoutGroup("Settings")]
   public bool enableMovement = true;


   [FoldoutGroup("Settings")]
   public bool enableLook = true;


   [FoldoutGroup("Settings")]
   public bool enableJump = true;


   [FoldoutGroup("Settings")]
   public bool isNonPhysics;


   [FoldoutGroup("Physics Based Movements")]
   public float dynamicTargetSpeed = 1f;


   [FoldoutGroup("Physics Based Movements")]
   public float dynamicWalkSpeed = 1.5f;


   [FoldoutGroup("Physics Based Movements")]
   public float dynamicSprintSpeed = 2.5f;
  
   
   [FoldoutGroup("Physics Based Movements")]
   public bool sprinting;
  
   
   [FoldoutGroup("Physics Based Movements")]
   public NetworkVariable<bool> sprintingNetworkVariable = new (writePerm: NetworkVariableWritePermission.Owner);
  
   
   [FoldoutGroup("Physics Based Movements")]
   public bool crouching;
  
   
   [FoldoutGroup("Physics Based Movements")]
   public NetworkVariable<bool> crouchingNetworkVariable = new (writePerm: NetworkVariableWritePermission.Owner);


   [FoldoutGroup("Physics Based Movements")]
   public Vector3 vel;


   [FoldoutGroup("Physics Based Movements")]
   public Vector3 gVel;


   [FoldoutGroup("Physics Based Movements")]
   private Vector3 gDir;


   [FoldoutGroup("Physics Based Movements")]
   private Vector3 gDirCross;


   [FoldoutGroup("Physics Based Movements")]
   private Vector3 gDirCrossProject;


   [FoldoutGroup("Physics Based Movements")]
   private RaycastHit hit;


   [FoldoutGroup("Physics Based Movements")]
   public float groundMovementControl = 1f;


   [FoldoutGroup("Physics Based Movements")]
   public float groundMovementControlCoolDown = 0f;


   [FoldoutGroup("Physics Based Movements")]
   public float airMovementControl = 0.5f;


   [FoldoutGroup("Physics Based Movements")]
   public float airMovementControlTarget = 0.5f;


   [FoldoutGroup("Physics Based Movements")]
   public Vector3 jumpForce = new Vector3(0f, 15f, 0f);


   [FoldoutGroup("Physics Based Movements")]
   public float jumpCooldown;
  
   
   [FoldoutGroup("Physics Based Movements")]
   public float jumpCooldownSetting = 0.5f;


   [FoldoutGroup("Physics Based Movements")]
   public float gravity = -40f;


   [FoldoutGroup("Physics Based Movements")]
   public int climbState;


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


   /*[FoldoutGroup("Kinematic Movements")]
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
   public LayerMask nonPhysicsCollisions;*/
  
   
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
   public NetworkVariable<float> health = new (100, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);


   [FoldoutGroup("Health")] public float stamina = 100;


   [FoldoutGroup("Health")]
   public float staminaReplenishPerSecond = 25f;


   [FoldoutGroup("Health")]
   public float staminaReplenishCooldown;


   [FoldoutGroup("Health")]
   public float staminaCostPerJump;


   [FoldoutGroup("Health")]
   public float staminaCostSprintPerSecond;


   [FoldoutGroup("Health")]
   public GameObject ragdollPrefab;
  
   
   [FoldoutGroup("Emote")]
   public NetworkVariable<int> currentEmoteIndex =  new(-1, writePerm: NetworkVariableWritePermission.Owner);


   [FoldoutGroup("Emote")]
   public NetworkVariable<bool> inSpecialAnimation =  new(writePerm: NetworkVariableWritePermission.Owner);
   
   [FoldoutGroup("Emote")]
   public List<EmoteData> emoteDataList = new List<EmoteData>();

   
   [FoldoutGroup("Emote")] 
   public AudioClip emote1Sound;

   
   [FoldoutGroup("Effects")]
   public float damageTimer;
  
   
   [FoldoutGroup("Effects")]
   public float drunkTimer;
  
   
   [FoldoutGroup("Currency")]
   public int baseCurrencyBalance;
  
   
   [FoldoutGroup("Model")]
   public NetworkVariable<int> currentCharacterModelIndex = new (writePerm: NetworkVariableWritePermission.Owner);
   
   

   private void Awake()
   {
       playerInputActions = new PlayerInputActions();
   }

   
   public void Start()
   {
       playerUsername = $"Player #{localPlayerId}";
       playerUsernameText.text = playerUsername;
   }


   public override void OnNetworkSpawn()
   {
       base.OnNetworkSpawn();
      
       OnIsPlayerDeadChanged(false, isPlayerDead.Value);
       OnCurrentCharacterModelIndexChanged(0, currentCharacterModelIndex.Value);
       
       isPlayerDead.OnValueChanged += OnIsPlayerDeadChanged;
       currentCharacterModelIndex.OnValueChanged += OnCurrentCharacterModelIndexChanged;
   }


   public override void OnNetworkDespawn()
   {
       base.OnNetworkDespawn();
      
       isPlayerDead.OnValueChanged -= OnIsPlayerDeadChanged;
       currentCharacterModelIndex.OnValueChanged -= OnCurrentCharacterModelIndexChanged;
   }


   private void OnEnable()
   {
       playerInputActions.Enable();
       
       playerInputActions.FindAction("Sprint").performed += Sprint_performed;
       playerInputActions.FindAction("Jump").performed += Jump_performed;
       playerInputActions.FindAction("Crouch").performed += Crouch_performed;
       playerInputActions.FindAction("ActivateItem").performed += ActivateItem_performed;
       playerInputActions.FindAction("ActivateItem").canceled += ActivateItem_canceled;
       // playerInputActions.FindAction("ItemSecondaryUse").performed += ItemSecondaryUse_performed;
       playerInputActions.FindAction("Interact").performed += Interact_performed;
       playerInputActions.FindAction("Interact").canceled += Interact_canceled;
       playerInputActions.FindAction("Discard").performed += Discard_performed;
       playerInputActions.FindAction("SwitchItem").performed += SwitchItem_performed;
       playerInputActions.FindAction("Emote1").performed += Emote1_performed;
       playerInputActions.FindAction("Emote2").performed += Emote2_performed;
       //playerInputActions.FindAction("Emote3").performed += Emote3_performed;
       //playerInputActions.FindAction("Emote4").performed += Emote4_performed;
       playerInputActions.FindAction("Inventory").performed += Inventory_performed;
       playerInputActions.FindAction("ScoreBoard").performed += ScoreBoard_performed;
       playerInputActions.FindAction("ScoreBoard").canceled += ScoreBoard_canceled;
       playerInputActions.FindAction("Pause").performed += Pause_performed;
   }


   private void OnDisable()
   {
       playerInputActions.Disable();
       
       playerInputActions.FindAction("Sprint").performed -= Sprint_performed;
       playerInputActions.FindAction("Jump").performed -= Jump_performed;
       playerInputActions.FindAction("Crouch").performed -= Crouch_performed;
       playerInputActions.FindAction("ActivateItem").performed -= ActivateItem_performed;
       playerInputActions.FindAction("ActivateItem").canceled -= ActivateItem_canceled;
       // playerInputActions.FindAction("ItemSecondaryUse").performed -= ItemSecondaryUse_performed;
       playerInputActions.FindAction("Interact").performed -= Interact_performed;
       playerInputActions.FindAction("Interact").canceled -= Interact_canceled;
       playerInputActions.FindAction("Discard").performed -= Discard_performed;
       playerInputActions.FindAction("SwitchItem").performed -= SwitchItem_performed;
       playerInputActions.FindAction("Emote1").performed -= Emote1_performed;
       playerInputActions.FindAction("Emote2").performed -= Emote2_performed;
       //playerInputActions.FindAction("Emote3").performed -= Emote3_performed;
       //playerInputActions.FindAction("Emote4").performed -= Emote4_performed;
       playerInputActions.FindAction("Inventory").performed -= Inventory_performed;
       playerInputActions.FindAction("ScoreBoard").performed -= ScoreBoard_performed;
       playerInputActions.FindAction("ScoreBoard").canceled -= ScoreBoard_canceled;
       playerInputActions.FindAction("Pause").performed -= Pause_performed;
   }

   
   private void Update()
   {
       if (IsOwner && controlledByClient.Value)
       {
           if (!playerControllerInitialized)
           {
               ConnectClientToPlayerObject();
           }

           InputUpdate();
           LookUpdate();
           InteractionUpdate();
          
           cameraBob.BobUpdate();
           headPosition.PositionUpdate();
          
           ClimbingUpdate();
           StaminaUpdate();


           EffectUpdate();


       }
       /*else
       {
           if (playerControllerInitialized)
           {
               DisconnectClientFromPlayerObject();
           }
       }*/
   }


   private void FixedUpdate()
   {
       if (base.IsOwner && controlledByClient.Value)
       {
           MovementUpdate();
       }
   }


   private void LateUpdate()
   {
       //Rotate Username Canvas facing local player's camera
       if (!base.IsOwner && GameSessionManager.Instance.localPlayerController != null)
       {
           playerUsernameCanvasTransform.LookAt(GameSessionManager.Instance.localPlayerController.gameplayCamera.transform);
       }
   }


   public void ConnectClientToPlayerObject()
   {
       GameSessionManager.Instance.localPlayerController = this;
       
       if (!GameNetworkManager.Instance.isSteamDisabled)
       {
           localSteamId.Value = SteamClient.SteamId;
           playerUsername = SteamClient.Name.ToString();
           StartCoroutine(SyncPlayerSteamInfoCoroutine());
       }
       playerUsernameText.text = string.Empty;
       
       TeleportPlayer(GameSessionManager.Instance.lobbySpawnTransform.position);
       
       gameplayCamera.enabled = true;
       UICamera.enabled = true;
       gameplayCamera.tag = "MainCamera";
       EnviroManager.instance.Camera = gameplayCamera;
       gameplayCamera.GetComponent<AudioListener>().enabled = true;
       GameSessionManager.Instance.audioListener = gameplayCamera.GetComponent<AudioListener>();

       Cursor.lockState = CursorLockMode.Locked;
       Cursor.visible = false;

       playerControllerInitialized = true;
   }
   
   
   /*public void DisconnectClientFromPlayerObject()
   {
       Cursor.lockState = CursorLockMode.Confined;
       Cursor.visible = true;
      
       playerControllerInitialized = false;
   }*/   
   
   IEnumerator SyncPlayerSteamInfoCoroutine()
   {
       yield return new WaitForSeconds(1f);
       SyncPlayerSteamInfoClientRpc();
   }
  
   
   [Rpc(SendTo.Everyone)]
   private void SyncPlayerSteamInfoClientRpc()
   {
       foreach (PlayerController playerController in GameSessionManager.Instance.playerControllerList)
       {
           if (!playerController.controlledByClient.Value)
           {
               break;
           }
           string playerName = new Friend(playerController.localSteamId.Value).Name;
           playerController.playerUsername = playerName;
           if (playerController != GameSessionManager.Instance.localPlayerController)
           {
               playerController.playerUsernameText.text = playerName;
           }
       }

       SyncPlayerSteamAvatar();
   }
   
   private async void SyncPlayerSteamAvatar()
   {
       foreach (PlayerController playerController in GameSessionManager.Instance.playerControllerList)
       {
           if (!playerController.controlledByClient.Value)
           {
               break;
           }

           var image = await SteamFriends.GetLargeAvatarAsync(playerController.localSteamId.Value);
           playerController.steamAvatar = GetTextureFromImage(image.Value);
       }
   }
   
   
   public static Texture2D GetTextureFromImage(Steamworks.Data.Image image)
   {
       Texture2D texture2D = new Texture2D((int)image.Width, (int)image.Height);
       texture2D.filterMode = FilterMode.Point;
      
       for (int x = 0; x < image.Width; x++)
       {
           for (int y = 0; y < image.Height; y++)
           {
               var pixel = image.GetPixel(x, y);
               texture2D.SetPixel(x, (int)image.Height - y, new UnityEngine.Color(pixel.r / 255f, pixel.g / 255f, pixel.b / 255f, pixel.a / 255f));
           }
       }
       texture2D.Apply();
       return texture2D;
   }


   [Button]
   public void TeleportPlayer(Vector3 targetPosition)
   {
       rb.position = targetPosition;
       transform.position = targetPosition;
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
       if (grounder.grounded.Value
           || grounder.airTime < 0.2f
           || (climbState == 2 && climbTimer > 0.8f)
           || waterObject.IsTouchingWater())
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
           //if climbing, or no surface to climb up to, or surface too low, or obsticle on top of landing spot, too close to ground
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
           
           //sets target position and start climbing
           climbTargetPos = hit.point + hit.normal;
           climbState = 3;
       }
   }


   public void Jump(float multiplier = 1f)
   {
       if (isNonPhysics)
       {
           //return;
       }
       if (!enableJump || jumpCooldown > 0 || stamina < staminaCostPerJump)
       {
           return;
       }

       //ungrounds and jumps
      
       jumpCooldown = jumpCooldownSetting;
      
       rb.AddForce(jumpForce * multiplier, ForceMode.Impulse);


       grounder.Unground();
       grounder.regroundCooldown = grounder.regroundCooldownSetting;


       crouching = false;
       crouchingNetworkVariable.Value = crouching;
      
       stamina -= staminaCostPerJump;
       staminaReplenishCooldown = 0.5f;
       //playerAudio.PlayJumpSound();
       
       JumpRpc();
   }
   
   
   [Rpc(SendTo.Everyone)]
   public void JumpRpc()
   {
       animator.SetTrigger("Jump");
   }

   
   private void ClimbingUpdate()
   {
       if (climbState < 1)
       {
           return;
       }
      
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
           if (sprinting)
           {
               stamina -= staminaCostSprintPerSecond * Time.fixedDeltaTime;
               staminaReplenishCooldown = 0.5f;
              
               //Stops sprinting if not inputing forward
               if (stamina <= 0f || inputDir == Vector3.zero || inputDir.z < 0)
               {
                   sprinting = false;
                   sprintingNetworkVariable.Value = false;
                   dynamicTargetSpeed = dynamicWalkSpeed;
               }
           }


           //sets ground control back to 1 over time
           airMovementControl = Mathf.MoveTowards(airMovementControl, airMovementControlTarget, Time.fixedDeltaTime);
           groundMovementControl = Mathf.MoveTowards(groundMovementControl, 1f, Time.fixedDeltaTime);
          
           if (groundMovementControlCoolDown > 0f)
           {
               groundMovementControlCoolDown -= Time.fixedDeltaTime;
           }
          
           if (jumpCooldown > 0f)
           {
               jumpCooldown -= Time.fixedDeltaTime;
           }


           //if moving fast, apply the calculated movement.
           //based on new input subtracted by previous velocity
           //so that player accelerates faster when start moving.
           if (grounder.grounded.Value)
           {
               if (inputDir.sqrMagnitude > 0.25f && groundMovementControlCoolDown <= 0f)
               {
                   rb.AddForce((gDir * 100f - gVel * 10f * dynamicTargetSpeed) * Mathf.Max(0, groundMovementControl));
               }
               //if not fast, accelerates the slowing down process
               else if (gVel.sqrMagnitude != 0f)
               {
                   rb.AddForce(-gVel * 10f);
               }
              
           }
           else
           {
               rb.AddForce((gDir * 100f - gVel * 10f * dynamicTargetSpeed) * airMovementControl);
               //rb.AddForce(-gVel * 5f);
           }


           //applies gravity in the direction of ground normal
           //so player does not slide off within the tolerable angle
           if(grounder.grounded.Value)
           {
               //lerp from current position to target ground position
               Vector3 targetGroundPosition = new Vector3(rb.position.x, grounder.groundPosition.y + grounder.groundedPositionOffset.y, rb.position.z);
               targetGroundPosition = Vector3.Lerp(rb.position, targetGroundPosition, Time.fixedDeltaTime * 15f);
               rb.MovePosition(targetGroundPosition);
               rb.velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
           }
           else
           {
               //rb.AddForce(grounder.groundNormal * gravity);
               rb.AddForce(Vector3.up * gravity);
           }


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


   public void EffectUpdate()
   {
       if (damageTimer > 0)
       {
           damageTimer -= Time.deltaTime;
       }
      
       if (drunkTimer > 0)
       {
           drunkTimer -= Time.deltaTime;
           Volume volume = PostProcessEffects.Instance.gameVolume.GetComponent<Volume>();
           volume.profile.TryGet(out UnityEngine.Rendering.Universal.ChromaticAberration chromaticAberration);
           chromaticAberration.intensity.value = Mathf.Clamp(drunkTimer, 0, 1);
       }
   }


   public void StaminaUpdate()
   {
       if (staminaReplenishCooldown > 0)
       {
           staminaReplenishCooldown -= Time.deltaTime;
       }
       else if (stamina < 100)
       {
           stamina += staminaReplenishPerSecond * Time.deltaTime;
       }
   }


   #region Interaction


   public void InteractionUpdate()
   {
       if (Physics.Raycast(gameplayCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f)), out RaycastHit hitInfo, interactDistance, ~0, QueryTriggerInteraction.Ignore) && (interactableLayer.value & 1 << hitInfo.collider.gameObject.layer) > 0)
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
       else
       {
           UIManager.instance.interactionName.text = "";
           UIManager.instance.interactionPrompt.text = "";
       }
   }
  
   #endregion


   public void LockMovement(bool state)
   {
       enableMovement = !state;


       if (!enableMovement)
       {
           if (currentEquippedItem != null &&
               currentEquippedItem.TryGetComponent<ItemController>(out var itemController))
           {
               if (itemController.buttonHeld)
               {
                   itemController.Cancel();
               }
           }
       }
   }
   
   [Rpc(SendTo.Owner)]
   public void LockMovementRpc(bool state)
   {
       LockMovement(state);
   }


   public void LockCamera(bool state)
   {
       enableLook = !state;
   }


   public void ResetCamera()
   {
       mouseLookX.Reset();
       mouseLookY.Reset();
       mouseLookX.transform.localRotation = Quaternion.identity;
       mouseLookY.transform.localRotation = Quaternion.identity;
   }


   /*public void SetCameraClamp(float x1, float x2, float y1, float y2)
   {
       GetComponent<MouseLook>().SetClamp(x1, x2, y1, y2);


       foreach (MouseLook look in GetComponentsInChildren<MouseLook>())
       {
           look.SetClamp(x1, x2, y1, y2);
       }
   }*/


   /*public void AttachToBoat(Transform playerParent)
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
   }*/


   /*public void DetachFromBoat()
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
   }*/


   // private void OnDrawGizmosSelected()
   // {
   //     Gizmos.color = new Color(0.1f, 0.1f, 0.9f, 0.8f);
   //     Gizmos.DrawSphere(base.transform.position + base.transform.up * 0.5f, radius);
   //     Gizmos.DrawSphere(base.transform.position + base.transform.up * -0.5f, radius);
   // }


   #region Damage & Death
  
   [Button]
   public void TakeDamage(float damage, Vector3 direction, float stunTime = 1f)
   {
       if (base.IsOwner)
       {
           health.Value -= damage;


           damageTimer = 0.5f;

           if (stunTime != 0)
           {
               groundMovementControl = 1 - stunTime;
               jumpCooldown = stunTime;
           }

           rb.AddForce(direction.normalized * damage, ForceMode.Impulse);


           if (health.Value <= 0 && !isPlayerDead.Value)
           {
               Die();
           }
           
           GetComponent<PlayerRating>().AddScore((int)damage * 5, "Took Damage");
           Debug.Log($"{playerUsernameText} took {damage} damage.");
       }
       
       playerAnimationController.HitReaction(FullBodyBipedEffector.LeftShoulder, direction);
   }
  
   [Rpc(SendTo.Everyone)]
   public void TakeDamageRpc(float damage, Vector3 direction)
   {
       TakeDamage(damage, direction);
   }


   [Button]
   public void Die()
   {
       if (IsOwner && !isPlayerDead.Value)
       {
           isPlayerDead.Value = true;
           //LevelManager.Instance.CheckGameOverServerRpc();
           InventoryManager.instance.DropAllItemsFromInventory();
           InventoryManager.instance.CloseInventory();
          
           LockMovement(true);
           crouching = false;
           crouchingNetworkVariable.Value = crouching;
           animator.enabled = false;
           SpectateManager.Instance.StartSpectating();
           InstantiateRagdollServerRPC();
       }
   }
  
   [Rpc(SendTo.Server)]
   public void InstantiateRagdollServerRPC()
   {
       GameObject ragdoll = Instantiate(ragdollPrefab, transform.position, transform.GetChild(0).rotation);
       ragdoll.GetComponent<NetworkObject>().Spawn();
       InstantiateRagdollClientRPC(ragdoll.GetComponent<NetworkObject>());
   }
   
   [Rpc(SendTo.Everyone)]
   public void InstantiateRagdollClientRPC(NetworkObjectReference ragdoll)
   {
       if (ragdoll.TryGet(out NetworkObject ragdollObject))
       {
           ragdollObject.transform.GetChild(currentCharacterModelIndex.Value).gameObject.SetActive(true);
       }
   }


   [Button]
   public void Respawn()
   {
       if (IsOwner)
       {
           isPlayerDead.Value = false;
           health.Value = maxHp;
          
           LockMovement(false);
           ResetCamera();
           animator.enabled = true;
           
           SpectateManager.Instance.StopSpectating();
           GetComponent<PlayerRating>().rating.Value = PlayerRating.Rating.B;
           GetComponent<PlayerRating>().ratingMeter = 0.5f;
           GetComponent<PlayerRating>().UpdateRatingText();
          
       }
   }


   public void OnIsPlayerDeadChanged(bool prevIsPlayerDead, bool newIsPlayerDead)
   {
       /*for (int i = 0; i < playerMeshRendererList.Count; i++)
       {
           playerMeshRendererList[i].enabled = !isPlayerDead.Value;
       }*/
      
       playerAnimationController.modelList[currentCharacterModelIndex.Value].SetActive(!isPlayerDead.Value);
       
       playerUsernameCanvasTransform.gameObject.SetActive(!isPlayerDead.Value);

       playerCollider.enabled = !isPlayerDead.Value;
   }
  


  
   #endregion


   #region Input Performed


   private void InputUpdate()
   {
       if (base.IsOwner && controlledByClient.Value & enableMovement)
       {
           inputDir.x = playerInputActions.Player.Move.ReadValue<Vector2>().x;
           inputDir.y = 0f;
           inputDir.z = playerInputActions.Player.Move.ReadValue<Vector2>().y;
           inputDir = inputDir.normalized;
       }
       else
       {
           inputDir = Vector3.zero;
       }

       if (inputDirX.Value != inputDir.x)
       {
           inputDirX.Value = inputDir.x;
       }
       if (inputDirZ.Value != inputDir.z)
       {
           inputDirZ.Value = inputDir.z;
       }
       
       if (base.IsOwner && controlledByClient.Value & enableLook)
       {
           mouseInput = playerInputActions.FindAction("Look").ReadValue<Vector2>();
       }
       else
       {
           mouseInput = Vector2.zero;
       }
   }


   private void LookUpdate()
   {
       if (base.IsOwner && controlledByClient.Value)
       {  
           if (mouseLookX.enabled)
           {
               mouseLookX.UpdateCameraRotation(mouseInput.x);
           }


           if (mouseLookY.enabled)
           {
               mouseLookY.UpdateCameraRotation(mouseInput.y);
           }
       }
   }




   private void Sprint_performed(InputAction.CallbackContext context)
   {
       if (base.IsOwner && controlledByClient.Value & enableMovement)
       {
           if (inputDir.z >= 0 && grounder.grounded.Value)
           {
               if (dynamicTargetSpeed == dynamicSprintSpeed)
               {
                   sprinting = false;
                   sprintingNetworkVariable.Value = false;
                   dynamicTargetSpeed = dynamicWalkSpeed;
               }
               else if (dynamicTargetSpeed == dynamicWalkSpeed)
               {
                   sprinting = true;
                   sprintingNetworkVariable.Value = true;
                   dynamicTargetSpeed = dynamicSprintSpeed;
                   crouching = false;
                   crouchingNetworkVariable.Value = crouching;
               }
           }
           else
           {
               sprinting = false;
               sprintingNetworkVariable.Value = false;
               dynamicTargetSpeed = dynamicWalkSpeed;
           }
       }
   }


   private void Jump_performed(InputAction.CallbackContext context)
   {
       if (base.IsOwner && controlledByClient.Value)
       {
           if (enableMovement)
           {
               JumpOrClimb();
           }
           else if (isPlayerDead.Value)
           {
               Respawn();
               
               Vector3 spawnPosition;
               
               if (!GameSessionManager.Instance.gameStarted.Value)
               {
                   spawnPosition = GameSessionManager.Instance.lobbySpawnTransform.position;
               }
               else
               {
                   spawnPosition = LevelManager.Instance.playerSpawnTransform.position;
               }
               TeleportPlayer(spawnPosition);
           }
       }
   }


   private void Crouch_performed(InputAction.CallbackContext context)
   {
       if (base.IsOwner && controlledByClient.Value & enableMovement)
       {
           crouching = !crouching;
           crouchingNetworkVariable.Value = crouching;
       }
      
   }


   private void ActivateItem_performed(InputAction.CallbackContext context)
   {
       if (base.IsOwner && controlledByClient.Value)
       {
           if (enableMovement)
           {
               if (InventoryManager.instance.equippedItem && InventoryManager.instance.equippedItem.owner &&
                   InventoryManager.instance.equippedItem.owner == this)
               {
                   if (InventoryManager.instance.equippedItem.GetComponent<ItemController>())
                   {
                       InventoryManager.instance.equippedItem.GetComponent<ItemController>().UseItem(true);
                       if (currentEmoteIndex.Value != -1)
                       {
                           StopEmote();
                       }
                   }
               }
           }
           else if(SpectateManager.Instance.isSpectating)
           {
               SpectateManager.Instance.SpectateNextPlayer();
           }
       }
   }
  
   private void ActivateItem_canceled(InputAction.CallbackContext context)
   {
       if (base.IsOwner && controlledByClient.Value & enableMovement)
       {
           if (InventoryManager.instance.equippedItem && InventoryManager.instance.equippedItem.owner && InventoryManager.instance.equippedItem.owner == this)
           {
               if (InventoryManager.instance.equippedItem.GetComponent<ItemController>())
               {
                   InventoryManager.instance.equippedItem.GetComponent<ItemController>().UseItem(false);
               }
           }
       }
   }


   private void Discard_performed(InputAction.CallbackContext context)
   {
       if (base.IsOwner && controlledByClient.Value & !isPlayerDead.Value)
       {       
           if (!InventoryManager.instance.inventoryOpened)
           {
               InventoryManager.instance.DiscardEquippedItem();
           }
           else
           {
               InventoryManager.instance.DiscardSelectedItem();
           }
       }
   }


   private void SwitchItem_performed(InputAction.CallbackContext context)
   {
       if (base.IsOwner && controlledByClient.Value && !isPlayerDead.Value)
       {
           InventoryManager.instance.SwitchEquipedItem(Math.Sign(context.ReadValue<float>()));
           if (currentEmoteIndex.Value != -1)
           {
               StopEmote();
           }
       }
   }
   
   private void Interact_performed(InputAction.CallbackContext context)
   {
       if (base.IsOwner && controlledByClient.Value & enableMovement)
       {
           if (enableMovement && targetInteractable != null)
           {
               targetInteractable.PerformInteract();
               if (currentEmoteIndex.Value != -1)
               {
                   StopEmote();
               }
           }
       }
   }
   
   private void Interact_canceled(InputAction.CallbackContext context)
   {
       if (base.IsOwner && controlledByClient.Value)
       {
           if (enableMovement && targetInteractable != null)
           {
               targetInteractable.StartCoroutine(targetInteractable.ResetInteract());
           }
       }
   }

   private void Emote1_performed(InputAction.CallbackContext context)
   {
       if (base.IsOwner && controlledByClient.Value & enableMovement)
       {
           PlayEmote(0);
           //SoundManager.Instance.PlayClientSoundEffect(emote1Sound,transform.position);
       }
   }
  
   private void Emote2_performed(InputAction.CallbackContext context)
   {
       if (base.IsOwner && controlledByClient.Value & enableMovement)
       {
           PlayEmote(1);
       }
   }
  
   private void Emote3_performed(InputAction.CallbackContext context)
   {
       if (base.IsOwner && controlledByClient.Value & enableMovement)
       {
           PlayEmote(2);
       }
   }
  
   private void Emote4_performed(InputAction.CallbackContext context)
   {
       if (base.IsOwner && controlledByClient.Value & enableMovement)
       {
           PlayEmote(3);
       }
   }


   private void Inventory_performed(InputAction.CallbackContext context)
   {
       if (base.IsOwner && controlledByClient.Value)
       {
           if (!InventoryManager.instance.inventoryOpened & enableMovement)
           {
               InventoryManager.instance.OpenInventory();
           }
           else
           {
               InventoryManager.instance.CloseInventory();
           }
       }
   }
  
   private void ScoreBoard_performed(InputAction.CallbackContext context)
   {
       if (base.IsOwner && controlledByClient.Value)
       {
           UIManager.instance.scoreBoardUI.SetActive(true);
       }
   }
  
   private void ScoreBoard_canceled(InputAction.CallbackContext context)
   {
       if (base.IsOwner && controlledByClient.Value)
       {
           UIManager.instance.scoreBoardUI.SetActive(false);
       }
   }
  
   private void Pause_performed(InputAction.CallbackContext context)
   {
       if (base.IsOwner && controlledByClient.Value)
       {
           if (InventoryManager.instance.inventoryOpened)
           {
               InventoryManager.instance.CloseInventory();
           }
           else
           {
               if (!UIManager.instance.pauseUI.activeInHierarchy)
               {
                   UIManager.instance.OpenMenu();
               }
               else
               {
                   UIManager.instance.CloseMenu();
               }
           }
       }
   }
  
   #endregion


   #region Emote
  
   public void PlayEmote(int index)
   {
       if (currentEmoteIndex.Value != -1 && index == currentEmoteIndex.Value)
       {
           currentEmoteIndex.Value = -1;
           return;
       }
          
       if (emoteDataList[index].fullBodyAnimation)
       {
           crouching = false;
           crouchingNetworkVariable.Value = crouching;
       }
          
       currentEmoteIndex.Value = index;
       
       if (InventoryManager.instance.equippedItem && InventoryManager.instance.equippedItem.owner &&
           InventoryManager.instance.equippedItem.owner == this)
       {
           if (InventoryManager.instance.equippedItem.GetComponent<ItemController>())
           {
               InventoryManager.instance.equippedItem.GetComponent<ItemController>().Cancel();
           }
       }
   }
   
   public void StopEmote()
   {
       currentEmoteIndex.Value = -1;
   }
  
   #endregion


   public void Extract()
   {
       isPlayerExtracted.Value = true;
       TeleportPlayer(GameSessionManager.Instance.lobbySpawnTransform.position);
       LockMovement(true);
       ResetCamera();
       SpectateManager.Instance.StartSpectating();
   }


   public void Unextract()
   {
       isPlayerExtracted.Value = false;
       LockMovement(false);
       ResetCamera();
       SpectateManager.Instance.StopSpectating();
   }
  

   public void SwitchCharacterModel()
   {
       currentCharacterModelIndex.Value = (currentCharacterModelIndex.Value + 1) % playerAnimationController.modelList.Count;
   }

   public void OnCurrentCharacterModelIndexChanged(int prevValue, int newValue)
   {
       foreach (GameObject model in playerAnimationController.modelList)
       {
           model.SetActive(false);
       }
       playerAnimationController.modelList[currentCharacterModelIndex.Value].SetActive(!isPlayerDead.Value);
       playerAnimationController.bodyAnimator.avatar = playerAnimationController.avatarList[currentCharacterModelIndex.Value];
   }
  
}



