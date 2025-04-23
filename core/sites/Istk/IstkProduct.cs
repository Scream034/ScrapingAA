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

public class IstkProduct : BrowserProduct<IstkCharacteristicsParser, IstkParser, IstkProduct>
{
    public static class XPath
    {
        public const string Characteristics = "//div[@id='nav-features']//div[@class='small-desc']//tbody";
        public const string Images = "//div[@itemscope]/div[@class='row']/div[1]/div[2]//div[@data-slick-index]/a";
    }

    public IstkProduct(IElementHandle? element = null) : base(new IstkCharacteristicsParser(), element) { }

    public override async Task ParseAsync(IstkParser parser)
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

        // await CharacteristicsParser.AssertParseAsync(await Page!.QuerySelectorAsync(XPath.Characteristics));
        Log.Print("Successfully get characteristics");
        SaveManager.ImagePaths = (await GetImagesAsync(parser.Domain!, Title)).ToList();
        Log.Print("Successfully download images");

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
            string? url = await element.GetAttributeAsync("href");
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