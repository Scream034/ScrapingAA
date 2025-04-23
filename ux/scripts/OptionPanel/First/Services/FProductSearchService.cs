using System;
using System.Threading.Tasks;

using Core.Search;
using Core.Components;

using UX.Components.Table;

using Godot;

namespace UX.Option.First.Services;

public class FProductSearchService : IFService<FProductSearchInitializeArguments, FProductSearchStartArguments>
{
	public static string Name = "Поиск";

	private Search _search = null!;
	private ChromiumBrowser _spider = null!;
	private bool _isInitialized = false;


	public void Init(FProductSearchInitializeArguments args)
	{
		if (_isInitialized) return;

		_spider = args.ChromiumBrowser;
		_search = new(_spider);
		_isInitialized = true;
	}

	public async Task Start(FProductSearchStartArguments args)
	{
		if (_spider.Context == null)
		{
			OS.Alert("Контекст браузера не инициализирован", "Ошибка!");
			return;
		}

		foreach (ProductTableRow row in args.Rows)
		{
			if (row.Title == null) continue;

			var foundProduct = await _search.FindAsync(row.Title, args.Domain);
			bool isFound = foundProduct != null;

			row.Product.SaveManager.SetIsAdded(isFound);
			if (isFound)
			{
				args.Rows.Remove(row);
			}
		}

		if (_spider.CurrentPage != null)
		{
			await _spider.CloseCurrentPageAsync();
		}

		args.TableView.DoShowAdded = args.TableView.DoShowAdded;
		args.TableView.UpdateRows();
	}
}

public class FProductSearchInitializeArguments : IFInitializeArguments
{
	public ChromiumBrowser ChromiumBrowser { get; set; }

	public FProductSearchInitializeArguments(ChromiumBrowser chromiumBrowser)
	{
		ChromiumBrowser = chromiumBrowser;
	}
}

public class FProductSearchStartArguments : IFStartArguments
{
	public ProductsTableView TableView { get; }
	public TableView.CSelectedRows Rows { get; set; }
	public string Domain { get; set; }

	public FProductSearchStartArguments(ProductsTableView tableView, TableView.CSelectedRows rows, string domain)
	{
		Rows = rows;
		Domain = domain;
		TableView = tableView;
	}
}