﻿namespace Sandbox.Tools
{
	[Library( "tool_axis_center", Title = "Axis Center", Description = "Axis center", Group = "gmod" )]
	public partial class AxisCenterTool : BaseTool
	{
		private PhysicsBody body1, body2;
		private Vector3 norm1, norm2;

		public override void Simulate()
		{
			if ( !Host.IsServer )
				return;

			using ( Prediction.Off() )
			{
				if ( !Input.Pressed( InputButton.Attack1 ) )
					return;

				var startPos = Owner.EyePos;
				var dir = Owner.EyeRot.Forward;

				var tr = Trace.Ray( startPos, startPos + dir * MaxTraceDistance )
					.Ignore( Owner )
					.Run();

				if ( !tr.Hit )
					return;

				if ( !tr.Entity.IsValid() )
					return;

				if ( !(tr.Body.IsValid() && (tr.Body.PhysicsGroup != null) && tr.Body.Entity.IsValid()) ) return;

				if ( !body1.IsValid() )
				{
					if ( tr.Entity.IsWorld || tr.Entity is WorldEntity ) return;

					body1 = tr.Body;
					norm1 = tr.Normal;
				}
				else
				{
					body2 = tr.Body;
					norm2 = tr.Normal;

					if ( body1 == body2 )
					{
						body1 = null;
						return;
					}

					// body2.Transform.PointToLocal( body1.Transform.PointToWorld( body1.Position ) + norm1 )

					var j = PhysicsJoint.Revolute
						.From( body1 )
						.To( body2, body2.Transform.PointToLocal( body1.Transform.PointToWorld( Vector3.Zero ) + norm1 ) )
						.WithBasis( Rotation.LookAt( tr.Normal ) * Rotation.From( new Angles( 90, 0, 0 ) ) )
						.Create();

					// if ( Host.IsServer )
					// 	Undo.Add( Owner.GetClientOwner(), new PhysicsJointUndo( j ) );

					body1.PhysicsGroup?.Wake();
					body2.PhysicsGroup?.Wake();

					body1 = null;
					body2 = null;
				}

				CreateHitEffects( tr.EndPos );
			}
		}
	}
}
