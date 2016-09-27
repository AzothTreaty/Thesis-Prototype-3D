using UnityEngine;
using System.Collections;
public class Character : MonoBehaviour {// also acts a node in a linked list
	public Material[] mat;
	Character next;
	//static bool init = false;
	static int counter = 0;
	Tile tile, prevTile;
	Barkada team;
	int currentDir, teamID;//since di naman siya pwedeng magpalit ng team
	//static Sprite[] sprites;
	StrangerAI sAI;
	bool aiEnabled;
	float targetTime, curTime;
	// Use this for initialization
	void Start () {

	}

	public int getTeamID(){// -1 for strangers
		return teamID;
	}

	public Tile getPrevTile(){
		return prevTile;
	}

	public void startProperly(){//just to instantiate the character
		//spriteIndex = 0;
		aiEnabled = false;
		//chSCounter = 1;//used if you want a delay in animation, represents number of moves needed before animation
		//chSIndex = 0;

		/*if (!init) {
			sprites = new Sprite[12];
			GameObject temp = new GameObject();
			for (int q = 0; q < sprites.Length; q++) {
				temp = (GameObject)Instantiate (Resources.Load ("Prefabs/Character" + q, typeof(GameObject)));
				sprites[q] = temp.GetComponent<SpriteRenderer>().sprite;
				Destroy (temp);
			}
			init = true;
		}*/
		this.name = "Character" + counter++;
		//this.gameObject.GetComponent<SpriteRenderer>().enabled = false;
	}

	public Barkada getBarkada(){
		return team;
	}

	//before actually using the character
	public void initializeMe(Tile newTile, Barkada b){//b can be null for strangers
		tile = newTile;
		tile.setLaman (this);
		prevTile = tile;
		team = b;
		teamID = b == null ? -1 : team.getTeam ().getID ();
		int directiones = tile.getDirection ();
		currentDir = directiones;
		//spriteSetIndex = directiones;
		this.transform.position = tile.transform.position;
		this.gameObject.SetActive(true);
		//this.gameObject.GetComponent<SpriteRenderer>().sprite = sprites[directiones * 3];//currentDir, which is 2, times 3
		//this.gameObject.GetComponent<SpriteRenderer>().enabled = true;
	}
	public void disableMe(){//reverse ng initializeMe, for normal characters lang to
		//append it back to the team
		tile.setLaman (null);
		tile = null;
		prevTile = null;
		team.getTeam ().append (this.gameObject);
		//Debug.Log ("Hi");
		this.gameObject.SetActive(false);
		//this.gameObject.GetComponent<SpriteRenderer>().enabled = false;
		//Debug.Log("Dapat disabled na ako " + this.gameObject.name);
	}

	public Character getNext(){
		return next;
	}
	public void setNext(Character c){
		next = c;
	}
	
	// Update is called once per frame
	void Update () {
		if (sAI != null && aiEnabled) {
			if (curTime >= targetTime) {
				sAI.think ();
				sAI.doIt ();
				curTime = 0f;
			} else {
				curTime += Time.deltaTime;
			}
		}
	}

	public Tile getCurrentTile(){
		return tile;
	}

	public bool Move(Tile newTile, int dir){//0 == success, 1 == fail
		//checker ng tile kasi di gumagana collider e
		//Debug.Log(team.getTeam().getID() + " is mhy team and moving in " + dir);
		bool success = true;
		//Debug.Log("Command is to " + dir);
		//Debug.Log("I am in " + tile.getMapIndex().x + ", " + tile.getMapIndex().y + " going to " + newTile.getMapIndex().x + ", " + newTile.getMapIndex().y);
		if (newTile != null) {
			if (currentDir == dir) {
				if (newTile.getPointVal () == 3 && teamID != -1) {//ilagay sila sa table
					newTile.getTable ().seatCharacters (this);
					return false;
				} else if (newTile.equals (tile)) {//meaning hindi pwedeng gumalaw yung character dahil sabi ng map collision checker bawal niya puntahan yung gusto niyang puntahan
					//Debug.Log("Hindi na sila dapat gumalaw");
					success = false;
				} else if (newTile.getLaman () != null) {//meaning occupied ng character
					//Debug.Log ("tumama ako kay " + newTile.getLaman ().name);
					if (newTile.getLaman ().getTeamID () == teamID || newTile.getLaman ().name.Contains ("MainGuy")) {
						success = false;
					} else {
						//Debug.Log ("Tumama po kami sa kalaban");
						newTile.getLaman ().getBarkada ().splitMe (newTile.getLaman ());
					}
				} 
			}
		} else {//kasi wala namang laman si newTile e
			return false;
		}
	
		prevTile = tile;
		//para gumalaw

		if (name.Equals ("MainGuy" + teamID)) {
			if (success && currentDir == dir) {
				//move character
				tile.setLaman (null);
				tile = newTile;
				tile.setLaman (this);
				//Debug.Log ("I am moving " + this.name);
				this.gameObject.transform.position = newTile.getPosition ();

				//change animation
				//show movement in the same direction
				/*if (spriteIndex < 2) {
					spriteIndex = 2;
				} else {
					spriteIndex = 1;
				}
				chSIndex++;*/
			} else {
				currentDir = dir;
				//Debug.Log ("curDir is " + dir);
				//show turning animation
				/*spriteSetIndex = dir;
				chSIndex = 10;
				spriteIndex = 0;*/
				success = false;
			}
		} else {
			currentDir = dir;
			//Debug.Log ("curDir is " + dir);
			//show turning animation
			/*spriteSetIndex = dir;
			chSIndex = 10;
			spriteIndex = 0;*/
			if (success) {
				//move character
				//Debug.Log ("I am moving " + this.name);
				tile.setLaman (null);
				tile = newTile;
				tile.setLaman (this);
				this.gameObject.transform.position = newTile.getPosition ();

				//change animation
				//show movement in the same direction animation
				/*if (spriteIndex < 2) {
					spriteIndex = 2;
				} else {
					spriteIndex = 1;
				}
				chSIndex++;*/
			}
		}
		//for changing the actual sprites
		/*if (chSIndex >= chSCounter) {
			//Debug.Log ("Using sprite " + ((spriteSetIndex * 3) + spriteIndex));
			this.gameObject.GetComponent<SpriteRenderer> ().sprite = sprites [(spriteSetIndex * 3) + spriteIndex];
			chSIndex = 0;
		}*/
		return success;
	}

	public void changeColor(int i){
		//Debug.Log (mat.Length);
		if (i < mat.Length) {
			foreach (MeshRenderer ii in GetComponentsInChildren<MeshRenderer>()) {
				ii.material = mat [i];
			}
		}
	}

	public int getHeading(){
		return currentDir;
	}
	/*
	void OnTriggerEnter2D (Collider2D collision)
	{
		GameObject obj = collision.gameObject;
		Tile collided = obj.GetComponent<Tile> ();
		if (collided != null) {
			if (collided.getPointVal () == 3) {
				Debug.Log ("Icollided");
				collided.getTable ().seatCharacters (this);
			}
		}
	}*/
	public void setUpStranger(GameManager gm){
		startProperly ();//dapat dito dahil pinapalitan ng constructor ng strangerai ang name ng character
		sAI = new StrangerAI (gm, this);
		targetTime = 2f;
		curTime = 0f;
	}
	public void startAI(){
		aiEnabled = true;
	}
	public void setTile(Tile tilee){
		tile = tilee;
		this.gameObject.transform.position = tilee.getPosition();
	}
	public GameObject getObject(){
		return this.gameObject;
	}
}

public class Team : MonoBehaviour{
	Character head, tail;//since it is a linked list
	Barkada barkada;
	int size, ID, splits, seatedChars, acuSplits;
	float currentAcumulativeScore, curTime, maxTime, curMaxTime;
	Tile spawnPoint;
	bool paused;
	const int maxChars = 20;
	public int getID(){
		return ID;
	}
	public bool isPaused(){
		return paused;
	}
	public bool timeToMove(float additionalTime){
		curMaxTime = maxTime - ((6 - barkada.getSize ()) * (maxTime/10));
		//Debug.Log (curMaxTime);
		if (curTime >= curMaxTime) {
			curTime = 0;
			return true;
		} else {
			curTime += additionalTime;
			return false;
		}
	}
	public Tile getSpawnPoint(){
		if (spawnPoint == null)
			Debug.Log ("why the fuck is this null?");
		return spawnPoint;
	}
	public int getSplits(){
		int temp = splits;
		splits = 0;
		acuSplits += temp;
		return temp;
	}
	public int getAcumulatedSplits(){
		return acuSplits;
	}
	public float getScore(){
		return currentAcumulativeScore;
	}
	public void addSplit(){
		splits++;
	}
	public int getSeated(){
		return seatedChars;
	}
	public int getAcuSeated(){
		return maxChars - getSize ();
	}
	public void addScore(float score){//addScore lang kasi it doesn't make sense for the team to lose points
		currentAcumulativeScore += score;
	}
	public void initialize(int id, Tile ti, float mT){
		size = 0;
		curTime = 0;
		maxTime = mT;
		curMaxTime = maxTime;
		ID = id;
		splits = 0;
		acuSplits = 0;
		spawnPoint = ti;
		seatedChars = 0;
		paused = false;
		currentAcumulativeScore = 0;

		for(int q = 0; q < maxChars; q++){
			append ((GameObject)Instantiate (Resources.Load("Prefabs/PlayerPrefab", typeof(GameObject))));//characters are automatically disabled at start
		}
		//Debug.Log ("initial size is " + size);
		barkada = new Barkada (this);
		getNewBarkada ();
	}
	void Update(){
		
	}

	public void pauseMe(){
		Character current = head;
		while (current != null) {
			current.enabled = false;
			current = current.getNext ();
		}
		paused = true;
	}

	public void unPauseMe(){
		Character current = head;
		while (current != null) {
			current.enabled = true;
			current = current.getNext ();
		}
		paused = false;
	}

	public void getNewBarkada(){
		//showCharacters ();
		barkada.setCharacters (splitCharacters (2, 5));
	}
	public void append(GameObject c){
		c.GetComponent<Character> ().startProperly ();
		c.GetComponent<Character> ().changeColor (2);
		if (head == null) {
			head = c.GetComponent<Character> ();
		} else if (tail == null) {
			tail = c.GetComponent<Character> ();
			head.setNext (tail);
		} else {
			tail.setNext (c.GetComponent<Character> ());
			tail = c.GetComponent<Character> ();
		}
		size++;
		seatedChars--;
	}
	public void showCharacters(){
		Character tempoKo = head;
		string returnValMo = "";
		while (tempoKo != null) {
			returnValMo += tempoKo.name + " - ";
			tempoKo = tempoKo.getNext ();
		}
		Debug.Log (returnValMo);
	}
	public Barkada getBarkada(){
		return barkada;
	}
	public Character splitCharacters(int max, int min){//magrereturn to ng null kapag walang laman yung linked list
		int range = Random.Range (min, max);
		Character returnVal = head;
		Character toBeReturned = head;
		for (int q = 0; q < range; q++) {
			if (returnVal == null || returnVal == tail) {
				head = null;
				tail = null;
				break;
			}
			returnVal = returnVal.getNext ();
		}
		if (head != null && tail != null) {
			head = returnVal.getNext ();
			returnVal.setNext (null);
		}
		//Debug.Log("Current number of players in " + getID() + " is " + getQueueSize());

		//count the to be returned set
		seatedChars = 0;//para macounter yung minus minus na nilagay ko sa append function ng teams
		returnVal = toBeReturned;
		while (returnVal != null) {
			seatedChars++;
			size--;
			returnVal = returnVal.getNext ();
		}
		//Debug.Log ("I hope you can handle " + seatedChars + " characters");
		return toBeReturned;
	}
	public int getSize(){
		return size + barkada.getSize();
	}
	public int getQueueSize(){
		return size;
	}
}

public class Barkada{
	Character head, current;
	Team team;
	int size;
	float time;
	public Barkada(Team t){
		team = t;
		size = 0;

	}
	public bool isDisabled(){
		return head == null;
	}
	public int getSize(){
		return size;
	}
	public Team getTeam(){
		return team;
	}
	public void splitMe(Character start){//start ng pagsplit
		current = head;
		bool startMeUp = false;
		Character prev = head;
		Character tempoKo = null;
		Character[] babies = new Character[10];//kasi max of 6 CHaracters in a barkada
		int index = 0;
		while (current != null) {
			if (current.name.Equals(start.name)) {//find the start character
				startMeUp = true;
			}
			tempoKo = current.getNext ();
			if (startMeUp) {
				babies [index++] = current;//ilagay siya sa array
				prev.setNext (null);//inull yung connections ng previous sa kanya to destroy the character's connection to the barkada
			}
			prev = current;
			current = tempoKo;
		}
		for (int q = 0; q < index; q++) {
			babies [q].disableMe ();
		}
		size -= index;
	}
	public void saveTime(float y){
		time = y;
	}
	public float getTime(){
		return time;
	}
	public void setCharacters(Character h){
		head = h;
		if (h != null) {
			head.getObject ().name = "MainGuy" + team.getID ();
			//palitan head ng kulay
			h.changeColor (0);
		}
		Character current = head;
		size = 0;
		while (current != null) {
			size++;
			current = current.getNext ();
		}
	}
	public Character getHead(){
		return head;
	}
	public void start(Tile tile){
		current = head;
		while (current != null) {
			current.initializeMe (tile, this);
			current = current.getNext ();
		}
	}
	public void move(int dir, Tile newTile){//acceptor of inputs from user
		//Debug.Log("We are " + size + " characters");
		//Debug.Log(head.name);
		//if(team.getID() == 1) Debug.Log("HIhyhyhy");
		if (head != null) {
			int dirToMove = head.getHeading ();
			int prevDir = dirToMove;
			if (dir == 3) {//move left
				dirToMove = UtilsKo.mod (dirToMove + 1, 4);
			} else if (dir == 1) {//move right
				dirToMove = UtilsKo.mod (dirToMove - 1, 4);
			} else {
				dirToMove = head.getHeading ();
			}
			if (!head.Move (newTile, dirToMove)) {
				//Debug.Log ("Should stop");
				return;
			}
			int curDir = prevDir;
			Tile prev = head.getPrevTile ();
			current = head.getNext ();
			while (current != null && newTile != null) {
				prevDir = current.getHeading ();
				if (!current.Move (prev, curDir)) {
					//Debug.Log ("Stopping");
					return;
				}
				prev = current.getPrevTile ();
				current = current.getNext ();
				curDir = prevDir;
			}
		}
	}
	public Tile getCurrentHeadTile(){
		return head.getCurrentTile ();
	}
}

public class StrangerAI{
	Character toMove;
	int currentAction;
	GameManager gm;
	public StrangerAI(GameManager g, Character c){
		toMove = c;
		gm = g;
		currentAction = 0;
		toMove.name = "MainGuy69";
		//Debug.Log ("Spawned at " + c.getCurrentTile ().getMapIndex ().x + ", " + c.getCurrentTile ().getMapIndex ().y);
	}
	public void think(){
		currentAction = Random.Range (0, 4);
	}
	public void doIt(){
		//Debug.Log (toMove == null ? "toMove" : gm == null ? "gm" : "wtf?");
		toMove.Move (gm.getNewTile(currentAction, toMove.getCurrentTile()), currentAction);

		//Debug.Log ("Moved to " + toMove.getCurrentTile ().getMapIndex ().x + ", " + toMove.getCurrentTile ().getMapIndex ().y);
	}
}