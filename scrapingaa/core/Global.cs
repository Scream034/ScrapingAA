using System;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Playwright;
using Godot;
using Core;
using Core.Log;
using Core.Chat;
using Core.Components;
using Core.Admin;
using Core.Manager;
using Core.Components.Github;
using Core.Components.Archive;
using Core.Components.Network;
using Core.Components.Characteristics;

public sealed partial class Global : Node
{
	public static Global Instance { get; private set; } = null!;

	public Admin? Admin { get; set; }
	public ChromiumBrowser Spider = new();
	public SiteManager? Manager;
	public List<IProductBase<IBaseCharacteristicsParser>> Products = new();
	public ChatBot ChatBot = new ChatBot("http://127.0.0.1:8080/backend-api/v2/", "Use russian language.");
	public GitRepository Git = new GitRepository("Scream034", "ScrapingAA");
	public ThemeManager Theme { get; private set; } = new(Constants.Path.Folder.Themes, Constants.PathName.File.ThemeBase, Constants.Path.File.ThemeChoice);

	private Provider _provider = new("Liaobots");

	/// <summary>
	/// Поставщик для обработки данных.
	/// </summary>
	public Provider Provider
	{
		get { return _provider; }
		set { _provider = value; }
	}

	public Global()
	{
		if (System.Environment.OSVersion.Platform != System.PlatformID.Win32NT)
		{
			OS.Alert("Программа доступна только для Windows 10+!");
			return;
		}
		else if (System.Environment.OSVersion.Version.Major < 10)
		{
			OS.Alert("Программа доступна только для Windows 10+!");
			return;
		}
		else if (!Core.IO.Path.Exists(Constants.Path.Folder.ProgramData))
		{
			OS.Alert($"Программа не установлена полностью, необходим: {Constants.Path.Folder.ProgramData}!");
			return;
		}

		Instance = this;

		// Инициализируем логирование
		Log.Initialize();

		// Создаем необходимые директории
		EnsureDirectoriesExist();

		Log.Print($"Version: {Constants.Version}");
		Log.Print($"Path to program: {ProjectSettings.GlobalizePath("res://")}");
		Log.Print($"Path to program user: {Constants.Path.Folder.User}");
		Log.Print($"Path to User: {ProjectSettings.GlobalizePath("user://")}");

		Provider = LoadProvider();
		CheckPythonExists();
	}

	public override void _EnterTree()
	{
		if (!Theme.Load())
		{
			Theme.SetCurrentTheme(ThemeManager.Theme.Auto);
		}
	}

	/// <summary>
	/// Убедитесь, что все необходимые директории существуют.
	/// </summary>
	private void EnsureDirectoriesExist()
	{
		// Массив с путями к директориям
		string[] directories =
		{
			Constants.Path.Folder.User,
			Constants.Path.Folder.Temp,
			Constants.Path.Folder.Logs,
			Constants.Path.Folder.Update,
			Constants.Path.Folder.Results,
			Constants.Path.Folder.AiSystemMessages
		};

		// Проходим по каждому пути и создаем директорию
		foreach (var directory in directories)
		{
			DirAccess.MakeDirAbsolute(directory);
		}
	}

	public override void _ExitTree()
	{
		SaveProvider();
		Log.Dispose();
		OS.Alert("Перед выходом накормите автора!", "^_^");
	}

	public override void _UnhandledKeyInput(InputEvent @event)
	{
		if (Input.IsActionJustReleased("check_update"))
		{
			OS.Alert("Попытка обновления...", "Уведомление!");
			CheckVersionAsync();
		}
	}

	/// <summary>
	/// Проверяет, установлен ли Python 3.12.x.
	/// </summary>
	public void CheckPythonExists()
	{
		if (!Core.IO.Path.Exists(Constants.Path.Folder.Python312))
		{
			Log.Error("Needed Python 3.12.x!");
			OS.Alert("Установите python 3.12.x", "Ошибка!");
			Log.Print("Exit program!");
			System.Environment.Exit(1);
		}
	}

	/// <summary>
	/// Асинхронно проверяет наличие обновлений версии.
	/// </summary>
	public async void CheckVersionAsync()
	{
		var release = await Git.GetLatestReleaseAsync();
		if (release == null)
		{
			Log.Error("Release is null");
			return;
		}

		Log.Print($"Github latest release: {release.TagName}");
		string newVersion = release.TagName.Replace('.', ',');
		if (Constants.Version < float.Parse(newVersion))
		{
			Log.Print($"Update available installing to {Constants.Path.Folder.Update}");

			string updateUrl = release.Assets.First().BrowserDownloadUrl;
			string? filePath = await FileDownloader.DownloadAndSaveWithProgressAsync(GetTree(), updateUrl, Constants.Path.Folder.Update, "Скачивание архива обновления", newVersion + '_' + StringExtensions.GetFile(updateUrl), false);
			if (filePath == null)
			{
				Log.Error("Failed download archive with updates!");
				return;
			}

			ArchiveUtils.ExtractArchive(filePath, Constants.Path.Folder.Update, @"/MLcz%d_I+aADpI2}%;983\@>~V\U6A:wE]_B_4LW[:uLs>Kd3`:UFw#W71]:KDF!MlmwVZp~5IHNd`tN9r@8xc8m>vsBtV~ZY\]LFg=zwA8[5E\bsP'7#@y?n(H8/IA[Z1zWoX9Lgjvu+irIZly42D4`Lew\:64S$I.q'V@?yH;w:[j$F}i\@A)U\IrV2XNTm[9V)Jz1n:(vBB@PkdAZI|8V[o):z4}@Ujmnl&:8jV{\BIW;R3c/w]WbTmz;_+-");

			OS.Alert($"Обновление установлено: {release.TagName}. Программа откроется после нажатия ОК.", "Успешно, поздравляем!");

			Log.Print("Перенос файлов..");

			GitReleaseInfo? releaseUpdate = await Git.GetReleaseAsync("win-auto-update");
			if (releaseUpdate != null)
			{
				Log.Print($"Update auto-update: {releaseUpdate.TagName}");

				string autoUpdateUrl = releaseUpdate.Assets.First().BrowserDownloadUrl;
				string? updateFilePath = await FileDownloader.DownloadAndSaveWithProgressAsync(GetTree(), autoUpdateUrl, Constants.Path.Folder.Update, "Скачивание авто-обновления", newVersion + '_' + StringExtensions.GetFile(autoUpdateUrl), false);
				if (updateFilePath != null)
				{
					Log.Print($"Auto-update downloaded: {updateFilePath}");

					// Запускаем отдельно BAT файл для переноса файлов
					Process.Start(new ProcessStartInfo()
					{
						FileName = updateFilePath,
						Arguments = $"{Constants.ProjectName} {Constants.Path.Folder.Update} {ProjectSettings.GlobalizePath("res://")}",
						UseShellExecute = true
					});

					GetTree().Quit();
					return;
				}
				else
				{
					Log.Error("Failed download file to auto-update!");
					OS.Alert("Не удалось скачать авто-обновление!", "Ошибка!");
				}
			}
			else
			{
				Log.Error("Failed get release file to auto-update!");
				OS.Alert("Не удалось получить информацию о авто-обновлении!", "Ошибка!");
			}

			OS.Alert($"Не удалось автоматически обновить.\n\nДля обновления перейдите в: \n{Constants.Path.Folder.Update}\n\nи перекиньте все файлы в главную папку с программой.");
		}
	}

	/// <summary>
	/// Инициализирует контекст браузера.
	/// </summary>
	/// <returns>Контекст браузера или null.</returns>
	public async Task<IBrowserContext?> InitAsync()
	{
		if (Spider.Context != null) return null;
		return await Spider.CreateAsync(await Playwright.CreateAsync());
	}

	/// <summary>
	/// Очищает контекст браузера, если он существует.
	/// </summary>
	/// <returns>Результат выполнения.</returns>
	public async Task<bool> ClearAsync()
	{
		if (Spider.Context == null) return false;

		await Spider.CloseAsync();
		return true;
	}

	/// <summary>
	/// Устанавливает имя сайта.
	/// </summary>
	/// <param name="siteName">Имя сайта.</param>
	public void SetSite(in string siteName)
	{
		Manager = new SiteManager(siteName);
	}

	/// <summary>
	/// Сохраняет список продуктов в файл Excel.
	/// </summary>
	/// <param name="products">Список продуктов для сохранения.</param>
	public void SaveProductsToExcel(in List<IProductBase<IBaseCharacteristicsParser>> products)
	{
		string fileWithTimestamp = $"{Constants.PathName.File.Excel.Replace(".xlsx", "")}_{System.DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
		string fullPath = StringExtensions.PathJoin(Constants.Path.Folder.Results, fileWithTimestamp);
		Log.Print("Попытка сохранения excel", fullPath);

		using (var workbook = new ClosedXML.Excel.XLWorkbook())
		{
			var worksheet = workbook.Worksheets.Add("Товары");

			// Заголовки
			worksheet.Cell(1, 1).Value = "Название";
			worksheet.Cell(1, 2).Value = "Конечный URL";
			worksheet.Cell(1, 3).Value = "Полученный URL";

			// Сбор данных из продуктов
			int currentRow = 2; // Начинаем со 2-ой строки, так как первая для заголовков
			foreach (var product in products)
			{
				worksheet.Cell(currentRow, 1).Value = product.Title;
				worksheet.Cell(currentRow, 2).Value = product.CompletedURL;
				worksheet.Cell(currentRow, 3).Value = product.URL;

				currentRow++;
			}

			workbook.SaveAs(fullPath);
		}

		Log.Print($"Продукты успешно сохранены в Excel по пути {fullPath}");
		OS.Alert($"Данные успешно сохранены в Excel: {fileWithTimestamp}", "Уведомление");
	}

	/// <summary>
	/// Загружает данные провайдера из файла.
	/// </summary>
	/// <returns>Загруженный провайдер.</returns>
	public Provider LoadProvider()
	{
		return Core.IO.Path.Exists(Constants.Path.File.Provider) ? System.Text.Json.JsonSerializer.Deserialize<Provider>(System.IO.File.ReadAllText(Constants.Path.File.Provider)) ?? Provider : Provider;
	}

	/// <summary>
	/// Сохраняет данные провайдера в файл.
	/// </summary>
	public void SaveProvider()
	{
		System.IO.File.WriteAllText(Constants.Path.File.Provider, System.Text.Json.JsonSerializer.Serialize(Provider));
	}

	/// <summary>
	/// Загружает выбранное сообщение от ИИ из файла.
	/// </summary>
	/// <returns>Выбранное сообщение или null, если файл не существует.</returns>
	public string? LoadSelectedAISystemMessage()
	{
		return Core.IO.Path.Exists(Constants.Path.File.SelectedAISystemMessage) ? System.IO.File.ReadAllText(Constants.Path.File.SelectedAISystemMessage) : null;
	}

	/// <summary>
	/// Сохраняет выбранное сообщение от ИИ в файл.
	/// </summary>
	/// <param name="fileName">Имя файла, содержащего сообщение.</param>
	public void SaveSelectedAISystemMessage(in string fileName)
	{
		System.IO.File.WriteAllText(Constants.Path.File.SelectedAISystemMessage, fileName);
	}
}