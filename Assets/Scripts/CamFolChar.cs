using UnityEngine;
using System.Collections;

public class CamFolChar : MonoBehaviour {
	public float dampTime = 0.15f;
	private Vector3 velocity = Vector3.zero;
	Transform target;
	// Use this for initialization
	void Start () {
		target = null;
	}
	
	// Update is called once per frame
	void Update () {
		GameObject temp = GameObject.Find ("MainGuy0");
		if (temp != null) {
			target = temp.transform;
		}
		if (target != null) {
			Camera camera = GetComponent<Camera> ();
			Vector3 babyKo = target.position;
			//babyKo.z -= 10f;
			Vector3 point = camera.WorldToViewportPoint (babyKo);
			Vector3 delta = target.position - camera.ViewportToWorldPoint (new Vector3 (0.5f, 0.5f, point.z));
			Vector3 destination = transform.position + delta;
			transform.position = Vector3.SmoothDamp (transform.position, destination, ref velocity, dampTime);
		}
	}
}
