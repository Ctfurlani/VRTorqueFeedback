using UnityEngine;
using System.Collections.Generic;
using Valve.VR.InteractionSystem;

public enum XYZAxis {  X , Y , Z }

public class TorqueAwareFeedbackController : MonoBehaviour
{
    // External References
    public Hand hand;
    public Transform feedback;

    // Range of achievable angles (can be overriden in editor)
    public const float MAX_THETA = 80;
    public const float MIN_THETA = -80;
    
    private static readonly float FEEDBACK_SPEED = 0.01f;
    private static readonly float FEEDBACK_LENGHT = 1;
    private static readonly float FEEDBACK_MASS = 1;
    private static readonly float GRAVITY = 9.8f;

    // Move feedback to desired location
    private void FixedUpdate()
    {
        float xRotation = CalculateFeedbackAngle(UnitVector(XYZAxis.X));
        float yRotation = 0;
        float zRotation = CalculateFeedbackAngle(UnitVector(XYZAxis.Z));

        Quaternion desiredRotation = Quaternion.Euler(new Vector3(xRotation, yRotation, zRotation));

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

            // Find component of ideal torque of the axis we are analysing (Axis torqueComponentAxis)
            float dotProduct = Vector3.Dot(idealTorque, torqueComponentUnitVector);
            Vector3 torqueComponent = dotProduct * torqueComponentUnitVector;
            float sign = (dotProduct > 0) ? 1 : -1;


            float asinParameter = torqueComponent.magnitude / (FEEDBACK_LENGHT * FEEDBACK_MASS * GRAVITY);
            asinParameter = Mathf.Clamp(asinParameter, -1, 1);

            // Calculate angle the feedback should move to
            angle = sign * Mathf.Asin(asinParameter) * Mathf.Rad2Deg;
            angle = Mathf.Clamp(angle, MIN_THETA, MAX_THETA);


            /*Debug.DrawLine(controllerCenterOfMass, attachedCenterOfMass);
            Debug.Log(string.Format("sign={0}", sign));
            Debug.DrawLine(controllerCenterOfMass, torqueComponent);
            Debug.Log(string.Format("|idealTorque|={0} , |torqueComponent|={1} , sign={2}, theta={3}",
                    idealTorque.magnitude, torqueComponent.magnitude, sign, angle));*/
        }

        return angle;
    }

    private bool ObjectIsAttached()
    {
        return hand.currentAttachedObject && hand.currentAttachedObject.CompareTag("Interactable");
    }
}
