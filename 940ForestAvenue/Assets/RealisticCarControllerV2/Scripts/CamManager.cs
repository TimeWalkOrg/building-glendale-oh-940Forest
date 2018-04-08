using UnityEngine;
using System.Collections;

public class CamManager: MonoBehaviour {

	private SmoothFollow cameraScript;
	private MouseOrbit orbitScript;
	public float dist = 10.0f;
	public int cameraChangeCount = 0;
	public GameObject target;

	void Start () {
	
		cameraScript = GetComponent<SmoothFollow>();
		orbitScript = GetComponent<MouseOrbit>();

	}

	void Update () {

		cameraScript.target = target.transform;
		cameraScript.distance = dist;
		cameraScript.height = dist / 3;
		orbitScript.target = target.transform;
		orbitScript.distance = dist;


		if(Input.GetKeyDown(KeyCode.C)){
			cameraChangeCount++;
			if(cameraChangeCount == 3)
				cameraChangeCount = 0;
		}
	
		switch(cameraChangeCount){
		case 0:
			orbitScript.enabled = false;
			cameraScript.enabled = true;
			break;
		case 1:
			orbitScript.enabled = true;
			cameraScript.enabled = false;
			break;
		case 2:
			orbitScript.enabled = false;
			cameraScript.enabled = false;
			break;
		}

	}

}
