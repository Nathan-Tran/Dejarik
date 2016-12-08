using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BoardManager : MonoBehaviour
{
	public static BoardManager Instance{set;get;}
	private bool[,] allowedMoves{set;get;}

	public Chessman[,] Chessmans{ set; get;}
	private Chessman selectedChessman;

	private const float TILE_SIZE = 1.0f;
	private const float TILE_OFFSET = 0.5f;

	private int selectionX = -1;
	private int selectionY = -1;

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
                if (selectionX >= 0 && selectionY >= 0)
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

	private void MoveChessman(int x,int y)
	{
        Debug.Log("Move Chessman called");
        Chessman c = Chessmans[x, y];

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
            selectedChessman.transform.position = GetTileCenter(x, y);
            selectedChessman.SetPosition(x, y);
            Chessmans[x, y] = selectedChessman;
            isWhiteTurn = !isWhiteTurn;
        }
        else if(c == null)
        {
            Chessmans[selectedChessman.CurrentX, selectedChessman.CurrentY] = null;
            selectedChessman.transform.position = GetTileCenter(x, y);
            selectedChessman.SetPosition(x, y);
            Chessmans[x, y] = selectedChessman;
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
                selectionX = -1;
                selectionY = -1;
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

                    selectionX = -1;
                    selectionY = -1;
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
            selectionX = (int)hit.point.x;
			selectionY = (int)hit.point.z;
		}
		else
        {
            selectionX = -1;
			selectionY = -1;
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

        SpawnChessman(5, 1, 1);

        SpawnChessman(5, 2, 7);

        // Spawn the white team!
        /*
		//King
		SpawnChessman (0,3,0);

		//Queen
		SpawnChessman (1,4,0);

		//Rooks
		SpawnChessman (2,0,0);
		SpawnChessman (2,7,0);

		//Bishops
		SpawnChessman (3,2,0);
		SpawnChessman (3,5,0);

		//Knights
		SpawnChessman (4,1,0);
		SpawnChessman (4,6,0);

		//Pawns
		for (int i = 0; i < 8; i++)
			SpawnChessman (5,i,1);

		// Spawn the Black team!

		//King
		SpawnChessman (6,4,7);

		//Queen
		SpawnChessman (7,3,7);

		//Rooks
		SpawnChessman (8,0,7);
		SpawnChessman (8,7,7);

		//Bishops
		SpawnChessman (9,2,7);
		SpawnChessman (9,5,7);

		//Knights
		SpawnChessman (10,1,7);
		SpawnChessman (10,6,7);

		//Pawns
		for (int i = 0; i < 8; i++)
			SpawnChessman (11,i,6);

        */
    }

	private Vector3 GetTileCenter(int x,int y)
	{
        //8x8 grid math

        /*Vector3 origin = Vector3.zero;
		origin.x += (TILE_SIZE * x) + TILE_OFFSET;
		origin.z += (TILE_SIZE * y) + TILE_OFFSET;
		return origin;*/

        //Dejarik board math

        Vector3 origin = Vector3.zero;
        float tileLength = 3.2f;

        Quaternion lol = Quaternion.AngleAxis(15, Vector3.up);

        if (x == 0)
        {
            return origin;
        }
        else
        {
            //Maybe normalize the vector here?

            return origin + (Quaternion.AngleAxis(15 + (30 * y), Vector3.up) * Vector3.forward) * (tileLength * x);
        }

    }

	private void DrawChessboard()
	{
        float lineLength = 8f;

        //Draw debug circles

        for (int i = 0; i <= 12; i++)
        {
            Vector3 start = (Quaternion.AngleAxis((30 * i), Vector3.up) * Vector3.forward);
            Debug.DrawLine(Vector3.zero, start * lineLength);
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