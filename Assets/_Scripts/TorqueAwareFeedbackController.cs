using UnityEngine;
using System.Collections.Generic;
using Valve.VR.InteractionSystem;

public enum XYZAxis {  X , Y , Z }

public class TorqueAwareFeedbackController : MonoBehaviour
{
    // External References
    public Hand mainHand;
    public Transform feedback;
    public bool debuggableIn2D;

    private Hand hand;

    // Range of achievable angles (can be overriden in editor)
    public const float MAX_THETA = 80;
    public const float MIN_THETA = -80;
    
    private static readonly float FEEDBACK_SPEED = 0.01f;
    private static readonly float FEEDBACK_LENGHT = 1;
    private static readonly float FEEDBACK_MASS = 1;
    private static readonly float GRAVITY = 9.8f;

    private void Update()
    {
        if (debuggableIn2D)
        {
            hand = mainHand;
        }
        else
        {
            // If there is no controller (device) for the Hand, we take the other one
            hand = (mainHand.controller != null) ? mainHand : mainHand.otherHand;
        }
    }

    // Move feedback to desired location
    private void FixedUpdate()
    {
        // Components of the torque that we are interested. They are the local XYZ axis of the hand
        Vector3 xComponent = hand.transform.rotation * UnitVector(XYZAxis.X);
        Vector3 zComponent = hand.transform.rotation * UnitVector(XYZAxis.Z);

        float xRotation = CalculateFeedbackAngle(xComponent);
        float yRotation = 0;
        float zRotation = CalculateFeedbackAngle(zComponent);

        Vector3 eulerAngles = new Vector3(xRotation, yRotation, zRotation);
        Quaternion desiredRotation = Quaternion.Euler(eulerAngles);

        feedback.rotation = Quaternion.Lerp(feedback.rotation, desiredRotation, FEEDBACK_SPEED * Time.time);
    }

    private Vector3 UnitVector(XYZAxis axis)
    {
        switch (axis)
        {
            default:
            case XYZAxis.X: return new Vector3(1, 0, 0); 
            case XYZAxis.Y: return new Vector3(0, 1, 0);
            case XYZAxis.Z: return new Vector3(0, 0, 1);
        }
    }

    private float CalculateFeedbackAngle(Vector3 torqueComponentUnitVector)
    {        
        // Start the angle as 0 so if no object is being held no rotation will occur
        float angle = 0;

        // Only update angle if a new object is attached
        if (ObjectIsAttached())
        {
            GameObject attachedObject = hand.currentAttachedObject;

            // Find vector R (distance from controller to weight vector)
            Vector3 controllerCenterOfMass = hand.GetComponent<Transform>().position;
            Vector3 attachedCenterOfMass = attachedObject.GetComponent<Transform>().position;
            Vector3 controllerToAttached = attachedCenterOfMass - controllerCenterOfMass;

            // Find vector W (weight of the virtual object)
            Vector3 objectWeight = Vector3.down * attachedObject.GetComponent<Rigidbody>().mass;

            // Calculate ideal torque by T = R x W
            Vector3 idealTorque = Vector3.Cross(controllerToAttached, objectWeight);

            // Find component of ideal torque over the axis (relative to the hand) we are analysing
            float projectedMagnitude = Vector3.Dot(idealTorque, torqueComponentUnitVector);
            Vector3 torqueComponent = projectedMagnitude * torqueComponentUnitVector;

            // Calculate angle the feedback should move to
            float sign = (projectedMagnitude > 0) ? 1 : -1;
            float asinParameter = torqueComponent.magnitude / (FEEDBACK_LENGHT * FEEDBACK_MASS * GRAVITY);
            asinParameter = Mathf.Clamp(asinParameter, -1, 1);
            angle = sign * Mathf.Asin(asinParameter) * Mathf.Rad2Deg;
            angle = Mathf.Clamp(angle, MIN_THETA, MAX_THETA);

            /*Debug.Log("ControllerToAttached= " + controllerToAttached);
            Debug.Log("Weight= " + objectWeight);
            Debug.Log("IdealTorque=" + idealTorque);
            Debug.Log("ProjectedMagnitude= " + projectedMagnitude);
            Debug.Log("TorqueComponent= " + torqueComponent);
            Debug.Log("AsinParameter=" + asinParameter);
            Debug.Log("Angle=" + angle);*/

            /*Debug.DrawLine(controllerCenterOfMass, attachedCenterOfMass);
            Debug.DrawLine(controllerCenterOfMass, torqueComponent);*/
        }
        else
        {
            Debug.Log("Nothing attached!");
        }

        return angle;
    }    

    private bool ObjectIsAttached()
    {
        return hand.currentAttachedObject && hand.currentAttachedObject.CompareTag("Interactable");
    }
}
