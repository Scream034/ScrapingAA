namespace UX.Option.First.Services;

using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Log;
using Godot;
using UX.Components.Table;
using UX.Option.First.Services.Title;

public class FProductTitleService : IFService<FServiceNoneType, FProductTitleStartArguments>
{
	public static string Name = "Название";
	public WindowServiceHandler? Handler;

	private ProductProcessor processor = new();

	public void Init(FServiceNoneType xArgs = new())
	{
		if (Handler != null && Node.IsInstanceValid(Handler))
		{
			Log.Print("WindowServiceHandler is not null");
			OS.Alert("Окно с настройкой <название> уже открыто", "Ошибка!");
			return;
		}

		PackedScene scene = GD.Load<PackedScene>("res://ux/windows/service_title.scn");
		Handler = scene.Instantiate<WindowServiceHandler>();
		Global.Instance.GetTree().Root.AddChild(Handler);
	}

	public Task Start(FProductTitleStartArguments xArgs)
	{
		if (Handler == null)
		{
			Log.Print("WindowServiceHandler is null");
			return Task.CompletedTask;
		}

		var titles = Handler.GetCurrentTitles();
		if (titles == null)
		{
			Log.Error("No focused main line edit");
			OS.Alert("Вы не выбрали слово", "Ошибка!");
			return Task.CompletedTask;
		}

		processor.ProcessProducts(xArgs.Products, titles);

		return Task.CompletedTask;
	}
}

public class FProductTitleStartArguments(in List<ProductTableRow> products) : IFStartArguments
{
	public readonly List<ProductTableRow> Products = products;
}