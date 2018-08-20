using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LookAtPlayer : MonoBehaviour
{

    public Transform target;
    
    void FixedUpdate()
    {
        GetComponent<Transform>().LookAt(target);
    }
}