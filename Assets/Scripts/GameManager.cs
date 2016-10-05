using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public static class UtilsKo{
	public static int mod(int a, int b){
		return (a % b + b) % b;
	}
}


//actual game termination is seen in checkForTermin() and in roundmanager's update(), gameOver() is called by boths
public class GameManager : MonoBehaviour{
	public static List<int[,]> maps;
	static Map map;
	Barkada tBC;
	HUDManager dm;
	RoundManager rm;
	bool paused, once;
	DQNAI ai;
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
		maps = new List<int[,]> ();
		readMaps();
		//map.initialize (30, 20, 4);
		map.initialize(maps[0]);

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

		this.gameObject.AddComponent<DQNAI> ();
		ai = GetComponent<DQNAI> ();
		ai.init (this, 1, map.getWidth(), map.getHeight());
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

	public int [,] getDisplayInformation(){
		return map.getEnvironmentDisplay ();
	}

	public int[,,] getEnvironment(int teamID){
		return map.getFeatureVectors (teamID);
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
						ti.setDeltaScore(-5f);
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
		int spawnIndexKo = 0;
		Vector3 curPos = new Vector3(0, 0, 0);
		for (int q = 0; q < yQuant; q++) {
			float initX = 0.0f;
			for (int w = 0; w < xQuant; w++) {
				if (tiles [w, q] != null) continue;
				switch (blueprint [q, w]) {//for graphical tile instantiation
				case 0://normal tile
					tiles [w, q] = (GameObject)Instantiate (backOrig);
					break;
				case 1://obstacled tile
					break;
				case 2://spawnpoint
					tiles [w, q] = (GameObject)Instantiate (backOrig);
					spawnPoints[spawnIndex++] = tiles[w, q];
					break;
				case 3://table entry point, signals start of table generation
					tiles [w, q] = (GameObject)Instantiate (backOrig);
					break;
				case 4://chair
					tiles [w, q] = (GameObject)Instantiate (chairOrig);
					break;
				case 5://table
					tiles [w, q] = (GameObject)Instantiate (tableOrig);
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
					GameManager.makeAStranger (chairs [randomIndex].GetComponent<Tile> ());
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
		//Debug.Log ("I have " + availSeats + " # of seats left");
		float calculatedScore = (numOfNaiupo * (30f - GameManager.getTime())/30f) - (temp == null ? 0 : 1);
		tropa.getTeam ().addScore (calculatedScore);
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
	protected int currentAction, currentCorrectAction;
	float timeCur;
	protected Team toControl;
	public virtual void init(GameManager g, int teamNumToControl){
		gm = g;
		timeCur = 0;
		currentAction = 0;//idle
		currentCorrectAction = -1;//meaning wala pa siyang alam na currently correct action
		toControl = gm.gameObject.GetComponents<Team> () [teamNumToControl];
	}
	public virtual void think(int[,] information){//choose a currentAction
		currentAction = Random.Range (0, 3);
	}
	public virtual void learn(){
		
	}
	void Update(){
		if (!toControl.isPaused ()) {// no sense updating if the team is paused
			if (timeCur > 1) {//temporary lang to, dapat laging kapag tapos na mag-animate na gumagalaw ang barkada saka siya ulit mag-iisip
				doIt ();
				timeCur = 0;
			} else {
				if (timeCur + Time.deltaTime > 1) {
					think (gm.getDisplayInformation ());
				} else {
					learn ();
				}
				timeCur += Time.deltaTime;
			}
		}
	}
	public virtual void doIt(){//interpret the chosen currentAction
		if (currentAction != 0) gm.move (currentAction, toControl.getBarkada ());
	}
}

public class DQNAI : AI{
	//currentAction acts as currentStates
	string weightsFilePath = "Weights.txt";
	string logsFilePath = "Logs.txt";
	Text forShow;
	double biasConstant, learningRate;//actually di ako sure kung kailangan biasConstant e
	List<double[]> weights, qtable;
	List<string> states;
	List<Vector3> memoryPool;//state index, action taken, learning rate

	public void init (GameManager g, int teamNumToControl, int width, int height){//kapag nag-error pagpalitin mo na lang yung height and width sa parameters
		base.init (g, teamNumToControl);
		forShow = GameObject.Find ("AIText").GetComponent<Text>();
		biasConstant = 1;
		states = new List<string> ();
		qtable = new List<double[]>();
		memoryPool = new List<Vector3> ();
		weights = new List<double[]> ();

		string weightBabyInputs = "";
		try{
			weightBabyInputs = System.IO.File.ReadAllText (weightsFilePath);
		}catch{

		}
		string logs = System.DateTime.UtcNow.ToString() + "Starting AI\n";

		if (weightBabyInputs.Equals ("")) {//if the weights file doesn't exist
			//instantiate the weights array, convoluted neural network with an area of 9
			//calculate for dimension of the weights array first
			logs += weightsFilePath + " is empty, so generating random weights now\n";
			int curW = width - 2;
			int curH = height - 2;
			while (curH > 2 && curW > 2) {//kasi sakto pa kapag == 3
				double[] temp = new double[12];//9 for actual weights, 1 for bias weight, 2 for width height
				logs += "Layer #" + weights.Count + " weights: ";
				for (int q = 0; q < 10; q++) {
					//read the weights from file, but for now instantiate it as random first
					temp [q] = Random.value;
					logs += temp [q] + " ";
				}
				logs += "\n";
				curH -= 2;
				curW -= 2;
				temp [10] = curW;
				temp [11] = curH;
				weights.Add (temp);
			}
			//instantiate the weights for the 3 categories
			for (int q = 0; q < 3; q++) {
				logs += "Categorical Layer #" + q + " weights: ";
				double[] temp = new double[curH * curW];
				for (int w = 0; w < temp.Length; w++) {
					temp [w] = Random.value;
					logs += temp [w] + " ";
				}
				logs += "\n";
				weights.Add (temp);
			}
		} else {//read it from file
			logs += "Reading weights from " + logs + weightsFilePath + "\n";
			string[] baby1 = weightBabyInputs.Split('|');
			for (int q = 0; q < baby1.Length; q++) {
				string[] baby2 = baby1 [q].Split ('\n');
				for (int w = 0; w < baby2.Length - 1; w++) {
					logs += "Layer " + w + "'s weights: ";
					string[] baby3 = baby2 [w].Split (' ');
					double[] newInputs = new double[baby3.Length];
					for (int e = 0; e < baby3.Length; e++) {
						logs += baby3[e] + " ";
						newInputs [e] = double.Parse (baby3[e]);
					}
					logs += "\n";
					weights.Add (newInputs);
				}
			}
		}
			
		//record it all in logs file
		System.IO.File.WriteAllText(logsFilePath, logs);
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
	public override void learn (){//since score is only calculated at every end of round, learning actually starts at round 2
		//get the team's deltascore and check if deltaScore is greater than 0
		float deltaScore = toControl.getDeltaScore();
		string logs = System.DateTime.UtcNow.ToString () + "Starting learning phase with deltaScore: " + deltaScore + "\n";
		if (deltaScore != 0.0) {
			toControl.addScore (0);//to offset the deltascore gotten by this AI
			//Debug.Log ("I am supposed to start the learning phase with this score: " + deltaScore);
			double learningRate = deltaScore / 5f;//5 because based on computation, 6 is the theoretical maximum score that they can get

			//change the learningRate of everyone whose learning rate is 0 in the memoryPool
			for (int q = 0; q < memoryPool.Count; q++) {
				if (memoryPool [q].z == 0f) {
					memoryPool [q] = new Vector3 (memoryPool [q].x, memoryPool [q].y, (float)learningRate);
					logs += "Modified index " + q + "'s learning in memoryPool to " + memoryPool [q].z + "\n";
				}
			}
		} else {
			//loop through the memories to learn from, remember to remove the memory after you are done with it
			int counter = memoryPool.Count;
			for (int q = 0; q < 10 && q < counter; q++) {//10 is the max number of batch updates per round
				logs += "Trying to learn from memory " + memoryPool[q].x + ", " + memoryPool[q].y + ", " + memoryPool[q].z + "\n";
				//start the learning process
				//calculate difference rate
				double sum = getSum(qtable[(int)memoryPool[q].x]);
				double distri = qtable [(int)memoryPool [q].x] [(int)memoryPool [q].y];
				double diff = distri > (sum * 0.75) ? 1 : (0.75 * sum) - distri;

				//calculate for condensation rate
				double condRate = 1/(weights.Count-3);

				//adjust categorical weights
				logs += "Changed category " + memoryPool[q].y + " weights from \n"; 
				for (int w = 0; w < weights [weights.Count - 3 + (int)memoryPool [q].y].Length; w++) {
					logs += weights [weights.Count - 3 + (int)memoryPool [q].y] [w] + " to ";
					weights [weights.Count - 3 + (int)memoryPool [q].y] [w] += memoryPool [q].z * diff * condRate;
					logs += weights [weights.Count - 3 + (int)memoryPool [q].y] [w] + "\n";
				}
				for (int w = 0; w < weights.Count - 3; w++) {
					logs += "Changing layer " + w + "'s weights from \n";
					for (int e = 0; e < weights [w].Length - 2; e++) {
						logs += weights [w] [e] + " to ";
						weights [w] [e] += memoryPool [q].z * diff * condRate;
						logs += weights [w] [e] + "\n";
					}
				}
			}

			//remove the memories who are used to adjust the weights
			for (int q = 0; q < 10 && q < counter; q++) {
				memoryPool.RemoveAt (0);
			}

			//finally record the recorded weights in the file "Weights"
			string toBeWritten = "";
			for (int q = 0; q < weights.Count; q++) {
				for (int w = 0; w < weights [q].Length; w++) {
					toBeWritten += weights [q] [w] + (w == (weights [q].Length - 1) ? "" : " ");
				}
				toBeWritten += "\n";
			}
			toBeWritten += "|";
			System.IO.File.WriteAllText (weightsFilePath, toBeWritten);
		}
		System.IO.File.WriteAllText (logsFilePath, logs);
	}
	public double getSum(double[] baby){
		double sum = baby [0];
		for (int q = 1; q < baby.Length; q++) {
			sum += baby [q];
		}
		return sum;
	}
	public double getSignal (int[,] info){//uses sigmoid function to depict the final signal
		return 0;
	}
	public override void doIt (){

		//since technically nasa learning phase pa lang siya, bagay pa to, pero kapag nasa production phase na, kunin mo na lang yung max distribution tapos yun ang gamitin mo
		//calculate for distributions
		int babyMoTo = currentAction;
		double a = qtable[currentAction][0];
		double b = qtable[currentAction][1];
		double c = qtable[currentAction][2];
		double sum = a + b + c;
		a = a / sum;
		b = b / sum;
		c = c / sum;
		if (a > 1 || b > 1 || c > 1)
			Debug.Log ("What the fuck? check the distributions calculation in doIt()");
		//revert the currentAction variable to its original usage
		double chosen = Random.value;
		if (chosen < a)
			currentAction = 0;
		else if (chosen > a && chosen < a + b)
			currentAction = 1;
		else
			currentAction = 2;
		memoryPool.Add (new Vector3 (babyMoTo, currentAction, 0));
		base.doIt ();
	}
	public override void think(int [,] info){//don't use the feature verctors yet, take note that it follows x, y convention
		/*currentAction = 0;
		string forText = "";
		for (int q = 0; q < info.GetLength (1); q++) {
			for (int w = 0; w < info.GetLength (0); w++) {
				forText += info [w, q] + " ";
			}
			forText += "\n";
		}
		Debug.Log (forText);
		forShow.text = forText;*/

		//transform info into a double [,]
		double[,] currentMap = new double[info.GetLength (0), info.GetLength (1)];
		for (int q = 0; q < currentMap.GetLength (0); q++) {
			for (int w = 0; w < currentMap.GetLength (1); w++) {
				currentMap [q, w] = (double)info [q, w];
			}
		}
		//start feeding data to the convoluted network
		for (int q = 0; q < weights.Count - 3; q++) {
			double[,] constructingMap = new double[(int)weights [q] [10], (int)weights [q] [11]];
			//Debug.Log (currentMap.GetLength (0) + ", " + currentMap.GetLength (1));
			for (int w = 0; w < weights [q] [10]; w++) {//width
				for (int h = 0; h < weights [q] [11]; h++) {
					//w,h represents index of top left field, the above configuration moves up down then to the right
					int indexNgWeight = 0;
					double currentResult = 0;
					for (int g = h; g < h + 3; g++) {//this configuration moves from left to right then up down
						for (int i = w; i < w + 3; i++) {
							currentResult += currentMap [i, g] * weights [q] [indexNgWeight++];
						}
					}
					currentResult += weights [q] [indexNgWeight] * biasConstant;//para sa bias constant
					constructingMap [w, h] = currentResult;
				}
			}
			currentMap = constructingMap;
		}
		//Debug.Log (currentMap.GetLength (0) + ", " + currentMap.GetLength (1));
		//hopefully by this part, currentMap contains the prefinal form of the convoluted network
		//calculate for final distributions
		double[] finalDistributions = new double[3];
		int distriIndex = 0;
		for (int q = weights.Count - 3; q < weights.Count; q++) {
			int indexNgWeightKo = 0;
			double currentDistribution = 0;
			for (int h = 0; h < currentMap.GetLength (1); h++) {
				for (int w = 0; w < currentMap.GetLength (0); w++) {
					currentDistribution += currentMap [w, h] * weights [q] [indexNgWeightKo++];
				}
			}
			finalDistributions [distriIndex++] = currentDistribution;
		}

		//store the states and their distributions
		currentAction = states.Count;
		string stateRep = getStateRep (info);
		if (states.Count == 0) {
			states.Add (stateRep);
			qtable.Add (finalDistributions);
		} else {
			bool nakapasok = false;
			for (int q = 0; q < states.Count; q++) {
				if (states [q].Equals (stateRep)) {
					qtable [q] = finalDistributions;
					nakapasok = true;
					currentAction = q;
					break;
				}
			}
			if (!nakapasok) {
				states.Add (stateRep);
				qtable.Add (finalDistributions);
			}
		}
	}
}