namespace UX;

using System;
using Godot;
using Core.Log;
using Head;

public partial class CollectInfo : Button
{
	[Export] private InputArgsPanel _inputArgs = null!;

	public override async void _Pressed()
	{
		await Global.Instance.InitAsync();

		if (Global.Instance.Manager == null)
		{
			Log.Error("manager is null");
			throw new NullReferenceException("manager is null");
		}

		try
		{
			await Global.Instance.Manager.Parser.Main(_inputArgs.GetArguments());
			OS.Alert("Собирание данных завершено", "Уведомление!");
		}
		catch (Exception ex)
		{
			Log.Error(ex.Message);
			OS.Alert("Не удалось правильно собрать данные! Попробуйте перезапустить сбор.", "Ошибка!");
		}
	}
}
