using Godot;

namespace UX.Additions;

public partial class BackToMainMenu : Button
{
	public override void _Pressed()
	{
		GetTree().ChangeSceneToFile("res://ux/main.scn");
	}
}
