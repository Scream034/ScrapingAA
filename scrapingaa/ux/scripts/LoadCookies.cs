using Godot;

namespace UI;

using System.Threading.Tasks;

using Core.Log;
using Core.Components;
using Microsoft.Playwright;

public partial class LoadCookies : Button
{
	public string CookiesFileName
	{
		get
		{
			Log.Print("Get CookiesFileName");
			return GetNode<LineEdit>("/root/Main/Head/InputCookiesFileNamePanel/LineEdit").Text;
		}
	}

	public override async void _Pressed()
	{
		IBrowserContext? context = await Global.Instance.InitAsync();
		if (context == null)
		{
			Log.Print("context is already exists");
			ConfirmationDialog dialog = new();
			dialog.DialogText = "Создать новый контекст браузера?";
			dialog.OkButtonText = "Хочу";
			dialog.CancelButtonText = "Не хочу";

			dialog.Confirmed += async () =>
			{
				Log.Print("OK");

				if (!await Global.Instance.ClearAsync())
				{
					Log.Error("ClearAsync failed");
					OS.Alert("Не удалось очистить контекст", "Ошибка");
					return;
				}

				GetTree().Paused = true;
				context = await Global.Instance.InitAsync();
				if (context == null)
				{
					Log.Error("context not be replaced");
					return;
				}

				await LoadCookiesAsync(CookiesFileName, context);
			};

			dialog.Canceled += () =>
			{
				Log.Print("Cancel");
			};

			AddChild(dialog);
			dialog.PopupCentered();
		}
		else
		{
			GetTree().Paused = true;
			await LoadCookiesAsync(CookiesFileName, context);
		}

		GetTree().Paused = false;
	}

	public static async Task LoadCookiesAsync(string fileName, IBrowserContext context)
	{
		await CookieManager.LoadAsync(fileName, context);
		OS.Alert("Печеньки скушены!", "🍪");
	}
}
