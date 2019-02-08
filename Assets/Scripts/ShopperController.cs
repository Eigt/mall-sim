using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShopperController : MonoBehaviour {

	public bool atDest;

	private GameController GameController;
	private LinkedListNode<Vector2> current;
	private Vector2 dest;
	private LinkedList<Vector2> path = new LinkedList<Vector2>();

	// initialization
	void Start () {
		GameController = GameObject.FindWithTag ("GameController").GetComponent<GameController> ();
		current = path.First;
	}
	
	// Update is called once per frame
	void Update () {
		if (atDest == false) {
			Move (Time.deltaTime);
		}
	}

	void Move (float delta) {
		if (current.Next != null) {
			Vector3 target = transform.position;
			float step = delta;
			// movement interval of distance on stairs depends on length
			// a shopper crossing a stair has 7 units z-units and 3 y-units to travel
			float intZ = 7f / (GameController.stairLength + 1f);
			float intY = 3f / (GameController.stairLength + 1f);

			if (current.Next.Value.y < 9f) {
				// y offset for lower level movement
				target = new Vector3 (current.Next.Value.x, -2.5f, current.Next.Value.y);
			}
			if (current.Next.Value.y >= 9f + GameController.stairLength) {
				// z offset for upper level movement
				target = new Vector3 (current.Next.Value.x, 0.5f, 15 + current.Next.Value.y - (9 + GameController.stairLength));
			}
			if (current.Next.Value.y >= 9f && current.Next.Value.y < 9f + GameController.stairLength) {
				// special case movement for stairways (z and y)
				float nextZ = 8f + intZ * (current.Next.Value.y - 8f);
				float nextY = -2.5f + intY * (current.Next.Value.y - 8f);
				target = new Vector3 (current.Next.Value.x, nextY, nextZ);
				step = delta * Mathf.Sqrt (intZ * intZ + intY * intY);
			}
			if (current.Value.y >= 9f && current.Value.y < 9f + GameController.stairLength) {
				// speed on stairs adjusts accoding to stair length (backup check in case shopper is leaving stair on current move)
				step = delta * Mathf.Sqrt (intZ * intZ + intY * intY);
			}
			transform.position = Vector3.MoveTowards (transform.position, target, step);

			if (transform.position == target) {
				current = current.Next;
			}
		} else { // if shopper is at destination
			if (transform.position.x == dest.x && transform.position.z == dest.y) {
				atDest = true;
				Debug.Log ("Reached destination in " + path.Count + " steps.");
			}
		}
	}
	// set and get methods for GameController
	public void setDest (Vector2 newDest) {
		dest = newDest;
		atDest = false;
	}
	public Vector2 getDest () {
		return dest;
	}

	public void setPath (LinkedList<Vector2> newPath) {
		path.Clear ();
		LinkedListNode<Vector2> head = newPath.First;
		current = head;
		Vector2 newFirst = new Vector2 (head.Value.x, head.Value.y);
		path.AddLast (newFirst);
		while (head.Next != null) {
			head = head.Next;
			Vector2 newNode = new Vector2 (head.Value.x, head.Value.y);
			path.AddLast (newNode);
		}
		//current = path.First;
		atDest = false;
	}
}
