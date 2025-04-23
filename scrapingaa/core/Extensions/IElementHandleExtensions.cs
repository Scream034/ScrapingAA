namespace Core.Extensions;

using Log;
using IO;

using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Playwright;

public static class IElementHandleExtensions
{
    public static async Task<IElementHandle?> GetParentAsync(this IElementHandle elementHandle)
    {
        return await elementHandle.QuerySelectorAsync("xpath=..");
    }

    public static async Task<string> GetXPathAsync(this IElementHandle elementHandle)
    {
        return await elementHandle.EvaluateAsync<string>(@"element => {
            if (!element) return '';

            const idx = (e) => {
                let siblings = e.parentNode.children;
                let count = 1;
                for (let i=0; i<siblings.length; i++) {
                    if (siblings[i].nodeName == e.nodeName) {
                        if (siblings[i]===e) return count;
                        count++;
                    }
                }
                return 0; // Should not happen
            };

            const tagName = (e) => {
                return e.tagName.toLowerCase();
            }

            let path = '';
            let currentElement = element;
            while(currentElement && currentElement.nodeType === Node.ELEMENT_NODE){
                let currentPath = tagName(currentElement);
                let index = idx(currentElement);
                if(index > 1){
                    currentPath += '[' + index + ']';
                }
                if(path){
                    path = '/' + currentPath + path;
                } else {
                    path = '/' + currentPath;
                }
                currentElement = currentElement.parentNode;
            }
            return path;
        }");
    }

    public static async Task<int> GetPositionOfParentAsync(this IElementHandle elementHandle)
    {
        int position = 0;

        if (await elementHandle.GetParentAsync() is IElementHandle parent)
        {
            foreach (IElementHandle child in await parent.QuerySelectorAllAsync("xpath=*"))
            {
                position++;
                if (child == elementHandle) break;
            }
        }

        return position;
    }

    public static async Task<(string? base64Image, string? mimeType)> GetBackgroundImageAsBase64Async(this IElementHandle element)
    {
        // Проверяем, что элемент найден
        if (element == null)
        {
            Log.Error("Element not found");
            throw new NullReferenceException("Element not found");
        }

        // Извлекаем стиль background-image
        string? backgroundImage = await element.EvaluateAsync<string>("el => getComputedStyle(el).backgroundImage");

        // Если стиль найден, разбираем URL
        if (!string.IsNullOrEmpty(backgroundImage) && backgroundImage.StartsWith("url("))
        {
            // Удаляем 'url(' и ')'
            string url = backgroundImage.Substring(4, backgroundImage.Length - 5).Trim('"');

            // Получаем изображение по URL
            using HttpClient httpClient = new HttpClient();
            byte[] imageData = await httpClient.GetByteArrayAsync(url);

            return (Convert.ToBase64String(imageData), GetMimeTypeFromUrl(url)); // Предполагаем, что это PNG. Можно изменить тип в зависимости от вашего изображения.
        }

        return (null, null); // Если не найдено
    }

    private static string GetMimeTypeFromUrl(string url)
    {
        string extension = File.GetExtension(url).ToLower();
        return extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".bmp" => "image/bmp",
            ".webp" => "image/webp",
            _ => "application/octet-stream", // стандартный тип, если расширение не распознано
        };
    }
}