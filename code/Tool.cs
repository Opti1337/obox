using System;
using System.Collections.Generic;
using Sandbox;
using Sandbox.Tools;

[Library( "weapon_tool", Title = "Toolgun" )]
partial class Tool : Carriable
{
	[ConVar.ClientData( "tool_current" )]
	public static string UserToolCurrent { get; set; } = "tool_boxgun";

	public override string ViewModelPath => "weapons/rust_pistol/v_rust_pistol.vmdl";

	[Net, Predicted]
	public BaseTool CurrentTool { get; set; }

	private Vector3 lookPos, lookDir;

	public Entity AimEntity { get; private set; }
	public Vector3 AimPos { get; private set; }
	public Vector3 AimNormal { get; private set; }

	private struct Edges
	{
		public Vector3 LSW, LSE, LNW, LNE, USW, USE, UNW, UNE;

		public Edges( Entity entity, Vector3 obbMax, Vector3 obbMin )
		{
			LSW = entity.Transform.PointToWorld( new Vector3( obbMin.x, obbMin.y, obbMin.z ) );
			LSE = entity.Transform.PointToWorld( new Vector3( obbMax.x, obbMin.y, obbMin.z ) );
			LNW = entity.Transform.PointToWorld( new Vector3( obbMin.x, obbMax.y, obbMin.z ) );
			LNE = entity.Transform.PointToWorld( new Vector3( obbMax.x, obbMax.y, obbMin.z ) );
			USW = entity.Transform.PointToWorld( new Vector3( obbMin.x, obbMin.y, obbMax.z ) );
			USE = entity.Transform.PointToWorld( new Vector3( obbMax.x, obbMin.y, obbMax.z ) );
			UNW = entity.Transform.PointToWorld( new Vector3( obbMin.x, obbMax.y, obbMax.z ) );
			UNE = entity.Transform.PointToWorld( new Vector3( obbMax.x, obbMax.y, obbMax.z ) );
		}
	}

	private BBox aimBBox;
	private Edges obvgrid;
	private readonly List<List<Vector3>> faces = new();

	private Vector3? test;
	private BBox testBBox = BBox.FromHeightAndRadius( 10, 10 );// new(new Vector3(-5, -5, -5), new Vector3(5, 5, 5));

	public override void Spawn()
	{
		base.Spawn();

		SetModel( "weapons/rust_pistol/rust_pistol.vmdl" );
	}

	public override void Simulate( Client owner )
	{
		UpdateCurrentTool( owner );

		lookPos = Owner.EyePos;
		lookDir = Owner.EyeRot.Forward;

		TraceResult tr = Trace.Ray( lookPos, lookPos + lookDir * 1000f )
			.Ignore( Owner )
			.Run();

		if ( tr.Entity.IsValid() )
		{
			if ( !tr.Entity.IsWorld )
			{
				AimEntity = tr.Entity;
				AimPos = tr.EndPos;
				AimNormal = tr.Normal;

				if ( AimEntity is ModelEntity modelEnt )
				{
					aimBBox = modelEnt.CollisionBounds;
					test = RayOBBIntersect(
						lookPos,
						lookPos + lookDir * 1000f,
						AimEntity as ModelEntity
					);

					float offset = 0;

					obvgrid = new Edges( AimEntity, aimBBox.Maxs - new Vector3( offset, offset, offset ), aimBBox.Mins + new Vector3( offset, offset, offset ) );
				}
			}
			else
			{
				AimEntity = null;
			}
		}

		CurrentTool?.Simulate();
	}

	private void UpdateCurrentTool( Client owner )
	{
		var toolName = owner.GetClientData<string>( "tool_current", "tool_boxgun" );
		if ( toolName == null )
			return;

		// Already the right tool
		if ( CurrentTool != null && CurrentTool.Parent == this && CurrentTool.Owner == owner.Pawn && CurrentTool.ClassInfo.IsNamed( toolName ) )
			return;

		if ( CurrentTool != null )
		{
			CurrentTool?.Deactivate();
			CurrentTool = null;
		}

		CurrentTool = Library.Create<BaseTool>( toolName, false );

		if ( CurrentTool != null )
		{
			CurrentTool.Parent = this;
			CurrentTool.Owner = owner.Pawn as Player;
			CurrentTool.Activate();
		}

		Log.Info( Vector3.Forward.ToString() );
		Log.Info( Vector3.Right.ToString() );
		Log.Info( Vector3.Up.ToString() );
	}

	public override void ActiveStart( Entity ent )
	{
		base.ActiveStart( ent );

		CurrentTool?.Activate();
	}

	public override void ActiveEnd( Entity ent, bool dropped )
	{
		base.ActiveEnd( ent, dropped );

		CurrentTool?.Deactivate();
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();

		CurrentTool?.Deactivate();
		CurrentTool = null;
	}

	public override void OnCarryDrop( Entity dropper )
	{
	}

	private Vector3? RayOBBIntersect( Vector3 rayStart, Vector3 rayEnd, ModelEntity entity )
	{
		BBox bbox = entity.CollisionBounds;

		Vector3 rayDir = rayEnd - rayStart;
		Vector3 bbRayDelta = entity.Position - rayStart;
		List<Vector3> axes = new()
		{
			entity.Rotation.Forward,
			entity.Rotation.Right,
			entity.Rotation.Up
		};
		float tMin = 0;
		float tMax = 100000;

		foreach ( Vector3 axis in axes )
		{
			int componentId = axes.FindIndex( a => a == axis );

			float bbMin = bbox.Mins[componentId];
			float bbMax = bbox.Maxs[componentId];


			float nomLen = Vector3.Dot( axis, bbRayDelta );
			float denomLen = Vector3.Dot( rayDir, axis );

			if ( Math.Abs( denomLen ) > 0.00001f )
			{
				float min = (nomLen + bbMin) / denomLen;
				float max = (nomLen + bbMax) / denomLen;

				if ( min < max ) { tMin = Math.Max( tMin, min ); tMax = Math.Min( tMax, max ); }
				else { tMin = Math.Max( tMin, max ); tMax = Math.Min( tMax, min ); }

				if ( tMax < tMin )
				{
					return null;
				}
			}
			else if ( -nomLen + bbMin > 0 || -nomLen + bbMax < 0 )
			{
				return null;
			}
		}

		// DebugOverlay.Sphere(rayDir * tMin + rayStart, 0.5f, Color.Blue, false);
		// DebugOverlay.Sphere(rayDir * tMax + rayStart, 0.5f, Color.Blue, false);
		// DebugOverlay.Line(rayDir * tMin + rayStart, rayDir * tMax + rayStart, Color.Blue, 0, false);

		return rayDir * tMin + rayStart;
	}

	private Vector3 GetSide( Vector3 worldPoint, ModelEntity entity )
	{
		BBox bbox = entity.CollisionBounds;
		Vector3 localPoint = entity.Transform.PointToLocal( worldPoint );
		Vector3 temp1 = localPoint;
		Vector3 temp2 = bbox.Size / 2;

		for ( int i = 0; i < 3; i++ )
		{
			temp1[i] /= temp2[i];
		}

		Vector3 localDir = temp1;

		float upDot = localDir.Dot( entity.Rotation.Up );
		float fwdDot = localDir.Dot( entity.Rotation.Forward );
		float rightDot = localDir.Dot( entity.Rotation.Right );

		float upPower = Math.Abs( upDot );
		float fwdPower = Math.Abs( fwdDot );
		float rightPower = Math.Abs( rightDot );

		float max = Math.Max( upPower, Math.Max( fwdPower, rightPower ) );

		if ( max == upPower )
		{
			return Vector3.Up * Math.Sign( upDot );
		}
		else if ( max == fwdPower )
		{
			return Vector3.Forward * Math.Sign( fwdDot );
		}
		else
		{
			return Vector3.Right * Math.Sign( rightDot );
		}
	}

	private void DrawBoundaryLine( Vector3 origin, Vector3 opposite )
	{
		Vector3 endPoint;

		if ( origin.Distance( opposite ) > 8 )
		{
			Vector3 x = opposite - origin;
			x = x.Normal;
			endPoint = origin + x * 8;
		}
		else
		{
			endPoint = origin + (opposite - origin) / 2;
		}

		DebugOverlay.Line( origin, endPoint, Color.Blue, 0, false );
	}

	private void DrawBoundary( Vector3 origin, Vector3 x, Vector3 y, Vector3 z )
	{
		DrawBoundaryLine( origin, x );
		DrawBoundaryLine( origin, y );
		DrawBoundaryLine( origin, z );
	}

	private void DrawGrid( Vector3 origin, Vector3 x, Vector3 y )
	{
		DebugOverlay.Line( origin, x, Color.Yellow, 0, false );
		DebugOverlay.Line( origin, y, Color.Blue, 0, false );
		DebugOverlay.Line( x, y, Color.Red, 0, false );
	}

	[Event.Frame]
	public void OnFrame()
	{
		if ( !IsActiveChild() ) return;

		if ( AimEntity.IsValid() )
		{
			if ( test.HasValue )
			{
				DebugOverlay.Sphere( test.Value, 0.5f, Color.Yellow );
				Vector3 side = GetSide( test.Value, AimEntity as ModelEntity );
				DebugOverlay.ScreenText( 17, AimEntity.Transform.PointToLocal( test.Value ).ToString() );
				DebugOverlay.ScreenText( 18, side.ToString() );
				// DebugOverlay.Box();
				DebugOverlay.Line( AimEntity.Transform.PointToWorld( aimBBox.Center ), AimEntity.Transform.PointToWorld( side * 30 ), Color.Yellow, 0, false );
			}

			// DebugOverlay.Text( obvgrid.LNE, "LNE", Color.Green );
			// DebugOverlay.Text( obvgrid.LNW, "LNW", Color.Green );
			// DebugOverlay.Text( obvgrid.LSE, "LSE", Color.Green );
			// DebugOverlay.Text( obvgrid.LSW, "LSW", Color.Green );
			// DebugOverlay.Text( obvgrid.UNE, "UNE", Color.Green );
			// DebugOverlay.Text( obvgrid.UNW, "UNW", Color.Green );
			// DebugOverlay.Text( obvgrid.USE, "USE", Color.Green );
			// DebugOverlay.Text( obvgrid.USW, "USW", Color.Green );

			DrawBoundary( obvgrid.UNW, obvgrid.LNW, obvgrid.USW, obvgrid.UNE );
			DrawBoundary( obvgrid.UNE, obvgrid.LNE, obvgrid.USE, obvgrid.UNW );
			DrawBoundary( obvgrid.LNW, obvgrid.UNW, obvgrid.LSW, obvgrid.LNE );
			DrawBoundary( obvgrid.LNE, obvgrid.UNE, obvgrid.LSE, obvgrid.LNW );
			DrawBoundary( obvgrid.USW, obvgrid.LSW, obvgrid.UNW, obvgrid.USE );
			DrawBoundary( obvgrid.USE, obvgrid.LSE, obvgrid.UNE, obvgrid.USW );
			DrawBoundary( obvgrid.LSW, obvgrid.USW, obvgrid.LNW, obvgrid.LSE );
			DrawBoundary( obvgrid.LSE, obvgrid.USE, obvgrid.LNE, obvgrid.LSW );

			// DrawGrid( vectorOrigin, vectorX, vectorY );

			DebugOverlay.Axis( AimEntity.Transform.PointToWorld( aimBBox.Center ), AimEntity.Rotation, Math.Min( 8f, Math.Min( aimBBox.Size.x / 2, Math.Min( aimBBox.Size.y / 2, aimBBox.Size.z / 2 ) ) ), 0, false );
		}

		CurrentTool?.OnFrame();
	}
}

namespace Sandbox.Tools
{
	public partial class BaseTool : NetworkComponent
	{
		public Tool Parent { get; set; }
		public Player Owner { get; set; }

		protected virtual float MaxTraceDistance => 10000.0f;

		public virtual void Activate()
		{
			CreatePreviews();
		}

		public virtual void Deactivate()
		{
			DeletePreviews();
		}

		public virtual void Simulate()
		{

		}

		public virtual void OnFrame()
		{
			UpdatePreviews();
		}

		public virtual void CreateHitEffects( Vector3 pos )
		{
			Parent?.CreateHitEffects( pos );
		}
	}
}
