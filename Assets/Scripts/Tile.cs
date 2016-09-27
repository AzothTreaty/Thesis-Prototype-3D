using UnityEngine;
using System.Collections;

/*display legend for pointVal
-1 == i have no idea
0 == normal tile
1 == obstacle
2 == spawnpoint
3 == tableentry point
4 == chair
5 == table
6 >= team# ng laman niyang character
*/

/*feature vector for ai
passable?
entrance?
may laman?
kalaban?
*/

public class Tile : MonoBehaviour {
	int pointVal;
	int direction;
	Vector3 index;
	Character laman;
	Table myTable;
	bool occupied;
	// Use this for initialization
	void Start () {
		//occupied = false;
	}

	public void setTable(Table t){
		myTable = t;
		//Debug.Log ("I got my table I'm in" + index.x + ":" + index.y);
	}

	public void setLaman(Character c){
		if (pointVal == 4)
			c.changeColor (1);
		laman = c;
	}

	public bool equals(Tile a){
		if (a == null)
			return false;
		if (index.x == a.getMapIndex ().x && index.y == a.getMapIndex ().y)
			return true;
		else
			return false;
	}

	public Character getLaman(){
		return laman;
	}

	public Table getTable(){
		return myTable;
	}
	
	// Update is called once per frame
	void Update () {
		
	}

	public int getDirection(){
		return direction;
	}

	public void setSpawn(int dir){
		pointVal = 2;
		direction = dir;
	}

	public int getPointVal(){
		return pointVal;
	}

	public void setOccupied(bool b){
		occupied = b;
	}

	public bool isOccupied(){
		return occupied;
	}

	public void setPointVal(int y){
		pointVal = y;
	}

	public Vector3 getMapIndex(){
		return index;
	}

	public Vector3 getPosition(){
		return this.gameObject.transform.position;
	}

	public void initialize(int pV, int indexX, int indexY){
		this.name = indexX + ", " + indexY;
		pointVal = pV;
		index = new Vector3(indexX, indexY, 0);
		setName ();
	}
	public void setName(){
		this.gameObject.name = pointVal + " + " + index.x + ":" + index.y;
	}
}