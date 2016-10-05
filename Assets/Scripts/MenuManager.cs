using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class MenuManager : MonoBehaviour {
	static MenuManager mm;
	// Use this for initialization
	string textForGameOver;
	float timePassed;
	bool startCounting;
	void Start () {
		timePassed = 0f;
		startCounting = false;
		GameObject baby = GameObject.Find ("DontKillMe");
		if (baby != null) {
			mm = baby.GetComponent<MenuManager>();
		} else {
			GameObject temp = new GameObject ();
			temp.name = "DontKillMe";
			temp.AddComponent<MenuManager> ();
			mm = temp.GetComponent<MenuManager> ();
			DontDestroyOnLoad (temp);
		}

		//put code here that assigns variables
		baby = GameObject.Find ("RestartButton");
		if(baby != null)
			baby.GetComponent<Button>().onClick.AddListener (() => restartButtonListener());

		baby = GameObject.Find ("ResultText");
		if (baby != null)
			baby.GetComponent<Text> ().text = mm.textForGameOver;
		//else
			//Debug.Log ("Di ko nahanap si baby")
	}

	public void inputTeamData(Team[] teams){
		//process the data from the teams
		mm.textForGameOver = "";
		Debug.Log ("SUsubukan ko nang bigyan ng information si menumanager");
		foreach (Team t in teams) {
			mm.textForGameOver += "Team " + t.getID () + " has seated " + t.getAcuSeated () + " and got " + t.getAcumulatedSplits () + " splits thereby gaining " + t.getScore () + " points\n";
			Debug.Log ("Napalitan ko na");
		}
		if (teams.Length == 0)
			Debug.Log ("Bakit walang laman tong si kuya?");
	}

	void restartButtonListener(){
		loadLevel (0);
	}

	private void loadMe(int i){
		if (i == 0) {
			SceneManager.LoadScene ("MainGame");
			startCounting = false;
		} else {
			startCounting = true;
			SceneManager.LoadScene ("GameOver");
		}
	}

	public void loadLevel(int i){
		GUI.tooltip = "0 == MainGame, * == GameOver";
		mm.loadMe (i);
	}
	
	// Update is called once per frame
	void Update () {
		if (timePassed > 20f) {
			timePassed = 0f;
			restartButtonListener ();
		}
		if(startCounting) timePassed += Time.deltaTime;
	}
}
