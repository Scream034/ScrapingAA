namespace UX;

using System.Threading.Tasks;
using Godot;
using Core.Log;

public partial class SaveCookies : Button
{
	[Export] public LineEdit SaveCookiesFileNameLineEdit = null!;

	public string CookiesFileName
	{
		get
		{
			Log.Print("Get CookiesFileName");
			return SaveCookiesFileNameLineEdit.Text;
		}
	}


	public override async void _Pressed()
	{
		await SaveCookiesAsync(CookiesFileName, GD.RandRange(1000, 3000));
	}

	public static async Task SaveCookiesAsync(string fileName, int millisecondsDelay)
	{
		Log.Print($"Start ({millisecondsDelay})");

		await Global.Instance.InitAsync();

		bool isCompleted = await Global.Instance.Spider.TestCookies(fileName, millisecondsDelay);
		if (isCompleted)
		{
			Log.Print("Completed");
		}
		else
		{
			Log.Error("Failed");
		}
	}
}
