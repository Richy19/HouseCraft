﻿using UnityEngine;
using System.Collections;

public class Manager : MonoBehaviour {

	enum Modes{
		Conditions,Build,Process,Verify,Sold
	}

	Modes state = Modes.Conditions;

	CameraController camCon;

	int mouseBlockCount = 0;

	public Stats Statistic;
	public HouseController House;
	public OverlayController Overlay;

	public UIController UI;

	public bool BlockMouseInput 
	{
		get
		{
			return mouseBlockCount>0;
		}
		set
		{
			if(value)
				mouseBlockCount++;
			else
				mouseBlockCount--;
			if(mouseBlockCount<0)
				mouseBlockCount=0;
		}
	}

	void Awake()
	{
		camCon = Camera.main.GetComponent<CameraController>();
	}
	// Use this for initialization
	void Start () {

	}
	
	// Update is called once per frame
	void Update () {
	
	}

	public void Scroll(Vector2 delta)
	{
		camCon.Scroll(delta);
	}

	public void OnSale()
	{
		if(state== Modes.Build)
		{
			state = Modes.Process;

			House.SetHouseMode(HouseModes.Sale,null);

		}

	}

	public void OnCancelSale()
	{
		if(state==Modes.Verify)
		{
			state = Modes.Build;
			UI.OnShowBuild();
			Overlay.RemoveOverlay();
			House.SetHouseMode(HouseModes.SetWalls,null);
		}
	}

	public void OnProcessed(Segmentator s, Evaluator e)
	{
		state = Modes.Verify;
		UI.OnShowVerify();
	}

	public void OnVerified()
	{
		state = Modes.Sold;
		UI.OnShowResults();
	}

	public void OnBuild()
	{
		state = Modes.Build;
		UI.OnShowBuild();
	}
}
