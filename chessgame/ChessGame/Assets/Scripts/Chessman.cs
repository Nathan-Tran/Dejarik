using UnityEngine;
using System.Collections;

public abstract class Chessman : MonoBehaviour
{
	public int CurrentTrack{set;get;}
	public int CurrentSegment{set;get;}
	public bool isWhite;

	public void SetPosition(int a,int b)
	{
		CurrentTrack = a;
		CurrentSegment = b;
	}

	public virtual bool[,] PossibleMove()
	{
		return new bool[8,8];
	}
}