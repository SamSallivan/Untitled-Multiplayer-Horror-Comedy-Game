using System;
//using Cinemachine;
using UnityEngine;

public class CameraBob : MonoBehaviour
{
	public AnimationCurve swayCurve; 
	public AnimationCurve fovCurve; 

	private Transform headTransform;

	private Vector3 bob;

	private Vector3 headAngles;

	private float bobMagnitude;

	private float bobMagnitudeSpeed = 4f;

	private float bobTime;

	[SerializeField]
	private float xAmp = 0.02f;

	[SerializeField]
	private float yAmp = 0.06f;

	private float rotTimer;

	private float rotSpeed;
    public float defaultFOV = 90;

	private Quaternion rot;

	private Quaternion startRot = Quaternion.identity;

	private void Awake()
	{
		headTransform = transform.parent;
		headAngles = default(Vector3);
	}

	//enables camera bob;
	public void Bob(float speed = 1f)
	{
		//loops bobTime from 0 to 2pi;
		if (bobTime < (float)Math.PI * 2f)
		{
			bobTime += Time.deltaTime / speed;
		}
		else
		{
			bobTime = 0;
		}

		//lerps the bob magnitude from 0 to 1;
		//calculates bob offset using Sig(bobTime), modified with xy amplitute and bob magnitude.
		//applies bob offset to camera.
		if (bobMagnitude != 1f)
		{
			bobMagnitude = Mathf.Lerp(bobMagnitude, 1f, Time.deltaTime * bobMagnitudeSpeed);
		}
		bob.x = Mathf.Sin(bobTime * 8f) * xAmp * bobMagnitude;
		bob.y = Mathf.Sin(bobTime * 16f) * yAmp * bobMagnitude;
		transform.localPosition = bob;
	}

	//disables camera bob, sets bob magnitude to 0, sets camera position to 0.
	public void Reset()
	{
		if (bobMagnitude != 0f)
		{
			bobMagnitude = Mathf.Lerp(bobMagnitude, 0f, Time.deltaTime * bobMagnitudeSpeed);
		}
		transform.localPosition = bob * bobMagnitude;
	}

	//lerps head rotation.z to given value over time.
	public void Angle(float z)
	{
		headAngles.z = Mathf.LerpAngle(headAngles.z, z, Time.deltaTime * 6f);
		headTransform.localEulerAngles = headAngles;
	}

	//sets rotTimer to 0;
	//sets camera target rotation to given value.
	public void Sway(Vector4 sway)
	{
		rotTimer = 0f;
		rotSpeed = sway.w;
		rot = Quaternion.Euler(sway);
	}
	
	private void Update()
	{
		//moves rotTimer to 1;
		//lerps between camera rotation and target rotation based on rotTimer on curve.
		// if (rotTimer != 1f)
		// {
		// 	rotTimer = Mathf.MoveTowards(rotTimer, 1f, Time.deltaTime * rotSpeed);
		// 	transform.localRotation = Quaternion.SlerpUnclamped(startRot, rot, swayCurve.Evaluate(rotTimer));
		// }

        //float fov = Mathf.Lerp(GetComponent<Camera>().fieldOfView, defaultFOV + (PlayerController.instance.GetClimbState() != 0 ? 15f : 0f), Time.deltaTime * 20f);

        // foreach (CinemachineVirtualCamera camera in GetComponentsInChildren<CinemachineVirtualCamera>())
        // {
        //     float fov = Mathf.Lerp(camera.m_Lens.FieldOfView, defaultFOV + (PlayerController.instance.GetClimbState() != 0 ? 15f : 0f), Time.deltaTime * 20f);
        //     camera.m_Lens.FieldOfView = fov;

        // }

        // foreach (Camera camera in GetComponentsInChildren<Camera>())
        // {
        //     float fov = Mathf.Lerp(camera.fieldOfView, defaultFOV + (GameSessionManager.Instance.localPlayerController.GetClimbState() != 0 ? 15f : 0f), Time.deltaTime * 20f);
        //     camera.fieldOfView = fov;

        // }
		/*
        WeaponManager weapon = transform.parent.GetComponentInChildren<WeaponManager>();
		if (weapon.isActiveAndEnabled && weapon.Holding() > 0f)
		{
			GetComponent<Camera>().fieldOfView = Mathf.LerpUnclamped(defaultFOV, defaultFOV + 6f, fovCurve.Evaluate(weapon.Holding()));
		}
		else
		{
			GetComponent<Camera>().fieldOfView = Mathf.Lerp(GetComponent<Camera>().fieldOfView, defaultFOV + (PlayerController.instance.rb.isKinematic ? 15f : 0f), Time.deltaTime * 20f);
		}
		*/ 

	}

	public void BobUpdate()
	{
		if (rotTimer != 1f)
		{
			rotTimer = Mathf.MoveTowards(rotTimer, 1f, Time.deltaTime * rotSpeed);
			transform.localRotation = Quaternion.SlerpUnclamped(startRot, rot, swayCurve.Evaluate(rotTimer));
		}

        foreach (Camera camera in GetComponentsInChildren<Camera>())
        {
            float fov = Mathf.Lerp(camera.fieldOfView, defaultFOV + (GameSessionManager.Instance.localPlayerController.GetClimbState() != 0 ? 15f : 0f), Time.deltaTime * 20f);
            camera.fieldOfView = fov;

        }

        //tilts camera based on horizontal input
        if (GameSessionManager.Instance.localPlayerController.climbState == 0)
        {
            Angle(GameSessionManager.Instance.localPlayerController.inputDir.x * -1f - GameSessionManager.Instance.localPlayerController.damageTimer * 3f);
        }

        //applies camera bob when grounded, walking, and not sliding
        //or sets camera position back to 0
        if (GameSessionManager.Instance.localPlayerController.grounder.grounded && GameSessionManager.Instance.localPlayerController.inputDir.sqrMagnitude > 0.25f)
        {
            if (GameSessionManager.Instance.localPlayerController.gVel.sqrMagnitude > 1f)
            {
                Bob(GameSessionManager.Instance.localPlayerController.dynamicSpeed);
            }
            else
            {
                Reset();
            }
        }
        else
        {
            Reset();
        }
	}
}
