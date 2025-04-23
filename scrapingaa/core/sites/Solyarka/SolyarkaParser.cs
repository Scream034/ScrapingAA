namespace Core.Sites.Solyarka;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Godot;

using Microsoft.Playwright;

using Core.Log;
using Core.Components;
using Core.Extensions;
using Core.Components.Characteristics;
using System.Linq;

public partial class SolyarkaParser : BaseSiteParser
{
	public const string Name = "Solyarka";

	public static partial class Xpath
	{
		public const string ProductTitleAndLink = "//a[@href and contains(@class, 'vehicle-mainname')]";
		public const string MoreProductsButton = "//a[@href and @rel='next']";
	}

	public uint LimitPages;


	public SolyarkaParser(ChromiumBrowser browser)
	{
		Browser = browser;
	}


	public override async Task Main(string[] args)
	{
		string? url = args.ElementAtOrDefault(0);
		if (string.IsNullOrEmpty(url) || !url.StartsWith("https://"))
		{
			OS.Alert("Первым аргументом должна быть ссылка", "Ошибка");
			return;
		}

		string? limitPages = args.ElementAtOrDefault(1);
		if (string.IsNullOrEmpty(limitPages))
		{
			LimitPages = 0;
			Log.Print("Limit pages is null");
		}
		else
		{
			LimitPages = uint.Parse(limitPages);
			Log.Print("Limit pages ", LimitPages);
		}

		URL = url;

		Global.Instance.Products.AddRange(await GetProductsInPageAsync());
	}

	public override async Task<IProductBase<IBaseCharacteristicsParser>[]> GetProductsInPageAsync()
	{
		IPage page = await OpenPageAsync();

		List<SolyarkaProduct> products = new();

		uint parsedPages = 0;

		while (true)
		{
			IReadOnlyList<IElementHandle> elements = await page.QuerySelectorAllAsync(Xpath.ProductTitleAndLink);
			Log.Print("Products count " + elements.Count);
			for (ushort i = 0; i < elements.Count; i++)
			{
				SolyarkaProduct product = new(elements[i]);
				await product.ParseAsync(this);
				products.Add(product);
			}

			IElementHandle? moreProductsButton = await page.QuerySelectorAsync(Xpath.MoreProductsButton);
			if (moreProductsButton == null)
			{
				// Если нет кнопки, то это конец
				Log.Print("No button more products");
				break;
			}

			string previousUrl = page.Url;

			Log.Print("More products button click");
			await moreProductsButton.ClickAsync();

			while (previousUrl == page.Url)
			{
				Log.Print("Waiting for products", previousUrl, page.Url);
				await Task.Delay(600);
			}

			await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

			// Увеличиваем счетчик страниц
			parsedPages++;
			if (LimitPages > 0 && parsedPages >= LimitPages)
			{
				Log.Print("Limit of pages reached");
				break;
			}
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