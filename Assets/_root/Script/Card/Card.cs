using _root.Script.Network;
using UnityEngine;

namespace _root.Script.Card
{
public class Card : MonoBehaviour
{
	[HideInInspector] public SpriteRenderer frontSide;
	[HideInInspector] public SpriteRenderer backSide;

	public void Start()
	{
		frontSide       = transform.GetChild(0).GetComponent<SpriteRenderer>();
		backSide        = transform.GetChild(1).GetComponent<SpriteRenderer>();
		backSide.sprite = Resources.Load<Sprite>("Card/card_frame");
	}
}
}