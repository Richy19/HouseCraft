﻿using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class NumericFieldController : BaseController {

	float Speed = 1000;
	float TimeToEquality = 1;
	Text text;
	int fieldValue = 0;
	int displayedValue = 0;

	protected override void Awake ()
	{
		base.Awake ();
		text = GetComponent<Text>();
	}

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
		if(displayedValue!=fieldValue)
		{
			int  iDelta = fieldValue - displayedValue;

			iDelta=(int)Mathf.Sign(iDelta)*Mathf.Min( (int)(Speed*Time.smoothDeltaTime),Mathf.Abs(iDelta));

			displayedValue+=iDelta;
			UpdateText();

		}
	}

	public int Value{
		get{
			return fieldValue;
		}
		set{
			fieldValue = value;
			UpdateSpeed();
		}
	}
	public void AddValue(int delta)
	{
		fieldValue+=delta;
		UpdateSpeed();
	}

	protected void UpdateSpeed()
	{
		float delta = Mathf.Abs(fieldValue-displayedValue);
		Speed = delta/TimeToEquality;
	}

	protected void UpdateText()
	{
		text.text = displayedValue.ToString();
	}
}
