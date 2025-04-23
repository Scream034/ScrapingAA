namespace Core.Sites;

using System;
using System.Threading.Tasks;
using System.Collections.Generic;

using Microsoft.Playwright;

using Core.Log;
using Core.Components;
using Core.Components.Characteristics;

public class BrowserProduct<TCharacteristicsParser, TBaseSiteParser, TProductType> : ProductBase<TCharacteristicsParser>, IBrowserProduct<TCharacteristicsParser, TBaseSiteParser, TProductType>
	where TCharacteristicsParser : IBaseCharacteristicsParser
	where TBaseSiteParser : IBaseSiteParser
	where TProductType : IBrowserProduct<TCharacteristicsParser, TBaseSiteParser, TProductType>
{
	public IElementHandle? Element { get; set; }
	public IPage? Page { get; set; }
	public List<TProductType>? SubProducts { get; set; }

	public BrowserProduct(TCharacteristicsParser characteristicsParser, IElementHandle? element = null) : base(characteristicsParser)
	{
		Element = element;
	}

	public virtual Task ParseAsync(TBaseSiteParser parser)
	{
		throw new NotImplementedException();
	}

	public async Task OpenPageAsync(ChromiumBrowser browser, string url, WaitUntilState waitUntilState = WaitUntilState.DOMContentLoaded, int timeout = 180000, string xpathWait = "")
	{
		Log.Print($"Open product: {url}");
		Page = await browser.NewPageAsync();
		if (Page == null)
		{
			Log.Error("Page is null and context busy");
			throw new NullReferenceException("Page is null and context busy");
		}

		await Page.RouteAsync("**/*", handler: static (route) =>
		{
			if (route.Request.ResourceType switch { "image" => true, "font" => true, "stylesheet" => true, _ => false } || route.Request.Url.Contains("analytics") || route.Request.Url.Contains("metrika") || route.Request.Url.Contains("captcha") || route.Request.Url.Contains("guard"))
				return route.AbortAsync();

			return route.ContinueAsync();
		});

		try
		{
			await Page.GotoAsync(url, new PageGotoOptions { WaitUntil = waitUntilState, Timeout = timeout });
		}
		catch (Exception ex)
		{
			Log.Error($"Failed open page: {ex.Message}");
		}

		if (!string.IsNullOrEmpty(xpathWait))
		{
			ILocator locator = Page.Locator(xpathWait);
			await locator.WaitForAsync(new() { State = WaitForSelectorState.Attached, Timeout = timeout });
		}
	}

	public static async Task<string> GetTextAsync(IElementHandle element) => (await element.InnerTextAsync()).Trim();

	public static async Task<string> GetURLAsync(BaseSiteParser parser, IElementHandle element)
	{
		string? url = await element.GetAttributeAsync("href");
		if (string.IsNullOrEmpty(url))
		{
			Log.Error("URL is null or empty");
			throw new NullReferenceException("URL is null or empty");
		}

		return parser.Domain + url;
	}
}