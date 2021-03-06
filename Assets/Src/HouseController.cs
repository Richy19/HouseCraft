﻿using UnityEngine;
using System;
using System.Collections.Generic;

[RequireComponent (typeof (HouseController))]
public class HouseController : BaseController {

	enum Modes{
		Idle, SetWalls, SetDoors, SetObject, RemoveWall,RemoveObject,Sale
	}



	public CellController CellPrefab;
	public WallController ThickWallPrefab;
	public WallController WallPrefab;
	public WallController WindowPrefab;
	public WallController EntrancePrefab;
	public WallController GaragePrefab;

	public PhantomController Phantom;
	public LevelConditions LevelConditions;

	Dictionary<int,CellController> cells = new Dictionary<int, CellController>();
	Dictionary<int,WallController> walls = new Dictionary<int, WallController>();

	GameObject selectedPrefab = null;

	public Rect MapBounds;

	CellController.CellRotation selectedRotation = CellController.CellRotation.None;

	Modes state = Modes.Idle;

	Vector3 markerPos;
	public Vector3 MarkerPosition{
		set{
			markerPos = value;
		}
	}

	public void RestoreCache(bool removeDuplicates)
	{
		CellController[] c = GetComponentsInChildren<CellController>();
		if(c.Length>0)
		{
			MapBounds.xMin = c[0].Position.X;
			MapBounds.yMin = c[0].Position.Y;
			MapBounds.xMax = MapBounds.xMin+1;
			MapBounds.yMax = MapBounds.yMin+1;
		}

		foreach(CellController cell in c)
		{
			if(cells.ContainsKey(cell.Position.toInt()))
			{
				if(removeDuplicates)
					GameObject.DestroyImmediate(cell.gameObject);

				continue;
			}
			else
			{
				cells.Add(cell.Position.toInt(),cell);
			}


			if(cell.Position.X>=MapBounds.xMax)
				MapBounds.xMax = cell.Position.X+1;

			if(cell.Position.X<MapBounds.xMin)
				MapBounds.xMin = cell.Position.X;

			if(cell.Position.Y>=MapBounds.yMax)
				MapBounds.yMax = cell.Position.Y+1;
			
			if(cell.Position.Y<MapBounds.yMin)
				MapBounds.yMin = cell.Position.Y;
		}
		
		WallController[] w = GetComponentsInChildren<WallController>();
		
		foreach(WallController wall in w)
		{
			if(walls.ContainsKey(wall.Position.toInt()))
			{
				if(removeDuplicates)
					GameObject.DestroyImmediate(wall.gameObject);
				continue;
			}
			else
			{
				walls.Add(wall.Position.toInt(),wall);
			}
		}
	}
	protected override void Awake ()
	{
		base.Awake ();
		selectedPrefab = WallPrefab.gameObject;
		RestoreCache(false);

		GetComponent<TapController>().OnTap+=OnTap;
	}
	// Use this for initialization
	void Start () {


		M.Statistic.StartPrice = LevelConditions.PricePerCell*cells.Count;
		M.Statistic.SellPrice = LevelConditions.ExpectedIncome;

		M.UI.bCostsPanel.EstimatedProfit.Value = M.Statistic.SellPrice;
		M.UI.bCostsPanel.HouseCost.Value = M.Statistic.StartPrice;

		M.UI.bCommandsPanel.ShowRotateButton(false);
	}
	
	// Update is called once per frame
	void Update () {

	}

	public void SwitchRotation()
	{
		selectedRotation = selectedRotation.Next();
		if(state!=Modes.SetObject || !Phantom.IsDisplayed())
			return;
		CellController cellPrefab = selectedPrefab.GetComponent<CellController>();
		
		cellPrefab.PrefabValidatePosition(M,Phantom.LastPhantomPosition,selectedRotation);

	}

	void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.blue;
		Gizmos.DrawWireCube(MapBounds.center,MapBounds.size);
		Gizmos.color = Color.red;    
		Gizmos.DrawWireCube(markerPos, new Vector3(1,1, 1) * 1.1f);
	}


	static readonly Corner[] cornersTable = {Corner.BottomLeft,Corner.BottomRight,Corner.TopLeft,Corner.TopRight}; 
	public void ForEachWall(MapPoint point, Action<WallPoint,WallController,Corner> action)
	{
		WallController wc = null;
		WallPoint wp;

		for(int i=0;i<4;i++)
		{
			wp = new WallPoint(point.X+i%2,point.Y+(i>1?1:0));

			if(wp.X<0 || wp.Y<0)
				continue;
			walls.TryGetValue(wp.toInt(),out wc);
			action(wp,wc,cornersTable[i]);
			wc = null;
		}



	}

	public bool IsInsideBuilding(WallPoint wallPoint)
	{
		bool res =
				cells.ContainsKey(wallPoint.toInt()) 
			&& cells.ContainsKey(new WallPoint(wallPoint.X-1,wallPoint.Y).toInt()) 
			&& cells.ContainsKey(new WallPoint(wallPoint.X,wallPoint.Y-1).toInt()) 
			&& cells.ContainsKey(new WallPoint(wallPoint.X-1,wallPoint.Y-1).toInt()) ;
		return res;
	}

	public CellController GetCell(MapPoint point)
	{
		if(point.X<0 || point.Y<0 || point.X>0xffff || point.Y>0xffff)
			return null;

		int key = point.toInt();
		if(cells.ContainsKey(key))
			return cells[key];

		return null;
	}

	public CellController ReplaceCell(MapPoint point, CellController prefab)
	{
		if(point.X<0 || point.Y<0 || point.X>0xffff || point.Y>0xffff)
			return null;


		CellController newCell = CellController.InstantiateMe(prefab,transform,point);


		MapRect rect = prefab.GetCellIndexes(point,selectedRotation);
		rect.Foreach((MapPoint p) => {

			int key = p.toInt();
			if(cells.ContainsKey(key))
			{
				GameObject.Destroy(cells[key].gameObject);
				cells.Remove(key);
				cells[key] = newCell;
			}
		});

		newCell.SetRotation(selectedRotation);

		return newCell;
	}



	public WallController ReplaceWall(WallPoint point, WallController prefab)
	{
		if(point.X<0 || point.Y<0 || point.X>0xffff || point.Y>0xffff)
			return null;

		WallController res = null;
		int key = point.toInt();

		if(walls.ContainsKey(key))
		{
			GameObject.Destroy(walls[key].gameObject);
			walls.Remove(key);
		}

		res = SetWall(point, prefab);

		return res;
	}

	public WallController SetWall(WallPoint point, WallController prefab)
	{
		if(point.X<0 || point.Y<0 || point.X>0xffff || point.Y>0xffff)
			return null;

		WallController res = null;
		int key = point.toInt();
		if(!walls.ContainsKey(key))
		{
			WallController newWall = Instantiate<WallController>(prefab);
			newWall.transform.parent = transform;
			newWall.Position = point;
			walls.Add(key,newWall);
			res = newWall;
		}
		else
			res = walls[key];
		return res;
	}

	public WallController GetWall(WallPoint point)
	{

		if(walls.ContainsKey(point.toInt()))
			return walls[point.toInt()];

		return null;
	}
	void OnTap()
	{
		Vector3 pz = Camera.main.ScreenToWorldPoint(Input.mousePosition)+transform.position;
		switch(state)
		{
		case Modes.SetWalls:
			OnPlaceWall(pz,false);
			break;
		case Modes.SetDoors:
			OnPlaceWall(pz,true);
			break;
		case Modes.SetObject:
			OnPlaceObject(pz);
			break;
		case Modes.RemoveWall:
			OnRemoveWall(pz);
			break;
		case Modes.RemoveObject:
			OnRemoveObject(pz);
			break;

		}

	}

	private void OnRemoveObject(Vector3 pz)
	{
		MapPoint mp = new MapPoint(Mathf.FloorToInt(pz.x),Mathf.FloorToInt(pz.y));
		CellController cellToRemove = null;
		if(cells.TryGetValue(mp.toInt(),out cellToRemove))
		{

			if(cellToRemove.CellObject!=null)
				M.UI.bCostsPanel.Expences.AddValue(-cellToRemove.CellObject.GetCost());

			MapRect rect = cellToRemove.GetCellIndexes(cellToRemove.Position,cellToRemove.Rotation);
			rect.Foreach( (MapPoint p) => {
				cells.Remove(p.toInt());
				EditorSetCell(p,CellPrefab);
			});
			Destroy(cellToRemove.gameObject);
		}
	}

	private void OnPlaceObject(Vector3 pz)
	{
		MapPoint mp = new MapPoint(Mathf.FloorToInt(pz.x),Mathf.FloorToInt(pz.y));
		CellController cellPrefab = selectedPrefab.GetComponent<CellController>();
		
		if(cellPrefab.PrefabValidatePosition(M,mp,selectedRotation))
		{

			CellObjectController cobj = ReplaceCell(mp,cellPrefab).GetComponent<CellObjectController>();
			if(cobj!=null)
				M.UI.bCostsPanel.Expences.AddValue(cobj.Cost);
			Phantom.RemovePhantom();
		}
	}

	private void OnPlaceWall(Vector3 pz, bool door)
	{
		WallController wallPrefab = selectedPrefab.GetComponent<WallController>();
		WallPoint wp = new WallPoint(Mathf.RoundToInt(pz.x),Mathf.RoundToInt(pz.y));
		
		Side side;
		if(new Rect(wp.X-0.25f,wp.Y-0.25f,0.5f,0.5f).Contains(pz))
		{
			side = Side.Middle;
		}
		else if(new Rect(wp.X-0.5f,wp.Y-0.25f,0.25f,0.5f).Contains(pz))
		{
			side = Side.Left;
		}
		else if(new Rect(wp.X-0.25f,wp.Y+0.25f,0.5f,0.25f).Contains(pz))
		{
			side = Side.Top;
		}
		else if(new Rect(wp.X+0.25f,wp.Y-0.25f,0.25f,0.5f).Contains(pz))
		{
			side = Side.Right;
		}
		else if(new Rect(wp.X-0.25f,wp.Y-0.5f,0.5f,0.25f).Contains(pz))
		{
			side = Side.Bottom;
		}
		else
		{
			side = Side.Undefined;
		}


		if(side==Side.Undefined)
			return;
		//new Rect(wp.X-0.5f,wp.Y-0.5f,wp.X+0.5f,wp.Y+0.5f).Contains(pz)
		if(IsInsideBuilding(wp))
		{
			WallController wall = null, adjWall=null;
			if(side==Side.Middle)
			{
				wall = wallPrefab.PrefabSetWall(M,wp);
			}
			else
			{
				wall = wallPrefab.PrefabSetWall(M,wp);
				SetAdjacentWall(wall,side);
			}

			UpdateWallsAround(wp);

			
		}
		else
		{
			// not inside building
			WallController wall = GetWall(wp);
			if(wall!=null && wall.WallObject==null)
			{
				wp.Foreach( (WallPoint p, Side wallSide) => {
					if(wallPrefab.PrefabValidatePosition(M,p) && side == wallSide)
						SetAdjacentWall(wall,side);
				});
				UpdateWallsAround(wp);
			}
		}
		Phantom.RemoveIndicators();
		Phantom.PlaceIndicators(cells,walls,wallPrefab);
	}


	private void DeleteWall(WallPoint wp)
	{
		WallController wall = GetWall(wp);
		if(wall!=null)
		{
			Destroy(wall.gameObject);
			walls.Remove(wp.toInt());
			UpdateWallsAround(wp);
		}
	}

	public void Save()
	{
		string prefName = Application.loadedLevelName+".save";
		string data = "";
		data+="1;"; //version
		foreach(CellController cell in cells.Values)
		{
			if(cell.CellObject==null ||  string.IsNullOrEmpty(cell.PrefabName))
				continue;

			data+=string.Format("{0}({1},{2},{3});",cell.PrefabName,cell.Position.X,cell.Position.Y,
			                    Enum.GetName(typeof(CellController.CellRotation),cell.Rotation));
		}
		
	}

	public void Load()
	{
		string prefName = Application.loadedLevelName+".save";

	}

	private void OnRemoveWall(Vector3 pz)
	{
		WallPoint wp = new WallPoint(Mathf.RoundToInt(pz.x),Mathf.RoundToInt(pz.y));

		if(IsInsideBuilding(wp))
		{
			DeleteWall(wp);

			WallController w;
			w = GetWall(new WallPoint(wp.X+1,wp.Y));
			if(w!=null && w.PrefabIsDoor())
				DeleteWall(w.Position);

			w = GetWall(new WallPoint(wp.X-1,wp.Y));
			if(w!=null && w.PrefabIsDoor())
				DeleteWall(w.Position);

			w = GetWall(new WallPoint(wp.X,wp.Y+1));
			if(w!=null && w.PrefabIsDoor())
				DeleteWall(w.Position);
			
			w = GetWall(new WallPoint(wp.X,wp.Y-1));
			if(w!=null && w.PrefabIsDoor())
				DeleteWall(w.Position);
		}
	}

	private void UpdateWallsAround(WallPoint wp)
	{
		for(int x = wp.X-1;x<=wp.X+1;x++)
		{
			for(int y = wp.Y-1;y<=wp.Y+1;y++)
			{
				WallController w = GetWall(new WallPoint(x,y));
				if(w!=null)
					w.UpdateWall();
			}
			
		}
	}
	private void SetAdjacentWall(WallController wall, Side side)
	{
		WallPoint wp = wall.Position;
		WallPoint adjWallPoint = null;
		switch(side)
		{
		case Side.Bottom: adjWallPoint=new WallPoint(wp.X,wp.Y-1); break;
		case Side.Left: adjWallPoint=new WallPoint(wp.X-1,wp.Y); break;
		case Side.Right: adjWallPoint=new WallPoint(wp.X+1,wp.Y); break;
		case Side.Top: adjWallPoint=new WallPoint(wp.X,wp.Y+1); break;
		}

		WallController adjWall = GetWall(adjWallPoint);
		if(adjWall==null)
		{
			if(WallPrefab.PrefabValidatePosition(M,adjWallPoint))
			{
				wall.wallSprite.SetSide(side,true);
				SetWall(adjWallPoint, WallPrefab);
			}
		}
		else if(adjWall.WallObject==null)
		{
			wall.wallSprite.SetSide(side,true);
		}
	}
	public void SetHouseMode(HouseModes mode, GameObject prefab)
	{
		if(state==Modes.SetWalls || state==Modes.SetDoors)
			Phantom.RemoveIndicators();
		if(state==Modes.SetObject)
			Phantom.RemovePhantom();
		bool showRotationBtn = false;

		switch(mode)
		{
		case HouseModes.Idle:
			state = Modes.Idle;
			selectedPrefab = null;
			break;
		case HouseModes.SetWalls:
			state = Modes.SetWalls;
			selectedPrefab = prefab;
			Phantom.PlaceIndicators(cells,walls,prefab.GetComponent<WallController>());
			break;
		case HouseModes.SetDoors:
			state = Modes.SetDoors;
			selectedPrefab = prefab;
			Phantom.PlaceIndicators(cells,walls,prefab.GetComponent<WallController>());
			break;
		case HouseModes.SetObject:
			state = Modes.SetObject;
			selectedPrefab = prefab;

			showRotationBtn = !prefab.GetComponent<CellController>().IsSquare;
			break;
		case HouseModes.RemoveWalls:
			state = Modes.RemoveWall;
			selectedPrefab = null;
			break;
		case HouseModes.RemoveObjects:
			state = Modes.RemoveObject;
			selectedPrefab = null;
			break;
		case HouseModes.Sale:
			state = Modes.Sale;
			selectedPrefab = null;
			Sale ();
			break;
		}

		M.UI.bCommandsPanel.ShowRotateButton(showRotationBtn);
	}

	private void Sale()
	{
		M.Statistic.EquipmentCost = M.UI.bCostsPanel.Expences.Value;
		Segmentator s = GetComponent<Segmentator>();
		s.Launch(LevelConditions,cells,walls);

	}

	#region Editor Methods
	public void EditorCheckCells ()
	{
		cells.Clear();
		walls.Clear();
		RestoreCache(true);
	}

	public void EditorUpdateThickWalls (MapPoint point)
	{

		ForEachWall(point, (WallPoint wp, WallController wc,Corner cor) => {
			if(wc==null)
			{
				wc = Instantiate<WallController>(ThickWallPrefab,transform);
				wc.Position = wp;
				walls.Add(wp.toInt(),wc);

			}

			if(wc.EditorUpdateWall())
			{
				GameObject.DestroyImmediate(wc.gameObject);
				walls.Remove(wp.toInt());
			}
			
		});


	}

	public int EditorGetCellCount()
	{
		if(cells==null)
			return 0;

		return cells.Values.Count;
	}
	public void EditorRemoveWall(WallPoint point)
	{
		WallController w = null;
		if(walls.TryGetValue(point.toInt(),out w))
		{
			GameObject.DestroyImmediate(w.gameObject);
			walls.Remove(point.toInt());
		}
	}

	public void EditorRemoveAll()
	{
		foreach(CellController c in cells.Values)
		{
			DestroyImmediate(c.gameObject);
		}
		foreach(WallController w in walls.Values)
		{
			DestroyImmediate(w.gameObject);
		}
		cells.Clear();
		walls.Clear();

	}

	public void EditorRemoveCell(MapPoint point)
	{
		if(point.X<0 || point.Y<0 || point.X>0xffff || point.Y>0xffff)
			return;

		int key = point.toInt();
		if(!cells.ContainsKey(key))
			return;

		DestroyImmediate(cells[key].gameObject);
		cells.Remove(key);

	}
	public CellController EditorSetCell(MapPoint point, CellController prefab)
	{
		if(point.X<0 || point.Y<0 || point.X>0xffff || point.Y>0xffff)
			return  null;
		
		CellController newCell = null;
		int key = point.toInt();
		if(!cells.ContainsKey(key))
		{
			newCell = CellController.InstantiateMe(prefab,transform,point);
			cells.Add(key,newCell);
		}
		else
			newCell = cells[key];
		
		return newCell;
	}

	#endregion
}
