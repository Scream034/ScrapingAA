namespace Core.Components;

using System;
using System.IO;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;

using Godot;

using Log;

public static partial class NetworkUtils
{
	private static readonly System.Net.Http.HttpClient client = new();

	public static async Task<string> PostAsync<T>(string url, T data)
	{
		// Сериализуем объект в JSON
		string? json = System.Text.Json.JsonSerializer.Serialize(data);
		StringContent? content = new(json, Encoding.UTF8, "application/json");

		// Выполняем POST-запрос
		HttpResponseMessage response = await client.PostAsync(url, content);

		// Проверяем успешность ответа
		if (response.IsSuccessStatusCode)
		{
			// Возвращаем ответ от сервера
			return await response.Content.ReadAsStringAsync();
		}
		else
		{
			throw new Exception($"POST Error: {response.StatusCode}, Message: {await response.Content.ReadAsStringAsync()}");
		}
	}

	/// <summary>
	/// Загружает изображение по указанному URL и записывает его в буфер обмена в формате Base64.
	/// </summary>
	/// <param name="url">URL изображения.</param>
	/// <returns>True, если изображение успешно записано в буфер обмена; иначе False.</returns>
	public static async Task<bool> CopyImageToClipboardAsync(string url)
	{
		try
		{
			// Загружаем изображение как массив байтов
			byte[] imageData = await client.GetByteArrayAsync(url);

			if (imageData == null || imageData.Length == 0)
			{
				Log.Error($"Failed to download image from {url}: Image data is empty.");
				return false;
			}

			// Преобразуем массив байтов в Base64-строку
			string base64Image = Convert.ToBase64String(imageData);

			// Определяем MIME-тип изображения на основе расширения URL
			string mimeType = GetMimeTypeFromUrl(url);
			string base64Prefix = $"data:{mimeType};base64,";

			// Формируем полную Base64-строку
			string base64String = base64Prefix + base64Image;

			// Записываем строку в буфер обмена
			DisplayServer.ClipboardSet(base64String);

			Log.Print($"Image copied to clipboard from {url}");
			return true;
		}
		catch (HttpRequestException ex)
		{
			Log.Error($"HTTP error while downloading image from {url}: {ex.Message}");
		}
		catch (Exception ex)
		{
			Log.Error($"Error while copying image to clipboard: {ex.Message}");
		}

		return false;
	}

	/// <summary>
	/// Определяет MIME-тип изображения на основе расширения URL.
	/// </summary>
	/// <param name="url">URL изображения.</param>
	/// <returns>MIME-тип или "image/jpeg" по умолчанию.</returns>
	public static string GetMimeTypeFromUrl(string url)
	{
		string fileExtension = Path.GetExtension(url).ToLowerInvariant();
		return fileExtension switch
		{
			".png" => "image/png",
			".jpg" or ".jpeg" => "image/jpeg",
			".gif" => "image/gif",
			".bmp" => "image/bmp",
			".tiff" => "image/tiff",
			_ => "image/jpeg" // По умолчанию JPEG
		};
	}
}