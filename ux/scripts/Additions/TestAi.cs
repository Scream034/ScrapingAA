using Godot;
using System;

namespace UX.Additions;

public partial class TestAi : Button
{
	public override async void _Pressed()
	{
		string message;
		try
		{
			message = await Global.Instance.ChatBot.SendMessageAsync("Привет, как дела?");
			message = string.IsNullOrEmpty(message) ? "Нет сообщения, смените провайдера" : message;
		}
		catch (Exception e)
		{
			message = $"Ошибка: {e.Message}";
		};

		OS.Alert(message, "Уведомление");
	}
}
