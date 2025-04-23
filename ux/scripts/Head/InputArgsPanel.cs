namespace UX.Head;

using System;
using Godot;
using Core.Log;
using Core.UI;

public sealed partial class InputArgsPanel : AlternativePanel
{
	[Export] public string Separator = ";";
	[Export] public Button SubmitButton = null!;

	[Export] private LineEdit _inputArgs = null!;

	public override void _EnterTree()
	{
		base._EnterTree();

		SubmitButton.Pressed += _Pressed;
	}

	public string[] GetArguments()
	{
		return _inputArgs.Text.Split(Separator);
	}

	private async void _Pressed()
	{
		await Global.Instance.InitAsync();

		if (Global.Instance.Manager == null)
		{
			Log.Error("global manager is null");
			throw new NullReferenceException("global manager is null");
		}

		await Global.Instance.Manager.Parser.Main(GetArguments());
		OS.Alert("Собирание данных завершено", "Уведомление");
	}
}
