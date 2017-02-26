using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class GAScreenHandler : MonoBehaviour {
	GameManagerOld gm;
	MenuManager mm;
	// Use this for initialization
	void Start () {
		gm = GetComponent<GameManagerOld> ();
		mm = GetComponent<MenuManager> ();
	}
	
	// Update is called once per frame
	void Update () {
		gm.runGAKoBaby ();
		string forText = "Running: " + gm.ai.getDS() + " vs Standard AI\n" + gm.getDisplayableTime () + "\nGeneration #" + mm.getGenCounter();
		foreach (Team t in GetComponents<Team>()) {
			forText += "\nTeam " + t.getID () + ": " + t.getScore ();
		}
		GameObject.Find ("StatusReport").GetComponent<Text> ().text = forText;

		if(mm.gaRunning()){
			Vector2[] scoresKo = mm.getFitScores ();
			forText = "";
			for (int q = 0; q < scoresKo.Length; q++) {
				forText += scoresKo[q].y + ": " + scoresKo [q].x + "\n";
			}
			GameObject.Find ("Scores").GetComponent<Text> ().text = forText;
		}
	}
}
