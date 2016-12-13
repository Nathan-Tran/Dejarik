using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BoardManager : MonoBehaviour
{
	public static BoardManager Instance{set;get;}
	private bool[,] allowedMoves{set;get;}

	public Chessman[,] Chessmans{ set; get;}
	private Chessman selectedChessman;

    Vector3 origin;

    //Let -10000 be the value for an invalid selection
    private float selectionX = -10000;
	private float selectionY = -10000;

    //I should base these values off the scale of the board
    private const float tileOffset = 0.15f;
    private const float innerRingRadius = 0.06f;
    private const float middleRingRadius = 0.23f;
    private const float boardRadius = 0.37f;

    public List<GameObject> chessmanPrefabs;
	private List<GameObject> activeChessman;

    private Material previousMat;
	public Material selectedMat;

	public bool isWhiteTurn = true;

	private void Start()
	{
		Instance = this;

        //The center of the game board is the center of the box collider on the BoardPlane gameObject
        origin = GetComponentInChildren<BoxCollider>().transform.position;

        SpawnAllChessmans ();
	}

	private void Update()
	{
		if (Input.GetMouseButtonDown (0))
        {
            UpdateSelection();

            if (selectedChessman != null)
            {
                //If the selection even makes contact with the plane collider
                if (selectionX > -10000 && selectionY > -10000)
                {
                    MoveChessman(CalculateBoardQuadrant(selectionX, selectionY));
                }
            }
        }
    }

	private void ActivateChessman()
	{
        if (selectedChessman == null)
			return;

        if (selectedChessman.isWhite != isWhiteTurn)
        {
            selectedChessman = null;
            return;
        }

        /*bool hasAtleastOneMove = false;
		allowedMoves = selectedChessman.PossibleMove ();
		for (int i = 0; i < 8; i++)
			for (int j = 0; j < 8; j++)
				if (allowedMoves [i, j])
					hasAtleastOneMove = true;
        
		if (!hasAtleastOneMove)
			return;
        */

        //selectedChessman = Chessmans [x, y];
        previousMat = selectedChessman.GetComponentInChildren<MeshRenderer> ().material;
		selectedMat.mainTexture = previousMat.mainTexture;
		selectedChessman.GetComponentInChildren<MeshRenderer> ().material = selectedMat;
		//BoardHighlights.Instance.HighlightAllowedMoves (allowedMoves);
	}

    private int[] CalculateBoardQuadrant(float x, float y)
    {
        Debug.Log("The points to be calculated are");
        Debug.Log(x);
        Debug.Log(y);

        //Check if the x and y are even in the dejarik circle, then calculate what quadrant was intended to be selected
        //Else return null

        float distanceFromCentre = Mathf.Sqrt((x * x) + (y * y));
        float sectorAngle = 0;

        int boardTrack, boardSector;
        int[] boardQuadrant = new int[2];

        if (distanceFromCentre > boardRadius)
        {
            Debug.Log("Clicked on invalid board location");

            return null;
        }

        if (distanceFromCentre < innerRingRadius)
        {
            boardTrack = 0;
            boardSector = 0;
        }
        else
        {
            if (distanceFromCentre < middleRingRadius)
            {
                boardTrack = 1;
            }
            else
            {
                boardTrack = 2;
            }

            if (x >= 0)
            {
                if (y < 0)
                {
                    sectorAngle += 90;

                    boardSector = (int)(sectorAngle + (Mathf.Atan((y * -1) / x) * Mathf.Rad2Deg)) / 30;
                }
                else
                {
                    boardSector = (int)(sectorAngle + (Mathf.Atan(x / y) * Mathf.Rad2Deg)) / 30;
                }
            }
            else
            {
                sectorAngle += 180;

                if (y >= 0)
                {
                    sectorAngle += 90;

                    boardSector = (int)(sectorAngle + (Mathf.Atan(y / (x * -1)) * Mathf.Rad2Deg)) / 30;
                }
                else
                {
                    boardSector = (int)(sectorAngle + (Mathf.Atan(x / y) * Mathf.Rad2Deg)) / 30;
                }
            }

        }

        //I store the value in the array only at the end of this method so that "boardTrack" and "boardSector" are used throughout, improving readability.
        boardQuadrant[0] = boardTrack;
        boardQuadrant[1] = boardSector;

        Debug.Log("The calculated board position is:");
        Debug.Log(boardTrack);
        Debug.Log(boardSector);

        return boardQuadrant;

    }

    private void MoveChessman(int[] boardQuadrant)
    {
        if (boardQuadrant != null)
        {
            Chessman c = Chessmans[boardQuadrant[0], boardQuadrant[1]];

            if (((c != null && c.isWhite != isWhiteTurn) || (c == null)))
            {
                if (c != null && c.isWhite != isWhiteTurn)
                {
                    Debug.Log("Passed the opposite team check");
                    //Capture a piece

                    activeChessman.Remove(c.gameObject);
                    Destroy(c.gameObject);
                }

                Chessmans[selectedChessman.CurrentTrack, selectedChessman.CurrentSegment] = null;

                Transform gamePieceTransform = selectedChessman.transform;
                gamePieceTransform.position = GetTileCenter(boardQuadrant[0], boardQuadrant[1]);
                gamePieceTransform.rotation = GetBoardOrientation(gamePieceTransform.position);

                selectedChessman.SetPosition(boardQuadrant[0], boardQuadrant[1]);
                Chessmans[boardQuadrant[0], boardQuadrant[1]] = selectedChessman;
                isWhiteTurn = !isWhiteTurn;
            }
        }

		selectedChessman.GetComponentInChildren<MeshRenderer> ().material = previousMat;
		//BoardHighlights.Instance.Hidehighlights ();
		selectedChessman = null;
	}

	private void UpdateSelection()
	{
		if (!Camera.main)
			return;

		RaycastHit hit;

        if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, 25.0f, LayerMask.GetMask("GamePieces")))
        {
            if(selectedChessman == null)
            {
                selectedChessman = hit.transform.gameObject.GetComponentInParent<Chessman>();
                ActivateChessman();
                selectionX = -10000;
                selectionY = -10000;
            }
            else
            {
                /*
                 * What happens when a friendly or enemy piece is clicked on while another (or even the same) piece is selected.
                 * 
                 * This is a TO DO for after the radial movement is implemented as currently we need to get the grid 
                 * coordinates (not the translation) of a piece in order to check if we should let the player move there
                 * or if we should just consider it an invalid move and deselect everything.
                 */

                //If the player clicked on the same piece twice
                if (selectedChessman == hit.transform.gameObject.GetComponentInParent<Chessman>())
                {
                    selectedChessman.GetComponentInChildren<MeshRenderer>().material = previousMat;
                    
                    selectedChessman = null;

                    selectionX = -10000;
                    selectionY = -10000;
                }
                else
                {

                    Debug.Log("clicked on a different piece");

                    /*
                     * 
                     * UGLY and WRONG
                     * 
                     */
                    //selectionX = hit.transform.gameObject.transform.position.x;// - Instance.transform.position.x;
                    //selectionY = hit.transform.gameObject.transform.position.z;// - Instance.transform.position.z;

                    Vector3 relativeHitPosition = Instance.transform.InverseTransformPoint(hit.transform.gameObject.transform.position);

                    selectionX = relativeHitPosition.x;
                    selectionY = relativeHitPosition.z;
                }
            }
        }
        else if (Physics.Raycast (Camera.main.ScreenPointToRay (Input.mousePosition), out hit, 25.0f, LayerMask.GetMask ("BoardPlane"))) 
		{
            Vector3 relativeHitPosition = Instance.transform.InverseTransformPoint(hit.point);

            selectionX = relativeHitPosition.x;
            selectionY = relativeHitPosition.z;

        }
        else
        {
            selectionX = -10000;
			selectionY = -10000;
		}
	}

	private void SpawnChessman(int index,int x,int y)
	{
        Vector3 spawnPoint = GetTileCenter(x, y);

        GameObject go = Instantiate(chessmanPrefabs[index], spawnPoint, GetBoardOrientation(spawnPoint)) as GameObject;

        go.transform.SetParent (transform);
		Chessmans [x, y] = go.GetComponent<Chessman> ();
		Chessmans [x, y].SetPosition (x, y);
		activeChessman.Add (go);
	}

    private Quaternion GetBoardOrientation(Vector3 boardTranslation)
    {
        //Quaternion facingDirection = Quaternion.FromToRotation(Vector3.forward, boardTranslation);
        Quaternion facingDirection = Quaternion.FromToRotation(Vector3.forward, boardTranslation - origin);

        return facingDirection;
    }


    private void SpawnAllChessmans()
	{
		activeChessman = new List<GameObject> ();
		Chessmans = new Chessman[3, 12];
        //EnPassantMove = new int[2]{-1,-1};

        SpawnChessman(0, 2, 0);

        SpawnChessman(1, 2, 1);

        SpawnChessman(2, 2, 2);

        SpawnChessman(3, 2, 3);

        SpawnChessman(4, 2, 6);

        SpawnChessman(5, 2, 7);

        SpawnChessman(6, 2, 8);

        SpawnChessman(7, 2, 9);
    }

	private Vector3 GetTileCenter(float x, float y)
	{
        //8x8 grid math

        /*Vector3 origin = Vector3.zero;
		origin.x += (TILE_SIZE * x) + TILE_OFFSET;
		origin.z += (TILE_SIZE * y) + TILE_OFFSET;
		return origin;*/

        //Dejarik board math
        if (x == 0)
        {
            return origin;
        }
        else
        {
            //Maybe normalize the vector here?

            return origin + (Quaternion.AngleAxis(15 + (30 * y), Vector3.up) * Instance.transform.forward) * (tileOffset * x);
        }

    }

	private void EndGame()
	{
		if (isWhiteTurn)
			Debug.Log ("White team wins");
		else
			Debug.Log ("Black team wins");

		foreach (GameObject go in activeChessman)
			Destroy (go);

		isWhiteTurn = true;
		//BoardHighlights.Instance.Hidehighlights ();
		SpawnAllChessmans ();
	}
}