
using System;
public class LogicBoiler : ILogicObject
{
	public bool HasHotWater = false;

	public LogicBoiler(MapRect objectRect, int cost) 
		: base(CellObjects.Sink,objectRect,cost)
	{
	
	}
}

