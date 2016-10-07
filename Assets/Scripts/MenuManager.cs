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
	int difficulty;
	void Start () {
		timePassed = 0f;
		difficulty = 0;
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

		//for the scene gameOver
		//put code here that assigns variables
		baby = GameObject.Find ("RestartButton");
		if(baby != null)
			baby.GetComponent<Button>().onClick.AddListener (() => restartButtonListener());

		baby = GameObject.Find ("ResultText");
		if (baby != null)
			baby.GetComponent<Text> ().text = mm.textForGameOver;
		//else
			//Debug.Log ("Di ko nahanap si baby")

		baby = GameObject.Find ("BackToStart");
		if (baby != null) {
			baby.GetComponent<Button>().onClick.AddListener (() => goBackToStart());
		}

		//for scene gameStart
		baby = GameObject.Find("Diff0");
		if (baby != null) {
			baby.GetComponent<Button>().onClick.AddListener (() => selectedDifficulty(0));
		}

		baby = GameObject.Find("Diff1");
		if (baby != null) {
			baby.GetComponent<Button>().onClick.AddListener (() => selectedDifficulty(1));
		}

		baby = GameObject.Find("Diff2");
		if (baby != null) {
			baby.GetComponent<Button>().onClick.AddListener (() => selectedDifficulty(2));
		}
	}

	public int getDifficulty(){
		return mm == null ? 0 : mm.difficulty;
	}

	void selectedDifficulty(int diff){
		difficulty = diff;
		loadLevel (0);
	}

	void goBackToStart(){
		loadLevel (2);
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
		} else if (i == 1) {
			startCounting = true;
			SceneManager.LoadScene ("GameOver");
		} else {
			startCounting = false;
			SceneManager.LoadScene ("GameStart");
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
