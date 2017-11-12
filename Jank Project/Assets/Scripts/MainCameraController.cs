using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainCameraController : MonoBehaviour {

    public GameObject m_TrackedObject;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update ()
    {
        if (m_TrackedObject != null)
        {
            Vector3 v3TrackedPosition = m_TrackedObject.transform.position;
            float z = transform.position.z;
            transform.position = new Vector3(v3TrackedPosition.x, v3TrackedPosition.y, z);
        }
    }
}
