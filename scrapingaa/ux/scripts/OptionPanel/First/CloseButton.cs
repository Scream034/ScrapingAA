using Godot;

namespace UX.Option.First;

public partial class CloseButton : Button
{
	public override void _Pressed()
	{
		var tree = GetTree();
		var parent = (OptionPanel)tree.CurrentScene;

		tree.ChangeSceneToPacked(parent.PreviousScene);
	}
}
