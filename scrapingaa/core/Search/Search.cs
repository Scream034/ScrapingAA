using System.Threading.Tasks;
using Core.Components;
using Microsoft.Playwright;

namespace Core.Search;

using System.Threading;
using Godot;
using Log;

public partial class Search
{
	public static class XPath
	{
		public const string ProductLink = "//h3[@class='search-item-title']/a";
	}

	public const string URL = "https://www.ileasing.ru/search/?q=";

	public ChromiumBrowser Browser;


	public Search(ChromiumBrowser browser)
	{
		Browser = browser;
	}


	public async Task<SearchProduct?> FindAsync(string query, string? domain)
	{
		IPage? page = Browser.CurrentPage?.Url.Contains(URL) == true ? Browser.CurrentPage : await Browser.NewPageAsync();
		if (page == null)
		{
			Log.Error("Browser is busy");
			return null;
		}

		await page.RouteAsync("**/*", static (route) =>
		{
			if (route.Request.ResourceType switch { "image" => true, "font" => true, "stylesheet" => true, _ => false } || route.Request.Url.Contains("rutube.ru") || route.Request.Url.Contains("analytics") || route.Request.Url.Contains("captcha") || route.Request.Url.Contains(".default/") || route.Request.Url.Contains("bitrix/js/") || route.Request.Url.Contains("jquery")) {
				route.AbortAsync();
			};

			return route.ContinueAsync();
		});


		page = await Browser.OpenWithRetriesAsync(page, URL + query, 3, WaitUntilState.DOMContentLoaded);
		if (page == null)
		{
			Log.Error("Page not loaded");
			return null;
		}

		IElementHandle? searchField = await page.WaitForSelectorAsync("//form[@class='search']//input[@name='q']", new PageWaitForSelectorOptions { Timeout = 60000 });
		if (searchField == null)
		{
			Log.Error("Search field not found");
			return null;
		}

		IElementHandle? element = await page.QuerySelectorAsync(XPath.ProductLink);
		if (element == null)
		{
			Log.Error("Element not found");
			return null;
		}

		SearchProduct product = new(element);

		await product.InitAsync(domain);

		return product;
	}
}