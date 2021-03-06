﻿using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class DoorButton : BaseButtonController {
	public WallController WallPrefab;
	// Use this for initialization
	void Start () {
		GetComponent<Button>().onClick.AddListener(OnClick);
	}

	protected override void OnClick()
	{
		base.OnClick();
		M.House.SetHouseMode(HouseModes.SetDoors,WallPrefab.gameObject);
	}
}
