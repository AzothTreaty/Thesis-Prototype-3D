using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class HUDManager : MonoBehaviour {
	GameObject team1, team2, timer;
	string textForTeam1, textForTeam2, textForTimer;
	// Use this for initialization
	void Start(){
		team1 = GameObject.Find ("Team1");
		team2 = GameObject.Find ("Team2");
		timer = GameObject.Find ("Timer");
	}

	public void setText(int index, string text){
		if (index == 0) {
			textForTeam1 = text;
		} else if (index == 1) {
			textForTimer = text;
		} else if(index == 2) {
			textForTeam2 = text;
		}
	}
	
	// Update is called once per frame
	void Update () {
		if(team1 != null) team1.GetComponent<Text> ().text = textForTeam1;
		if(team2 != null) team2.GetComponent<Text> ().text = textForTeam2;
		if(timer != null) timer.GetComponent<Text> ().text = textForTimer;
	}
}
