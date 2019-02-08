using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameController : MonoBehaviour {
	// NOTE: Manhattan coordinate system
	public int population;
	public int stairLength;
	public int window;

	public GameObject plantObj;
	public GameObject shopperObj;

	private float time = 0.0f;
	private float windowf;
	private bool[][][] grid; // true == obstacle, impassable
	private List<GameObject> shoppers = new List<GameObject>();

	// Use this for initialization
	void Awake () {
		windowf = (float) window;
		grid = new bool[35][][];
		for (int i=0; i<35; i++) {
			grid[i] = new bool[18+stairLength][];
			for (int j = 0; j < 18 + stairLength; j++) {
				grid [i] [j] = new bool[window];
			}
		}
		resetGrid ();
		// randomly place 4 plants
		for (int p = 0; p < 4; p++) {
			bool flag = true;
			int x=0;
			int z=0;
			while (flag) {
				x = Random.Range (0, 35);
				z = Random.Range (4, 14);
				// clear to spawn if no other plant already placed there
				if (z < 9 && !getCell (x, z, 0)) {
					flag = false;
				} else if (z >= 9 && !getCell (x, z + 6, 0)) {
					flag = false;
				}
				// not clear if spawning in front of a shop entrance or stair access
				if (z == 4 && (x == 2 || x == 8 || x == 14 || x == 20 || x == 26 || x == 32)) {
					flag = true;
				} else if (z == 19 && (x == 2 || x == 8 || x == 14 || x == 20 || x == 26 || x == 32)) {
					flag = true;
				} else if (z == 8 && (x == 7 || x == 14 || x == 21 || x == 28)) {
					flag = true;
				} else if (z == 15 && (x == 7 || x == 14 || x == 21 || x == 28)) {
					flag = true;
				}
				// NOTE: plants could still box in a shop but it is very unlikely
			}
			float y;
			if (z < 9) {
				y = -2.75f;
				grid[x][z][0] = true;
			} else {
				y = 0.25f;
				grid[x][z+stairLength][0] = true;
				z += 6;
			}
			Instantiate(plantObj, new Vector3((float)x, y, (float)z), Quaternion.identity);

		}
		// randomly place n shoppers
		for (int s = 0; s < population; s++) {
			// very similar process to plants, except shoppers can spawn in shops
			bool flag = true;
			int x=0;
			int z=0;
			while (flag) {
				x = Random.Range (0, 35);
				z = Random.Range (0, 18); // seems super biased towards 9 and lower
				// clear to spawn if no other obstacle already placed there
				if (z < 9 && !getCell (x, z, 0)) {
					flag = false;
				} else if (z >= 9 && !getCell (x, z + 6, 0)) {
					flag = false;
				}
			}
			float y;
			if (z < 9) {
				y = -2.75f;
				grid[x][z][0] = true;
			} else {
				y = 0.25f;
				grid[x][z+stairLength][0] = true;
				z += 6;
			}
			GameObject newShopper = Instantiate(shopperObj, new Vector3((float)x, y, (float)z), Quaternion.identity);
			shoppers.Add (newShopper);
		}
		//GameObject newS = Instantiate(shopperObj, new Vector3(4, -2.5f, 5), Quaternion.identity);
		//shoppers.Add (newS);
		// first execution of Update code to avoid NullReference in ShopperController
		resetGrid (); // redundant call as plants and shoppers self-declare in grid at t=0
		// run 3D A* for each shopper
		foreach (GameObject shopper in shoppers) {
			reservePath(shopper.GetComponent<ShopperController>(), true);
		}
	}
	
	// Update is called once per frame
	void Update () {
		time += Time.deltaTime;
		if (time >= windowf) {
			time -= windowf;
			resetGrid ();
			// randomly determine order of reservations
			for (int s=shoppers.Count-1; s>0; s--) {
				int r = Random.Range(0,s+1);
				GameObject temp = shoppers [r];
				shoppers [r] = shoppers [s];
				shoppers [s] = temp;
			}
			// then run 3D A* for each one
			foreach (GameObject shopper in shoppers) {
				reservePath(shopper.GetComponent<ShopperController>(), false);
			}
		}
	}

	void resetGrid () {
		for (int i = 0; i < 35; i++) {
			for (int j = 0; j < 18 + stairLength; j++) {
				for (int k = 0; k < window; k++) {
					// all shop walls are impassable
					if (j == 3) {
						if (i <= 1 || (i >= 3 && i <= 7) || (i >= 9 && i <= 13) || (i >= 15 && i <= 19) || (i >= 21 && i <= 25) || (i >= 27 && i <= 31) || i >= 33) {
							grid [i] [j] [k] = true;
						} else {
							grid [i] [j] [k] = false;
						}
					} else if (j == 14 + stairLength) {
						if (i <= 1 || (i >= 3 && i <= 7) || (i >= 9 && i <= 13) || (i >= 15 && i <= 19) || (i >= 21 && i <= 25) || (i >= 27 && i <= 31) || i >= 33) {
							grid [i] [j] [k] = true;
						} else {
							grid [i] [j] [k] = false;
						}
					} else if (j < 3) {
						if (i == 0 || (i >= 4 && i <= 6) || (i >= 10 && i <= 12) || (i >= 16 && i <= 18) || (i >= 22 && i <= 24) || (i >= 28 && i <= 30) || i == 34) {
							grid [i] [j] [k] = true;
						} else {
							grid [i] [j] [k] = false;
						}
					} else if (j > 14 + stairLength) {
						if (i == 0 || (i >= 4 && i <= 6) || (i >= 10 && i <= 12) || (i >= 16 && i <= 18) || (i >= 22 && i <= 24) || (i >= 28 && i <= 30) || i == 34) {
							grid [i] [j] [k] = true;
						} else {
							grid [i] [j] [k] = false;
						}
					} else if (j >= 9 && j < 9 + stairLength) {
						// space between stairs is impassable
						if (i != 7 && i != 14 && i != 21 && i != 28) {
							grid [i] [j] [k] = true;
						} else {
							grid [i] [j] [k] = false;
						}
					} else {
						// sweep away traces of reservations before restating plant and shopper locations
						grid [i] [j] [k] = false;
					}
				}
			}
		}
		// plants are impassable
		GameObject[] plants = GameObject.FindGameObjectsWithTag("Obstacle");
		if (plants != null) {
			foreach (GameObject plant in plants) {
				for (int t = 0; t < window; t++) {
					if (plant.transform.position.z < 9) {
						grid [(int)plant.transform.position.x] [(int)plant.transform.position.z] [t] = true;
					} else {
						grid [(int)plant.transform.position.x] [(int)plant.transform.position.z-6+stairLength] [t] = true;	
					}
				}
			}
		}
		// shoppers occupy a space in t=0
		if (shoppers != null) {
			foreach (GameObject shopper in shoppers) {
				if (shopper.transform.position.z < 9) {
					grid [(int)shopper.transform.position.x] [(int)shopper.transform.position.z] [0] = true;
				} else {
					grid [(int)shopper.transform.position.x] [(int)shopper.transform.position.z-6+stairLength] [0] = true;
				}
			}
		}
	}
	// returns false if cell is passable, true if impassable at time%window
	bool getCell(int x, int z, int t) {
		if (x >= 0 && x < grid.Length && z >= 0 && z < grid[0].Length && t < grid[0][0].Length) {
			return grid[x][z][t];
		}
		else {
			return true;
		}
	}

	void reservePath (ShopperController s, bool firstCall) {
		// first set a new destination if the shopper has reached their previous target
		Vector2 d = new Vector2(0, 0);
		if (s.atDest || firstCall) {
			bool distFlag = true;
			while (distFlag) {
				if (Random.value > 0.5f) { // Move objective
					bool flag = true;
					int x = 0;
					int y = 0;
					while (flag) {
						x = Random.Range (0, 35);
						y = Random.Range (4, 14);
						// choose as dest if location unoccupied
						if (y < 9 && !getCell (x, y, 0)) {
							flag = false;
						} else if (y >= 9 && !getCell (x, y + 6, 0)) {
							flag = false;
							y += 6;
						}
					}
					d = new Vector2 (x, y);
				} else { // Shop objective
					int shopNum = Random.Range (0, 12);
					int localX = Random.Range (1, 4);
					int localY = Random.Range (0, 3);
					// NOTE: two shoppers may choose same point in same shop but 1) unlikely they actually do and 2) unlikely they arrive simultaneously
					if (shopNum > 5) { // upper level shop, large Y offset
						localY += 21;
						shopNum -= 6; // essentially modulo 6
					}
					localX += shopNum * 6;
					d = new Vector2 (localX, localY);
				}
				distFlag = false;
				/*if (manhattanDist (s.transform.position, d) > window-1) {
					distFlag = true;
				}*/
			}
			s.setDest (d);
		} else {
			d = s.getDest ();
		}
		// then do A* to find a (window-length segment of a) path to the destination
		List<Node> close = new List<Node>();
		List<Node> fringe = new List<Node> ();
		Node starting = default(Node);
		if (s.transform.position.z < 9) {
			starting = new Node (new Vector2 (s.transform.position.x, s.transform.position.z), 0);
		} else {
			starting = new Node (new Vector2 (s.transform.position.x, s.transform.position.z-6+stairLength), 0);
		}
		starting.g = 0f;
		starting.f = manhattanDist (starting.position, d);
		fringe.Add (starting);
		while (fringe.Count > 0) {
			Node current = fringe[0];
			for (int i=0; i<fringe.Count; i++) {
				if (fringe [i].f < current.f) {
					current = fringe [i];
				}/* else if (fringe [i].f - fringe [i].g < current.f - current.g) {
					current = fringe [i];
				}*/
			}
			if (current.position == d) {
				// pathCreate and assign to s
				s.setPath (pathCreate (current, close));
				return;
			} else if (current.time >= window-1) {
				Debug.Log ("Failed to reach destination in window.");
				for (int t = window - 1; t > current.time; t--) {
					grid[(int)current.position.x][(int)current.position.y][t] = true;
				}
				s.setPath (pathCreate (current, close));
				return;
			}
			/*} else if (current.time >= 4) {
				for (int j=0; j<close.Count; j++) {
					if (manhattanDist(close[j].position, d) < manhattanDist(current.position, d)) {
						current = close[j];
					}
				}
				s.setPath (pathCreate (current, close));	
			}*/
			fringe.Remove (current);
			close.Add (current);
			Node[] neighbours = new Node[5];
			// 5 neighbours at current.time +1
			// move up
			neighbours [0] = new Node (new Vector2(current.position.x, current.position.y+1), current.time+1);
			// move down
			neighbours [1] = new Node (new Vector2(current.position.x, current.position.y-1), current.time+1);
			// move right
			neighbours [2] = new Node (new Vector2(current.position.x+1, current.position.y), current.time+1);
			// move left
			neighbours [3] = new Node (new Vector2(current.position.x-1, current.position.y), current.time+1);
			// wait at cell
			neighbours [4] = new Node (current.position, current.time+1);
			for (int i=0; i<neighbours.Length; i++) {
				if (close.Contains (neighbours[i])) {
					continue;
				} else if (getCell ((int)neighbours[i].position.x, (int)neighbours[i].position.y, neighbours[i].time)) {
					// if neighbour is impassable, add to close and ignore
					close.Add (neighbours[i]);
					continue;
				}
				fringe.Add (neighbours[i]);
				float gNB = current.g + manhattanDist(current.position, neighbours[i].position);
				if (gNB >= neighbours[i].g) {
					continue;
				}
				fringe.Remove (neighbours [i]);
				neighbours [i].parent = current.position;
				neighbours [i].g = gNB;
				neighbours [i].f = neighbours[i].g + manhattanDist (neighbours[i].position, d);
				if (i == 4) {
					neighbours [i].f++;
				}
				fringe.Add (neighbours [i]);
			}
		}

	}

	LinkedList<Vector2> pathCreate (Node final, List<Node> close) {
		LinkedList<Vector2> path = new LinkedList<Vector2> ();
		Node current = final;
		while (!current.parent.Equals(default(Vector2))) {
			// add node to path
			path.AddFirst (current.position);
			// make cell impassable
			grid [(int)current.position.x] [(int)current.position.y] [current.time] = true;
			current = close.Find (p => (p.position.Equals(current.parent)) && (p.time == current.time-1));
			// make "tail" cell impassable
			grid [(int)current.position.x] [(int)current.position.y] [current.time+1] = true;
		}
		path.AddFirst (current.position);
		grid [(int)current.position.x] [(int)current.position.y] [current.time] = true;
		if (path.Count > window) {
			Debug.Log ("A path of length greater than the window was created.");
		}
		return path;
	}

	float manhattanDist (Vector2 s, Vector2 t) {
		return (float) Mathf.Abs (s.x - t.x) + Mathf.Abs (s.y - t.y);
	}
}

// Node type is for A*
public struct Node {
	public Vector2 position;
	public int time;
	public Vector2 parent;
	public float g;
	public float f;

	public Node (Vector2 pos, int t) {
		position = pos;
		time = t;
		parent = default(Vector2);
		g = Mathf.Infinity;
		f = Mathf.Infinity;
	}
}