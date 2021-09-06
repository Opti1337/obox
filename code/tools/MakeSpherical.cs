using System;

namespace Sandbox.Tools
{
	[Library( "tool_make_spherical", Title = "Make Spherical", Description = "Make Spherical", Group = "gmod" )]
	public partial class MakeSphericalTool : BaseTool
	{
		private PhysicsBody body1;

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

				if (tr.Entity is ModelEntity modelEntity) {
					BBox bbox = modelEntity.CollisionBounds;
					float radius = Math.Max(bbox.Size.x, Math.Max(bbox.Size.y, bbox.Size.z)) / 2;

					modelEntity.PhysicsBody.Sleep();
					modelEntity.SetupPhysicsFromSphere(PhysicsMotionType.Dynamic, modelEntity.CollisionBounds.Center, radius);
					modelEntity.PhysicsBody.Wake();
				}

				CreateHitEffects( tr.EndPos );
			}
		}
	}
}
