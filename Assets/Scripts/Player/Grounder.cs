using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using System.Linq;
using Unity.Netcode;
using UnityEngine.Serialization;

public class Grounder : NetworkBehaviour
{	
	public delegate void Action();

	public Action OnGround;

	public Action OnUnground;

	[Header("Settings")]

	public bool detectGroundUsingFootPosition;

	public LayerMask groundMask;

    public LayerMask boatMask;

	public float maxGroundAngle = 40f;

	public float detectionWidth = 0.75f;

	public float detectionDepth = 0.75f;

	public float detectionDistance = 0.75f;

	public Vector3 detectionOffset;

	public Vector3 groundedPositionOffset;

	public float fallDistanceThreshold = 1f;

	public float lethalFallDistance = 10f;

	public float movementCooldownOnLanding = 0.25f;


	[Header("Values")]
	
    public NetworkVariable<bool> grounded = new (false, writePerm: NetworkVariableWritePermission.Owner);

	public float minGroundNormal;

	public float airTime;
	
	public float groundTime;

    public float regroundCooldown;

    public float regroundCooldownSetting = 0.2f;

	public float highestPoint;

	public float  fallDistance;

	public int groundContactCount;

    public ContactPoint contactPoint;

	public Collider groundCollider;

	public Vector3 tempGroundNormal;

	public Vector3 groundNormal;

	public Vector3 tempGroundPosition;

	public Vector3 groundPosition;


	[Header("References")]
	public PlayerController playerController;

	private RaycastHit hit;

	private RaycastHit hitBoat;


    void Awake()
    {
        playerController = GetComponent<PlayerController>();
		groundNormal = Vector3.up;
		highestPoint = transform.position.y;
		fallDistance = 0f;
		minGroundNormal = Mathf.Cos(maxGroundAngle * ((float)Mathf.PI / 180f)); //translates a 0-90 angle to a 1-0 normal value.
    }	

    void Update()
    {
		if (!grounded.Value)
		{
			//calculates the time player has been in air.
			airTime += Time.fixedDeltaTime;
			groundTime = 0;
			
			//updates the highestpoint during an unground.
			if (transform.position.y > highestPoint)// || pc.waterObject.IsTouchingWater())
			{
				highestPoint = transform.position.y;
			}
			
			//calculates how high the player has fell.
			fallDistance = highestPoint - transform.position.y;
			
			
			if (IsOwner)
			{
			    //if airtime >= fallAirTimeThreshold
			    // set animator trigger Fall
		    }

		}

		if (grounded.Value)
		{
			groundTime += Time.fixedDeltaTime;
			airTime = 0;
			
			highestPoint = transform.position.y;
			
			if (IsOwner)
			{
			}
		}
    }

	private void FixedUpdate()
	{
		if (regroundCooldown > 0f)
		{
			regroundCooldown -= Time.fixedDeltaTime;
			return;
		}

		if (IsOwner)
		{
			GroundContactDetection();

			UpdateState();
		}
	}



	private void UpdateState()
	{

        // if (!pc.isNonPhysics && !pc.rb.isKinematic && CheckBoat())
        // {
        //     pc.AttachToBoat(BoatController.instance.transform);
        // }
        // if (pc.isNonPhysics && !CheckBoat())
        // {
        //     pc.DetachFromBoat();
        // }

        if (groundContactCount > 0)
		{
			if (groundContactCount > 1)
			{
				//normalizes the sum of ground normal, if there are multiple
				tempGroundNormal.Normalize();
				tempGroundPosition = tempGroundPosition / groundContactCount;
			}

			groundNormal = tempGroundNormal;
			groundPosition = tempGroundPosition;
			
			float groundDistance = transform.position.y - groundPosition.y;

			if (groundDistance >= -detectionOffset.y && groundDistance <= groundedPositionOffset.y)
			{
				Ground();
			}
		}
		//if player was ungrounded not long ago, and the normal of the ground below it still meets the minimal ground normal, ground the player.
		//for smoother control on bumpy surfaces.
		//if (pc.isNonPhysics || stepSinceUngrounded < 5 && CheckWithRaycast(minGroundNormal)) 
		else if (playerController.isNonPhysics || (airTime < 0.2f && CheckWithRaycast(minGroundNormal)))
		{
			Ground();

			tempGroundNormal = hit.normal;
			groundNormal = tempGroundNormal;
			tempGroundPosition = hit.point;
			groundPosition = tempGroundPosition;

			//recalculates velocity based on ground normal.
			//applies the velocity back if player is not climbing.
			if (!playerController.isNonPhysics)
			{
				Vector3 normalized = Vector3.ProjectOnPlane(playerController.rb.velocity, hit.normal).normalized;
				if (playerController.GetClimbState() == 0 && playerController.rb.velocity.y > normalized.y)
				{
					playerController.rb.velocity = normalized * playerController.rb.velocity.magnitude;
				}
			}

		}
		else
		{	
			//if not on any ground, unground player.
			Unground();
		}
        
		//clears temporary values
		groundContactCount = 0;
		tempGroundNormal = Vector3.zero;
		tempGroundPosition = Vector3.zero;
	}

	//executes when lands from air.
	public void Ground()
	{
		if(!grounded.Value)
		{
			grounded.Value = true;
			
			playerController.headPosition.Bounce((0f - fallDistance) / 12f);
			if (fallDistance > fallDistanceThreshold)
			{
				playerController.groundMovementControlCoolDown = movementCooldownOnLanding; 
				HardLandRpc();
			}
			else
			{
				SoftLandRpc();
			}
			if (fallDistance > lethalFallDistance)
			{
				playerController.TakeDamage((fallDistance - lethalFallDistance) * 10f, Vector3.zero);
			}

			//if not climbing
			if (!playerController.isNonPhysics && playerController.GetClimbState() == 0)
			{
				//recalculates velocity based on ground normal. 
				playerController.rb.velocity = Vector3.ProjectOnPlane(playerController.vel, groundNormal);
			}

			if(OnGround != null)
				OnGround();
		}

	}

	[Rpc(SendTo.Everyone)]
	public void SoftLandRpc()
	{
		playerController.animator.SetTrigger("Soft Land");
	}

	[Rpc(SendTo.Everyone)]
	public void HardLandRpc()
	{
		playerController.animator.SetTrigger("Hard Land");
	}

	public void Unground()
	{
		//sets a delay that pauses ground checking in a short while
		//prevent player from jump again before leaving the grounded raycast distance.

		if (grounded.Value)
		{
			grounded.Value = false;
			groundContactCount = 0;
			tempGroundNormal = Vector3.zero;
			groundNormal = Vector3.up;
			tempGroundPosition = Vector3.zero;
			groundPosition = Vector3.zero;
			//regroundCooldown = regroundCooldownSetting;
			
			//sets a timeframe that allows player to jump after ungrounding

			/*if (playerController.isNonPhysics)
			{
				playerController.DetachFromBoat();
			}*/

			if (OnUnground != null)
				OnUnground();
        }
		
	}


	//checks whether player is close enough to ground, if not grounded.
	public bool CheckWithRaycast(float dot = 0f)
	{
		Physics.Raycast(playerController.transform.position + detectionOffset, Vector3.down, out hit, detectionDistance, groundMask);
		return false;
		return (hit.normal.y > dot) && (hit.distance >= detectionOffset.y || hit.distance <= groundedPositionOffset.y + 0.1f);
    }

    public bool CheckBoat()
    {
        return Physics.Raycast(playerController.transform.position, -playerController.transform.up, out hitBoat, 0.8f, boatMask);
        //return hitBoat.normal.y > minGroundNormal;
    }

	private void GroundContactDetection()
	{
		List<RaycastHit> hitList = new List<RaycastHit>();

		if(GroundRaycast(0, 0, out RaycastHit hit0))
		{
			hitList.Add(hit0);
		}

		if (!detectGroundUsingFootPosition)
		{
			if(GroundRaycast(detectionWidth, 0, out RaycastHit hit1))
			{
				hitList.Add(hit1);
			}
			if(GroundRaycast(0, detectionDepth, out RaycastHit hit2))
			{
				hitList.Add(hit2);
			}
			if(GroundRaycast(-detectionWidth, 0, out RaycastHit hit3))
			{
				hitList.Add(hit3);
			}
			if(GroundRaycast(0, -detectionDepth, out RaycastHit hit4))
			{
				hitList.Add(hit4);
			}		
		}
		else
		{
			Vector3 leftFootPos = playerController.animator.GetIKPosition(AvatarIKGoal.LeftFoot);
			Vector3 rightFootPos = playerController.animator.GetIKPosition(AvatarIKGoal.RightFoot);

			if (GroundRaycast(leftFootPos, out RaycastHit hit5))
			{
				hitList.Add(hit5);
			}

			if (GroundRaycast(rightFootPos, out RaycastHit hit6))
			{
				hitList.Add(hit6);
			}
		}

		foreach (RaycastHit hit in hitList)
        {
			if (hit.normal.y > minGroundNormal && hit.point != Vector3.zero)
			{
				groundCollider = hit.collider;
				tempGroundNormal += hit.normal;
				tempGroundPosition += hit.point;
				groundContactCount++;
            }
        }
        
	}    
	
	bool GroundRaycast(float offsetx, float offsetz, out RaycastHit hitOut)
    {
        RaycastHit hit;
        Vector3 raycastFloorPos = transform.TransformPoint(offsetx, 0, offsetz) + detectionOffset;
 
        if (Physics.Raycast(raycastFloorPos, -Vector3.up, out hit, detectionDistance, groundMask, QueryTriggerInteraction.Ignore))
        {
			Debug.DrawLine(raycastFloorPos, raycastFloorPos + -Vector3.up * detectionDistance, Color.green);
			hitOut = hit;
            return true;
        }
        else 
		{
			Debug.DrawLine(raycastFloorPos, raycastFloorPos + -Vector3.up * detectionDistance, Color.red);
			hitOut = hit;
			return false;
		}
    }
	
	bool GroundRaycast(Vector3 position, out RaycastHit hitOut)
	{
		RaycastHit hit;
		Vector3 raycastFloorPos = new Vector3(position.x, transform.position.y, position.z) + detectionOffset;
 
		if (Physics.Raycast(raycastFloorPos, -Vector3.up, out hit, detectionDistance))
		{
			Debug.DrawLine(raycastFloorPos, raycastFloorPos + -Vector3.up * detectionDistance, Color.green);
			hitOut = hit;
			return true;
		}
		else 
		{
			Debug.DrawLine(raycastFloorPos, raycastFloorPos + -Vector3.up * detectionDistance, Color.red);
			hitOut = hit;
			return false;
		}
	}

}
