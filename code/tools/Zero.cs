namespace Sandbox.Tools
{
	[Library( "tool_zero", Title = "Zero", Description = "Zero", Group = "gmod" )]
	public partial class ZeroTool : BaseTool
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

				tr.Entity.Rotation = Rotation.Identity;

				CreateHitEffects( tr.EndPos );
			}
		}
	}
}
