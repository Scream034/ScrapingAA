using Godot;

public partial class Additions : Button
{
	public override void _Pressed()
	{
		GetTree().ChangeSceneToFile("res://ux/adds.scn");
	}
}