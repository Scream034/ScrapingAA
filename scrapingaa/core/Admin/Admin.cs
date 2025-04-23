using Microsoft.Playwright;

using Godot;

using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;

namespace Core.Admin;

using Core.Components;
using Core.Log;
using Core.Extensions;
using Core.Components.Characteristics;


public sealed class Admin
{
	public const int MaxImagesToLoad = 4;
	public static class Selector
	{
		public const string ButtonAddElement = ".ui-btn-split.ui-btn-primary > a";
		public const string ButtonGotoCharacteristics = ".adm-detail-tab:nth-child(2)";
		public const string ButtonApplyElement = "input[type='submit'][name='apply']";
		public const string ButtonSaveElement = "input[type='submit'][name='save']";
		public const string ButtonViewElement = "a#asd_iblock_show_element";
		public const string ButtonAddCharacteristic = "//table/tbody/tr/td[contains(text(), '$')]/..//tbody/tr[last()]//input";
		public const string InputTitle = "td > input[name='NAME']";
		public const string InputPictures = ".adm-fileinput-drag-area-input[type='file']";
		public const string InputCharacteristicName = "//table/tbody/tr/td[contains(text(), '$')]/..//tbody/tr/td/input[@name]";
		public const string InputCharacteristicValue = "//table/tbody/tr/td[contains(text(), '$')]/..//tbody/tr/td/span/input";
		public const string DivImages = "//div[@class='adm-fileinput-area-container']";
		public const string DivImage = "//div[@class='adm-fileinput-area-container']/div[not(contains(@class, 'adm-fileinput-item-uploading'))]";
		public const string AdminErrorMessage = "//div[@class='adm-info-message']";
	}

	public enum AdminError
	{
		RequiredFieldSymbolCodeEmpty = 1,
		Other = 2
	}

	public string? Domain { get; private set; }
	public readonly AdminInfo Info;

	public ChromiumBrowser Browser { get; }
	public IPage? Page { get; private set; }

	public Admin(ChromiumBrowser browser, AdminInfo info)
	{
		Browser = browser;
		Info = info;
	}

	public async Task<string?> AddProductAsync(IProductBase<IBaseCharacteristicsParser> product)
	{
		if (string.IsNullOrEmpty(product.Title))
		{
			Log.Error("Product title is null or empty");
			return "Название продукта пустое";
		}
		else if (string.IsNullOrEmpty(product.URL))
		{
			Log.Error("Product URL is null or empty");
			return "URL продукта пустой";
		}
		else if (Browser.Context == null)
		{
			Log.Error("Browser context is null");
			OS.Alert("Откройте браузер для продолжения", "Ошибка");
			return "Браузер не открыт";
		}

		Log.Print("Try to add", product.Title, product.URL);

		IElementHandle? addProductButton = await OpenPageAsync();
		if (addProductButton == null)
		{
			Log.Error("Add product button is null");
			return "Кнопка добавления продукта не найдена";
		}
		else if (Page == null)
		{
			Log.Error("Page is null");
			return "Нет страницы, возможно закрыли";
		}

		await addProductButton.ClickAsync();
		IElementHandle? inputTitle = await WaitForElementAsync(Page, Selector.InputTitle);
		if (inputTitle == null)
		{
			Log.Error("Input title is null");
			return "Ввод Названия продукта не найден";
		}

		await Page.FillAsync(Selector.InputTitle, product.Title);
		await WaitForInputValueAsync(Page, inputTitle, product.Title);

		IEnumerable<string> imagesPaths = product.SaveManager.ImagePaths.Take(MaxImagesToLoad);
		int imagesPathsCount = imagesPaths.Count();

		Log.Print($"Images to load: {imagesPathsCount}");
		if (imagesPathsCount > 0)
		{
			await Page.SetInputFilesAsync(Selector.InputPictures, imagesPaths);
			IElementHandle? divImages = await Page.QuerySelectorAsync(Selector.DivImages);
			if (divImages == null)
			{
				Log.Error("Div images is null");
				return "Блок с изображениями не найден";
			}

			while ((await Page.QuerySelectorAllAsync(Selector.DivImage)).Count < imagesPathsCount)
			{
				Log.Print($"Waiting for images to load - in site: {(await Page.QuerySelectorAllAsync(Selector.DivImage)).Count}; count: {imagesPathsCount}");
				await Task.Delay(millisecondsDelay: 500);
			}

			Log.Print("Images loaded successfully.");

			await Task.Delay(200);
		}
		else
		{
			Log.Warning("No images to load");
		}

		await Page.ClickAsync(selector: Selector.ButtonApplyElement);

		if (!await WaitForViewButtonAsync(Page, Domain ?? throw new NullReferenceException("Domain is null"), product))
		{
			var adminError = await WaitForAdminError(Page);
			if (adminError != null)
			{
				switch (adminError)
				{
					case AdminError.RequiredFieldSymbolCodeEmpty:
						while (adminError == AdminError.RequiredFieldSymbolCodeEmpty)
						{
							Log.Print("Symbol code is empty, trying to fill it");
							await Page.ClickAsync(Selector.ButtonApplyElement);
							await Task.Delay(1000);
						}
						break;

					case AdminError.Other:
						return "Ошибка в админке";
				}
			}

			return "Error waiting for button view element";
		};

		await Page.ClickAsync(Selector.ButtonGotoCharacteristics);
		await FillCharacteristicsAsync(product);

		await Page.ClickAsync(Selector.ButtonSaveElement);
		product.SaveManager.SetIsAdded(true);

		Log.Print("Added", product.Title, product.CompletedURL ?? "no completed url");
		return null;
	}

	private static async Task<string?> WaitForAdminErrorMessage(IPage page, int timeout = 200)
	{
		try
		{
			IElementHandle? adminErrorMessage = await page.WaitForSelectorAsync(Selector.AdminErrorMessage, new PageWaitForSelectorOptions { Timeout = timeout });
			if (adminErrorMessage == null)
			{
				return null;
			}

			string? message = await adminErrorMessage.TextContentAsync();
			if (message == null)
			{
				return null;
			}

			message = message.RemoveControlChars().Trim().Replace("Ошибка", "");
			Log.Error($"Error message: {message}");
			return message;
		}
		catch (TimeoutException)
		{
			return null;
		}
	}

	private static AdminError? AdminErrorMessageToError(string? message)
	{
		if (message == null) return null;

		if (message.Contains("Необходимо заполнить поле \"Символьный код\""))
		{
			return AdminError.RequiredFieldSymbolCodeEmpty;
		}

		return AdminError.Other;
	}

	private static async Task<AdminError?> WaitForAdminError(IPage page, int timeout = 200) => AdminErrorMessageToError(await WaitForAdminErrorMessage(page, timeout));

	private static async Task<bool> WaitForViewButtonAsync(IPage page, string domain, IProductBase<IBaseCharacteristicsParser> product)
	{
		try
		{
			IElementHandle? buttonViewElement = await page.WaitForSelectorAsync(Selector.ButtonViewElement, new PageWaitForSelectorOptions { Timeout = 25000 });
			if (buttonViewElement != null)
			{
				product.CompletedURL = page.Url;
				if (string.IsNullOrEmpty(product.CompletedURL))
				{
					Log.Error("Product completed URL is null or empty");
					return false;
				}

				return true;
			}
		}
		catch (TimeoutException)
		{
			Log.Error("IMPORTANT: Timeout waiting for button view element, maybe it already exists");
		}

		return false;
	}

	private async Task FillCharacteristicsAsync(IProductBase<IBaseCharacteristicsParser> product)
	{
		string unknown = ProductCharacteristics.Type.Unknown.ToDisplayString();
		foreach (var pair in product.CharacteristicsParser.Characteristics.Storage.Dictionary.Where(x => x.Key.ToDisplayString() != unknown))
		{
			IReadOnlyList<IElementHandle>? characteristicNames = await Page!.QuerySelectorAllAsync(Selector.InputCharacteristicName.Replace("$", pair.Key.ToDisplayString()));
			IElementHandle? buttonAddCharacteristic = await Page.QuerySelectorAsync(Selector.ButtonAddCharacteristic.Replace("$", pair.Key.ToDisplayString()));

			if (buttonAddCharacteristic == null)
			{
				Log.Error("Button add characteristic is null");
				continue;
			}

			int remainingCount = pair.Value.Count - characteristicNames.Count;
			Log.Print($"Remaining count: {remainingCount}");
			for (ushort i = 0; i < remainingCount; i++)
			{
				await buttonAddCharacteristic.ClickAsync();
			}

			characteristicNames = await Page.QuerySelectorAllAsync(Selector.InputCharacteristicName.Replace("$", pair.Key.ToDisplayString()));
			IReadOnlyList<IElementHandle>? characteristicValues = await Page.QuerySelectorAllAsync(Selector.InputCharacteristicValue.Replace("$", pair.Key.ToDisplayString()));

			ushort index = 0;
			foreach (var item in pair.Value)
			{
				Log.Print($"Filling characteristic {index}");
				await characteristicNames[index].FillAsync(item.Key);
				await WaitForInputValueAsync(Page, characteristicNames[index], item.Key); // Ждем, пока значение будет введено
				await characteristicValues[index].FillAsync(item.Value);
				await WaitForInputValueAsync(Page, characteristicValues[index], item.Value); // Ждем, пока значение будет введено
				Thread.Sleep(100);
				index += 1;
			}
		}
	}

	public async Task<IElementHandle?> OpenPageAsync()
	{
		try
		{
			if (Page != null && !Page.IsClosed && Domain != null && Page.Url.Contains(Domain))
			{
				await Page.FocusAsync(Selector.ButtonAddElement);
				return await Page.QuerySelectorAsync(Selector.ButtonAddElement);
			}

			Page = await Browser.NewPageAsync();
			if (Page == null)
			{
				Log.Error("Page is null");
				return null;
			}

			await Page.GotoAsync(Info.Url, new() { Timeout = 60000, WaitUntil = WaitUntilState.DOMContentLoaded });

			IElementHandle? addProductButton = await WaitForElementAsync(Page, Selector.ButtonAddElement, new PageWaitForSelectorOptions { Timeout = 2000 });
			if (addProductButton != null) return addProductButton;

			// Если кнопка не найдена, выполняем вход
			await Page.FillAsync("input[name='USER_LOGIN'][tabindex='1']", Info.Login);
			await Page.FillAsync("input[name='USER_PASSWORD'][tabindex='2']", Info.Password);
			await Page.ClickAsync("input[name='Login'][type='submit'][tabindex='4']");

			Domain = new Uri(Page.Url).Host;

			return await Page.WaitForSelectorAsync(Selector.ButtonAddElement, new PageWaitForSelectorOptions { Timeout = 99999 });
		}
		catch (Exception ex)
		{
			Log.Error(ex.Message);
			return null;
		}
	}

	private static async Task<IElementHandle?> WaitForElementAsync(IPage page, string selector, PageWaitForSelectorOptions? options = null)
	{
		try
		{
			return await page.WaitForSelectorAsync(selector, options);
		}
		catch (TimeoutException)
		{
			Log.Error($"Timeout while waiting for selector: {selector}");
		}
		catch (Exception ex)
		{
			Log.Error(ex.Message);
		}

		return null;
	}

	private static async Task WaitForInputValueAsync(IPage page, IElementHandle element, string expectedValue)
	{
		await page.WaitForFunctionAsync(@"(element, expectedValue) => { return element && element.value === expectedValue; }", new { element, expectedValue });
	}
}