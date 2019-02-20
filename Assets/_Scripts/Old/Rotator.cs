using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rotator : MonoBehaviour {

    public float m_Speed;

    private float m_XRotation;
    private float m_YRotation;
    private float m_ZRotation;
    private Rigidbody m_RigidBody;

    // Use this for initialization
    void Start () {
        m_RigidBody = GetComponent<Rigidbody>();
	}
	
	// Update is called once per frame
	void Update () {
        m_XRotation = Input.GetAxis("Rotate X");
        m_YRotation = Input.GetAxis("Rotate Y");
        m_ZRotation = Input.GetAxis("Rotate Z");
    }

    // Physics related updates once per frame
    private void FixedUpdate()
    {
        Rotate();
    }

    private void Rotate()
    {
        Quaternion inputRotation = Quaternion.Euler(new Vector3(m_XRotation, m_YRotation, m_ZRotation) * m_Speed * Time.deltaTime);
        Quaternion result = m_RigidBody.rotation * inputRotation;
        m_RigidBody.MoveRotation(result);
    }
}
