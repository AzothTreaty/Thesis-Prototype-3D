﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public static class UtilsKo{
	public static int mod(int a, int b){
		return (a % b + b) % b;
	}
}


//actual game termination is seen in checkForTermin() and in roundmanager's update(), gameOver() is called by boths
public class GameManager : MonoBehaviour{
	static Map map;
	Barkada tBC;
	HUDManager dm;
	RoundManager rm;
	bool paused, once;
	AI ai;
	static GameManager gm;
	float spawnTime, generalTimer;//generalTimer should not be reset, just modulo it if you want to know if 30 seconds have passed
	int numTeams;
	GameObject roundMaster, minimap;
	void Start(){
		spawnTime = 0f;
		generalTimer = 0f;
		paused = false;
		once = true;

		//get the static version of this
		if (gm == null)
			gm = this.GetComponent<GameManager> ();

		//initialize map
		this.gameObject.AddComponent<Map> ();
		map = this.gameObject.GetComponent<Map> ();
		//map.initialize (30, 20, 4);
		map.initialize(Map.map1);

		//initialize team
		numTeams = 2;
		for (int q = 0; q < numTeams; q++) {
			this.gameObject.AddComponent<Team> ();
		}

		//initialize barkada, get spawnpoint from map and give it to barkada, initialize characters then give spawnpoint
		for (int q = 0; q < numTeams; q++) {
			Team temp = this.gameObject.GetComponents<Team>()[q];
			temp.initialize (q, map.getSpawnPoint(), 0.25f);
			startBarkada (temp.getBarkada());
		}
		tBC = this.gameObject.GetComponents<Team> () [0].getBarkada ();
	
		//instantiate the round manager after the teams
		rm = GameObject.Find("GameRoundCanvas").GetComponentInChildren<RoundManager>();
		rm.initMe (GetComponents<Team>(), this);

		//strangers are taken care of by table

		//get the HUD Panel
		dm = this.gameObject.GetComponent<HUDManager> ();

		//initialize the round manager
		roundMaster = GameObject.Find ("GameRoundCanvas");
		roundMaster.SetActive (false);

		//instantiate mini-map
		minimap = GameObject.Find("PauseCanvas");
		minimap.AddComponent<MiniMapManager>();
		minimap.GetComponent<MiniMapManager> ().initMe(map);

		this.gameObject.AddComponent<AI> ();
		ai = GetComponent<AI> ();
		ai.init (this);
	}

	public float getTimeKo(){//always from zero to 30
		if ((int)generalTimer == 0) {
			return 0f;
		} else {
			int babyKo = UtilsKo.mod ((int)generalTimer, 30);
			if (babyKo == 0f) {
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
		//GetComponent<GameManager> ().enabled = false;
	}

	public void unPauseMe(){
		enabled = true;
		foreach(Team t in GetComponents<Team>()){
			t.unPauseMe ();
		}
		paused = false;
		//GetComponent<GameManager> ().enabled = true;
	}

	public void gameOver(){
		//put data into menu manager
		GetComponent<MenuManager>().inputTeamData(GetComponents<Team>());
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
			//ayusin mo muna ang generalTimer
			if((int)generalTimer == 0) 
				generalTimer += 30f;
			else 
				generalTimer += (30f - UtilsKo.mod((int) generalTimer, 30));
			finishRound ();
		}
		int numChairs = map.getNumChairs ();
		if ((numPlayers > numChairs && numChairs == 0) || (numPlayers == 0 && numChairs > 0)) {
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
		roundMaster.SetActive (false);
		unPauseMe ();//dapat nandito or else baka walang mahanap yung GETCOMPONENTS
		foreach(Team ti in GetComponents<Team>()){
			ti.getNewBarkada ();
			startBarkada (ti.getBarkada());
		}
	}

	public void finishRound(){
		pauseMe ();
		//enable roundMaster, all data is already accessible by roundMaster, in case new data is needed, just put in roundMaster's init function
		roundMaster.SetActive (true);
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

	void Update () {
		//process inputs
		if (Input.GetKeyDown (KeyCode.LeftArrow)) {
			move (2, tBC);
			//Debug.Log ("Clicked left");
		} else if (Input.GetKeyDown (KeyCode.RightArrow)) {
			move (1, tBC);
			//Debug.Log("Right");
		} else {//move everyone
			//move all barkadas
			foreach (Team i in GetComponents<Team>()) if(!(i.isPaused()) && i.timeToMove(Time.deltaTime)) move (0, i.getBarkada());
		}
		if (UtilsKo.mod((int)generalTimer, 20) == 19) {//meaning every 10 seconds gagalaw ang isang stranger
			//makeAStranger();
			//dito dapat code for stranger exiting
		}

		//update the HUDPanel
		generalTimer += Time.deltaTime;
		if (dm == null) {
			dm = GetComponent<HUDManager> ();
		}
		dm.setText (0, "Team 1: " + GetComponents<Team> () [0].getQueueSize ());
		dm.setText (1, (int)generalTimer / 60 + ":" + UtilsKo.mod ((int)generalTimer, 60));
		dm.setText (2, "Team 2: " + GetComponents<Team> () [1].getQueueSize ());

		//check for round termination based on time
		if (getTimeKo () == 30) {
			if (once) {//kailangan tong if block na to para mapigilan siyang magexecute ng mmarami sa second na "30"
				//put all unseated characters back to their respective teams
				//Debug.Log ("Pinapasok ko na naman to");
				foreach (Team ti in GetComponents<Team>()) {
					if (!ti.getBarkada ().isDisabled ()) {
						//isplit mo yung barkada dahil babalik naman yung character sa team kapag na disable siya
						ti.getBarkada ().splitMe (ti.getBarkada ().getHead ());//putulin sila based sa head nila
						//at iset yung time nila as 30 secs if hindi siya na split
						if(ti.getBarkada().getTime() == 0f)
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

		//update the map
		minimap.GetComponent<MiniMapManager>().updateMe(map.getEnvironmentDisplay());
	}
	public static void makeAStranger(Tile tile1){
		GameObject temp = (GameObject)Instantiate (Resources.Load ("Prefabs/PlayerPrefab", typeof(GameObject)));
		temp.GetComponent<Character> ().setUpStranger (gm);//dapat mauna ang setUpStranger dahil kinacall nito to ang startProperly() ng character na ginamit dito
		temp.GetComponent<Character> ().initializeMe (tile1, null);
	}
}

public class Map : MonoBehaviour{//Monobehaviour kasi kailangan ko yung "Instantiate" function
	public static int[,] map1 = {	{2, 0, 0, 0, 0, 0, 0, 2},
									{0, 0, 0, 0, 0, 0, 0, 0},
									{0, 0, 3, 4, 3, 0, 0, 0},
									{0, 0, 4, 5, 4, 0, 0, 0},
									{0, 0, 4, 5, 4, 0, 0, 0},
									{0, 0, 3, 4, 3, 0, 0, 0},
									{0, 0, 0, 0, 0, 0, 0, 0},
									{2, 0, 0, 0, 0, 0, 0, 2}};

	GameObject[,] tiles;
	GameObject[] spawnPoints;//size is determined in init statement
	Table[] tables;
	int xQuant, yQuant, numTables;
	public int getNumChairs(){
		int returnVal = 0;
		foreach(Table t in tables){
			returnVal += t.getNumChairs();
		}
		return returnVal;
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

	public int[,] getEnvironmentDisplay(){
		int[,] returnVal = new int[xQuant, yQuant];
		Tile temp;
		for (int q = 0; q < xQuant; q++) {
			for (int w = 0; w < yQuant; w++) {
				temp = tiles [q, w].GetComponent<Tile> ();
				if (temp.getLaman () != null) {
					if (temp.getLaman ().getTeamID () == -1)
						returnVal [q, w] = -2;
					else returnVal [q, w] = temp.getLaman ().getTeamID () + 6;
				}
				else{ 
					returnVal [q, w] = temp.getPointVal ();
				}
			}
		}
		return returnVal;
	}

	//sobrang fucked up pa ng pointVal system
	public void initialize(int[,] blueprint){//just follow x, y convention system but blueprint follows y, x convention
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
		float initY = 0.0f;
		Vector3 curPos = new Vector3(0, 0, 0);
		for (int q = 0; q < yQuant; q++) {
			float initX = 0.0f;
			for (int w = 0; w < xQuant; w++) {
				if (tiles [w, q] != null) continue;
				int typeOfTile = 0;
				switch (blueprint [q, w]) {//for graphical tile instantiation
				case 0://normal tile
					tiles [w, q] = (GameObject)Instantiate (backOrig);
					break;
				case 1://obstacled tile
					break;
				case 2://spawnpoint
					tiles [w, q] = (GameObject)Instantiate (backOrig);
					typeOfTile = 2;
					spawnPoints[spawnIndex++] = tiles[w, q];
					break;
				case 3://table entry point, signals start of table generation
					tiles [w, q] = (GameObject)Instantiate (backOrig);
					typeOfTile = 3;
					break;
				case 4://chair
					tiles [w, q] = (GameObject)Instantiate (chairOrig);
					typeOfTile = 4;
					break;
				case 5://table
					tiles [w, q] = (GameObject)Instantiate (tableOrig);
					typeOfTile = 5;
					break;
				default:
					Debug.Log ("Wtf is this?");
					break;
				}
				tiles [w, q].AddComponent<Tile> ();
				tiles [w, q].GetComponent<Tile> ().initialize (blueprint[q, w], w, q);
				curPos.x = initX;
				curPos.y = initY;
				tiles [w, q].transform.position = curPos;
				initX -= tiles [w, q].GetComponent<BoxCollider> ().bounds.size.x;
			}
			initY -= tiles [0, q].GetComponent<BoxCollider> ().bounds.size.y;
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
							tableTiles [index++] = tiles [p, o];
						}
					}

					tempTables.Add(new Table (tableTiles, tableWidth - 2, tableHeight - 2));
					//takeCare of entry points
				}
			}
		}
		tables = tempTables.ToArray ();
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
			int counter = 30;
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
	public Table(GameObject[] nb, int xS, int yS){//ayusin ang pointVal flags ng tiles
		tableTiles = nb;
		entryPoints = new GameObject[4];
		chairs = new GameObject[(xS * 2)+(yS * 2)];
		availSeats = chairs.Length;

		for (int q = 0; q < tableTiles.Length; q++) {
			tableTiles [q].GetComponent<Tile> ().setTable (this);
		}
		//set the entry points
		entryPoints[0] = tableTiles[0];
		entryPoints [1] = tableTiles [xS + 1];
		entryPoints [2] = tableTiles [(yS + 1) * (xS + 2)];
		entryPoints[3] = tableTiles[tableTiles.Length - 1];

		//set the chairs
		int index = 0;
		for(int q = 1; q < xS + 1; q++){
			chairs [index] = tableTiles [q];
			Debug.Log (chairs [index].GetComponent<Tile> ().getMapIndex ().x + ", " + chairs [index].GetComponent<Tile> ().getMapIndex ().y + " is a chair1");
			chairs [index].GetComponent<Tile> ().setPointVal (4);
			chairs [index++].GetComponent<Tile> ().setOccupied (false);
		}
		for (int q = xS + 2; q < ((yS + 1) * (xS + 2)); q += xS + 2) {
			chairs [index] = tableTiles [q];
			Debug.Log (chairs [index].GetComponent<Tile> ().getMapIndex ().x + ", " + chairs [index].GetComponent<Tile> ().getMapIndex ().y + " is a chair2");
			chairs [index].GetComponent<Tile> ().setPointVal (4);
			chairs [index++].GetComponent<Tile> ().setOccupied (false);
		}
		for(int q = (2 * (xS + 1)) + 1; q < ((yS + 1) * (xS + 2)); q += xS + 2){
			chairs [index] = tableTiles [q];
			Debug.Log (chairs [index].GetComponent<Tile> ().getMapIndex ().x + ", " + chairs [index].GetComponent<Tile> ().getMapIndex ().y + " is a chair3");
			chairs [index].GetComponent<Tile> ().setPointVal (4);
			chairs [index++].GetComponent<Tile> ().setOccupied (false);
		}
		for(int q = ((yS + 1) * (xS + 2)) + 1; q < tableTiles.Length - 1; q++){
			chairs [index] = tableTiles [q];
			Debug.Log (chairs [index].GetComponent<Tile> ().getMapIndex ().x + ", " + chairs [index].GetComponent<Tile> ().getMapIndex ().y + " is a chair");
			chairs [index].GetComponent<Tile> ().setPointVal (4);
			chairs [index++].GetComponent<Tile> ().setOccupied (false);
		}

		//take care of strangers
		int randomBlah = Random.Range(0, (int)(chairs.Length/2));
		for (int q = 0; q < randomBlah; q++) {
			//get random chair index
			while (true) {
				int randomIndex = Random.Range (0, chairs.Length);
				if (!chairs [randomIndex].GetComponent<Tile> ().isOccupied ()) {
					GameManager.makeAStranger (chairs [randomIndex].GetComponent<Tile> ());
					chairs [randomIndex].GetComponent<Tile> ().setOccupied (true);
					break;
				}
			}
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
					GameManager.makeAStranger (chairs [randomIndex].GetComponent<Tile> ());
					chairs [randomIndex].GetComponent<Tile> ().setOccupied (true);
					break;
				}
			}
		}
		//Debug.Log (nb[0].GetComponent<Tile>().getMapIndex().x + " " + nb[0].GetComponent<Tile>().getMapIndex().y);
		//Debug.Log ("I am " + xS + " " + yS);
	}
	public int getNumChairs(){//gets available number of seats
		return availSeats;
	}
	public void seatCharacters(Character head){
		Character temp = head;
		Character temp2 = temp;
		Barkada tropa= head.getBarkada ();
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
			//turn every entry point into an obstacle
			for (int q = 0; q < entryPoints.Length; q++) {
				entryPoints [q].GetComponent<Tile> ().initialize (1, (int)entryPoints [q].GetComponent<Tile> ().getMapIndex().x, (int)entryPoints [q].GetComponent<Tile> ().getMapIndex().y);
			}
			//ipakontrol ulit sa player yung mga natitira
			tropa.getTeam().addSplit();
			tropa.setCharacters (temp);
		}
		tropa.saveTime (GameManager.getTime ());
		if (GameManager.checkForTermination ()) {//nagcacause ng premature termination to
			//Debug.Log ("It is finished");
			return;
		}
		//Debug.Log ("Naipasok ko na lahat ng kasya");
	}
}

public class AI : MonoBehaviour{//mono dahil kailangan ng sariling update method para makapag-isip ng maayos ang AI
	protected GameManager gm;
	protected int currentAction;
	float timeCur;
	Team toControl;
	public void init(GameManager g){
		gm = g;
		timeCur = 0;
		currentAction = 0;//idle
		toControl = gm.gameObject.GetComponents<Team> () [1];
	}
	public virtual void think(){//choose a currentAction
		currentAction = Random.Range (0, 3);
	}
	public virtual void learn(GameObject[] information, int action){

	}
	void Update(){
		if (!toControl.isPaused ()) {// no sense updating if the team is paused
			if (timeCur > 1) {//temporary lang to, dapat laging kapag tapos na mag-animate na gumagalaw ang barkada saka siya ulit mag-iisip
				think ();
				doIt ();
				timeCur = 0;
			} else {
				timeCur += Time.deltaTime;
			}
		}
	}
	public virtual void doIt(){//interpret the chosen currentAction
		if (currentAction != 0) gm.move (currentAction, toControl.getBarkada ());
	}
}