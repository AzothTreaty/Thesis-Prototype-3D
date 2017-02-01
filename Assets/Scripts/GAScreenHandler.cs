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
		string forText = "Running: " + gm.ai.getDS () + " vs " + gm.ai2.getDS () + "\n" + gm.getDisplayableTime () + "\nGeneration #" + mm.generationCounter;
		foreach (Team t in GetComponents<Team>()) {
			forText += "\nTeam " + t.getID () + ": " + t.getScore ();
		}
		GameObject.Find ("StatusReport").GetComponent<Text> ().text = forText;
	}
}
