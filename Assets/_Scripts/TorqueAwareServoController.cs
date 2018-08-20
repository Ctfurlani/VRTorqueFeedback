using UnityEngine;
using System.Collections.Generic;
using Valve.VR.InteractionSystem;

namespace Ardunity
{	
	[AddComponentMenu("ARDUnity/Reactor/Transform/TorqueAwareServoController")]
    [HelpURL("https://sites.google.com/site/ardunitydoc/references/reactor/rotationaxisreactor")]
	public class TorqueAwareServoController : ArdunityReactor
	{
        public Axis torqueComponentAxis;
		public bool invert = false;

        public Hand hand;
        private Vector3 torqueComponentUnitVector;

        // Range of achievable angles (can be overriden in editor)
        public const float MAX_THETA = 80;
        public const float MIN_THETA = -80;

        // Constants for the feedback
        private static readonly float FEEDBACK_LENGHT = 1;
        private static readonly float FEEDBACK_MASS = 1;
        private static readonly float GRAVITY = 9.8f;
        
        private IWireInput<float> _analogInput;
        private IWireOutput<float> _analogOutput;
        private IWireInput<DragData> _dragInput;

        private void Start()
        {
            switch (torqueComponentAxis)
            {
                case Axis.X: 
                    torqueComponentUnitVector = new Vector3(1, 0, 0);
                    break;
                case Axis.Y:
                    torqueComponentUnitVector = new Vector3(0, 1, 0);
                    break;
                case Axis.Z:
                    torqueComponentUnitVector = new Vector3(0, 0, 1);
                    break;
            }

            if (invert) torqueComponentUnitVector *= -1;
        }
        
        void FixedUpdate()
        {
            // if this reactor has an output, set output value as "angle" 
            if (_analogOutput != null)
            {
                // Start the angle as 0 so if no object is being held no rotation will occur
                float theta = 0;

                // If the hand is holding an object tagged as "Interactable", account for its torque
                if (hand.currentAttachedObject && hand.currentAttachedObject.CompareTag("Interactable"))
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
                    theta = sign * Mathf.Asin(asinParameter) * Mathf.Rad2Deg;
                    theta = Mathf.Clamp(theta, MIN_THETA, MAX_THETA);


                    Debug.DrawLine(controllerCenterOfMass, attachedCenterOfMass);
                    Debug.Log(string.Format("sign={0}", sign));
                    Debug.DrawLine(controllerCenterOfMass, torqueComponent);
                    Debug.Log(string.Format("|idealTorque|={0} , |torqueComponent|={1} , sign={2}, theta={3}", 
                            idealTorque.magnitude, torqueComponent.magnitude, sign, theta));
                }

                _analogOutput.output = theta;
            }
        }
		
		protected override void AddNode(List<Node> nodes)
        {
			base.AddNode(nodes);
			
			nodes.Add(new Node("setAngle", "Set Angle", typeof(IWireInput<float>), NodeType.WireFrom, "Input<float>"));
			nodes.Add(new Node("getAngle", "Get Angle", typeof(IWireOutput<float>), NodeType.WireFrom, "Output<float>"));
			nodes.Add(new Node("rotateDrag", "Rotate by drag", typeof(IWireInput<DragData>), NodeType.WireFrom, "Input<DragData>"));
        }
        
        protected override void UpdateNode(Node node)
        {
            if(node.name.Equals("setAngle"))
            {
				node.updated = true;
                if(node.objectTarget == null && _analogInput == null)
                    return;
                
                if(node.objectTarget != null)
                {
                    if(node.objectTarget.Equals(_analogInput))
                        return;
                }
                
                _analogInput = node.objectTarget as IWireInput<float>;
                if(_analogInput == null)
                    node.objectTarget = null;
                
                return;
            }
            else if(node.name.Equals("getAngle"))
            {
				node.updated = true;
                if(node.objectTarget == null && _analogOutput == null)
                    return;
                
                if(node.objectTarget != null)
                {
                    if(node.objectTarget.Equals(_analogOutput))
                        return;
                }
                
                _analogOutput = node.objectTarget as IWireOutput<float>;
                if(_analogOutput == null)
                    node.objectTarget = null;
                
                return;
            }
            else if(node.name.Equals("rotateDrag"))
            {
				node.updated = true;
                if(node.objectTarget == null && _dragInput == null)
                    return;
                
                if(node.objectTarget != null)
                {
                    if(node.objectTarget.Equals(_dragInput))
                        return;
                }
                
                _dragInput = node.objectTarget as IWireInput<DragData>;
                if(_dragInput == null)
                    node.objectTarget = null;
                
                return;
            }
                            
            base.UpdateNode(node);
        }
	}
}
