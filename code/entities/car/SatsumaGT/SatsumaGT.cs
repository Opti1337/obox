using Sandbox;

[Library( "ent_satsumagt", Title = "SatsumaGT", Group = "Vehicles", Spawnable = true )]
public class SatsumaGT : CarEntity
{
	public override string ModelPath => "cars/satsumagt_base.vmdl";
	public override string WheelModelPath => "cars/satsumagt_wheel.vmdl";
	public override Vector3 FrontAxlePosition => new Vector3(46,0,10);
	public override Vector3 RearAxlePosition => new Vector3(-46,0,10);
	public override Vector3 SeatPosition => new Vector3(-3,10,-7);
}
