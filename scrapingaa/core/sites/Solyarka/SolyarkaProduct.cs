namespace Core.Sites.Solyarka;

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.Playwright;

using Core.Log;
using Core.Components.Network;
using Core.Components.Characteristics;

public class SolyarkaProduct(IElementHandle? element = null) : BrowserProduct<SolyarkaCharacteristicsParser, SolyarkaParser, SolyarkaProduct>(new(), element)
{
	public static class XPath
	{
		public const string CharacteristicBlock = "//div[@class='content_tech_spec_list']/ul";
		public const string Images = "//div[contains(@class, 'slider-main')]//div[@class='swiper-wrapper']//img";
		public const string OneImage = "//div[@class='slider-container']/div/img";
	}

	public override async Task ParseAsync(SolyarkaParser parser)
	{
		if (Element == null)
		{
			Log.Error("Element is null");
			throw new NullReferenceException("Element is null");
		}

		Title = await GetTextAsync(Element);

		URL = await GetURLAsync(parser, Element);

		SaveManager.UpdateDirectoryPath();

		await OpenPageAsync(parser.Browser, URL);

		var characteristicsBlock = await Page!.QuerySelectorAsync(XPath.CharacteristicBlock);
		if (characteristicsBlock != null)
		{
			await CharacteristicsParser.ParseAsync(characteristicsBlock);
			Log.Print("Successfully get characteristics");
		}
		else
		{
			Log.Error("Characteristics block is null");
		}

		SaveManager.ImagePaths = (await GetImagesAsync(parser.Domain!, Title)).ToList();
		Log.Print("Successfully download images");

		await SaveManager.SaveAsync();

		Log.Print($"Close product: {URL}");
		await Page!.CloseAsync();
	}

	public async Task<IEnumerable<string>> GetImagesAsync(string domain, string directory)
	{
		List<Task<string?>> imagePaths = new();
		List<IElementHandle> elementHandles = new(await Page!.QuerySelectorAllAsync(XPath.Images));
		if (elementHandles.Count == 0)
		{
			elementHandles = new(await Page.QuerySelectorAllAsync(XPath.OneImage));
		}

		// Указываем путь к директории для изображений
		directory = Path.Combine(Constants.Path.Folder.Temp, directory, Constants.PathName.Folder.Images);
		Directory.CreateDirectory(directory);

		// Получаем все изображения по заданному XPath
		foreach (IElementHandle element in elementHandles)
		{
			string? url = await element.GetAttributeAsync("src");
			if (string.IsNullOrEmpty(url))
			{
				Log.Error("URL is null or empty");
				throw new NullReferenceException("URL is null or empty");
			}

			url = domain + url;

			// Вызываем метод загрузки изображения
			imagePaths.Add(FileDownloader.DownloadAndSaveAsync(url, directory, null, true));
		}

		// Ожидаем завершение всех задач и фильтруем результаты
		string?[] results = await Task.WhenAll(imagePaths);
		return results.Where(static x => !string.IsNullOrEmpty(x)).Cast<string>(); // Возвращаем только непустые ссылки
	}
}