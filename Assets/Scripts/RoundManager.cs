using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class RoundManager : MonoBehaviour {
	int numberOfRounds;
	Team[] teams;
	GameManager gm;
	bool calculated;
	float timePassed;
	// Use this for initialization
	void Start () {
		Button babyKo = GetComponentInChildren<Button> ();
		babyKo.onClick.AddListener(() => listenerKo());
		calculated = false;
		numberOfRounds = 10;
		timePassed = 0f;
	}

	void listenerKo(){
		gm.newRound ();
		calculated = false;
	}
		
	public void initMe(Team[] t, GameManager g){
		teams = t;
		gm = g;
	}

	public void calculatePoints(){//calculate points for each team and store it in their respective teams
		GetComponentInChildren<Text>().text = "";
		foreach(Team t in teams){
			//calculate the points for each team
			//point system = # of characters seated * (30 seconds/timeElapsed) - # of splits due to seating arrangement
			Debug.Log("Team " + t.getID() + " has seated " + t.getSeated() + " characters in " + t.getBarkada().getTime() + " seconds and has " + t.getSplits() + " splits");
			//commented out yung dalawang next lines dahil nasa seatCharacters() ng Table class ang adjustan ng score
			//float calculatedScore = (t.getSeated() * 30/t.getBarkada().getTime()) - t.getSplits();
			//t.addScore(calculatedScore);
			//display it in the text
			GetComponentInChildren<Text>().text += "Team " + t.getID() + " has " + t.getScore() + " points\n";
		}
		numberOfRounds--;
		if (numberOfRounds == 0) {
			gm.gameOver ();
		}
	}
	
	// Update is called once per frame
	void Update () {
		if (this.isActiveAndEnabled) {
			if (!calculated) {
				calculatePoints ();
				calculated = true;
			}
			timePassed += Time.deltaTime;
			if (timePassed > 10f) {
				timePassed = 0f;
				listenerKo ();
			}
		}
	}
}
