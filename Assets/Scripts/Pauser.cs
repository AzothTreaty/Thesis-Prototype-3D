using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class Pauser : MonoBehaviour {
	bool paused;
	GameManager gm;
	// Use this for initialization
	void Start () {
		gm = GetComponent<GameManager> ();
		paused = false;
		//Button b = GameObject.Find ("PauseButton").GetComponent<Button> ();
		//b.onClick.AddListener (() => listenerKo ());
	}

	void listenerKo(){
		togglePause ();
	}
	
	// Update is called once per frame
	void Update () {
			
	}

	bool isPaused(){
		return paused;
	}

	void togglePause(){
		if (gm.isPaused()) {
			Time.timeScale = 1f;
			gm.unPauseMe ();
			paused = false;
		} else {
			gm.pauseMe ();
			Time.timeScale = 0f;
			paused = true;
		}
	}
}
