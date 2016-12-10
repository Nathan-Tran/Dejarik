using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BoardManager : MonoBehaviour
{
	public static BoardManager Instance{set;get;}
	private bool[,] allowedMoves{set;get;}

	public Chessman[,] Chessmans{ set; get;}
	private Chessman selectedChessman;

	//private const float TILE_SIZE = 1.0f;
	//private const float TILE_OFFSET = 0.5f;

    //Let -10000 be the value for an invalid selection
	private float selectionX = -10000;
	private float selectionY = -10000;

    private const float tileOffset = 3.2f;
    private const float innerRingRadius = 1.8f;
    private const float middleRingRadius = 4.3f;
    private const float boardRadius = 8.0f;

    public List<GameObject> chessmanPrefabs;
	private List<GameObject> activeChessman;

	private Material previousMat;
	public Material selectedMat;

	public int[] EnPassantMove{ set; get;}

	private Quaternion orientation = Quaternion.Euler(0,180,0);

	public bool isWhiteTurn = true;

	private void Start()
	{
		Instance = this;
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
                    MoveChessman(selectionX, selectionY);
                }
            }
        }

        DrawChessboard();
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
        
		bool hasAtleastOneMove = false;
		allowedMoves = selectedChessman.PossibleMove ();
		for (int i = 0; i < 8; i++)
			for (int j = 0; j < 8; j++)
				if (allowedMoves [i, j])
					hasAtleastOneMove = true;
        /*
		if (!hasAtleastOneMove)
			return;
        */

        //selectedChessman = Chessmans [x, y];
        previousMat = selectedChessman.GetComponentInChildren<MeshRenderer> ().material;
		selectedMat.mainTexture = previousMat.mainTexture;
		selectedChessman.GetComponentInChildren<MeshRenderer> ().material = selectedMat;
		//BoardHighlights.Instance.HighlightAllowedMoves (allowedMoves);
	}

	private void MoveChessman(float x, float y)
	{
        //Check if the x and y are even on the dejarik board, then calculate what quadrant was intended to be selected
        //Else return

        float distanceFromCentre = Mathf.Sqrt((x * x) + (y * y));
        float sectorAngle = 0;

        int boardTrack, boardSector;

        if (distanceFromCentre > boardRadius)
        {
            return;
        }

        //XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
        //XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
        //These are arbitrary distances
        //XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
        //XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
        if (distanceFromCentre < 1.8f)
        {
            Debug.Log("CLICKED ON CENTRE");
            boardTrack = 0;
            boardSector = 0;
        }
        else
        {
            if(distanceFromCentre < 4.3f)
            {
                Debug.Log("CLICKED ON MIDDLE RING");
                boardTrack = 1;
            }
            else
            {
                Debug.Log("CLICKED ON OUTER RING");
                boardTrack = 2;
            }

            if(x >= 0)
            {
                if(y < 0)
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

        Chessman c = Chessmans[boardTrack, boardSector];

        if (c != null && c.isWhite != isWhiteTurn)
        {
            Debug.Log("Passed the opposite team check");
            //Capture a piece

            //If it is the king
            if (c.GetType () == typeof(King))
			{
				EndGame ();
				return;
			}

			activeChessman.Remove(c.gameObject);
			Destroy (c.gameObject);

            Chessmans[selectedChessman.CurrentX, selectedChessman.CurrentY] = null;
            selectedChessman.transform.position = GetTileCenter(boardTrack, boardSector);
            selectedChessman.SetPosition(boardTrack, boardSector);
            Chessmans[boardTrack, boardSector] = selectedChessman;
            isWhiteTurn = !isWhiteTurn;
        }
        else if(c == null)
        {
            Chessmans[selectedChessman.CurrentX, selectedChessman.CurrentY] = null;
            selectedChessman.transform.position = GetTileCenter(boardTrack, boardSector);
            selectedChessman.SetPosition(boardTrack, boardSector);
            Chessmans[boardTrack, boardSector] = selectedChessman;
            isWhiteTurn = !isWhiteTurn;
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
                    //BoardHighlights.Instance.Hidehighlights();
                    selectedChessman = null;

                    selectionX = -10000;
                    selectionY = -10000;
                }
                else
                {
                    selectionX = hit.transform.gameObject.GetComponentInParent<Chessman>().CurrentX;
                    selectionY = hit.transform.gameObject.GetComponentInParent<Chessman>().CurrentY;
                }
            }
        }
        else if (Physics.Raycast (Camera.main.ScreenPointToRay (Input.mousePosition), out hit, 25.0f, LayerMask.GetMask ("ChessPlane"))) 
		{
            selectionX = hit.point.x;
			selectionY = hit.point.z;
		}
		else
        {
            selectionX = -10000;
			selectionY = -10000;
		}
	}

	private void SpawnChessman(int index,int x,int y)
	{
		GameObject go = Instantiate (chessmanPrefabs [index], GetTileCenter(x,y), orientation) as GameObject;
		go.transform.SetParent (transform);
		Chessmans [x, y] = go.GetComponent<Chessman> ();
		Chessmans [x, y].SetPosition (x, y);
		activeChessman.Add (go);
	}

	private void SpawnAllChessmans()
	{
		activeChessman = new List<GameObject> ();
		Chessmans = new Chessman[3, 12];
        //EnPassantMove = new int[2]{-1,-1};

        SpawnChessman(0, 2, 0);

        SpawnChessman(1, 2, 1);

        SpawnChessman(2, 2, 2);

        SpawnChessman(3, 2, 6);

        SpawnChessman(4, 2, 7);

        SpawnChessman(5, 2, 8);
    }

	private Vector3 GetTileCenter(float x, float y)
	{
        //8x8 grid math

        /*Vector3 origin = Vector3.zero;
		origin.x += (TILE_SIZE * x) + TILE_OFFSET;
		origin.z += (TILE_SIZE * y) + TILE_OFFSET;
		return origin;*/

        //Dejarik board math

        Vector3 origin = Vector3.zero;

        if (x == 0)
        {
            return origin;
        }
        else
        {
            //Maybe normalize the vector here?

            return origin + (Quaternion.AngleAxis(15 + (30 * y), Vector3.up) * Vector3.forward) * (tileOffset * x);
        }

    }

	private void DrawChessboard()
	{
        /*
         * 
         *Draw debug circles
         *
         */

        for (int i = 0; i <= 12; i++)
        {
            Vector3 start = (Quaternion.AngleAxis((30 * i), Vector3.up) * Vector3.forward);
            Debug.DrawLine(Vector3.zero, start * boardRadius);
        }

        /*Vector3 widthLine = Vector3.right * 8;
		Vector3 heigthLine = Vector3.forward * 8;

		for (int i = 0; i <= 8; i++) 
		{
			Vector3 start = Vector3.forward * i;
			Debug.DrawLine (start, start + widthLine);
			for (int j = 0; j <= 8; j++) 
			{
				start = Vector3.right * j;
				Debug.DrawLine (start, start + heigthLine);
			}
		}*/

        // Draw the selection
        /*if (selectionX >= 0 && selectionY >= 0)
		{	
			Debug.DrawLine (
				Vector3.forward * selectionY + Vector3.right * selectionX,
				Vector3.forward * (selectionY + 1) + Vector3.right * (selectionX + 1));

			Debug.DrawLine (
				Vector3.forward * (selectionY + 1 )+ Vector3.right * selectionX,
				Vector3.forward * selectionY + Vector3.right * (selectionX + 1));
		}*/
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