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
	int generationCounter;
	int popNum, numLayers, protocolIndex;
	int[] nodesPerLayer;
	List<Vector2> fitnessScores;
	Vector2[] diffScores;//index is the diffArea, x is the subDiff, y is the currentAverage score
	int diffInterval;
	List<List<double[]>> populationWeights;
	List<int[]> GAProtocols;
	double mutationRate, crossoverRate, minNN, maxNN;
	string[] tempStringArray, tempStringArray2;//for getting inputs
	int[] tempIntArray;

	public int getGenCounter(){
		return mm.generationCounter;
	}

	void Start () {
		timePassed = 0f;
		runGA = false;
		player1 = -1;
		player2 = 0;
		startCounting = false;
		generationCounter = 0;
		diffScores = new Vector2[3];
		diffScores [0] = new Vector2 (0, 0);
		diffScores [1] = new Vector2 (1, 0);
		diffScores [2] = new Vector2 (2, 0);
		diffInterval = 3;
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
			baby.GetComponent<Button>().onClick.AddListener (() => mm.dynamicDiff(2));
		}

		baby = GameObject.Find("Diff1");
		if (baby != null) {
			baby.GetComponent<Button>().onClick.AddListener (() => mm.dynamicDiff(1));
		}

		baby = GameObject.Find("Diff2");
		if (baby != null) {
			baby.GetComponent<Button>().onClick.AddListener (() => mm.dynamicDiff(0));
		}

		baby = GameObject.Find ("GA");
		if (baby != null) {
			baby.GetComponent<Button>().onClick.AddListener (() => mm.startGA());
		}

		baby = GameObject.Find ("DoneGA");
		if (baby != null) {
			baby.GetComponent<Button>().onClick.AddListener (() => mm.stopGA());
		}

		baby = GameObject.Find ("SaveIt");
		if (baby != null) {
			baby.GetComponent<Button>().onClick.AddListener (() => mm.stopGA());
		}

		if (mm.gaRunning ()) {
			GameObject[] babiesKoTo = GameObject.FindGameObjectsWithTag ("Untagged");
			for (int q = 0; q < babiesKoTo.Length; q++) {
				babiesKoTo [q].SetActive (false);
			}
			Debug.Log (babiesKoTo.Length);
		} else {
			Debug.Log("ggdes");
		}
	}

	bool simpleBa(){
		if (generationCounter < 500)
			return true;
		else
			return false;
	}

	public bool isSimpleTraining(){
		return mm == null ? true : mm.simpleBa ();
	}

	void stopGA(){
		//just to consolidate the latest versions of the babies
		player1 = popNum + 1;
		runGACore ();//weights are supposed to be saved in runGACore()
		runGA = false;
		loadMe (2);
	}

	void dynamicDiff(int diffArea){
		selectedDifficulty ((int) (diffScores[diffArea].x * diffInterval), 0);
	}




	public Vector2[] getFitScores(){
		return mm.getFitScoresKo ();
	}

	Vector2[] getFitScoresKo(){
		Vector2[] returnVal = new Vector2[fitnessScores.Count];
		for (int q = 0; q < fitnessScores.Count; q++) {
			returnVal [q] = fitnessScores [q];
		}
		return returnVal;
	}

	int getDiffArea(int subDiff){
		return subDiff / diffInterval;
	}

	/*
	 * ============================================================================================================================================
	 * TRY MO ICHECK KUNG GINAGAMIT DIN TONG STARTGA() PARA SA PAGRESUME NG GA, DAHIL HINDI SIYA DAPAT GINAGAMIT PARA DOON
	 * ============================================================================================================================================
	 */

	void startGA(){//player2 is the alpha while player 1 is the rest of the population
		//meaning start pa lang ng GA session
		//Debug.Log("Hi");
		string gaconfigurations = System.IO.File.ReadAllText (UtilsKo.GAConfigFilePath);
		tempStringArray = gaconfigurations.Split (' ');
		populationWeights = new List<List<double[]>>();
		fitnessScores = new List<Vector2> ();
		popNum = int.Parse(tempStringArray[0]);
		mutationRate = double.Parse(tempStringArray[1]);
		crossoverRate = double.Parse(tempStringArray[2]);
		protocolIndex = 0;

		tempStringArray = gaconfigurations.Split ('\n');
		GAProtocols = new List<int[]> ();
		for (int q = 1; q < tempStringArray.Length; q++) {//load the remaining protocols in order of starting-generationNumber, popNum, parentsKept, children made, and random samplings
			tempStringArray2 = tempStringArray[q].Split(' ');
			tempIntArray = new int[tempStringArray2.Length];
			for (int qq = 0; qq < tempStringArray2.Length; qq++) {
				tempIntArray [qq] = int.Parse (tempStringArray2[qq]);
			}

			GAProtocols.Add (tempIntArray);
		}

		//------------------------------------------------------------------------------------Dito aayusin yung mga neural Network configurations, At magdagdag ka ng GA Protocols file
		//GA config like population size, breeding protocol, mutation rates, crossover rates
		//nodesPerLayer[0] == output layer
		string neuralnetconfigs = System.IO.File.ReadAllText (UtilsKo.NNConfigFilePath);
		tempStringArray = neuralnetconfigs.Split (' ');
		minNN = double.Parse (tempStringArray [0]);
		maxNN = double.Parse (tempStringArray [1]);
		numLayers = int.Parse(tempStringArray[2]);
		nodesPerLayer = new int[numLayers];
		for (int q = 0; q < numLayers; q++) {
			nodesPerLayer[q] = int.Parse(tempStringArray[q + 3]);
		}

		string weightBabyInputs = "";
		try{
			weightBabyInputs = System.IO.File.ReadAllText (UtilsKo.weightsFilePath + getMapSelected() + ".txt");
		}catch{

		}
		//generate the population and the random alpha
		if (weightBabyInputs.Equals ("")) {
			for (int i = 0; i < popNum; i++) {//for every member of the population
				List<double[]> tempWeights = UtilsKo.generateRandomNNWeights(nodesPerLayer, minNN, maxNN);
				populationWeights.Add (tempWeights);
			}
			//Debug.Log (populationWeights.Count);
			UtilsKo.writeNNWeights(populationWeights);
		} else {
			populationWeights = UtilsKo.readNNWeights (weightBabyInputs);
		}

		runGA = true;
		player1 = 0;
		selectedDifficulty (0, 3);
	}

	/*
	 * Try to consolidate this function and GameManagerOld's DQNAI saveTheWeight to the UtilsKo class
	 * Also try to consolidate the reading from and writing to of the 
	 * 
	 */ 

	public bool gaRunning(){
		return mm == null ? false : mm.runGA;
	}

	public int getDifficulty(){
		return mm == null ? 0 : mm.player2;
	}

	public int getPlayer1(){
		return mm == null ? 0 : mm.player1;
	}

	void selectedDifficulty(int diff, int levelToLoad){
		player1 = diff;
		mapSelected = GameObject.Find("MapSelect").GetComponent<Dropdown> ().value;
		loadLevel (levelToLoad);
	}

	void goBackToStart(){
		loadLevel (2);
	}

	void totoongInputTeamData(Team[] teams, int diffToBeSaved){
		if (runGA) {
			textForGameOver = "Pitting " + player1 + " in generation " + generationCounter + "\n";
			fitnessScores.Add (new Vector2 (teams [1].getScore (), player1));
		} else {
			diffToBeSaved = getDiffArea (diffToBeSaved);
			if (diffScores [diffToBeSaved].y == 0)
				diffScores [diffToBeSaved].y = teams [1].getScore ();
			else{
				if (diffScores [diffToBeSaved].y > teams [1].getScore ()) {
					diffScores [diffToBeSaved].x = diffScores [diffToBeSaved].x - 1 < 0 ? 0 : diffScores [diffToBeSaved].x - 1;
				}
				else if (diffScores [diffToBeSaved].y < teams [1].getScore ()) {
					diffScores [diffToBeSaved].x = diffScores [diffToBeSaved].x + 1 >= diffInterval ? diffInterval - 1 : diffScores [diffToBeSaved].x + 1;
				}
				diffScores [diffToBeSaved].y = ((diffScores [diffToBeSaved].y + teams [1].getScore ()) / 2.0f);
			}
		}
		//Debug.Log ("SUsubukan ko nang bigyan ng information si menumanager");
		foreach (Team t in teams) {
			textForGameOver += "Team " + t.getID () + " has seated " + t.getAcuSeated () + " and got " + t.getAcumulatedSplits () + " splits thereby gaining " + t.getScore () + " points\n";
			//Debug.Log ("Napalitan ko na");
		}
		if (teams.Length == 0)
			Debug.Log ("Bakit walang laman tong si kuya?");
	}

	public void inputTeamData(Team[] teams, int babyKo){
		//process the data from the teams
		mm.totoongInputTeamData(teams, babyKo);
	}

	public int getMapSelected(){
		return mm == null ? 0 : mm.mapSelected;
	}

	void runGACore(){
		mm.runGACoreKo ();
	}

	void runGACoreKo(){
		if (gaRunning() && player1 + 1 < popNum){
			//rearrange the list of popweights
			int currentIndex = player1;
			while (currentIndex >= 1 && fitnessScores[currentIndex].x > fitnessScores[currentIndex - 1].x) {
				//switch
				Vector2 temp = fitnessScores [currentIndex];
				fitnessScores [currentIndex] = fitnessScores [currentIndex - 1];
				fitnessScores [currentIndex - 1] = temp;
				currentIndex--;
			}
			player1++;
		}
		else if(gaRunning() && player1 + 1 >= popNum){
			//save scores of current generation for statistics
			string scores = "";
			for (int q = 0; q < fitnessScores.Count; q++) {
				for (int w = 0; w < fitnessScores.Count; w++) {
					if (fitnessScores [w].y == q) {
						scores += fitnessScores [w].x + " ";
						break;
					}
				}
			}
			scores += "\n";
			System.IO.File.AppendAllText (UtilsKo.GAGenScores, generationCounter + "\n" + scores);

			//=================================================================================================================================
			//check the GAProtocols list for any protocol that needs to be used
			//tempIntArray in this function will contain the necessary parameters for the GA protocols to be observed
			for (int q = 0; q < GAProtocols.Count; q++) {
				if (GAProtocols [q] [0] > generationCounter) {
					protocolIndex = q - 1;
					break;
				}
			}
			tempIntArray = GAProtocols[protocolIndex];
			popNum = tempIntArray [1];
			int numParents = tempIntArray [2];
			int numChildren = tempIntArray [3];
			int randomPeople = tempIntArray [4];


			//gets the number of parents required or at least the number of parents with scores and adds them to the end of the populationWeights
			int qHolder = 0;
			for (int q = 0; q < numParents && q < fitnessScores.Count; q++) {
				//populationWeights.Add (populationWeights[(int)fitnessScores[q].y]);
				qHolder = q;
			}
			//Debug.Log ("Popweights count before removal: " + populationWeights.Count);
			//Debug.Log ("PopNum is " + popNum);
			//clears the rest of the list of the unneeded parents
			populationWeights.RemoveRange (qHolder, popNum - (qHolder + 1));
			//Debug.Log ("Popweights count after removal: " + populationWeights.Count);
			

			//generate children from existing parents and run crossover and mutation algorithms
			for (int q = 0; q < numChildren; q++) {
				//get the randomized parents
				//Random.Range is [minimum inclusive, maximum exclusive], so i used qHolder + 1
				int p1 = Random.Range(0, qHolder + 1);
				int p2 = Random.Range (0, qHolder + 1);
				while (p1 == p2) {
					p2 = Random.Range (0, qHolder + 1);
				}

				bool crossNow = false;
				List<double[]> child = new List<double[]> ();
				for (int w = 0; w < populationWeights [p1].Count; w++) {
					double[] tempoLangTo = new double[populationWeights[p1][w].Length];
					for (int e = 0; e < populationWeights [p1] [w].Length - 2; e++) {
						//check if crossover or mutation
						if (Random.value < crossoverRate)
							crossNow = !crossNow;//basically flip crossNow
						bool willFlip = Random.value < mutationRate;

						//crossover algorithm == switch genes
						if (crossNow) tempoLangTo[e] = populationWeights[p2][w][e];
						else tempoLangTo[e] = populationWeights[p1][w][e];

						//mutation algorithm == multiply current gene by a certain value
						if(willFlip) tempoLangTo[e] = tempoLangTo[e] * Random.value;
					}
					child.Add(tempoLangTo);
				}
				//add the resulting weights to the popweights list
				populationWeights.Add(child);
			}
			Debug.Log ("After adding babies, the population weights count is: " + populationWeights.Count);

			//make the random babies
			for (int q = 0; q < randomPeople; q++) {
				List<double[]> weights2 = UtilsKo.generateRandomNNWeights (nodesPerLayer, minNN, maxNN);
				populationWeights.Add (weights2);
			}
			Debug.Log ("After adding randoms, the population weights count is: " + populationWeights.Count);
			UtilsKo.writeNNWeights (populationWeights);//to save the edited version of the weights after a generation

			//clear the fitnessScore array just to be sure
			fitnessScores.Clear();
			generationCounter++;
			player1 = 0;
			mapSelected = Random.Range (0, 3); 
			Debug.Log ("map selected is now " + mapSelected);
			Debug.Log ("\n");
			Debug.Log ("\n");Debug.Log ("\n");Debug.Log ("\n");Debug.Log ("\n");Debug.Log ("\n");Debug.Log ("\n");Debug.Log ("\n");Debug.Log ("\n");
		}
	}

	void restartButtonListener(){
		if (gaRunning ()) {
			runGACore ();
			loadLevel (3);
		} else {
			loadLevel (0);
		}
	}

	private void loadMe(int i){
		if (i == 0) {
			SceneManager.LoadScene ("MainGame");
			startCounting = false;
		} else if (i == 1) {
			startCounting = true;
			SceneManager.LoadScene ("GameOver");
		} else if (i == 2) {
			startCounting = false;
			SceneManager.LoadScene ("GameStart");
		} else if (i == 3) {
			startCounting = false;
			SceneManager.LoadScene ("GAScreen");
		} else {
			Debug.Log ("Hi Hello po");
		}
	}

	public void loadLevel(int i){
		mm.loadMe (i);
	}
	
	// Update is called once per frame
	void Update () {
		if (timePassed > 2.0f) {
			timePassed = 0f;
			restartButtonListener ();
		}
		if(startCounting) timePassed += Time.deltaTime;
	}
}
