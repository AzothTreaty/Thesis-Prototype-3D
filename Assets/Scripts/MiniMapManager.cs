using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class MiniMapManager : MonoBehaviour {
	Map map;
	static Sprite[] baby;
	GameObject[,] minimap;
	bool rightPositions;
	int h, w;
	// Use this for initialization
	void Start () {
	
	}

	public void initMe(Map m){
		map = m;
		rightPositions = false;
		//init the sprites for
		if (baby == null) {
			//initilize it
			baby = new Sprite[11];
			baby [0] = Resources.Load<Sprite> ("backgroundTile");
			baby [1] = Resources.Load<Sprite> ("obstacle");
			baby [2] = Resources.Load<Sprite> ("SpawnPoint");
			baby [3] = Resources.Load<Sprite> ("ErrorTile");//since technically table entry point is just the background tile
			baby[4] = Resources.Load<Sprite> ("ChairTile");
			baby[5] = Resources.Load<Sprite> ("TableTile");
			baby[6] = Resources.Load<Sprite> ("Team1");
			baby[7] = Resources.Load<Sprite> ("Team2");
			baby[8] = Resources.Load<Sprite> ("Team3");
			baby[9] = Resources.Load<Sprite> ("Team4");
			baby [10] = Resources.Load<Sprite> ("StrangerTile");
		}

		//init the actual gameobjects
		h = map.getHeight();
		w = map.getWidth ();
		GameObject original = (GameObject)Resources.Load ("Prefabs/MiniMapTile", typeof(GameObject));
		//maglagay ka ng box collider for mere checking ng bounds ng tile
		minimap = new GameObject[map.getWidth (), map.getHeight ()];//mas gusto ko kasi ang form na (x, y)
		for (int q = 0; q < w; q++) {
			for (int e = 0; e < h; e++) {
				minimap [q, e] = (GameObject)Instantiate (original);
				minimap [q, e].transform.SetParent (this.transform, false);
				minimap [q, e].GetComponent<Image> ().sprite = baby [1];
			}
		}
	}
	
	// Update is called once per frame
	void Update () {
		if (!rightPositions) {
			//ayusin ang positioning ng tiles
			float tileH = minimap [0, 0].GetComponent<RectTransform> ().rect.height;
			float tileW = minimap [0, 0].GetComponent<RectTransform> ().rect.width;
			float startingX = Screen.width - (tileW * w);
			float startingY = Screen.height - tileH;
			Vector3 positionKo = new Vector3 (0, 0, 0);
			for (int q = 0; q < h; q++) {
				float currentX = startingX;
				for (int e = 0; e < w; e++) {
					positionKo.x = currentX;
					currentX += tileW;
					positionKo.y = startingY;
					minimap [e, q].transform.position = positionKo;
				}
				startingY -= tileH;
			}

			//ensure na di na to mapapasok ulit
			rightPositions = true;
		}
	}

	public void updateMe(int[,] information){//3d array because each tile should contain information about itself
		//we assume na laging same lang ang height ng minimap na to sa height ng information na ibibigay dito
		for (int q = 0; q < h; q++) {
			for (int e = 0; e < w; e++) {
				switch (information [e, q]) {
				case -1:
					minimap [e, q].GetComponent<Image> ().sprite = baby [3];
					break;
				case -2:
					minimap [e, q].GetComponent<Image> ().sprite = baby [10];
					break;
				case 3:
					minimap [e, q].GetComponent<Image> ().sprite = baby [0];
					break;
				default:
					minimap [e, q].GetComponent<Image> ().sprite = baby [information[e, q]];
					break;
				}
			}
		}
	}
}

public class MiniTile : MonoBehaviour{
	public void initMe(){
		
	}
}