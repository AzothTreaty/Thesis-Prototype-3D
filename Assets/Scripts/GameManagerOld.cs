using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

public static class UtilsKo{
	public static string gameLogsFilePath = "GameLogs.txt";
	public static string weightsFilePath = "Weights";
	public static string logsFilePath = "Logs.txt";
	public static string directionalMapFilePath = "DirectionalMap";
	public static string NNConfigFilePath = "NeuralNetworkConfigurations.txt";
	public static string GAConfigFilePath = "GAConfigurations.txt";
	public static string GAGenScores = "GenScores.txt";
	public static int tileH = 10;
	public static int tileW = 10;
	public static int numDiffs = 9;
	public static int maxRounds = 10;
	public static int roundLength = 30;
	public static int iterationCount = 0;
	public static int mod(int a, int b){
		return (a % b + b) % b;
	}
	//automatic max of 1 and min of 0
	public static double logFunc(double value){
		double returnVal = 1 / (1 + Mathf.Exp(-1.0f * (float)value));
		return returnVal;
	}


	public static List<List<double[]>> readNNWeights(string inputString){
		List<List<double[]>> returnVal = new List<List<double[]>>();
		string[] baby1 = inputString.Split('|');
		for (int q = 0; q < baby1.Length; q++) {
			List<double[]> weights2 = new List<double[]> ();
			string[] baby2 = baby1 [q].Split ('\n');
			for (int w = 0; w < baby2.Length - 1; w++) {
				string[] baby3 = baby2 [w].Split (' ');
				double[] newInputs = new double[baby3.Length];
				for (int e = 0; e < baby3.Length; e++) {
					newInputs [e] = double.Parse (baby3[e]);
				}
				weights2.Add (newInputs);
			}
			returnVal.Add (weights2);
		}
		return returnVal;
	}


	public static void writeNNWeights(List<List<double[]>> weightsKo){
		string toBeWritten = "";
		for (int e = 0; e < weightsKo.Count; e++) {
			for (int q = 0; q < weightsKo [e].Count; q++) {
				for (int w = 0; w < weightsKo [e] [q].Length; w++) {
					toBeWritten += weightsKo [e] [q] [w] + (w == (weightsKo [e] [q].Length - 1) ? "" : " ");
				}
				toBeWritten += "\n";
			}
			toBeWritten += (e == weightsKo.Count - 1 ? "" : "|");
		}
		System.IO.File.WriteAllText (UtilsKo.weightsFilePath + ".txt", toBeWritten);
	}


	//generate's values given a min and max and the list 
	//returnVal has length of nodesPerLayer.length - 1
	//assumption is that nodesPerLayer[0] is the inputLayer
	//returnVal[0] are the weights between the input layer and the first hidden layer
	//returnVal[0][0] is the weight between the inputlayer's first node and the first hidden layer's first node
	//returnVal[0][1] is the weight between the inputlayer's first node and the first hidden layer's second node
	//etc
	public static List<double[]> generateRandomNNWeights(int[] nodesPerLayer, double min, double max){
		List<double[]> returnVal = new List<double[]> ();
		for (int q = 0; q < nodesPerLayer.Length - 1; q++) {
			int lengthKo = nodesPerLayer [q] * nodesPerLayer [q + 1];
			double[] temp = new double[lengthKo];
			for (int w = 0; w < lengthKo; w++) {
				temp [w] = Random.Range((float)min, (float)max);
			}
			returnVal.Add (temp);
		}
		return returnVal;
	}
}


//actual game termination is seen in checkForTermin() and in roundmanager's update(), gameOver() is called by boths
public class GameManagerOld : MonoBehaviour{
	public static List<int[,]> maps;
	public bool forGA;
	List<StrangerAI> strangersThatMove;
	static List<StrangerAI> strangersPool;
	static Map map;
	Barkada tBC;
	HUDManager dm;
	RoundManager rm;
	bool paused, once;
	public AI ai, ai2;
	static GameManagerOld gm;
	float tempTimerKo;//generalTimer should not be reset, just modulo it if you want to know if 30 seconds have passed
	int numTeams, numRounds;
	GameObject roundMaster, minimap;
	public void removeStranger(StrangerAI sai){
		strangersPool.Remove (sai);
		strangersThatMove.Remove (sai);
		sai.disable ();
	}
	void Start(){
		tempTimerKo = 0;
		UtilsKo.iterationCount = 0;
		paused = false;
		once = true;
		numRounds = 0;

		strangersPool = new List<StrangerAI> ();
		strangersThatMove = new List<StrangerAI> ();
	
		//get the static version of this
		if (gm == null)
			gm = this.GetComponent<GameManagerOld> ();

		//initialize map
		this.gameObject.AddComponent<Map> ();
		map = this.gameObject.GetComponent<Map> ();
		maps = new List<int[,]> ();
		readMaps ();
		//map.initialize (30, 20, 4);
		Debug.Log("Hoy putangina ka talaga " + GetComponent<MenuManager> ().getMapSelected ());
		map.initialize (maps [GetComponent<MenuManager> ().getMapSelected ()], forGA);

		//initialize team
		numTeams = 2;
		for (int q = 0; q < numTeams; q++) {
			this.gameObject.AddComponent<Team> ();
		}

		//initialize barkada, get spawnpoint from map and give it to barkada, initialize characters then give spawnpoint
		for (int q = 0; q < numTeams; q++) {
			Team temp = this.gameObject.GetComponents<Team> () [q];
			temp.initialize (q, map.getSpawnPoint (), 0.25f, forGA);
			startBarkada (temp.getBarkada ());
		}
		tBC = this.gameObject.GetComponents<Team> () [0].getBarkada ();
	
		//instantiate the round manager after the teams
		if (!forGA) {
			rm = GameObject.Find ("GameRoundCanvas").GetComponentInChildren<RoundManager> ();
			rm.initMe (GetComponents<Team> (), this);
		} else {
			Time.timeScale = 2.0f;
		}

		//strangers are taken care of by table

		//get the HUD Panel
		dm = this.gameObject.GetComponent<HUDManager> ();

		//initialize the round manager
		if (!forGA) {
			roundMaster = GameObject.Find ("GameRoundCanvas");
			roundMaster.SetActive (false);
		}

		//instantiate mini-map
		if (!forGA) {
			minimap = GameObject.Find ("PauseCanvas");
			minimap.AddComponent<MiniMapManager> ();
			minimap.GetComponent<MiniMapManager> ().initMe (map);
		}

	
		this.gameObject.AddComponent<DQNAI> ();
		ai = GetComponent<DQNAI> ();
		ai.init (this, 1);
		ai.setDS (GetComponent<MenuManager> ().getPlayer1 ());

		if (forGA) {
			if (GetComponent<MenuManager> ().isSimpleTraining ()) {
				this.gameObject.AddComponent<SPAI> ();
				ai2 = GetComponent<SPAI> ();
				ai2.init (this, 0);

			} else {
				this.gameObject.AddComponent<DQNAI> ();
				ai2 = GetComponents<DQNAI> ()[1];
				ai2.init (this, 0);
				Debug.Log ("Ang kalaban ko ay diff: " + GetComponent<MenuManager> ().getDifficulty ());
				ai2.setDS (GetComponent<MenuManager> ().getDifficulty());
			}
			//para lang to sa DQNAI na ginagamit ko dati
			//ai2.setDS (GetComponent<MenuManager> ().getPlayer1 ());
		}
		Debug.Log ("Current Difficulty is " + ai.getDS ());
	}

	public void runGAKoBaby(){
			UpdateKo ();
	}

	public Tile getTile(int x, int y){
		return map.getTile (x, y);
	}

	public int getWidth(){
		return map.getWidth ();
	}

	public int getHeight(){
		return map.getHeight ();
	}

	void readMaps(){
		TextAsset baby = Resources.Load<TextAsset> ("MapsInput");
		string[] pinakaLabas = baby.text.Split ('|');
		for (int e = 0; e < pinakaLabas.Length; e++) {
			//Debug.Log (pinakaLabas [e]);
			string[] babyKo = pinakaLabas[e].Split ('\n');
			int[,] tempMapInput = new int[babyKo.Length - 1, babyKo [0].Split (' ').Length];
			for (int q = 0; q < babyKo.Length - 1; q++) {
				string[] tempKo = babyKo [q].Split (' ');
				for (int w = 0; w < tempKo.Length; w++) {
					//Debug.Log (tempKo [w]);
					tempMapInput [q, w] = int.Parse (tempKo [w]);
				}
			}
			maps.Add (tempMapInput);
		}
	}

	public Map getMap(){
		return map;
	}

	public int[,] getDisplayInformation(){
		return map.getEnvironmentDisplay ();
	}

	public float getTimeKo(){//always from zero to 30
		if (UtilsKo.iterationCount == 0) {
			return 0f;
		} else {
			int babyKo = UtilsKo.mod (UtilsKo.iterationCount, UtilsKo.roundLength);
			if (babyKo == 0) {
				return 30f;
			} else {
				return babyKo;
			}
		}
	}

	public bool isPaused(){
		return paused;
	}

	public void pauseMe(){
		enabled = false;
		foreach(Team t in GetComponents<Team>()){
			t.pauseMe ();
		}
		paused = true;
		//Debug.Log("Nothing should follow me");
		//GetComponent<GameManagerOld> ().enabled = false;
	}

	public void unPauseMe(){
		enabled = true;
		foreach(Team t in GetComponents<Team>()){
			t.unPauseMe ();
		}
		paused = false;
		//GetComponent<GameManagerOld> ().enabled = true;
	}

	public void gameOver(){
		//put data into menu manager
		GetComponent<MenuManager>().inputTeamData(GetComponents<Team>(), forGA ? -1 : ai.getDS());
		GetComponent<MenuManager> ().loadLevel(1);
	}

	public bool checkForTermin(){//usually called every time a barkada sits down on a table
		bool returnVal = false;
		//check if number of non-seated characters in teams is greater than available seats or vice versa
		int numPlayers = 0;
		int disabledTeams = 0;
		foreach (Team t in this.gameObject.GetComponents<Team>()) {
			if (t.getBarkada ().getHead () == null) {
				//Debug.Log ("Dito ko ilalagay ang code for team pausing");
				t.pauseMe ();
				disabledTeams++;
			}
			if (t.getSize () <= 0) {
				Debug.Log ("You won because a team has zero or less characters");
				gameOver ();
				returnVal = true;
				break;
			}
			numPlayers += t.getSize ();
		}
		if (disabledTeams == GetComponents<Team> ().Length) {
			//ayusin mo muna ang iteration counter
			if(UtilsKo.iterationCount == 0) 
				UtilsKo.iterationCount += 30;
			else 
				UtilsKo.iterationCount += (30 - UtilsKo.mod(UtilsKo.iterationCount, UtilsKo.roundLength));
			Debug.Log ("Diyos KO PO");
			finishRound ();
		}
		int numChairs = map.getNumChairs ();
		if (!returnVal && ((numPlayers > numChairs && numChairs == 0) || (numPlayers == 0 && numChairs > 0))) {
			Debug.Log ("Game stopped because numPlayers is greater than remaining chairs or all the chairs have been taken but there are still some players");
			gameOver ();
			returnVal = true;
			//determine the winner
		}
		if (returnVal) {
			finishRound ();
		}
		return returnVal;
	}

	public void newRound(){
		if (forGA) {
			if (++numRounds >= UtilsKo.maxRounds)
				gameOver ();
		}
		else roundMaster.SetActive (false);
		unPauseMe ();//dapat nandito or else baka walang mahanap yung GETCOMPONENTS
		foreach(Team ti in GetComponents<Team>()){
			ti.getNewBarkada ();
			startBarkada (ti.getBarkada());
		}
	}

	public void finishRound(){
		pauseMe ();
		//enable roundMaster, all data is already accessible by roundMaster, in case new data is needed, just put in roundMaster's init function
		if (forGA) newRound();
		else roundMaster.SetActive (true);

	}

	public int getOpponentHeading(int numNamin){
		int returnVal = -1;
		foreach (Team t in this.gameObject.GetComponents<Team>()) {
			if (t.getID () != numNamin) {
				returnVal = t.getBarkada ().getHead () == null ? -1 : t.getBarkada ().getHead ().getHeading ();
				break;
			}
		}
		return returnVal;
	}

	//this only checks for seat-based termination, nasa update() yung checking for time based termination
	public static bool checkForTermination(){//true if terminated, false if not terminated
		return gm.checkForTermin();
	}

	public static void startBarkada(Barkada b){
		b.start (b.getTeam().getSpawnPoint());
	}

	public void move(int y, Barkada tBC1){
		if (!tBC1.isDisabled()) {
			if (y == 0) {
				tBC1.move (0, map.getNewTile (tBC1.getHead ().getHeading (), tBC1.getCurrentHeadTile ()));
			} else if (y == 1) {//right
				tBC1.move (1, map.getNewTile (UtilsKo.mod (tBC1.getHead ().getHeading () - 1, 4), tBC1.getCurrentHeadTile ()));
			} else if (y == 2) {
				tBC1.move (3, map.getNewTile (UtilsKo.mod (tBC1.getHead ().getHeading () + 1, 4), tBC1.getCurrentHeadTile ()));
			}
		}
	}

	public static float getTime(){
		return gm.getTimeKo ();
	}

	public Tile getNewTile(int dir, Tile current){//used by strangers
		return map.getNewTile(dir, current);
	}

	public string getDisplayableTime(){
		string returnVal = UtilsKo.iterationCount / 60 + ":" + UtilsKo.mod (UtilsKo.iterationCount, 60);
		return returnVal;
	}

	void Update(){
		if(!forGA){
			//process inputs
			if (Input.GetKeyDown (KeyCode.LeftArrow)) {
				move (2, tBC);
				//Debug.Log ("Clicked left");
			} else if (Input.GetKeyDown (KeyCode.RightArrow)) {
				move (1, tBC);
				//Debug.Log("Right");
			}
			//Debug.Log (UtilsKo.iterationCount + " hohoho");

			//to regulate the speed of movement in the actual game
			if (tempTimerKo > 1.00f) {
				UpdateKo ();
				tempTimerKo = 0.0f;
			}else {
				tempTimerKo += Time.deltaTime;
			}

			//update the HUDPanel
			if (dm == null) {
				dm = GetComponent<HUDManager> ();
			}
			dm.setText (0, "Team 1: " + GetComponents<Team> () [0].getQueueSize ());
			dm.setText (1, getDisplayableTime ());
			dm.setText (2, "Team 2: " + GetComponents<Team> () [1].getQueueSize ());

			//update the map
			minimap.GetComponent<MiniMapManager>().updateMe(map.getEnvironmentDisplay());
		}
	}

	void UpdateKo () {
		//Debug.Log (UtilsKo.iterationCount);
		//ai behaviors
		if (ai != null) ai.UpdateKo();
		if (ai2 != null) ai2.UpdateKo ();
		foreach (Team i in GetComponents<Team>()) if(!(i.isPaused())) move (0, i.getBarkada());

		//stranger behaviors
		if (strangersThatMove.Count > 0) {
			strangersThatMove [0].think ();
			strangersThatMove [0].doIt ();
		}
		if (UtilsKo.mod (UtilsKo.iterationCount, 5) == 4) {//meaning every 60 seconds magsisimulang gumalaw ang isang stranger
			if (strangersThatMove.Count == 0 && strangersPool.Count > 0) {
				int tabIndex = Random.Range (0, strangersPool.Count);
				strangersPool [tabIndex].startMoving (map.getSpawnPoint ());
				strangersThatMove.Add (strangersPool [tabIndex]);
			}
			//dito dapat code for stranger exiting
		}

		//check for round termination based on time
		if (getTimeKo () == UtilsKo.roundLength) {
			if (once) {//kailangan tong if block na to para mapigilan siyang magexecute ng mmarami sa second na "30"
				//put all unseated characters back to their respective teams
				//Debug.Log ("Pinapasok ko na naman to");
				foreach (Team ti in GetComponents<Team>()) {
					if (!ti.getBarkada ().isDisabled ()) {
						//isplit mo yung barkada dahil babalik naman yung character sa team kapag na disable siya
						Debug.Log("naubusan ng oras mehn");
						ti.getBarkada ().splitMe (ti.getBarkada ().getHead ());//putulin sila based sa head nila
						//at iset yung time nila as 30 secs if hindi siya na split
						ti.setDeltaScore (-1f);
						if (ti.getBarkada ().getTime () == 0f)
							ti.getBarkada ().saveTime (getTimeKo ());
					}
				}
				finishRound ();
				//Debug.Log ("dito dapat ilagay ang code for termination based on time");
				//dapat iclick yung button sa gameroundcanvas para magpatuloy yung game
				//newRound ();
				once = false;
			}
			//Debug.Log ("Time is " + getTimeKo ());
		} else {
			//Debug.Log ("Ang time ngayon ay " + getTimeKo ());
			//Debug.Log ("Per ang nakuha ng mod ay " + UtilsKo.mod ((int)getTimeKo (), 31));
			once = true;
		}

		UtilsKo.iterationCount++;
	}
	public static void makeAStranger(Tile tile1, Table tableKo){
		GameObject temp = (GameObject)Instantiate (Resources.Load ("Prefabs/StrangerPrefab", typeof(GameObject)));
		strangersPool.Add(temp.GetComponent<Character> ().setUpStranger (gm, tableKo));//dapat mauna ang setUpStranger dahil kinacall nito to ang startProperly() ng character na ginamit dito
		temp.GetComponent<Character> ().initializeMe (tile1, null);
	}
	public int getDirections(Tile s, Tile d){
		//Debug.Log ("Pumasok sa game manager ang source tile at " + s.getMapIndex ().x + ", " + s.getMapIndex ().y + " at " + d.getMapIndex ().x + ", " + d.getMapIndex ().y + " with width of " + map.getWidth());
		int returnVal = map.fromDirectionalToReal (s, map.getProjectedNextTile((int)((s.getMapIndex().y * map.getWidth()) + s.getMapIndex().x), (int)((d.getMapIndex().y * map.getWidth()) + d.getMapIndex().x)));
		//Debug.Log ("Please turn " + returnVal);
		return returnVal;
	}
}

public class Map : MonoBehaviour{//Monobehaviour kasi kailangan ko yung "Instantiate" function
	GameObject[,] tiles;//x, y format
	GameObject[] spawnPoints;//size is determined in init statement
	int[,] dM;
	Table[] tables;
	int xQuant, yQuant, numTables;
	public int getNumChairs(){
		int returnVal = 0;
		foreach(Table t in tables){
			returnVal += t.getNumChairs();
		}
		return returnVal;
	}
	public Table[] getTables(){
		return tables;
	}
	public Tile getProjectedNextTile(int source, int destination){
		//Debug.Log ("trying to find way from " + source + " to " + destination);
		int tileIndex = dM [source, destination]; 
		//Debug.Log (tiles.GetLength (0) + "; " + tiles.GetLength (1) + "====" + tileIndex + "-" + (tileIndex / xQuant) + "--------:" + tileIndex);
		//Debug.Log ("go to tileIndex " + tileIndex);
		return tiles [tileIndex % xQuant, (int)(tileIndex / xQuant)].GetComponent<Tile>();
	}
	public int fromDirectionalToReal(Tile source, Tile desti){
		Vector3 sourceKo = source.getMapIndex ();
		Vector3 destiKo = desti.getMapIndex ();
		if (sourceKo.x > destiKo.x && sourceKo.y == destiKo.y) {//to the left
			return 3;
		} else if (sourceKo.x < destiKo.x && sourceKo.y == destiKo.y) {
			return 1;
		} else if (sourceKo.y > destiKo.y) {
			return 2;
		}else {
			//Debug.Log ("s = " + sourceKo.x + ", " + sourceKo.y + " d = " + destiKo.x + ", " + destiKo.y);
			return 0;
		}
	}
	public Tile getNewTile(int dir, Tile current){//1 is right, 2 is forward, 3 is left
		Vector3 currIndex = current.getMapIndex();
		Vector3 vari = currIndex;
		//getting the next tile
		if (dir == 0) {//down
			if (currIndex.y + 1 < yQuant) {
				vari.y = currIndex.y + 1;
				//return tiles [(int)currIndex.x, (int)currIndex.y + 1].GetComponent<Tile>();
			}
		} else if (dir == 1) {//right
			if (currIndex.x + 1 < xQuant) {
				vari.x = currIndex.x + 1;
				//return tiles [(int)currIndex.x + 1, (int)currIndex.y].GetComponent<Tile>();
			}
		} else if (dir == 2) {//up
			if (currIndex.y - 1 >= 0) {
				vari.y = currIndex.y - 1;
				//return tiles [(int)currIndex.x, (int)currIndex.y - 1].GetComponent<Tile>();
			}
		} else {//left
			if (currIndex.x - 1 >= 0) {
				vari.x = currIndex.x - 1;
				//return tiles [(int)currIndex.x - 1, (int)currIndex.y].GetComponent<Tile>();
			}
		}

		//check if the next tile is a background tile
		if (tiles [(int)vari.x, (int)vari.y].GetComponent<Tile> ().getPointVal () == 0 || tiles [(int)vari.x, (int)vari.y].GetComponent<Tile> ().getPointVal () == 3) {
			return tiles [(int)vari.x, (int)vari.y].GetComponent<Tile> ();
		}else {
			return current;
		}
	}

	public int[,,] getFeatureVectors(int teamID){
		int[,,] returnVal = new int[xQuant, yQuant, 4];
		int[] tempoKoTo;
		for (int q = 0; q < xQuant; q++) {
			for (int w = 0; w < yQuant; w++) {
				tempoKoTo = tiles [q, w].GetComponent<Tile> ().getFeatureVector ();
				for (int e = 0; e < 4; e++) {
					returnVal [q, w, e] = tempoKoTo [e];
				}
				returnVal [q, w, 3] = returnVal [q, w, 3] == teamID ? 0 : returnVal[q, w, 2] == 0 ? 0 : 1;
			}
		}
		return returnVal;
	}

	public int[,] getEnvironmentDisplay(){
		int[,] returnVal = new int[xQuant, yQuant];
		Tile temp;
		for (int q = 0; q < xQuant; q++) {
			for (int w = 0; w < yQuant; w++) {
				temp = tiles [q, w].GetComponent<Tile> ();
				if (temp.getLaman () != null) {
					if (temp.getLaman ().getTeamID () == -1)
						returnVal [q, w] = 10;
					else returnVal [q, w] = temp.getLaman ().getTeamID () + 6;
				}
				else{ 
					returnVal [q, w] = temp.getPointVal ();
				}
			}
		}
		return returnVal;
	}

	public void initializeDirectionalMap(){
		string inputInfo = "";
		try{
			inputInfo = System.IO.File.ReadAllText (UtilsKo.directionalMapFilePath + GetComponent<MenuManager>().getMapSelected() + ".txt");
		} catch{

		}
		if (inputInfo.Equals ("")) {
			//make mapa
			int[,] mapa = new int[yQuant, xQuant];
			for (int q = 0; q < yQuant; q++) {
				for (int w = 0; w < xQuant; w++) {
					int pV = tiles [w, q].GetComponent<Tile> ().getPointVal ();
					mapa [q, w] = (pV == 4 || pV == 5) ? -1 : 0;
				}
			}

			int numTiles = mapa.GetLength (0) * mapa.GetLength (1);
			Vector2[,] directionalMap = new Vector2[numTiles, numTiles];//2D array ng vector2 dapat to in c#
			//instantiate the directionalMap
			for (int q = 0; q < numTiles; q++) {
				for (int w = 0; w < numTiles; w++) {
					directionalMap [q, w] = new Vector2 (-2, -2);
				}
			}


			//build directionalMap
			//firstPass, build the initial directionalMap
			for (int q = 0; q < mapa.GetLength (0); q++) {
				for (int w = 0; w < mapa.GetLength (1); w++) {
					//q, w is the y, x coordinate of the source tile
					int dSI = (q * mapa.GetLength (1)) + w;

					//e, r is the y, x coordinate of the destination tile
					for (int e = 0; e < mapa.GetLength (0); e++) {
						for (int r = 0; r < mapa.GetLength (1); r++) {
							int dDI = (e * mapa.GetLength (1)) + r;
							if (dSI == dDI || mapa [e, r] == -1 || mapa [q, w] == -1) {//checks kung pwede mapuntahan yung tile
								directionalMap [dSI, dDI] = new Vector2 (-1, -1);
							} else if (dSI + mapa.GetLength (1) == dDI || dSI - mapa.GetLength (1) == dDI || (dSI % mapa.GetLength (1) != 0 && dSI - 1 == dDI) || (dSI % mapa.GetLength (1) != mapa.GetLength (1) - 1 && dSI + 1 == dDI)) {//nasa baba, nasa taas, nasa kaliwa, nasa kanan
								directionalMap [dSI, dDI] = new Vector2 (dDI, 1);
							}
						}
					}
				}
			}

			//fill all the other blanks
			for (int q = 0; q < mapa.GetLength (0); q++) {
				for (int w = 0; w < mapa.GetLength (1); w++) {
					//q, w is the y, x coordinate of the source tile
					int dSI = (q * mapa.GetLength (1)) + w;

					//e, r is the y, x coordinate of the destination tile
					for (int e = 0; e < mapa.GetLength (0); e++) {
						for (int r = 0; r < mapa.GetLength (1); r++) {
							int dDI = (e * mapa.GetLength (1)) + r;
							if (directionalMap [dSI, dDI].y == -2) {
								//iplug ang mga nasa row dDI whose [1] == 1
								//technically dapat queue ginagamit dito e
								//System.out.println("Looking for path from " + dSI + " to " + dDI);
								Queue<int[]> queue = new Queue<int[]> ();
								int tempDSI = dSI;
								int[] currentArr = new int[1];
								currentArr [0] = 0;
								while (true) {
									//push the adjacents
									for (int y = 0; y < numTiles; y++) {
										if (directionalMap [tempDSI, y].y == 1) {
											//first check if y is already inside the queue
											if (y == dSI)
												continue;
											bool nasaLoobNa = false;
											foreach (int[] arrayKo in queue) {
												bool kailangangLumabas = false;
												for (int u = 0; u < arrayKo.Length - 1; u++) {
													if (arrayKo [u] == y) {
														nasaLoobNa = true;
														kailangangLumabas = true;
														break;
													}
												}
												if (kailangangLumabas)
													break;
											}
											if (nasaLoobNa)
												continue;

											//proceed to add the newarray
											int[] newArr = new int[currentArr.Length + 1];
											newArr [currentArr.Length] = currentArr [currentArr.Length - 1] + 1;//put the new weight
											newArr [currentArr.Length - 1] = y;//put the additional tile
											for (int t = 0; t < (currentArr.Length - 1); t++) {//put the original ones
												newArr [t] = currentArr [t];
											}
											queue.Enqueue (newArr);
										}
									}

									//pop the first in the array
									int[] toProc = queue.Dequeue ();
									//check if its the destination
									if (toProc [toProc.Length - 2] == dDI) {
										//set the [0] and [1]
										directionalMap [dSI, dDI] = new Vector2 (toProc [0], toProc [toProc.Length - 1]);
										break;
									} else {
										tempDSI = toProc [toProc.Length - 2];
										currentArr = toProc;
									}
								}
							}
							//else{}, wala na tong else e
						}
					}
				}
			}

			//transform the directional map
			dM = new int[numTiles, numTiles];
			string toBeSaved = "";
			for (int q = 0; q < numTiles; q++) {
				for (int w = 0; w < numTiles; w++) {
					dM [q, w] = (int)directionalMap [q, w].x;
					toBeSaved += dM [q, w] + (w == numTiles - 1 ? "" : " ");
				}
				toBeSaved += (q == numTiles - 1 ? "" : "\n");
			}
			System.IO.File.WriteAllText (UtilsKo.directionalMapFilePath + GetComponent<MenuManager>().getMapSelected() + ".txt", toBeSaved);
		} else {
			//read from file
			string[] lines = inputInfo.Split('\n');
			string[] characters = lines [0].Split (' ');
			dM = new int[lines.Length, characters.Length];
			for (int q = 0; q < lines.Length; q++) {
				characters = q == 0 ? characters : lines [q].Split (' ');
				for (int w = 0; w < characters.Length; w++) {
					dM [q, w] = int.Parse (characters [w]);
				}
			}
		}
	}

	//sobrang fucked up pa ng pointVal system
	public void initialize(int[,] blueprint, bool forGA){//just follow x, y convention system but blueprint follows y, x convention
		yQuant = blueprint.GetLength (0);
		xQuant = blueprint.GetLength(1);
		tiles = new GameObject[xQuant, yQuant];
		List<Table> tempTables = new List<Table> ();
		spawnPoints = new GameObject[4];
		int spawnIndex = 0;

		//create the blueprint graphically
		Object backOrig = Resources.Load ("Prefabs/Tile");
		Object chairOrig = Resources.Load ("Prefabs/SeatPrefab");
		Object tableOrig = Resources.Load ("Prefabs/Table");
		Object emptyBoi = Resources.Load ("Prefabs/Empty");
		float initY = 0.0f;
		int spawnIndexKo = 0;
		Vector3 curPos = new Vector3(0, 0, 0);
		for (int q = 0; q < yQuant; q++) {
			float initX = 0.0f;
			for (int w = 0; w < xQuant; w++) {
				if (tiles [w, q] != null)
					continue;
				switch (blueprint [q, w]) {//for graphical tile instantiation
				case 0://normal tile
					tiles [w, q] = (GameObject)Instantiate (forGA ? emptyBoi : backOrig);
					break;
				case 1://obstacled tile
					break;
				case 2://spawnpoint
					tiles [w, q] = (GameObject)Instantiate (forGA ? emptyBoi : backOrig);
					spawnPoints[spawnIndex++] = tiles[w, q];
					break;
				case 3://table entry point, signals start of table generation
					tiles [w, q] = (GameObject)Instantiate (forGA ? emptyBoi : backOrig);
					break;
				case 4://chair
					tiles [w, q] = (GameObject)Instantiate (forGA ? emptyBoi : chairOrig);
					break;
				case 5://table
					tiles [w, q] = (GameObject)Instantiate (forGA ? emptyBoi : tableOrig);
					break;
				default:
					Debug.Log ("Wtf is this?");
					break;
				}
				tiles [w, q].AddComponent<Tile> ();
				tiles [w, q].GetComponent<Tile> ().initialize (blueprint[q, w], w, q);
				if (blueprint [q, w] == 2){
					tiles [w, q].GetComponent<Tile> ().setSpawn (spawnIndexKo == 1 ? 3 : spawnIndexKo == 2 ? 1 : spawnIndexKo == 3 ? 2 : 0);
					spawnIndexKo++;
				}
				curPos.x = initX;
				curPos.y = initY;
				tiles [w, q].transform.position = curPos;
				initX -= forGA ? UtilsKo.tileW : tiles [w, q].GetComponent<BoxCollider> ().bounds.size.x;
			}
			initY -= forGA ? UtilsKo.tileH : tiles [0, q].GetComponent<BoxCollider> ().bounds.size.y;
		}


		//group the table elements together
		//find a table
		for (int q = 0; q < xQuant; q++) {
			for (int w = 0; w < yQuant; w++) {
				if (blueprint [w, q] == 5) {
					//find table width
					int tableWidth = 3;
					for (int o = q + 1; o < xQuant; o++) {
						if (blueprint [w, o] != 5) {
							tableWidth += o - q - 1;
							break;
						} else {
							blueprint [w, o] = -1;//disable the == 5 thing
						}
					}
					int tableHeight = 3;
					for (int o = w + 1; o < yQuant; o++) {
						if (blueprint [o, q] != 5) {
							tableHeight += o - (w + 1);
							break;
						} else {
							blueprint [o, q] = -1;//disable the == 5 thing
						}
					}

					//load all table tiles here
					//Debug.Log("Size of table is " + tableWidth + ", " + tableHeight);
					GameObject[] tableTiles = new GameObject[tableWidth * tableHeight];
					int index = 0;
					for (int o = w - 1; o < (w - 1 + tableHeight); o++) {
						for (int p = q - 1; p < (q - 1 + tableWidth); p++) {
							//check for final stray 5s
							if(blueprint[o, p] == 5) blueprint[o, p] = -1;
							tableTiles [index++] = tiles [p, o];
						}
					}

					tempTables.Add(new Table (tableTiles, tableWidth - 2, tableHeight - 2));
					//takeCare of entry points
					//Debug.Log("I made a table");
				}
			}
		}
		tables = tempTables.ToArray ();
		initializeDirectionalMap ();
	}

	public void initialize(int xxQ, int yyQ, int tN){
		xQuant = xxQ;
		yQuant = yyQ;
		numTables = tN;

		spawnPoints = new GameObject[4];
		int[,] spawnInds = new int[,] { { 0, 0 }, { 0, yQuant - 1 }, { xQuant - 1, yQuant - 1 }, { xQuant - 1, 0 }};//in this order para madali lagyan ng direction
		//Debug.Log(spawnInds[0, 1]);

		tiles = new GameObject[xQuant, yQuant];
		tables = new Table[numTables];

		//preprocessing requirements for tiles initialization
		GameObject sample = (GameObject)Instantiate (Resources.Load("Prefabs/Tile", typeof(GameObject)));
		Vector3 samSize = sample.GetComponent<BoxCollider> ().bounds.size;
		float initX = (0f + (samSize.x * ((float)xQuant / 2f))) + (samSize.x / 2f);
		float initY = (samSize.y * ((float)yQuant / 2f)) - (samSize.y / 2f);
		Destroy (sample);

		//init the tiles 2D array
		for (int q = 0; q < yQuant; q++) {
			float tempInitX = initX;
			for (int w = 0; w < xQuant; w++) {
				tiles[w, q] = (GameObject)Instantiate (Resources.Load("Prefabs/Tile", typeof(GameObject)));
				tiles [w, q].name = w + " + " + q;
				Vector3 temp = tiles [w, q].transform.position;
				temp.x = tempInitX;
				temp.y = initY;
				tiles [w, q].AddComponent<Tile> ();
				tiles [w, q].GetComponent<Tile> ().initialize (0, w, q);
				tiles [w, q].transform.position = temp;
				tempInitX -= samSize.x;
			}
			initY -= samSize.y;
		}


		//init the spawnPoints
		for (int q = 0; q < spawnPoints.Length; q++) {
			tiles [spawnInds [q, 0], spawnInds [q, 1]] = switcheroo (tiles [spawnInds [q, 0], spawnInds [q, 1]], (GameObject)Instantiate (Resources.Load("Prefabs/SpawnPoint", typeof(GameObject))));
			spawnPoints [q] = tiles [spawnInds [q, 0], spawnInds [q, 1]];
			tiles [spawnInds [q, 0], spawnInds [q, 1]].AddComponent<Tile> ();
			tiles [spawnInds [q, 0], spawnInds [q, 1]].GetComponent<Tile> ().setSpawn (q);
			tiles [spawnInds [q, 0], spawnInds [q, 1]].GetComponent<Tile> ().initialize (2, spawnInds [q, 0], spawnInds [q, 1]);
		}

		//init the tables
		for (int q = 0; q < numTables; q++) {
			int tableSizeX = Random.Range (1, 3);
			int tableSizeY = Random.Range (2, 6);
			//Debug.Log ("Got " + tableSizeX + " " + tableSizeY);
			GameObject[] tabletiles = new GameObject[tableSizeX * tableSizeY];

			bool cond = true; 
			int counter = 30;//max times ng iteration, ata
			int cc = 0;
			int index, xPos, yPos;
			while (cond) {
				if (cc++ % (counter + 1) == counter) {
					if (cc > 100)
						cond = false;
					tableSizeX = Random.Range (1, 3);
					tableSizeY = Random.Range (2, 6);
					tabletiles = new GameObject[tableSizeX * tableSizeY];
				}
				//get possible top-left coordinates of table with allowance for the placement of chairs(2)
				index = 0;
				xPos = Random.Range (0, xQuant - tableSizeX - 4);
				yPos = Random.Range (0, yQuant - tableSizeY - 4);

				//test if these coordinates will fit the table
				bool cond2 = true;//break outerloop replacement
				for (int w = xPos; w < xPos + tableSizeX + 4 && cond2; w++) {
					for (int e = yPos; e < yPos + tableSizeY + 4; e++) {
						if ((w == xPos || w == xPos + 1 || e == yPos || e == yPos + 1 || e == yPos + tableSizeY + 3 || e == yPos + tableSizeY + 2 || w == xPos + tableSizeX + 3 || w == xPos + tableSizeX + 2)) {
							//meaning nasa gilid
							if (tiles [w, e].GetComponent<Tile> ().getPointVal () != 0) {
								cond2 = false;
								break;
							}
						}
						else if (tiles [w, e].GetComponent<Tile> ().getPointVal () == 0) {
							tabletiles [index++] = tiles [w, e];
						} else {
							cond2 = false;
							break;
						}
					}
				}

				if (cond2) {
					//meaning it found a combination that perfectlt matches its needs
					break;
				}
			}

			//load the found tiles into a single array array of tiles to be given to the table class
			for (int w = 0; w < tabletiles.Length; w++) {
				Vector3 baby = tabletiles [w].GetComponent<Tile> ().getMapIndex ();
				tabletiles [w] = switcheroo (tabletiles [w], (GameObject)Instantiate (Resources.Load ("Prefabs/Table", typeof(GameObject))));
				tabletiles [w].AddComponent<Tile> ();
				tabletiles [w].GetComponent<Tile> ().initialize (5, (int)baby.x, (int)baby.y);
				tabletiles [w].GetComponent<Tile> ().setOccupied (true);
				tabletiles [w].name = tableSizeX + " x " + tableSizeY + " : " + (int)baby.x + " - " + (int)baby.y;
				tiles [(int)baby.x, (int)baby.y] = tabletiles [w];
				//Debug.Log ("I am at " + baby.x + " " + baby.y);
			}

			//initialize the chairs and the entry points
			int xx = (int)tabletiles [0].GetComponent<Tile>().getMapIndex().x;
			int yy = (int)tabletiles [0].GetComponent<Tile>().getMapIndex().y;

			//entry points
			tiles [xx - 1, yy - 1].GetComponent<Tile> ().initialize (3, xx - 1, yy - 1);
			tiles [xx - 1, yy + tableSizeY].GetComponent<Tile> ().initialize (3, xx - 1, yy + tableSizeY);
			tiles [xx + tableSizeX, yy - 1].GetComponent<Tile> ().initialize (3, xx + tableSizeX, yy - 1);
			tiles [xx + tableSizeX, yy + tableSizeY].GetComponent<Tile> ().initialize (3, xx + tableSizeX, yy + tableSizeY);
			GameObject[] entries = new GameObject[4];
			entries [0] = tiles [xx - 1, yy - 1];
			entries [1] = tiles [xx - 1, yy + tableSizeY];
			entries [2] = tiles [xx + tableSizeX, yy - 1];
			entries [3] = tiles [xx + tableSizeX, yy + tableSizeY];

			//chairs
			GameObject[] chairs = new GameObject[(tableSizeX * 2) + (tableSizeY * 2)];
			int indexCounter = 0;
			for (int w = yy; w < yy + tableSizeY; w++) {
				tiles [xx - 1, w] = switcheroo (tiles [xx - 1, w], (GameObject)Instantiate (Resources.Load ("Prefabs/SeatPrefab", typeof(GameObject))));
				tiles [xx - 1, w].AddComponent<Tile> ();
				tiles [xx - 1, w].GetComponent<Tile> ().initialize (4, xx - 1, w);
				chairs [indexCounter++] = tiles [xx - 1, w];
			}
			for (int w = xx; w < xx + tableSizeX; w++) {
				tiles [w, yy - 1] = switcheroo (tiles [w, yy - 1], (GameObject)Instantiate (Resources.Load ("Prefabs/SeatPrefab", typeof(GameObject))));
				tiles [w, yy - 1].AddComponent<Tile> ();
				tiles [w, yy - 1].GetComponent<Tile> ().initialize (4, w, yy - 1);
				chairs [indexCounter++] = tiles [w, yy - 1];
			}
			for (int w = yy; w < yy + tableSizeY; w++) {
				tiles [xx + tableSizeX, w] = switcheroo (tiles [xx + tableSizeX, w], (GameObject)Instantiate (Resources.Load ("Prefabs/SeatPrefab", typeof(GameObject))));
				tiles [xx + tableSizeX, w].AddComponent<Tile> ();
				tiles [xx + tableSizeX, w].GetComponent<Tile>().initialize(4,xx + tableSizeX, w);
				chairs [indexCounter++] = tiles [xx + tableSizeX, w];
			}
			for (int w = xx; w < xx + tableSizeX; w++) {
				tiles [w, yy + tableSizeY] = switcheroo (tiles [w, yy + tableSizeY], (GameObject)Instantiate (Resources.Load ("Prefabs/SeatPrefab", typeof(GameObject))));
				tiles [w, yy + tableSizeY].AddComponent<Tile> ();
				tiles [w, yy + tableSizeY].GetComponent < Tile> ().initialize (4, w, yy + tableSizeY);
				chairs [indexCounter++] = tiles [w, yy + tableSizeY];
			}


			tables [q] = new Table (tabletiles, entries, chairs, tableSizeX, tableSizeY);
		}

		for (int q = 0; q < yQuant; q++) {
			for (int w = 0; w < xQuant; w++) {
				tiles [w, q].GetComponent<Tile> ().setName ();
			}
		}
		initializeDirectionalMap();
	}

	public Tile getTile(int x, int y){
		return tiles [y, x].GetComponent<Tile> ();
	}

	public Tile getSpawnPoint(){
		int baby = Random.Range (0, spawnPoints.Length);
		while (true) {
			if(spawnPoints[baby].GetComponent<Tile>().getLaman() == null) return spawnPoints [baby].GetComponent<Tile>();
			else baby = Random.Range (0, spawnPoints.Length);
		}
	}
	public int getHeight(){
		return yQuant;
	}
	public int getWidth(){
		return xQuant;
	}
	public GameObject switcheroo(GameObject a, GameObject b){//switches a with b
		Vector3 temp = a.transform.position;
		Destroy (a);
		a = b;
		a.transform.position = temp;
		return a;
	}
}

public class Table{
	int availSeats;
	GameObject[] tableTiles, entryPoints, chairs;
	public Tile getAnEntryPoint(){
		int baby = Random.Range (0, 4);
		return entryPoints [baby].GetComponent<Tile> ();
	}
	public void addASeat(){
		availSeats++;
	}
	public GameObject[] getEntries(){
		return entryPoints;
	}
	public Table(GameObject[] nb, int xS, int yS){//ayusin ang pointVal flags ng tiles
		tableTiles = nb;
		entryPoints = new GameObject[4];
		List<GameObject> babyChairs = new List<GameObject> ();

		int entryIndexes = 0;
		for (int q = 0; q < tableTiles.Length; q++) {
			int getType = tableTiles [q].GetComponent<Tile> ().getPointVal ();
			if (getType == 3) {
				entryPoints [entryIndexes++] = tableTiles [q];
			} else if (getType == 4) {
				tableTiles [q].GetComponent<Tile> ().setOccupied (false);
				babyChairs.Add (tableTiles [q]);
			}
			tableTiles [q].GetComponent<Tile> ().setTable (this);
		}
		//Debug.Log ("I instantiated " + entryIndexes + " entry points");
		chairs = babyChairs.ToArray ();
		availSeats = chairs.Length;

		//take care of strangers
		int randomBlah = Random.Range(0, (int)(chairs.Length/2));
		for (int q = 0; q < randomBlah; q++) {
			//get random chair index
			while (true) {
				int randomIndex = Random.Range (0, chairs.Length);
				if (!chairs [randomIndex].GetComponent<Tile> ().isOccupied ()) {
					GameManagerOld.makeAStranger (chairs [randomIndex].GetComponent<Tile> (), this);
					chairs [randomIndex].GetComponent<Tile> ().setOccupied (true);
					break;
				}
			}
			availSeats--;
		}
		//Debug.Log (nb[0].GetComponent<Tile>().getMapIndex().x + " " + nb[0].GetComponent<Tile>().getMapIndex().y);
		//Debug.Log ("I am " + xS + " " + yS);
	}
	public Table(GameObject[] nb, GameObject[] ent, GameObject[] ch, int xS, int yS){
		tableTiles = nb;
		entryPoints = ent;
		chairs = ch;
		availSeats = chairs.Length;

		for (int q = 0; q < entryPoints.Length; q++) {
			entryPoints [q].GetComponent<Tile> ().setTable (this);
		}
		//set the chairs
		for(int q = 0; q < chairs.Length; q++){
			chairs [q].GetComponent<Tile> ().setTable (this);
			chairs [q].GetComponent<Tile> ().setOccupied (false);
		}

		//take care of strangers
		int randomBlah = Random.Range(0, (int)(chairs.Length/2));
		for (int q = 0; q < randomBlah; q++) {
			//get random chair index
			while (true) {
				int randomIndex = Random.Range (0, chairs.Length);
				if (!chairs [randomIndex].GetComponent<Tile> ().isOccupied ()) {
					GameManagerOld.makeAStranger (chairs [randomIndex].GetComponent<Tile> (), this);
					chairs [randomIndex].GetComponent<Tile> ().setOccupied (true);
					break;
				}
			}
		}
		//Debug.Log (nb[0].GetComponent<Tile>().getMapIndex().x + " " + nb[0].GetComponent<Tile>().getMapIndex().y);
		//Debug.Log ("I am " + xS + " " + yS);
	}
	public int getNumChairs(){//gets available number of seats
		int returnVal = 0;
		for (int q = 0; q < chairs.Length; q++) {
			if (!(chairs [q].GetComponent<Tile> ().isOccupied ()))
				returnVal += 1;
		}
		return returnVal;
	}
	public void resetNgKonti(){
		for (int q = 0; q < entryPoints.Length; q++) {
			//since wala naman masyadong nagbabago in terms of graphics between an obstacled tile and normal tile, ito na lang
			entryPoints [q].GetComponent<Tile> ().setPointVal(3);
			//entryPoints [q].GetComponent<Tile> ().initialize (1, (int)entryPoints [q].GetComponent<Tile> ().getMapIndex().x, (int)entryPoints [q].GetComponent<Tile> ().getMapIndex().y);
		}
	}
	public void seatCharacters(Character head){
		Character temp = head;
		Character temp2 = temp;
		Barkada tropa= head.getBarkada ();
		int numOfNaiupo = 0;
		for (int q = 0; q < chairs.Length && temp != null; q++) {
			if (!chairs [q].GetComponent<Tile> ().isOccupied ()) {
				temp.getCurrentTile ().setLaman (null);
				temp.setTile (chairs [q].GetComponent<Tile> ());//set chair as tile of character
				chairs [q].GetComponent<Tile> ().setLaman (temp);//set character as char of tile
				temp2 = temp.getNext ();
				temp.setNext (null);
				temp.name = "Naiupo na ako";
				temp = temp2;
				chairs [q].GetComponent<Tile> ().setOccupied (true);
				availSeats--;
				numOfNaiupo++;
			}
		}
		if (temp == null) {
			//meaning naiupo lahat
			//iflag mo na lang muna sa barkada na tapos na sila by putting null in barkada
			tropa.setCharacters(null);
			//Debug.Log ("Lagay ng disability ng barkada: " + tropa.isDisabled());
			//Debug.Log ("Kukuha na ako ng bago");
		} else {
			//meaning may natira
			//ipakontrol ulit sa player yung mga natitira
			tropa.getTeam().setDeltaScore(-5f);
			tropa.getTeam().addSplit();
			tropa.setCharacters (temp);
		}
		if (availSeats <= 0) {
			//turn every entry point into an obstacle
			for (int q = 0; q < entryPoints.Length; q++) {
				//since wala naman masyadong nagbabago in terms of graphics between an obstacled tile and normal tile, ito na lang
				entryPoints [q].GetComponent<Tile> ().setPointVal(1);
				//entryPoints [q].GetComponent<Tile> ().initialize (1, (int)entryPoints [q].GetComponent<Tile> ().getMapIndex().x, (int)entryPoints [q].GetComponent<Tile> ().getMapIndex().y);
			}
		}
		System.IO.File.AppendAllText (UtilsKo.gameLogsFilePath, "I have seated " + numOfNaiupo + " characters from Team #" + head.getTeamID() + " giving me " + availSeats + " open seats\n");
		//Debug.Log ("I have " + availSeats + " # of seats left");
		float calculatedScore = (numOfNaiupo * (30f - GameManagerOld.getTime())/30f) - (temp == null ? 0 : 1);
		tropa.getTeam ().addScore (calculatedScore);
		tropa.saveTime (GameManagerOld.getTime ());
		if (GameManagerOld.checkForTermination ()) {//nagcacause ng premature termination to
			//Debug.Log ("It is finished");
			return;
		}
		//Debug.Log ("Naipasok ko na lahat ng kasya");
	}
}

public class AI : MonoBehaviour{//mono dahil kailangan ng sariling update method para makapag-isip ng maayos ang AI
	protected GameManagerOld gm;
	protected int currentAction;
	bool initialized = false;
	protected Team toControl;
	public virtual void init(GameManagerOld g, int teamNumToControl){
		gm = g;
		initialized = true;
		currentAction = 0;//idle
		toControl = gm.gameObject.GetComponents<Team> () [teamNumToControl];
	}
	public virtual void think(){//choose a currentAction
		currentAction = Random.Range (0, 3);
	}
	public virtual void setDS(int inputKo){

	}
	public virtual int getDS(){
		return -1;
	}
	public virtual void learn(){
		
	}
	public void UpdateKo(){
		if (!toControl.isPaused () && initialized) {// no sense updating if the team is paused
			think ();
			doIt ();
		}
	}
	public virtual void doIt(){//interpret the chosen currentAction
		if (currentAction != 0) gm.move (currentAction, toControl.getBarkada ());
	}
}

//the shortest path standard AI
public class SPAI : AI{
	List<Tile> entryPoints;
	Tile prevPosition;
	int counter;
	public override void init (GameManagerOld g, int teamNumToControl){
		base.init (g, teamNumToControl);
		entryPoints = new List<Tile> ();
		foreach (Table t in g.getMap().getTables()) {
			foreach (GameObject gg in t.getEntries()) {
				entryPoints.Add (gg.GetComponent<Tile>());
			}
		}
		counter = 0;
	}

	public override void think(){
		Tile currentPosition = toControl.getBarkada().getHead().getCurrentTile();
		if (prevPosition != null && currentPosition == prevPosition) {
			if (counter == 2) {
				currentAction = Random.value > 0.5f ? 1 : 2;
				counter = 0;
				return;
			} else
				counter++;
		}
		//get currentPosition of team's head
		//get destination from map
		int qq = 0;
		double currentDist = double.PositiveInfinity;
		for (int q = 0; q < entryPoints.Count; q++) {
			if (entryPoints [q].getPointVal () == 3) {
				float a = Mathf.Abs (currentPosition.getMapIndex().x - entryPoints[q].getMapIndex().x);
				float b = Mathf.Abs (currentPosition.getMapIndex().y - entryPoints[q].getMapIndex().y);
				double tempDist = (double)Mathf.Sqrt ((a * a) + (b * b));
				if (tempDist < currentDist) {
					currentDist = tempDist;
					qq = q;
				}
			}
		}
		Tile destination = entryPoints[qq];

		//Debug.Log ("Naghanap for " + counterKo);
		//Debug.Log ("Dest: " + destination.getMapIndex ().x + ", " + destination.getMapIndex ().y);
		//Debug.Log ("Source: " + currentPosition.getMapIndex ().x + ", " + currentPosition.getMapIndex ().y);
		currentAction = gm.getDirections(currentPosition, destination);
		//getDirections will return the normal coordinate position 0, 1, 2, 3
		//we need to transpose it to the left right straight command
		int heading = toControl.getBarkada().getHead().getHeading();
		if (UtilsKo.mod (heading + 1, 4) == currentAction) {//if turning left
			currentAction = 2;
		} else if (UtilsKo.mod (heading - 1, 4) == currentAction) {
			currentAction = 1;
		} else if (UtilsKo.mod (heading - 2, 4) == currentAction) {//meaning pinapaturn around ka niya, which is impossible so just pick either left or right
			currentAction = Random.value > 0.5f ? 1 : 2;
		} else {
			currentAction = 0;
		}
		prevPosition = currentPosition;
	}
}

public class DQNAI : AI{
	//currentAction acts as currentStates
	int dS;//difficulty Setting
	double result, weightedBias;//actually di ako sure kung kailangan biasConstant e
	List<List<double[]>> weights;//outermost		 list is the list of all available networks, inner list is the list of layers in a network, array is the list of weights for that corresponding layer
	int[] numNodesInLayer;
	int numLayers;

	//List<double> qtable;
	//List<string> states;
	//List<Vector2> memoryPool;//state index, learning rate, di na kailangan ang mga outputs kasi ang gusto ko lang ay i enhance siya by giving positive rewards
	//kung baga binibigyan ko lang siya ng pat on the back na yung magnitude ay nakadepende sa lakas ng sapak
	public override int getDS(){
		return dS;
	}
	public override void setDS(int y){
		dS = y;
	}
	/*
	 *==================================================================================================================================================== 
	 * Ito naman ang iadjust mo boi
	 * 
	 *====================================================================================================================================================
	 */

	public override void init (GameManagerOld g, int teamNumToControl){
		base.init (g, teamNumToControl);
		result = -1.0;

		//read configurations from file
		string[] tempStrings = System.IO.File.ReadAllText (UtilsKo.NNConfigFilePath).Split(' ');
		double minNN = double.Parse (tempStrings [0]);
		double maxNN = double.Parse (tempStrings [1]);
		numLayers = int.Parse (tempStrings [2]);
		numNodesInLayer = new int[numLayers];
		for (int q = 0; q < numLayers; q++) {
			numNodesInLayer[q] = int.Parse(tempStrings[q + 3]);
		}
		weightedBias = double.Parse(tempStrings[numLayers + 3]) * double.Parse(tempStrings[numLayers + 4]);
		weights = new List<List<double[]>> ();

		string weightBabyInputs = "";
		try{
			weightBabyInputs = System.IO.File.ReadAllText (UtilsKo.weightsFilePath + ".txt");
		}catch{
			Debug.Log ("Making the weights now");
		}
		string logs = System.DateTime.UtcNow.ToString() + "Starting AI\n";


		if (weightBabyInputs.Equals ("")) {//if the weights file doesn't exist
			//instantiate the weights array, convoluted neural network with an area of 9
			//calculate for dimension of the weights array first
			logs += UtilsKo.weightsFilePath + " is empty, so generating random weights now\n";
			for (int qq = 0; qq < UtilsKo.numDiffs; qq++) {//to generate 3 different weights
				List<double[]> weights2 = UtilsKo.generateRandomNNWeights(numNodesInLayer, minNN, maxNN);
				weights.Add (weights2);
			}
			UtilsKo.writeNNWeights (weights);
		} 
		else {//read it from file
			logs += "Reading weights from " + logs + UtilsKo.weightsFilePath + "\n";
			weights = UtilsKo.readNNWeights (weightBabyInputs);
		}
			
		//record it all in logs file
		System.IO.File.AppendAllText(UtilsKo.logsFilePath, logs);
	}
	public string getStateRep(int [, ] info){
		string returnVal = "";
		for (int q = 0; q < info.GetLength (0); q++) {
			for (int w = 0; w < info.GetLength (1); w++) {
				returnVal += info [q, w];
			}
		}
		return returnVal;
	}


	//dito ko ilalagay yung final calculation
	//currentAction will contain the value given by the neural network
	//nasa doIt() na kung paano niya yun iinterpret
	public override void doIt (){

		result = UtilsKo.logFunc (result);
		//Debug.Log ("Hi, activated function is " + result);

		//Debug.Log("Sum is " + sum
		if (result < 0)
			Debug.Log ("What the fuck? check the neural network's final calculated number and doIt()'s logistic function");


		if (result <= 0.51f)
			currentAction = 0;
		else if (0.51f < result && result < 0.63f)
			currentAction = 1;
		else
			currentAction = 2;
		base.doIt ();
	}


	public override void think(){
		//don't use the feature vectors yet, take note that it follows x, y convention
		int [,] info = gm.getDisplayInformation ();
		Debug.Log ("Yung input ko ay may length na " + info.GetLength (0) + ", " + info.GetLength (1));

		//transform the int[] input to a double[] general results
		//current consolidation method is to get succeeding inputs to one input node
		//the values placed in each input node is added and averaged
		//last node is reserved for the rest of the inputs and for the heading/direction of the team
		double[] genResults = new double[numNodesInLayer[0]];
		int numInputsPerNode = ((info.GetLength (0) * info.GetLength (1)) / (numNodesInLayer [0] - 1));
		int iX = 0;
		int iY = 0;
		//Debug.Log ("hello po " + numInputsPerNode);
		for(int q = 0; q < numNodesInLayer[0] - 1; q++){
			double tempHolder = 0;
			for (int w = 0; w < numInputsPerNode; w++) {
				if (iX >= info.GetLength (1) - 1) {
					if (iY >= info.GetLength (0) - 1)
						break;
					else iY += 1;
					iX = 0;
				} 
				else iX += 1;

				tempHolder += (double)info [iY, iX];
			}
			genResults [q] = tempHolder / (double)numInputsPerNode;
		}

		//process the input for the last input node
		double tempHold = 0;
		int counter = 0;
		while (iY < info.GetLength(0) - 1) {
			if (iX >= info.GetLength (1) - 1) {
				iY += 1;
				iX = 0;
			} 
			else iX += 1;
			tempHold += (double)info [iY, iX];
			counter++;
		}
		tempHold += (double) (toControl.getBarkada ().getHead () == null ? -1 : toControl.getBarkada ().getHead ().getHeading ());
		genResults [numNodesInLayer [0] - 2] += (double) (gm.getOpponentHeading(toControl.getID())) / (double)(2.0);
		genResults [numNodesInLayer [0] - 1] = tempHold / (double)(counter + 1);

		//The neural network
		for(int q = 0; q < numLayers - 1; q++){
			double[] tempStorage = new double[numNodesInLayer[q + 1]];
			for(int w = 0; w < numNodesInLayer[q + 1]; w++){

				//uses weighted sum for the consolidation algorithm
				double valueKo = 0;
				for(int e = 0; e < numNodesInLayer[q]; e++){
					valueKo += genResults [e] * weights [dS] [q] [(e * numNodesInLayer [q + 1]) + w];
				}
				valueKo += weightedBias;

				valueKo = UtilsKo.logFunc (valueKo);//prevents the numbers from blowing out of proportion
				tempStorage[w] = valueKo;//store it as the next layer's input
			}

			//ready the genResults for the next Layer
			for(int w = 0; w < numNodesInLayer[q + 1]; w++){
				genResults[w] = tempStorage[w];
			}
		}

		result = genResults [0];
	}
}