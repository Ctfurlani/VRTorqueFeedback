using UnityEngine;
using System.Collections.Generic;
using Valve.VR.InteractionSystem;

namespace Ardunity
{	
	[AddComponentMenu("ARDUnity/Reactor/Transform/MassAwareRotationAxisReactor")]
    [HelpURL("https://sites.google.com/site/ardunitydoc/references/reactor/rotationaxisreactor")]
	public class MassAwareRotationAxisReactor : ArdunityReactor
	{
		public Axis upAxis;
		public Axis forwardAxis;
		public bool invert = false;

        public Hand hand;

		private Quaternion _initRot;
		private Quaternion _dragRot;
		private IWireInput<float> _analogInput;
		private IWireOutput<float> _analogOutput;
		private IWireInput<DragData> _dragInput;

        // Mass constants
        private static readonly float MAX_MASS = 10;
        private static readonly float MIN_MASS = 1;

		protected override void Awake()
		{
            base.Awake();
			
			_initRot = transform.localRotation;
			_dragRot = Quaternion.identity;
		}

		// Use this for initialization
		void Start ()
		{
					
		}
		
		// Update is called once per frame
		void Update ()
		{
			if(_analogInput != null || _analogOutput != null || _dragInput != null)
			{
                Quaternion startRot = _initRot * _dragRot;
				
				Vector3 up = Vector3.zero;
				if(upAxis == Axis.X)
					up = Vector3.right;
				else if(upAxis == Axis.Y)
					up = Vector3.up;
				else if(upAxis == Axis.Z)
					up = Vector3.forward;
				if(invert)
					up = -up;
				
				Vector3 forward = Vector3.zero;
				if(forwardAxis == Axis.X)
					forward = Vector3.right;
				else if(forwardAxis == Axis.Y)
					forward = Vector3.up;
				else if(forwardAxis == Axis.Z)
					forward = Vector3.forward;
				
				Vector3 to = Vector3.ProjectOnPlane(transform.localRotation * forward, startRot * up);
				
				float angle = Vector3.Angle(startRot * forward, to);
				if(Vector3.Dot(startRot * up, Vector3.Cross(startRot * forward, to)) < 0f)
					angle = -angle;

                // if this reactor has an output, set output value as "angle" 
				if(_analogOutput != null)
                {
                    //set the mass as an identity value (1 for multiplication)
                    float mass = 1;

                    // set the correction factor as an identity value (1 for multiplication)
                    float correctionFactor = 1;

                    // if the hand is holding an object tagged as "Interactable", account for its mass
                    if (hand.currentAttachedObject && hand.currentAttachedObject.CompareTag("Interactable"))
                    {
                        Debug.Log("Attached object");

                        GameObject attached = hand.currentAttachedObject;
                        Rigidbody attachedRigidBody = attached.GetComponent<Rigidbody>();
                        if (attachedRigidBody)
                        {
                            mass = attachedRigidBody.mass;
                            Debug.Log(string.Format("Mass = {0}", correctionFactor));

                            if (mass < MIN_MASS || mass > MAX_MASS) {
                                throw new UnityException(string.Format("Mass {0} is out of [{0},{0}] range!", mass, MIN_MASS, MAX_MASS));
                            }
                        }
                    }
                    else
                    {
                        Debug.Log("No object");
                    }

                    correctionFactor = (mass - MIN_MASS) / (MAX_MASS - MIN_MASS);

                    Debug.Log(string.Format("Correction = {0}", correctionFactor));

                    _analogOutput.output = angle * correctionFactor;
                }

                if (_analogInput != null)
				{
					float value = _analogInput.input;
					if(value > 180f)
						value -= 360f;
					else if(value < -180f)
						value += 360f;

                    transform.Rotate(up, value - angle, Space.Self);
				}
				
				if(_dragInput != null)
				{
					DragData dragData = _dragInput.input;
					if(dragData.isDrag)
					{
						Quaternion delta = Quaternion.AngleAxis(dragData.delta, up);
						transform.localRotation *= delta;
						_dragRot *= delta;
					}
				}
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
