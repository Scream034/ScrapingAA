namespace Core.Sites.Istk;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.Playwright;

using Core.Log;
using Core.Components;
using Core.Components.Characteristics;
using Core.Extensions;
using Godot;
using ClosedXML.Excel;
using System.Linq;
using Core.IO;

public partial class LeasingParser : BaseSiteParser
{
	public const string Name = "Leasing";

	public static partial class Xpath
	{
		public const string ProductTitleAndLink = "//div[@class='product-item-block ']//a[@title and @href and @class and not(*)]";
		public const string MoreProductsButton = "//div[@class='catalog-new_more-box']/a";
	}


	public LeasingParser(ChromiumBrowser browser)
	{
		Browser = browser;
	}


	public override async Task Main(string[] args)
	{
		string? urlOrFile = args[0];
		if (string.IsNullOrEmpty(urlOrFile))
		{
			OS.Alert("Первым аргументом должна быть ссылка, а не пустая строка", "Ошибка");
			return;
		}
		else if (urlOrFile.StartsWith("--"))
		{
			urlOrFile = Path.Join(Constants.Path.Folder.User, urlOrFile[2..] + ".xlsx");

			string? downloadImages = args.ElementAtOrDefault(1);
			bool downloadImagesBool = downloadImages == "1";

			List<LeasingProduct> products = new();

			// Открываю Excel файл
			XLWorkbook package = new(urlOrFile);
			var worksheet = package.Worksheets.Worksheet(1);
			var rows = worksheet.RowsUsed();
			int count = rows.Count();
			for (int i = 1; i < count; i++)
			{
				Log.Print($"Parsing {i} of {count}");
				var row = rows.ElementAt(i);
				string value = row.Cell(1).Value.ToString();
				if (string.IsNullOrEmpty(value) || !value.StartsWith("http")) continue;

				LeasingProduct product = new() { URL = value, DownloadImages = downloadImagesBool };
				await product.ParseAsyncInternal(this);

				products.Add(product);
			}

			Global.Instance.Products.AddRange(products.ToAbstract());
			return;
		}
		else if (!urlOrFile.StartsWith("http://") && !urlOrFile.StartsWith("https://"))
		{
			OS.Alert("Первым аргументом должна быть ссылка", "Ошибка");
			return;
		}

		URL = urlOrFile;

		Global.Instance.Products.AddRange(await GetProductsInPageAsync());
	}

	public override async Task<IProductBase<IBaseCharacteristicsParser>[]> GetProductsInPageAsync()
	{
		IPage page = await OpenPageAsync();

		List<LeasingProduct> products = new();
		IReadOnlyList<IElementHandle> elements = await page.QuerySelectorAllAsync(Xpath.ProductTitleAndLink);

		while (true)
		{
			IElementHandle? moreProductsButton = await page.QuerySelectorAsync(Xpath.MoreProductsButton);
			if (moreProductsButton == null)
			{
				// Если нет кнопки, то это конец
				Log.Print("No button more products");
				break;
			}

			Log.Print("More products button click");
			await moreProductsButton.ClickAsync();

			while ((await page.QuerySelectorAllAsync(Xpath.ProductTitleAndLink)).Count == elements.Count)
			{
				Log.Print("Waiting for products");
				await Task.Delay(500);
			}

			elements = await page.QuerySelectorAllAsync(Xpath.ProductTitleAndLink);
		}

		Log.Print("Products count " + elements.Count);
		for (ushort i = 0; i < elements.Count; i++)
		{
			LeasingProduct product = new(elements[i]);
			await product.ParseAsync(this);

			products.Add(product);
		}

		await page.CloseAsync();

		return products.ToAbstract().ToArray();
	}

	public async Task<IPage> OpenPageAsync()
	{
		if (Browser.Context == null)
		{
			Log.Error("Browser context is null");
			throw new Exception("Browser context is null");
		}

		IPage? page = await Browser.Context.NewPageAsync();

		await page.RouteAsync("**/*", static (route) =>
		{
			if (route.Request.ResourceType switch { "image" => true, "font" => true, "stylesheet" => true, _ => false } || route.Request.Url.Contains("rutube.ru") || route.Request.Url.Contains("analytics")) return route.AbortAsync();

			return route.ContinueAsync();
		});

		page = await Browser.OpenWithRetriesAsync(page, URL!, 4, WaitUntilState.DOMContentLoaded, 30000);
		if (page == null)
		{
			throw new Exception("Failed to open page");
		}

		return page;
	}
}