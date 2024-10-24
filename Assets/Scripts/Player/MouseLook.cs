﻿// original by asteins
// adapted by @torahhorse
// http://wiki.unity3d.com/index.php/SmoothMouseLook

// Instructions:
// There should be one MouseLook script on the Player itself, and another on the camera
// player's MouseLook should use MouseX, camera's MouseLook should use MouseY

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using Unity.Netcode;
using System;
using Sirenix.OdinInspector;

public class MouseLook : NetworkBehaviour
{
    public enum RotationAxes { MouseX = 1, MouseY = 2 }
	public RotationAxes axes = RotationAxes.MouseX;
	public bool invertY = false;
	
	public float sensitivityX = 10F;
	public float sensitivityY = 9F;
 
	public float minimumX = -360F;
	public float maximumX = 360F;
 
	public float minimumY = -85F;
	public float maximumY = 85F;
 
	public float rotationX = 0F;
	public float rotationY = 0F;

	public List<float> rotArrayX = new List<float>();
    public float rotAverageX = 0F;	
 
    public List<float> rotArrayY = new List<float>();
    public float rotAverageY = 0F;
 
	public float framesOfSmoothing = 5;

	Quaternion originalRotation;
    Quaternion originalLocalRotation;
    
    void Start ()
	{			
		if (GetComponent<Rigidbody>())
		{
			GetComponent<Rigidbody>().freezeRotation = true;
		}
		
        originalRotation = Quaternion.identity;
        originalLocalRotation = Quaternion.identity;

    }

    public void UpdateCameraRotation(float input)
    {
        switch(axes)
        {
            case RotationAxes.MouseX:
	            
	            rotationX = input * sensitivityX;
                
	            Quaternion xQuaternion = Quaternion.AngleAxis(rotationX, Vector3.up);
	            transform.localRotation *= xQuaternion;
	            
                /*rotationX += input * sensitivityX * Time.timeScale;
                rotArrayX.Add(rotationX); 

                if (minimumX != -360 && maximumX != 360)
                {
                    rotationX = Mathf.Clamp(rotationX, minimumX, maximumX);
                }
                if (rotArrayX.Count >= framesOfSmoothing)
                {
                    rotArrayX.RemoveAt(0);
                }
                rotAverageX = 0f;
                for (int i = 0; i < rotArrayX.Count; i++)
                {
                    rotAverageX += rotArrayX[i];
                }
                rotAverageX /= rotArrayX.Count;
                rotAverageX = ClampAngle(rotAverageX, minimumX, maximumX);

                Quaternion xQuaternion = Quaternion.AngleAxis (rotAverageX, Vector3.up);
                if (transform.parent == null)
                {
                    transform.rotation = originalRotation * xQuaternion;
                }
                else
                {
                    transform.localRotation = originalLocalRotation * xQuaternion;
                }*/

                break;

            case RotationAxes.MouseY:

                float invertFlag = 1f;
                if( invertY )
                {
                    invertFlag = -1f;
                }
                rotationY += input * sensitivityY * invertFlag * Time.timeScale;
                rotationY = Mathf.Clamp(rotationY, minimumY, maximumY);
                rotArrayY.Add(rotationY);
                if (rotArrayY.Count >= framesOfSmoothing)
                {
                    rotArrayY.RemoveAt(0);
                }
                rotAverageY = 0f;
                for (int j = 0; j < rotArrayY.Count; j++)
                {
                    rotAverageY += rotArrayY[j];
                }
                rotAverageY /= rotArrayY.Count;

                Quaternion yQuaternion = Quaternion.AngleAxis (rotAverageY, Vector3.left);
                transform.localRotation = originalLocalRotation * yQuaternion;

                break;
        }
    }

 
	public static float ClampAngle (float angle, float min, float max)
	{
		angle = angle % 360;
		return Mathf.Clamp (angle, min, max);
	}

    public void Reset()
    {
        rotArrayX.Clear();
        rotArrayY.Clear();
        rotationX = 0f;
        rotationY = 0f;
        rotAverageX = 0f;
        rotAverageY = 0f;
    }

    [Button]
    public void SetRotation(float rotation)
    {
	    switch (axes)
	    {
		    case RotationAxes.MouseX:
			    transform.localEulerAngles = new Vector3(0f, rotation, 0f);
			    break;
		    
		    case RotationAxes.MouseY:
			    rotationY = rotation;
			    break;
	    }
    }
}