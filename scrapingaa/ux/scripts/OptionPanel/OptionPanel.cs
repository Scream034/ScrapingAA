namespace UX.Option;

using System.Linq;
using Godot;
using Core.Log;
using Core.Admin;

public sealed partial class OptionPanel : Control
{
	public PackedScene PreviousScene = null!;
	public AdminInfo AdminInfo = null!;

	[Export] public ProductsTableView TableView = null!;
	[Export] public Label WhereMyCookies = null!;


	public override void _EnterTree()
	{
		PreviousScene = (PackedScene)GetMeta("PreviousScene");
		AdminInfo = new((string)GetMeta("AdminLogin"), (string)GetMeta("AdminPassword"), (string)GetMeta("AdminUrl"));

		if (AdminInfo.Password == null || AdminInfo.Login == null || AdminInfo.Url == null)
		{
			throw new System.NullReferenceException("Admin is not initialized");
		}
		else if (PreviousScene == null)
		{
			throw new System.NullReferenceException("PreviousScene is null, can be set in the initialization");
		}

		WhereMyCookies.Text = Global.Instance.Spider.Context == null ? "Где мои печеньки !?" : "Спасибо за печеньки :D";
		WhereMyCookies.TooltipText = Global.Instance.Spider.Context == null ? "А чё я тоже кушать хочу" : "ПЕЧЕНЬКИИ!";
	}

	public override async void _Ready()
	{
		if (Global.Instance.Manager != null)
		{
			TableView.BufferedProducts = (await Global.Instance.Manager.Parser.GetIOProductsAsync()).ToList();
			UpdateTable();
		}
		else
		{
			Log.Error("Manager is null");
			OS.Alert("Не удалось открыть панель управления", "Ошибка!");
		}
	}

	public void UpdateTable()
	{
		TableView.UpdateRows();
		TableView.GenerateButtonPages();
	}
}