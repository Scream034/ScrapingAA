namespace Core.Sites.SpecAvto;

using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

using Godot;

using Microsoft.Playwright;

using Core.Log;
using Core.Components;
using Core.Extensions;
using Core.Components.Characteristics;

public partial class SpecAvtoParser : BaseSiteParser
{
	public const string Name = "SpecAvto";

	public static partial class Xpath
	{
		public const string ProductTitleAndLink = "//span[@class='ma-tovar__item-new' and contains(text(), 'Новый')]/../../../../../a[@class='ma__caption']";
		public const string NoPhotoProductTitleAndLink = "//div[@class='items']//a";
		public const string MoreProductsButton = "//div[@class='next after-icon-arrow-right']/a";
	}

	public ushort LimitPages;
	public ushort StartIndex;


	public SpecAvtoParser(ChromiumBrowser browser)
	{
		Browser = browser;
	}


	public override async Task Main(string[] args)
	{
		// --1;https://specavto.ru/marks/vostok_kapital/selhoztehnika/okuchniki/digger/;https://specavto.ru/marks/vostok_kapital/selhoztehnika/okuchniki/krtek/
		string? urlOrUseProducts = args.ElementAtOrDefault(0);
		if (string.IsNullOrEmpty(urlOrUseProducts))
		{
			OS.Alert("Первый аргумент URL (http) или помечает использование продуктов (--)");
			return;
		}
		else if (urlOrUseProducts.StartsWith("--"))
		{
			args[0] = args[0][2..];
			Global.Instance.Products.AddRange(await GetProductsFromArgsAsync(args));
			return;
		}
		else if (!urlOrUseProducts.StartsWith("http://") && !urlOrUseProducts.StartsWith("https://"))
		{
			OS.Alert("Первый аргумент URL должен начинаться с http:// или https://");
		}

		string? limitPages = args.ElementAtOrDefault(1);
		if (string.IsNullOrEmpty(limitPages))
		{
			LimitPages = 0;
			Log.Print("Limit pages is null");
		}
		else
		{
			LimitPages = ushort.Parse(limitPages);
			Log.Print("Limit pages ", LimitPages);
		}

		string? startIndex = args.ElementAtOrDefault(2);
		if (string.IsNullOrEmpty(startIndex))
		{
			StartIndex = 0;
			Log.Print("Start index is null");
		}
		else
		{
			StartIndex = (ushort)(ushort.Parse(startIndex) - 1);
			Log.Print("Start index ", StartIndex);
		}

		URL = urlOrUseProducts;

		Global.Instance.Products.AddRange(await GetProductsInPageAsync());
	}

	public override async Task<IProductBase<IBaseCharacteristicsParser>[]> GetProductsInPageAsync()
	{
		IPage page = await OpenPageAsync();

		List<SpecAvtoProduct> products = new();

		uint parsedPages = 0;

		bool isNoPhoto = await IsPageHasNoPhotoProducts(page);
		Log.Print($"Page has no photo products: {isNoPhoto}");

		while (true)
		{
			IReadOnlyList<IElementHandle> elements = await GetProductElementsAsync(page, isNoPhoto);
			LimitPages = (ushort)(elements.Count > 0 && LimitPages > 0 ? StartIndex + (ushort)elements.Count - ((ushort)elements.Count - LimitPages) : elements.Count);

			for (ushort i = StartIndex; i < LimitPages; i++)
			{
				Log.Print($"Parsing product {i + 1}/{LimitPages}");
				SpecAvtoProduct product = new(elements[i], isNoPhoto);
				await product.ParseAsync(this);

				await AddSubProductsAsync(product, products, i);
			}

			// Если страница не содержит товаров с фотками, то на ней нет навигации
			if (!isNoPhoto)
			{
				if (!await OpenNextPageAsync(page))
				{
					break; // Если нет кнопки "Далее", то выходим из цикла
				}

				// Увеличиваем счетчик страниц
				parsedPages++;
				if (LimitPages > 0 && parsedPages >= LimitPages)
				{
					Log.Print("Limit of pages reached");
					break;
				}
			}
			else
			{
				break;
			}
		}

		await page.CloseAsync();

		return products.ToAbstract().ToArray();
	}

	protected async Task AddSubProductsAsync(SpecAvtoProduct product, List<SpecAvtoProduct> products, ushort i = 0)
	{
		if (product.SubProducts == null || product.SubProducts.Count == 0)
		{
			products.Add(product);
			return;
		};

		// Предполагаемый главное название
		string suggestedMainTitle = product.SubProducts[0].Title!;
		string? @out = GetProductMainTitle(product.Title!, suggestedMainTitle);

		foreach (SpecAvtoProduct _product in product.SubProducts)
		{
			Log.Print($"Parsing sub product: {_product.Title} ({i + 1}/{LimitPages})");
			if (string.IsNullOrEmpty(_product.Title))
			{
				Log.Warning("Sub product title is null");
				OS.Alert($"Внутренний товар {product.Title!} не имеет названия", "Предупреждение!");
				_product.Title = $"{Constants.IncorrectTitleString}_{RandomUtils.RandomString(16)}";
			}
			else
			{
				_product.Title = FormatProductTitle(@out, product.Title!, suggestedMainTitle, _product.Title);
			}

			// Создаём продукт полностью в файловой системе
			_product.SaveManager.UpdateDirectoryPath();
			await _product.SaveManager.SaveAsync();
		}

		products.AddRange(product.SubProducts);

		// Удаляем так как он являлся группой продуктов
		if (!product.SaveManager.Remove())
		{
			Log.Error("Failed to remove product of group");
		}
	}

	protected override async Task<IEnumerable<IProductBase<IBaseCharacteristicsParser>>> GetProductsFromArgsAsync(string[] args)
	{
		if (args.Length == 0)
		{
			return [];
		}

		bool isNoPhoto = args[0].Equals("1");
		Log.Print($"Is no photo: {isNoPhoto}");

		List<SpecAvtoProduct> products = new();

		for (ushort i = 1; i < args.Length; i++)
		{
			string? url = args.ElementAtOrDefault(i);
			Log.Print($"Parsing product {i - 1}/{args.Length - 1}: {url}");
			if (string.IsNullOrEmpty(url))
			{
				Log.Warning("URL is null");
				OS.Alert($"Пустой URL для аргумента {i - 1}: {url}");
				continue;
			}

			SpecAvtoProduct product = new() { URL = url, IsNoPhoto = isNoPhoto };
			await product.ParseAsyncInternal(this);

			await AddSubProductsAsync(product, products);
		}

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

	public async Task<bool> IsPageHasNoPhotoProducts(IPage page)
	{
		return await page.QuerySelectorAsync(Xpath.NoPhotoProductTitleAndLink) != null;
	}

	protected async Task<bool> OpenNextPageAsync(IPage page)
	{
		IElementHandle? moreProductsButton = await page.QuerySelectorAsync(Xpath.MoreProductsButton);
		if (moreProductsButton == null)
		{
			Log.Print("No button more products");
			return false;
		}

		string? nextUrl = await moreProductsButton.GetAttributeAsync("href");
		if (string.IsNullOrEmpty(nextUrl))
		{
			Log.Print("No next url");
			return false;
		}

		nextUrl = Global.Instance.Manager!.Parser.Domain + '/' + nextUrl;

		Log.Print($"Goto next page: {nextUrl}");
		await page.GotoAsync(nextUrl);
		return true;
	}

	protected Task<IReadOnlyList<IElementHandle>> GetProductElementsAsync(IPage page, bool isNoPhoto)
	{
		return isNoPhoto ? page.QuerySelectorAllAsync(Xpath.NoPhotoProductTitleAndLink) : page.QuerySelectorAllAsync(Xpath.ProductTitleAndLink);
	}

	public static string FormatProductTitle(in string? @out, in string fullTitle, in string? mainTitle, in string targetTitle)
	{
		// Если скобок нет
		if (string.IsNullOrEmpty(@out))
		{
			// Возвращаем полное название с целевым названием
			return fullTitle + ' ' + targetTitle;
		}
		else if (string.IsNullOrWhiteSpace(mainTitle) || !@out.Contains(mainTitle))
		{
			// Если главное название не найдено
			try
			{
				string common = StringUtils.LongestCommonSubstring(@out, targetTitle);
				Log.Print("ALCSR:\n", "Common:", common, "\n Target:", targetTitle, "\n Result:", @out.Replace(common, targetTitle));
			}
			catch (Exception ex)
			{
				Log.Error("ALCSR:", ex.Message);
			}

			// Возвращаем без скобок с целевым названием
			return @out + ' ' + targetTitle;
		}

		// Возвращаем без скобок и с заменой главного названия на целевое название
		return @out.Replace(mainTitle, targetTitle);
	}

	public static string? GetProductMainTitle(in string fullTitle, in string suggestedMainTitle)
	{
		if (string.IsNullOrEmpty(fullTitle) || string.IsNullOrEmpty(suggestedMainTitle))
			return null;

		string? @out = null;
		string[] perhaps = fullTitle.FindMatches(@"\((?<in>.*?)\)", 1);
		if (perhaps.Length > 0)
		{
			@out = fullTitle;
			foreach (var per in perhaps)
			{
				@out = @out.Replace('(' + per + ')', string.Empty);
			}
			@out = @out.ReduceWhitespace().Trim();
		}

		return @out;
	}
}