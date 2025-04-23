namespace Core.Sites.SpecAvto;

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.Playwright;

using Core.Log;
using Core.Extensions;
using Core.Components.Network;
using Core.Components.Characteristics;
using static Core.Components.Characteristics.SpecAvto.SpecAvtoCharacteristicsParser_Special;
using Core.Components;

public class SpecAvtoProduct : BrowserProduct<SpecAvtoCharacteristicsParser, SpecAvtoParser, SpecAvtoProduct>
{
	public static class XPath
	{
		public const string Title = "//h1[@class='title']";

		public static class Special
		{
			public const string CharacteristicBlock = "//div[@class='specifications-text']//table/tbody";
		}

		public static class MultipleProductsName
		{
			public static class Thead
			{
				public static string[] Paths = [
					"//div[@class='specifications-text']//table[1]/thead[1]/tr[1]/th"
				];

				public static string[] Warnings = [
					"//div[@class='specifications-text']//table/thead[1]/tr[1]/th"
				];
			}

		}

		public static string[] HeadSpan = [
			"//div[@class='specifications-text']//table/tbody/tr[1]/th|//div[@class='specifications-text']//table/tbody/tr[1]/td"
		];

		public const string CharacteristicBlock = "//div[contains(@class, 'section-block-with-props')]";
		public const string CharacteristicBlockTitle = "//div[@class='section-sub-header']";
		public const string CharacteristicName = "//div[@class='list-items']/div[@class='item']/div[1]";
		public const string CharacteristicValue = "//div[@class='list-items']/div[@class='item']/div[2]";
		public const string Images = "//div[@class='slider-for slider-gallery-top-trade slick-initialized slick-slider']//div[@class='slick-track']//img";
	}

	public bool IsNoPhoto { get; set; }

	public SpecAvtoProduct(IElementHandle? element = null, bool isNoPhoto = false) : base(new(), element)
	{
		IsNoPhoto = isNoPhoto;
	}

	public async Task<IEnumerable<string>> GetImagesAsync(string domain, string directory)
	{
		List<Task<string?>> imagePaths = new();
		List<IElementHandle> elementHandles = new(await Page!.QuerySelectorAllAsync(XPath.Images));

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

	public override async Task ParseAsync(SpecAvtoParser parser)
	{
		if (Element == null) throw new NullReferenceException("Element is null");

		URL = await GetURLAsync(parser, Element);
		// URL = "https://specavto.ru/marks/kruvis/selhoztehnika/kutivatory/kpm-14/";
		// Title = "КРУВИС КПМ-14 (КПМ-16)";

		await ParseAsyncInternal(parser);
	}

	public async Task ParseAsyncInternal(SpecAvtoParser parser)
	{
		await OpenPageAsync(parser.Browser, URL!);

		IElementHandle? titleElement = await Page!.QuerySelectorAsync(XPath.Title);
		if (titleElement is null)
		{
			Log.Error("Title element is null");
			return;
		}

		Title = await GetTextAsync(titleElement);
		SaveManager.UpdateDirectoryPath(); // Применить новый путь к директории

		SubProducts = null; // Очистка перед добавлением

		var options = new Options(this);

		// https://specavto.ru/marks/vostok_kapital/selhoztehnika/kutivatory/kpp_12/ --- ul ебаные 
		if (IsNoPhoto)
		{
			// Если в товаре есть под-продукты, то они будут добавлены в список на добавление
			await GetMultipleProductsAsync(IsNoPhoto, options);
		}

		if (SubProducts == null)
		{
			await CharacteristicsParser.ParseAsync(Page!, options);
			Log.Print("Successfully get special characteristics");
		}

		await Task.Run(SaveManager.SaveAsync);

		Log.Print($"Close product: {URL}");
		await Page!.CloseAsync();
	}

	[Obsolete]
	public async Task<int> GetAverageCountOfCharacteristicsAsync()
	{
		return await CharacteristicsParser.GetAverageCountOfCharacteristicsAsync(Page!);
	}

	public async Task GetMultipleProductsAsync(bool isNoPhoto, Options options)
	{
		List<IElementHandle>? multipleProductsName = null;

		for (ushort i = 0; i < XPath.MultipleProductsName.Thead.Paths.Length; i++)
		{
			string xpath = XPath.MultipleProductsName.Thead.Paths[i];
			multipleProductsName = (await Page!.QuerySelectorAllAsync(xpath)).ToList();
			if (multipleProductsName.Count > 0)
			{
				// Проверка на различные сломанные продукты и предупреждение
				if (XPath.MultipleProductsName.Thead.Warnings.ElementAtOrDefault(i) is string xpathWarning)
				{
					IReadOnlyList<IElementHandle> warningElements = await Page.QuerySelectorAllAsync(xpathWarning);
					if (warningElements.Count > multipleProductsName.Count)
					{
						Log.Warning($"[CAREFUL] {xpathWarning} has more elements than {xpath}");
					}
				}

				options.Offset.General.Vertical = 1; // Пропуская имена моделей
				break;
			};
			multipleProductsName = null; // Очистка
		}

		// Проверяю колонку со значением на наличие колонки С НЕ ЗНАЧЕНИЕМ
		foreach (var xpath in XPath.HeadSpan)
		{
			if (await Page!.QuerySelectorAllAsync(xpath) is IReadOnlyList<IElementHandle> headSpans)
			{
				for (ushort i = 0; i < headSpans.Count; i++)
				{
					string text = (await headSpans[i].InnerTextAsync()).Trim().ToLower();
					if (text.Contains(["ед. изм.", "единица измерения", "ед.изм."]))
					{
						options.Offset.Value.Horizontal += i;
						options.AddedWord = AddedWord.UnitsOfMeasurement;
						multipleProductsName ??= await GetMultipleProductsNameBySimilarityAsync(headSpans.Skip(i + 1));

						goto MainLoopBreak;
					}
				}
			}

		MainLoopBreak: break;
		}

		bool haveProducts = multipleProductsName != null && multipleProductsName.Count > 0;

		if (haveProducts)
		{
			SubProducts = new();

			CharacteristicOffset originalOffset = options.Offset.Clone();

			for (ushort i = 0; i < multipleProductsName!.Count; i++)
			{
				SpecAvtoProduct product = new();
				product.URL = URL;
				product.Title = SpecAvtoCharacteristicItem.FormatName(await multipleProductsName[i].InnerTextAsync());
				product.Page = Page;
				product.IsNoPhoto = isNoPhoto;

				// Настройка смещения для получения хар-ик под-модели
				options.Offset = originalOffset.Clone();
				options.Offset.Value.Horizontal += i;

				await product.CharacteristicsParser.ParseAsync(Page!, options);

				SubProducts.Add(product);
			}
		}
	}

	protected async Task<List<IElementHandle>> GetMultipleProductsNameBySimilarityAsync(IEnumerable<IElementHandle> headSpans)
	{
		List<IElementHandle> result = new();

		foreach (IElementHandle headSpan in headSpans)
		{
			string text = await headSpan.InnerTextAsync();
			if (StringUtils.CalculateMaxSimilarityByWords(Title!, text) >= 0.5)
			{
				result.Add(headSpan);
			}
		}

		return result;
	}
}