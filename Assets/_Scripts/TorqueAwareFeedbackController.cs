using UnityEngine;
using Valve.VR.InteractionSystem;

public class TorqueAwareFeedbackController : MonoBehaviour
{
    // External References
    public Hand mainHand;
    public Transform feedback;
    public Transform servoPhi;
    public Transform servoTheta;
    public Transform feedbackPointer;
    public bool debuggableIn2D;
    public bool neutralizeCenterOfMass;
    public float maxAngle = 80;
    public float minAngle = -80;

    private Hand _hand;

    private const float FeedbackSpeed = 0.001f;
    private const float FeedbackLength = 1;
    private const float FeedbackMass = 1;
    private const float Gravity = 9.8f;
    private const float NeutralCenterOfMassXBias = 70;
    private const float NeutralCenterOfMassYBias = 20;

    private void Update()
    {
        if (debuggableIn2D)
        {
            _hand = mainHand;
        }
        else
        {
            // If there is no controller (device) for the Hand, we take the other one
            _hand = mainHand.controller != null ? mainHand : mainHand.otherHand;
        }
    }

    // Move feedback to desired location
    private void FixedUpdate()
    {
        MoveVirtualFeedback();
        MoveServos();
    }

    /**
     * Position virtual feedback to mimic ideal torque.
     * If no object is attached and "neutralize center of mass"  is true,
     * points in the direction to maintain center of mass neutral (avoiding creation of torque)
     */
    private void MoveVirtualFeedback() {
        Quaternion desiredRotation;

        if (ObjectIsAttached()) { 
            desiredRotation = AttachedObjectTorqueRotation();

        } else  {

            if (neutralizeCenterOfMass && _hand.controller != null) { 

                desiredRotation = NeutralCenterOfMassRotation();

            } else {
                desiredRotation = HomeRotation();
            }
        }

        // Rotate feedback
        feedback.rotation = Quaternion.Slerp(feedback.rotation, desiredRotation, 1 * FeedbackSpeed * Time.time);
    }

    /**
     * Rotation that offsets center of mass to cause the ideal torque
     */
    private Quaternion AttachedObjectTorqueRotation() {
        // Components of the torque that we are interested. They are the local XYZ axis of the hand
        var handRotation = _hand.transform.rotation;
        Vector3 xComponent = handRotation * UnitVector(XYZAxis.X);
        Vector3 zComponent = handRotation * UnitVector(XYZAxis.Z);

        // Obtain angles to rotate feedback to emulate ideal (virtual) torque
        float xRotation = CalculateFeedbackAngle(xComponent);
        float yRotation = 180;
        float zRotation = CalculateFeedbackAngle(zComponent);

        return Quaternion.Euler(xRotation, yRotation, zRotation);
    }

    /**
     * Rotation that positions feedback absolutely "upwards"
     */
    private Quaternion HomeRotation() {
        return Quaternion.Euler(0, 180, 0);
    }

    /**
     * Rotation that positions feedback in order to minimize torque on hand 
     */
    private Quaternion NeutralCenterOfMassRotation() {
        // From the controller rotation, X bias (quaternion multiplication)
        Quaternion rotation = _hand.controller.transform.rot * Quaternion.Euler(NeutralCenterOfMassXBias, 0, 0);
        // Now set Y to a constant (180, which is home, + Y bias) and negate X and Z angles (so feedback moves against hand rotation)
        rotation = Quaternion.Euler(-rotation.eulerAngles.x, 180 + NeutralCenterOfMassYBias, -rotation.eulerAngles.z);
        return rotation;
    }

    /**
     * Translate feedback pointer position to spherical coordinates transformation for the servos
     */
    private void MoveServos() {
        // Obtain vector from base to tip of the pointer and its projection in the XZ plane
        Vector3 pointer = feedbackPointer.position - feedback.position;
        Vector3 pointerXZProj = Vector3.ProjectOnPlane(pointer, UnitVector(XYZAxis.Y));

        if (neutralizeCenterOfMass) {
            MoveThetaServo(pointerXZProj);
            MovePhiServo(pointer, pointerXZProj);

        } else {
            // Consider if servos are returning to or leaving "home" position.
            // If returning to home, move phi first. If leaving home, move theta first

            if (pointer.normalized == new Vector3(0,1,0))
                MovePhiThenThetaServos(pointer, pointerXZProj);
            else
                MoveThetaThenPhiServos(pointer, pointerXZProj);
        }
    }

    private void MoveThetaThenPhiServos(Vector3 pointer, Vector3 pointerXZProj) {
        Quaternion thetaDesiredRotation = MoveThetaServo(pointerXZProj);

        if (QuaternionsClose(servoTheta.rotation, thetaDesiredRotation))
            MovePhiServo(pointer, pointerXZProj);
    }
    private void MovePhiThenThetaServos(Vector3 pointer, Vector3 pointerXZProj) {
        Quaternion phiDesiredRotation = MovePhiServo(pointer, pointerXZProj);

        if (QuaternionsClose(servoPhi.rotation, phiDesiredRotation))
            MoveThetaServo(pointerXZProj);
    }

    private Quaternion MoveThetaServo(Vector3 pointerXZProj) {
        // Calculate angles of rotation in XZ plane (theta)
        float theta = Vector3.Angle(GetThetaXAxisReferenceSign(pointerXZProj) * UnitVector(XYZAxis.X), pointerXZProj);

        // Rotate transforms (bound to individual servos) by theta degrees
        Quaternion thetaDesiredRotation = Quaternion.Euler(new Vector3(0, theta, 0));
        servoTheta.rotation = Quaternion.Lerp(servoTheta.rotation, thetaDesiredRotation, FeedbackSpeed * Time.time);

        return thetaDesiredRotation;
    }

    private Quaternion MovePhiServo(Vector3 pointer, Vector3 pointerXZProj) {
        // Calculate angles of rotation from the y axis (phi)
        int phiSign = GetPhiSign(pointerXZProj);
        float phi = phiSign * Vector3.Angle(UnitVector(XYZAxis.Y), pointer);

        // Rotate transforms (bound to individual servos) by phi and theta degrees
        Quaternion phiDesiredRotation = Quaternion.Euler(new Vector3(0, 0, phi));
        servoPhi.rotation = Quaternion.Lerp(servoPhi.rotation, phiDesiredRotation, FeedbackSpeed * Time.time);

        return phiDesiredRotation;
    }

    private bool QuaternionsClose(Quaternion q1, Quaternion q2, float threshold) {
        return Quaternion.Dot(q1, q2) > 1 - threshold;
    }

    private bool QuaternionsClose(Quaternion q1, Quaternion q2) {
        return QuaternionsClose(q1, q2, 0.005f);
    }

    private int GetXZPlaneQuadrant(Vector3 xzPlaneVector) {
        if (xzPlaneVector.x >= 0) { // quadrant 1 or 4
            return (xzPlaneVector.z >= 0) ? 1 : 4;
        } else { // quadrant 2 or 3
            return (xzPlaneVector.z >= 0) ? 2 : 3;
        }
    }
    
    private int GetPhiSign(Vector3 pointerXZProj) {
        int quadrant = GetXZPlaneQuadrant(pointerXZProj);

        return (quadrant == 1 || quadrant == 2) ? -1 : 1;
    }
    private int GetThetaXAxisReferenceSign(Vector3 pointerXZProj) {
        int quadrant = GetXZPlaneQuadrant(pointerXZProj);

        return (quadrant == 1 || quadrant == 2) ? -1 : 1;
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
            GameObject attachedObject = _hand.currentAttachedObject;

            // Find vector R (distance from controller to weight vector)
            Vector3 controllerCenterOfMass = _hand.GetComponent<Transform>().position;
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
            float asinParameter = torqueComponent.magnitude / (FeedbackLength * FeedbackMass * Gravity);
            asinParameter = Mathf.Clamp(asinParameter, -1, 1);
            angle = sign * Mathf.Asin(asinParameter) * Mathf.Rad2Deg;
            angle = Mathf.Clamp(angle, minAngle, maxAngle);
        }
        else
        {
            Debug.Log("Nothing attached!");
        }

        return angle;
    }

    private bool ObjectIsAttached()
    {
        return _hand.currentAttachedObject && _hand.currentAttachedObject.CompareTag("Interactable");
    }
}

public enum XYZAxis {  X , Y , Z }