namespace Core.UI;

using Godot;
using Log;

[GlobalClass]
public partial class AlternativePanel : Panel
{
	[Export] public string Index = "a";

	public override void _Ready()
	{
		UpdateStylebox();
	}

	private bool UpdateStylebox()
	{
		if (Global.Instance == null || Global.Instance.Theme.CurrentTheme == null) return false;

		StyleBoxFlat defaultStyle = (StyleBoxFlat)Global.Instance.Theme.CurrentTheme.GetStylebox(GetStyleboxName(), nameof(Panel));
		StyleBoxFlat style = (StyleBoxFlat)GetThemeStylebox(GetStyleboxName(), nameof(Panel));
		if (!IsInstanceValid(style)) return false;

		if (HasThemeStyleboxOverride("panel"))
		{
			RemoveThemeStyleboxOverride("panel");
		}

		AddThemeStyleboxOverride("panel", defaultStyle);
		Log.Print($"Added default stylebox {defaultStyle} to {GetPath()}");
		return true;
	}

	private string GetStyleboxName()
	{
		return GetStyleboxName(Index);
	}

	private static string GetStyleboxName(string style)
	{
		return $"panel_{style}";
	}
}