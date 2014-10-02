﻿using UnityEngine;
using System.Collections.Generic;

public class Room  {

	public readonly Color[] RoomColors = new Color[] {
		new Color(1,0,0, 0.4f),
		new Color(0,1,0, 0.4f),
		new Color(0,0,1, 0.4f),
		new Color(1,1,0, 0.4f),
		new Color(0,1,1, 0.4f),
		new Color(1,0,1, 0.4f),
		new Color(0.5f,1,0, 0.4f)

	};
	public RoomType TypeOfRoom = RoomType.Unknown;
	public bool Entrance = false;
	public bool GarageGate = false;
	public int Number=0;


	List<CellController> Cells = new List<CellController>();
	public Dictionary<int,LogicCell> LogicCells = new Dictionary<int, LogicCell>();
	//public List<MapPoint> VirtualCells = new List<MapPoint>();
	public List<Door> Doors =new List<Door>();
	public List<Room> ConnectedTo = new List<Room>();
	public List<ILogicObject> LogicObjects = new List<ILogicObject>();
	public bool Empty = true;

	public MapPoint LabelPos = null;
	//public int LabelSize;
	public Color RoomColor{
		get{
			return RoomColors[Number % RoomColors.Length];
		}
	}

	public int Size{
		get{
			return LogicCells.Count;
		}
	}
	public string Name{
		get{
			return System.Enum.GetName(typeof(RoomType),TypeOfRoom);
		}
	}

	public void AddDoor(Door d)
	{
		if(Doors.Contains(d))
			return;
		Doors.Add(d);

	}

	public void AddCell(LogicCell cell, LogicCache cache, CellController phCell)
	{
	
		LogicCells[cell.Position.toInt()] = cell;

		if(Cells.Contains(phCell))
			return;
		Cells.Add(phCell);
		if(phCell.CellObject!=null)
		{
			ILogicObject lo = phCell.CellObject.Fabricate();
			if(lo.ObjectType!=phCell.CellObject.GetCellObjectType())
				throw new UnityException("Wrong object type!");
			if(lo!=null)
			{
				LogicObjects.Add(lo);
				cache.Objects.Add(lo);
				switch(lo.ObjectType)
				{
				case CellObjects.Shower:
				case CellObjects.Sink:
				case CellObjects.Bathtub:
					cache.HotWaterConsumers.Add(lo as IHotWaterConsumer);
					break;
				case CellObjects.Boiler:
					cache.Boilers.Add(lo as LogicBoiler);
					break;
				case CellObjects.Fireplace:
					break;
				case CellObjects.Heater:
					cache.Heaters.Add(lo as LogicHeater);
					break;
				case CellObjects.HeatingPipe:
					cache.HeatingPipes.Add(lo as LogicHeatingPipe);
					break;
				case CellObjects.Hob:
					break;
				case CellObjects.Riser:
					cache.Risers.Add(lo as LogicRiser);
					break;
				case CellObjects.Toilet:
					break;
				case CellObjects.Ventshaft:
					break;
				}
			}


		}
	}

	public bool ContainsRectangle(int sizeX, int sizeY)
	{

		foreach(CellController c in Cells)
		{
			if(c.IsRectFree(
				new MapRect(c.Position.X, c.Position.Y, c.Position.X+sizeX-1,c.Position.Y+sizeY-1),false))
			{
			   return true;
			}
		}
		return false;

	}

	public void PrepareToRecognition()
	{
		ConnectedTo.Clear();
		LabelPos = null;
		Empty = true;
		foreach(CellController c in Cells)
		{
			if(LabelPos==null)
			{
				LabelPos = c.Position;
			}
			else if(LabelPos.X>=c.Position.X && LabelPos.Y<=c.Position.Y)
			{
				LabelPos = c.Position;
			}

			if(c.CellObject!=null)
			{
				Empty = false;
			}
		}

		foreach(Door d in Doors)
		{
			if(d.Rooms.Count == 2)
			{
				ConnectedTo.Add(d.GetAnotherRoom(this));
			}
		}
		
	}


	public bool Contains(CellObjects o)
	{
		foreach(ILogicObject l in LogicObjects)
		{
			if(l.ObjectType==o)
				return true;
		}

		return false;
	}

	public void ForeachCell(System.Action<MapPoint> action)
	{
		foreach(LogicCell c in LogicCells.Values)
		{
			action(c.Position);
		}

	}
}
