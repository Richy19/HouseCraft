﻿using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class BuildCommandsPanel : MonoBehaviour {

	public Button RotateButton;

	public void ShowRotateButton(bool show)
	{
		RotateButton.gameObject.SetActive(show);
	}



	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
