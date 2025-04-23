namespace Core.Sites.Istk;

using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.Playwright;

using Core.Log;
using System.Linq;
using Core.Components.Network;
using Core.Components.Characteristics;

public class LeasingProduct : BrowserProduct<LeasingCharacteristicsParser, LeasingParser, LeasingProduct>
{
    public static class XPath
    {
        public const string Title = "//div[@class='l-template__title-column']/h1";
        public const string Images = "//div[@class='l-catalog-card__gallery-wrap swiper-wrapper']/a/img[@src]";
    }

    public bool DownloadImages = true;

    public LeasingProduct(IElementHandle? element = null) : base(new LeasingCharacteristicsParser(), element) { }

    public override async Task ParseAsync(LeasingParser parser)
    {
        if (Element == null)
        {
            Log.Error("Element is null");
            throw new NullReferenceException("Element is null");
        }

        await ParseAsyncInternal(parser);
    }

    public async Task ParseAsyncInternal(LeasingParser parser)
    {
        // URL = "https://yandex.ru";
        await OpenPageAsync(parser.Browser, URL!, WaitUntilState.Commit, default, XPath.Title);

        IElementHandle? titleElement = await Page!.QuerySelectorAsync(XPath.Title);
        if (titleElement == null)
        {
            Log.Error("Title element is null");
            throw new NullReferenceException("Title element is null");
        }

        Title = await GetTextAsync(titleElement);

        SaveManager.UpdateDirectoryPath();

        await CharacteristicsParser.AssertParseAsync(Page!);
        Log.Print("Successfully get characteristics");

        if (DownloadImages)
        {
            SaveManager.ImagePaths = (await GetImagesAsync(parser.Domain!, Title)).ToList();
            Log.Print("Successfully download images");
        }

        await Task.Run(SaveManager.SaveAsync);

        Log.Print($"Close product: {URL}");
        await Page!.CloseAsync();
    }

    public async Task<IEnumerable<string>> GetImagesAsync(string domain, string directory)
    {
        List<Task<string?>> imagePaths = new();

        // Указываем путь к директории для изображений
        directory = Path.Combine(Constants.Path.Folder.Temp, directory, Constants.PathName.Folder.Images);
        Directory.CreateDirectory(directory);

        // Получаем все изображения по заданному XPath
        foreach (IElementHandle element in await Page!.QuerySelectorAllAsync(XPath.Images))
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