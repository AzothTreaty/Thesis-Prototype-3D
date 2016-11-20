using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public class MenuManager : MonoBehaviour {
	static MenuManager mm;
	// Use this for initialization
	string textForGameOver;
	float timePassed;
	bool startCounting, runGA;
	int player1, player2, mapSelected;

	//for GA
	int popNum, alphaCurFitIndex, width, height, generationCounter;
	List<Vector2> fitnessScores;
	List<List<double[]>> populationWeights;
	double mutationRate, crossoverRate;

	void Start () {
		timePassed = 0f;
		runGA = false;
		player1 = -1;
		player2 = 0;
		mutationRate = 0.005;
		crossoverRate = 0.05;
		alphaCurFitIndex = 0;
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
			baby.GetComponent<Button>().onClick.AddListener (() => mm.restartButtonListener());

		baby = GameObject.Find ("ResultText");
		if (baby != null)
			baby.GetComponent<Text> ().text = mm.textForGameOver;
		//else
			//Debug.Log ("Di ko nahanap si baby")

		baby = GameObject.Find ("BackToStart");
		if (baby != null) {
			baby.GetComponent<Button>().onClick.AddListener (() => mm.goBackToStart());
		}

		//for scene gameStart
		baby = GameObject.Find("Diff0");
		if (baby != null) {
			baby.GetComponent<Button>().onClick.AddListener (() => mm.selectedDifficulty(0));
		}

		baby = GameObject.Find("Diff1");
		if (baby != null) {
			baby.GetComponent<Button>().onClick.AddListener (() => mm.selectedDifficulty(1));
		}

		baby = GameObject.Find("Diff2");
		if (baby != null) {
			baby.GetComponent<Button>().onClick.AddListener (() => mm.selectedDifficulty(2));
		}

		baby = GameObject.Find("Diff3");
		if (baby != null) {
			baby.GetComponent<Button>().onClick.AddListener (() => mm.selectedDifficulty(3));
		}

		baby = GameObject.Find("Diff4");
		if (baby != null) {
			baby.GetComponent<Button>().onClick.AddListener (() => mm.selectedDifficulty(4));
		}

		baby = GameObject.Find("Diff5");
		if (baby != null) {
			baby.GetComponent<Button>().onClick.AddListener (() => mm.selectedDifficulty(5));
		}

		baby = GameObject.Find("Diff6");
		if (baby != null) {
			baby.GetComponent<Button>().onClick.AddListener (() => mm.selectedDifficulty(6));
		}

		baby = GameObject.Find("Diff7");
		if (baby != null) {
			baby.GetComponent<Button>().onClick.AddListener (() => mm.selectedDifficulty(7));
		}

		baby = GameObject.Find("Diff8");
		if (baby != null) {
			baby.GetComponent<Button>().onClick.AddListener (() => mm.selectedDifficulty(8));
		}

		baby = GameObject.Find ("GA");
		if (baby != null) {
			baby.GetComponent<Button>().onClick.AddListener (() => mm.startGA());
		}

		baby = GameObject.Find ("DoneGA");
		if (baby != null) {
			baby.GetComponent<Button>().onClick.AddListener (() => mm.stopGA());
		}
	}

	void stopGA(){
		//just to consolidate the latest versions of the babies
		player1 = popNum + 1;
		runGACore ();//weights are supposed to be saved in runGACore()
		runGA = false;
		loadMe (2);
	}

	void startGA(){//player2 is the alpha while player 1 is the rest of the population
		//meaning start pa lang ng GA session
		//Debug.Log("Hi");
		populationWeights = new List<List<double[]>>();
		fitnessScores = new List<Vector2> ();
		popNum = 20;
		//get the width and height
		TextAsset baby = Resources.Load<TextAsset> ("MapsInput");
		string[] pinakaLabas = baby.text.Split ('|');
		height = pinakaLabas [0].Split ('\n').Length;
		width = pinakaLabas[0].Split('\n')[0].Split (' ').Length;

		string weightBabyInputs = "";
		try{
			weightBabyInputs = System.IO.File.ReadAllText (UtilsKo.weightsFilePath + getMapSelected() + ".txt");
		}catch{

		}
		//generate the population and the random alpha
		if (weightBabyInputs.Equals ("")) {
			for (int qq = 0; qq < popNum; qq++) {//to generate 3 different weights
				int curW = width - 2;
				int curH = height - 2;
				List<double[]> weights2 = new List<double[]> ();
				while (curH > 2 && curW > 2) {//kasi sakto pa kapag == 3
					double[] temp = new double[12];//9 for actual weights, 1 for bias weight, 2 for width height
					for (int q = 0; q < 10; q++) {
						//read the weights from file, but for now instantiate it as random first
						temp [q] = Random.value;
					}
					curH -= 2;
					curW -= 2;
					temp [10] = curW;
					temp [11] = curH;
					weights2.Add (temp);
				}
				//instantiate the weights for the end categories
				double[] temp2 = new double[curH * curW];
				for (int w = 0; w < temp2.Length; w++) {
					temp2 [w] = Random.value;
				}
				weights2.Add (temp2);
				populationWeights.Add (weights2);
			}
			//Debug.Log (populationWeights.Count);
			writeTheWeights ();
		} else {
			string[] baby1 = weightBabyInputs.Split('|');
			//logs += "Detected " + baby1.Length + " sets of weights\n";
			for (int q = 0; q < baby1.Length; q++) {
				List<double[]> weights2 = new List<double[]> ();
				string[] baby2 = baby1 [q].Split ('\n');
				//logs += "Detected " + baby2.Length + " layers of weights\n";
				for (int w = 0; w < baby2.Length - 1; w++) {
					string[] baby3 = baby2 [w].Split (' ');
					double[] newInputs = new double[baby3.Length];
					for (int e = 0; e < baby3.Length; e++) {
						//System.IO.File.AppendAllText(logsFilePath, logs + "+======================");
						newInputs [e] = double.Parse (baby3[e]);
					}
					weights2.Add (newInputs);
				}
				populationWeights.Add (weights2);
			}
		}

		runGA = true;
		player1 = 1;
		fitnessScores.Add (new Vector2 (0, 0));
		selectedDifficulty (0);
	}

	void writeTheWeights(){
		string toBeWritten = "";
		for (int e = 0; e < populationWeights.Count; e++) {
			for (int q = 0; q < populationWeights [e].Count; q++) {
				for (int w = 0; w < populationWeights [e] [q].Length; w++) {
					toBeWritten += populationWeights [e] [q] [w] + (w == (populationWeights [e] [q].Length - 1) ? "" : " ");
				}
				toBeWritten += "\n";
			}
			toBeWritten += (e == populationWeights.Count - 1 ? "" : "|");
		}
		System.IO.File.WriteAllText (UtilsKo.weightsFilePath + getMapSelected() + ".txt", toBeWritten);
	}

	public bool gaRunning(){
		return mm == null ? false : mm.runGA;
	}

	public int getDifficulty(){
		return mm == null ? 0 : mm.player2;
	}

	public int getPlayer1(){
		return mm == null ? -1 : mm.player1;
	}

	void selectedDifficulty(int diff){
		player2 = diff;
		mapSelected = GameObject.Find("MapSelect").GetComponent<Dropdown> ().value;
		loadLevel (0);
	}

	void goBackToStart(){
		loadLevel (2);
	}

	void totoongInputTeamData(Team[] teams){
		if (runGA) {
			textForGameOver = "Pitting " + player1 + " in generation " + generationCounter + "\n";
			//Debug.Log (alphaCurFitIndex);
			fitnessScores [alphaCurFitIndex] = new Vector2(((fitnessScores [alphaCurFitIndex].x * player1) + teams [0].getScore ()) / (float)player1, 0);
			fitnessScores.Add(new Vector2(teams [1].getScore(), player1));
		}
		Debug.Log ("SUsubukan ko nang bigyan ng information si menumanager");
		foreach (Team t in teams) {
			textForGameOver += "Team " + t.getID () + " has seated " + t.getAcuSeated () + " and got " + t.getAcumulatedSplits () + " splits thereby gaining " + t.getScore () + " points\n";
			Debug.Log ("Napalitan ko na");
		}
		if (teams.Length == 0)
			Debug.Log ("Bakit walang laman tong si kuya?");
	}

	public void inputTeamData(Team[] teams){
		//process the data from the teams
		mm.totoongInputTeamData(teams);
	}

	public int getMapSelected(){
		return mm == null ? 0 : mm.mapSelected;
	}

	void runGACore(){
		if (gaRunning() && player1 + 1 < popNum){
			//rearrange the list of popweights
			int currentIndex = player1;
			while (currentIndex >= 1 && fitnessScores[currentIndex].x > fitnessScores[currentIndex - 1].x) {
				//switch
				Vector2 temp = fitnessScores [currentIndex];
				fitnessScores [currentIndex] = fitnessScores [currentIndex - 1];
				fitnessScores [currentIndex - 1] = temp;
				if (currentIndex - 1 == alphaCurFitIndex)//kasi nga nagswitch sila
					alphaCurFitIndex = currentIndex;

				currentIndex--;
			}
			player1++;
		}
		else if(gaRunning() && player1 >= popNum){
			//the top 10 in fitness function is the basis for making the population
			//rearrange the weights
			int qHolder = 0;
			for (int q = 0; q < 10 && q < fitnessScores.Count; q++) {
				populationWeights.Add (populationWeights[(int)fitnessScores[q].y]);
				qHolder = q;
			}
			Debug.Log ("Popweights count: " + populationWeights.Count);
			populationWeights.RemoveRange (0, qHolder + 1);
			populationWeights.RemoveRange (0, 10);//clear the remainder of the babies
			Debug.Log ("Popweights count is: " + populationWeights.Count);
			//run crossover algorithm and mutation rates
			for (int q = 0; q < popNum - 10 - (popNum / 10); q++) {//10% of population is going to be random
				//get the randomized parents
				int p1 = Random.Range(1, 10);
				int p2 = Random.Range (1, 10);
				while (p1 == p2) {
					p2 = Random.Range (1, 10);
				}
				//ignore last two values in every array because these are used to denote the 
				//do crossover on populationweights[p1] and populationweights[p2]
				bool crossNow = false;
				List<double[]> child = new List<double[]> ();
				for (int w = 0; w < populationWeights [p1].Count; w++) {
					double[] tempoLangTo = new double[populationWeights[p1][w].Length];
					for (int e = 0; e < populationWeights [p1] [w].Length - 2; e++) {
						if (Random.value < crossoverRate)
							crossNow = !crossNow;//basically flip crossNow
						bool willFlip = Random.value < mutationRate;
						if(crossNow && !willFlip) tempoLangTo[e] = (double)Mathf.Abs((float)populationWeights[p1][w][e] - (float)populationWeights[p2][w][e]);//distance
						else tempoLangTo[e] = (populationWeights[p1][w][e] + populationWeights[p2][w][e]) / 2.0;//average
					}
					child.Add(tempoLangTo);
				}
				//add the resulting weights to the popweights list
				populationWeights.Add(child);
			}
			Debug.Log ("After adding babies, the population weights count is: " + populationWeights.Count);

			//make the random babies
			for (int q = 0; q < (popNum / 10); q++) {
				int curW = width - 2;
				int curH = height - 2;
				List<double[]> weights2 = new List<double[]> ();
				while (curH > 2 && curW > 2) {//kasi sakto pa kapag == 3
					double[] temp = new double[12];//9 for actual weights, 1 for bias weight, 2 for width height
					for (int w = 0; w < 10; w++) {
						//read the weights from file, but for now instantiate it as random first
						temp [w] = Random.value;
					}
					curH -= 2;
					curW -= 2;
					temp [10] = curW;
					temp [11] = curH;
					weights2.Add (temp);
				}
				//instantiate the weights for the end categories
				double[] temp2 = new double[curH * curW];
				for (int w = 0; w < temp2.Length; w++) {
					temp2 [w] = Random.value;
				}
				weights2.Add (temp2);
				populationWeights.Add (weights2);
			}
			Debug.Log ("After adding randoms, the population weights count is: " + populationWeights.Count);
			writeTheWeights ();//to save the edited version of the weights after a generation
			//clear the fitnessScore array just to be sure
			fitnessScores.Clear();
			fitnessScores.Add (new Vector2 (0, 0));
			generationCounter++;
		}
	}

	void restartButtonListener(){
		runGACore ();
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
