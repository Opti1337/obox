using Sandbox.UI;
using Sandbox.UI.Construct;

// https://github.com/Nebual/sandbox-plus/blob/main/code/tools/Material.cs
namespace Sandbox.Tools
{
	[Library( "tool_material", Title = "Material", Group = "construction", Description = "Override model material" )]
	public partial class MaterialTool : BaseTool
	{
		[ConVar.ClientData( "tool_material_current" )]
		public static string CurentMaterial { get; set; }

		public override void Simulate()
		{
			using ( Prediction.Off() )
			{
				if ( Input.Pressed( InputButton.Attack1 ) )
				{
					var startPos = Owner.EyePos;
					var dir = Owner.EyeRot.Forward;

					var tr = Trace.Ray( startPos, startPos + dir * MaxTraceDistance )
					   .Ignore( Owner )
					   .UseHitboxes()
					   .HitLayer( CollisionLayer.Debris )
					   .Run();

					if ( !tr.Hit || !tr.Entity.IsValid() )
						return;

					if ( tr.Entity is not ModelEntity modelEnt )
						return;

					if ( Input.Pressed( InputButton.Attack1 ) )
					{
						modelEnt.SetClientMaterialOverride( GetConvarValue( "tool_material_current" ) );

						Log.Info("JOPAAAAAAAAAAAAA");
						foreach ( var file in FileSystem.Mounted.FindFile( "", "*.vmat", true ) )
						{
							if ( string.IsNullOrWhiteSpace( file ) ) continue;

							Log.Info(file);
						}

						CreateHitEffects( tr.EndPos );
					}
					else if ( Input.Pressed( InputButton.Attack2 ) )
					{
						modelEnt.SetMaterialGroup( modelEnt.GetMaterialGroup() + 1 );
						if ( modelEnt.GetMaterialGroup() == 0 )
						{
							// cycle back to start
							modelEnt.SetMaterialGroup( 0 );
						}

						CreateHitEffects( tr.EndPos );
					}

				}
			}
		}

		[ClientRpc]
		public static void SetEntityMaterialOverride( ModelEntity target, string path )
		{
			if ( Host.IsClient )
			{
				target?.SceneObject?.SetMaterialOverride( Material.Load( path ) );
			}
		}
	}

	public static partial class ModelEntityExtensions
	{
		public static void SetClientMaterialOverride( this ModelEntity instance, string material )
		{
			Sandbox.Tools.MaterialTool.SetEntityMaterialOverride( instance, material );
		}
	}
}
