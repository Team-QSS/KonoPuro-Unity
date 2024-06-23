using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using _root.Script.Data;
using _root.Script.Ingame;
using _root.Script.Manager;
using _root.Script.Network;
using TMPro;
using UnityEngine;

public class IngameCardInfoUi : MonoBehaviour
{
	private TextMeshProUGUI nameT;
	private TextMeshProUGUI timeT;
	private TextMeshProUGUI descriptionT;

	private void Awake()
	{
		var tmps = GetComponentsInChildren<TextMeshProUGUI>();
		nameT        = tmps[0];
		timeT        = tmps[1];
		descriptionT = tmps[2];
	}

	private void Start()
	{
		SetActive(false);
	}

	public void SetActive(bool active)
	{
		gameObject.SetActive(active);
	}

	public void SetInfo(IngameCard card)
	{
		if (card == null || card.type == IngameCardType.Deck ||
		    (card.type is not (IngameCardType.Field or IngameCardType.Student) && !card.isMine))
		{
			SetActive(false);
			return;
		}

		SetActive(true);

		if (card.type == IngameCardType.Student)
		{
			var studentData = card.GetStudentData();
			if (studentData != null)
			{
				var sInfo = GameStatics.studentCardDictionary[studentData.cardType];
				nameT.text    = sInfo.name;
				timeT.enabled = false;
				var description = studentData.passives.Select(passive => GameStatics.passiveDictionary[passive])
				                      .Aggregate(sInfo.description,
				                                 (current, pInfo) =>
						                                 $"{current}{pInfo.name}\n{pInfo.description}\n \n");
				descriptionT.text = description;
			}
			else
			{
				var defaultData = card.GetData();
				nameT.text = defaultData.cardType;
			}
		}
		else
		{
			var data = card.GetCardData();
			if (data == null) return;
			timeT.enabled = true;
			var info = GameStatics.defaultCardDictionary[data.defaultCardType];
			nameT.text        = info.name;
			timeT.text        = $"Usage : {info.time}";
			descriptionT.text = info.description;
		}
	}

	public void SetInfo(Tiers ability)
	{
		SetActive(true);

		timeT.enabled = true;
		var info = GameStatics.tierDictionary[ability];
		nameT.text        = info.name;
		timeT.text        = $"Usage : {info.time}";
		descriptionT.text = info.description;
	}
}