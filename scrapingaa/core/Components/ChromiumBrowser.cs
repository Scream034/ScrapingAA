using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Playwright;

namespace Core.Components;

using System.Diagnostics;
using Core.Log;

public partial class ChromiumBrowser
{
	public static BrowserTypeLaunchOptions DriverOptions = new()
	{
		Headless = false,
		ChromiumSandbox = true
	};

	public static BrowserNewContextOptions ContextOptions = new()
	{
		UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/74.0.3729.169 Safari/537.3",
		IgnoreHTTPSErrors = true,
		ViewportSize = new() { Width = 1280, Height = 720 },
		AcceptDownloads = true,
		Locale = "ru-RUS"
	};

	public IPlaywright? Playwright;
	public IBrowser? Browser;
	public IBrowserContext? Context;
	public IPage? CurrentPage;
	public List<string> WebsitesToVisit = [];
	public bool IsBusy { get; protected set; }

	public async Task<IBrowserContext> CreateAsync(IPlaywright playwright)
	{
		Playwright = playwright;
		try
		{
			Browser = await playwright.Chromium.LaunchAsync(DriverOptions);
		}
		catch (PlaywrightException ex)
		{
			if (ex.Message.Contains("download"))
			{
				Log.Print("Downloading Chrome...");
				if (Download())
				{
					Godot.OS.Alert("Пожалуйста подождите до конца установки Chrome. Это может занять несколько минут. После откройте программу снова.", "Действие!");
					await Task.Delay(1000);
					Global.Instance.GetTree().Paused = true;
					return null!;
				}
				else
				{
					Godot.OS.Alert("Не удалось скачать Chromium. Проверьте подключение к интернету.");
					throw new Exception("Chromium download failed");
				}
			}
			else
			{
				throw;
			}
		}

		Context = await Browser.NewContextAsync(ContextOptions);
		return Context;
	}

	public async Task<IPage?> NewPageAsync()
	{
		return IsBusy ? null : await _newPageAsync();
	}

	public async Task<IPage?> OpenWithRetriesAsync(IPage page, string url, int maxRetries = 3, WaitUntilState waitUntil = WaitUntilState.Load, float timeout = 30_000)
	{
		for (int attempt = 1; attempt <= maxRetries; attempt++)
		{
			try
			{
				await page.GotoAsync(url, new PageGotoOptions { WaitUntil = waitUntil, Timeout = timeout });
				CurrentPage = page;
				return page; // Возвращаем страницу, если открытие прошло успешно
			}
			catch (Exception ex)
			{
				Log.Error($"Attempt {attempt} failed to open URL {url}. Error: {ex.Message}");

				if (attempt == maxRetries)
				{
					Log.Error($"Exceeded max retries ({maxRetries}) for URL: {url}");
					return null; // Возвращаем null, если все попытки исчерпаны
				}

				// Ждем небольшой промежуток перед следующей попыткой
				await Task.Delay(Random.Shared.Next(888, 5555));
			}
		}

		return null; // В случае, если все попытки не удались
	}

	public async Task<IPage?> OpenLinkOnCurrentPageAsync(string link, WaitUntilState waitUntil = WaitUntilState.Load, float timeout = 30_000)
	{
		if (Context == null)
		{
			Log.Error("Context is not initialized");
			throw new InvalidOperationException("Context is not initialized");
		}
		else if (CurrentPage == null)
		{
			Log.Error("CurrentPage is not initialized");
			throw new InvalidOperationException("CurrentPage is not initialized");
		}
		else if (IsBusy)
		{
			Log.Error("The browser is currently busy.");
			return null;
		}

		try
		{
			// Здесь мы используем уже существующий метод для открытия URL
			await CurrentPage.GotoAsync(link, new PageGotoOptions { WaitUntil = waitUntil, Timeout = timeout });
			return CurrentPage; // Возвращаем текущую страницу, если открытие прошло успешно
		}
		catch (Exception ex)
		{
			Log.Error($"Failed to open link {link}. Error: {ex.Message}");
			return null; // Возвращаем null в случае ошибки
		}
	}

	public async Task<bool> TestCookies(string filename, int millisecondsDelay)
	{
		if (IsBusy)
		{
			return false;
		}
		else if (Context == null)
		{
			Log.Error("Context is not initialized");
			throw new InvalidOperationException("Context is not initialized");
		}

		IsBusy = true;

		string? json = JsonSerializer.Serialize(WebsitesToVisit);
		await File.WriteAllTextAsync(filename, json);

		if (WebsitesToVisit == null)
		{
			Log.Error("websites cannot initialized");
			throw new Exception("websites cannot initialized");
		}

		IPage page = await Context.NewPageAsync();
		CurrentPage = page;

		foreach (string website in WebsitesToVisit)
		{
			await page.GotoAsync(website);
			await page.WaitForLoadStateAsync(LoadState.Load);
			await Task.Delay(millisecondsDelay);
			Log.Print($"Visited: {website}");
		}

		await CookieManager.SaveAsync(filename, Context);

		await CloseAsync();

		IsBusy = false;
		return true;
	}

	public async Task CloseAsync()
	{
		CurrentPage = null;

		if (Context != null)
		{
			await Context.CloseAsync();
			Context = null;
		}
	}

	public async Task CloseCurrentPageAsync()
	{
		if (CurrentPage != null)
		{
			await CurrentPage.CloseAsync();
			CurrentPage = null;
		}
	}

	private async Task<IPage> _newPageAsync()
	{
		if (Context == null)
		{
			throw new InvalidOperationException("Context is not initialized");
		}

		IPage page = await Context.NewPageAsync();

		CurrentPage = page;

		return page;
	}

	public static bool Download()
	{
		using (var process = Process.Start(new ProcessStartInfo()
		{
			FileName = "powershell.exe",
			Arguments = $"-NoProfile -ExecutionPolicy ByPass -File \"{Constants.Path.Folder.ProgramData}\\playwright.ps1\" install",
			UseShellExecute = false
		}))
		{
			if (process == null)
			{
				Log.Print("Failed to start the process");
				Global.Instance.GetTree().Quit(1);
				return false;
			}
		}
		return true;
	}
}