using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

public class Grounder : MonoBehaviour
{	
	public delegate void Action();
	public Action OnGround;

	public Action OnUnground;

	public LayerMask groundMask;

    public LayerMask boatMask;

    public bool grounded;

	public int groundContactCount;

	public int stepSinceUngrounded;

    public float delay = 3f;

	public float highestPoint;

	public float jumpHeight;

	public float maxGroundAngle = 40f;

	public float minGroundNormal;

	public Vector3 tempGroundNormal;

	public Rigidbody rb;

	public PlayerController pc;

	public RaycastHit hit;
    public RaycastHit hitBoat;

    public ContactPoint contactPoint;

	public Collider groundCollider;

	public Vector3 groundNormal;


    void Awake()
    {
        pc = GetComponent<PlayerController>();
		rb = GetComponent<Rigidbody>();
		groundNormal = Vector3.up;
		highestPoint = transform.position.y;
		jumpHeight = 0f;
		minGroundNormal = Mathf.Cos(maxGroundAngle * ((float)Mathf.PI / 180f)); //translates a 0-90 angle to a 1-0 normal value.
    }	
    
	//executes when lands from air.
	public void Ground()
	{
        if (jumpHeight > 10)
        {
			//SaveManager.instance.Die("You died to a long fall.");
        }


		grounded = true;
		stepSinceUngrounded = 0;
		pc.ungroundedJumpGraceTimer = 0f;
        pc.headPosition.Bounce((0f - jumpHeight) / 12f);

        //if not climbing
        if (!pc.isNonPhysics && pc.GetClimbState() == 0)
		{
			//recalculates velocity based on ground normal. 
			pc.rb.velocity = Vector3.ProjectOnPlane(pc.vel, groundNormal);
		}

		if(OnGround != null)
			OnGround();

	}

    public void Unground()
	{
		//sets a delay that pauses ground checking in a short while
		//prevent player from jump again before leaving the grounded raycast distance.
		delay = 5f;

		if (grounded)
		{
			grounded = false;
			groundContactCount = 0;
			highestPoint = transform.position.y;
			tempGroundNormal = Vector3.zero;
			groundNormal = Vector3.up;
			
			//sets a timeframe that allows player to jump after ungrounding
            pc.ungroundedJumpGraceTimer = 0.2f;
        }

        if (pc.isNonPhysics)
        {
            pc.DetachFromBoat();
        }

        if (OnUnground != null)
			OnUnground();
		
	}

	//checks whether player is close enough to ground, if not grounded.
	public bool CheckWithRaycast(float dot = 0f)
	{
		Physics.Raycast(pc.transform.position, Vector3.down, out hit, 1f, groundMask);
		return hit.normal.y > dot;
    }

    public bool CheckBoat()
    {
        return Physics.Raycast(pc.transform.position, -pc.transform.up, out hitBoat, 0.8f, boatMask);
        //return hitBoat.normal.y > minGroundNormal;
    }

    void Update()
    {
		if (!grounded)
		{
            //updates the highestpoint during an unground.
		    if (transform.position.y > highestPoint || pc.waterObject.IsTouchingWater())
		    {
			    highestPoint = transform.position.y;
		    }

		    //calculates how high the player has fell.
		    jumpHeight = highestPoint - transform.position.y;

		}
    }

	private void FixedUpdate()
	{
		if (delay > 0f)
		{
			delay -= 1f;
			return;
		}

		UpdateState();
	}



	private void UpdateState()
	{

        if (!pc.isNonPhysics && !pc.rb.isKinematic && CheckBoat())
        {
            //pc.AttachToBoat(BoatController.instance.transform);
        }
        if (pc.isNonPhysics && !Physics.Raycast(pc.transform.position, -pc.transform.up, out hitBoat, 0.8f, boatMask))
        {
            pc.DetachFromBoat();
        }

        if (groundContactCount > 0)
		{
			if (groundContactCount > 1)
			{
				//normalizes the sum of ground normal, if there are multiple
				tempGroundNormal.Normalize();
			}

			groundNormal = tempGroundNormal;

			if (!grounded)
			{
				Ground();
			}
        }
		else
		{
            //if player was ungrounded not long ago, and the normal of the ground below it still meets the minimal ground normal, ground the player.
            //for smoother control on bumpy surfaces.
            //if (pc.isNonPhysics || stepSinceUngrounded < 5 && CheckWithRaycast(minGroundNormal)) 
            if (pc.isNonPhysics || (stepSinceUngrounded < 5 && CheckWithRaycast(minGroundNormal)))
			{
				if (!grounded)
				{
					Ground();
				}

				tempGroundNormal = hit.normal;

                //recalculates velocity based on ground normal.
                //applies the velocity back if player is not climbing.
                if (!pc.isNonPhysics)
                {
                    Vector3 normalized = Vector3.ProjectOnPlane(rb.velocity, hit.normal).normalized;
                    if (pc.GetClimbState() == 0 && rb.velocity.y > normalized.y)
                    {
                        rb.velocity = normalized * rb.velocity.magnitude;
                    }
                }

            }
			else
			{	//if not on any ground, unground player.
				if (grounded)
				{
					grounded = false;
					highestPoint = transform.position.y;

                    pc.ungroundedJumpGraceTimer = 0.2f;

                    if (pc.isNonPhysics)
                    {
						pc.DetachFromBoat();
                    }

				}
				tempGroundNormal = Vector3.up;
			}

			groundNormal = tempGroundNormal;

		}

		//calculates the time player has been in air.
		if (!grounded)
		{
			stepSinceUngrounded++;
		}

		//clears temporary values
		groundContactCount = 0;
		tempGroundNormal = Vector3.zero;
	}


	private void OnCollisionEnter(Collision c)
	{
			HandleCollision(c);
	}

	private void OnCollisionStay(Collision c)
	{
			HandleCollision(c);
	}
    
	private void HandleCollision(Collision c)
	{
		//if player just left ground, or is climbing
		//do not check ground collision
		if (delay > 0f || rb.isKinematic)
		{
			return;
		}

        //for each contact point, add to the temporary ground value.
        //adds to ground contact count.
        for (int i = 0; i < c.contactCount; i++)
		{
			contactPoint = c.GetContact(i);
			if ((groundMask.value & 1 << c.gameObject.layer) > 0  && contactPoint.normal.y > minGroundNormal)
			{
				groundCollider = c.collider;
				tempGroundNormal += contactPoint.normal;
				groundContactCount++;
            }
        }
	}

	void test()
	{
		RaycastHit[] hits = Physics.SphereCastAll(transform.position, 1f,transform.position, 1f);
	}

}
