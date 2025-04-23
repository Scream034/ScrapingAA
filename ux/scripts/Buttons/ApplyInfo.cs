using Godot;

namespace UX;

using Head;
using Option;
using Core.Log;


public partial class ApplyInfo : Button
{
	private PackedScene? _scene;


	public override void _Pressed()
	{
		var tree = GetTree();

		if (_scene == null)
		{
			_scene = GD.Load<PackedScene>("res://ux/option_panel.scn");
		}

		var optionPanel = _scene.Instantiate<OptionPanel>();

		var currentScene = new PackedScene();
		currentScene.Pack(tree.CurrentScene);

		var admin = tree.CurrentScene.GetNode<InputAdminPanel>($"Head/{nameof(InputAdminPanel)}").GetAdminInfo();

		optionPanel.SetMeta("PreviousScene", currentScene);
		optionPanel.SetMeta("AdminUrl", admin.Url);
		optionPanel.SetMeta("AdminPassword", admin.Password);
		optionPanel.SetMeta("AdminLogin", admin.Login);

		if (_scene.Pack(optionPanel) == Error.Ok)
		{
			tree.ChangeSceneToPacked(_scene);
		}
		else
		{
			Log.Error("Failed to pack option panel scene");
		}
	}
}