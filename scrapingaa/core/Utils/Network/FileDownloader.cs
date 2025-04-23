namespace Core.Components.Network;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Core.Log;
using Core.UI.Window;
using Godot;

using HttpClient = System.Net.Http.HttpClient;

public static class FileDownloader
{
	private static readonly HttpClient client = new HttpClient();

	// Событие для уведомления о прогрессе скачивания
	public static event Action<float>? DownloadProgressChanged;

	public static async Task<string?> DownloadAndSaveAsync(string url, string directoryPath, string? fileName =null, bool SHA256FileName = false, string defaultFileName = "error")
	{
		DownloadProgressChanged?.Invoke(0f);
		await Task.Delay(100); // Для демонстрации прогресса

		fileName ??= Path.GetFileName(url) ?? defaultFileName;
		string filePath = Path.Combine(directoryPath, fileName);
		if (File.Exists(filePath))
		{
			DownloadProgressChanged?.Invoke(0.99f);
			Log.Print($"File already exists:\nfrom {url} to {filePath}");
			await Task.Delay(100); // Для демонстрации прогресса
			return filePath;
		}

		var info = await DownloadAsync(url);
		if (info == null)
		{
			Log.Print("Download failed");
			return null;
		}

		// Генерируем имя файла
		fileName ??= (SHA256FileName ? CryptoUtils.GetByteArraySHA256(info.Bytes) : Path.GetFileName(url)) ?? defaultFileName;
		string fileExtension = Path.GetExtension(url);
		filePath = Path.ChangeExtension(fileName, fileExtension);

		if (string.IsNullOrEmpty(filePath))
		{
			Log.Error($"SHA256 hash is null for {url} with extension {fileExtension}");
			throw new InvalidOperationException("SHA256 hash is null");
		}

		filePath = Path.Combine(directoryPath, filePath);

		// Проверяем, существует ли файл
		if (File.Exists(filePath))
		{
			Log.Print($"File already exists:\nfrom {url} to {filePath}");
			return filePath;
		}

		// Сохраняем файл
		await File.WriteAllBytesAsync(filePath, info.Bytes);
		Log.Print($"Downloaded file:\nfrom {url} to {filePath}");

		return filePath;
	}

	/// <summary>
	/// Оповещает о прогрессе через событие DownloadProgressChanged.
	/// </summary>
	public static async Task<RawDownloadInfo?> DownloadAsync(string url)
	{
		try
		{
			// Проверяем существование файла по URL
			HttpResponseMessage response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
			response.EnsureSuccessStatusCode(); // Бросаем исключение, если статус неуспешный

			// Получаем общую длину файла (если доступна)
			long? totalBytes = response.Content.Headers.ContentLength;
			if (totalBytes == null)
			{
				Log.Print("Total bytes is null, no progress bar :(");
			}

			// Создаем имя файла
			byte[] bytes = Array.Empty<byte>();
			using (Stream contentStream = await response.Content.ReadAsStreamAsync())
			using (var memoryStream = new MemoryStream())
			{
				int bytesRead;
				byte[] buffer = new byte[81920];
				long downloadedBytes = 0;

				while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
				{
					await memoryStream.WriteAsync(buffer, 0, bytesRead);
					downloadedBytes += bytesRead;

					// Отправляем событие прогресса, если известен общий размер файла
					if (totalBytes.HasValue && DownloadProgressChanged != null)
					{
						float progress = (float)downloadedBytes / totalBytes.Value;
						GD.Print($"Downloading: {progress}");
						DownloadProgressChanged(progress); // Уведомляем подписчиков
					}
				}

				bytes = memoryStream.ToArray();
			}

			// Уведомляем о завершении скачивания (прогресс 1.0)
			DownloadProgressChanged?.Invoke(1.0f);

			return new(bytes, response);
		}
		catch (HttpRequestException ex)
		{
			Log.Error($"HTTP error while downloading from {url}: {ex.Message}");
			return null;
		}
		catch (InvalidOperationException ex)
		{
			Log.Error($"Invalid operation while downloading from {url}: {ex.Message}");
			return null;
		}
		catch (Exception ex)
		{
			Log.Error($"While saving file: {ex.Message}");
			throw; // Пробрасываем исключение дальше
		}
	}

	/// <summary>
	/// Загружает файл и показывает прогресс через ProgressWindow.
	/// </summary>
	/// <param name="tree">Дерево сцены.</param>
	/// <param name="url">URL для скачивания файла.</param>
	/// <param name="directoryPath">Путь к директории для сохранения файла.</param>
	/// <param name="label">Текст надписи в окне прогресса.</param>
	/// <param name="showCancelButton">Показывать кнопку отмены?</param>
	/// <returns>Путь к загруженному файлу или null при ошибке.</returns>
	public static async Task<string?> DownloadAndSaveWithProgressAsync(SceneTree tree, string url, string directoryPath, string label, string? fileName = null, bool SHA256FileName = false, bool showCancelButton = false)
	{
		var tcs = new TaskCompletionSource<string?>();

		// Создаем окно прогресса
		_ = ProgressWindow.ShowAndAwaitAsync(tree, label, async window =>
		{
			void OnDownloadProgressChanged(float progress)
			{
				window.SetProgress(progress);
			};

			// Подписываемся на событие прогресса
			DownloadProgressChanged += OnDownloadProgressChanged;

			try
			{
				// Выполняем скачивание
				string? result = await DownloadAndSaveAsync(url, directoryPath, fileName, SHA256FileName);
				tcs.TrySetResult(result);
			}
			catch (Exception ex)
			{
				tcs.TrySetException(ex);
			}
			finally
			{
				// Отписываемся от события после завершения
				DownloadProgressChanged -= OnDownloadProgressChanged;
			}
		}, showCancelButton);

		return await tcs.Task;
	}

	public static async Task<RawDownloadInfo?> DownloadWithProgressAsync(SceneTree tree, string url, string label, bool showCancelButton = false)
	{
		var tcs = new TaskCompletionSource<RawDownloadInfo?>();

		// Создаем окно прогресса
		_ = ProgressWindow.ShowAndAwaitAsync(tree, label, async window =>
		{
			// Подписываемся на событие прогресса
			DownloadProgressChanged += progress =>
			{
				window.SetProgress(progress);
			};

			try
			{
				// Выполняем скачивание
				var result = await DownloadAsync(url);
				tcs.TrySetResult(result);
			}
			catch (Exception ex)
			{
				tcs.TrySetException(ex);
			}
			finally
			{
				// Отписываемся от события после завершения
				DownloadProgressChanged -= progress =>
				{
					window.SetProgress(progress);
				};
			}
		}, showCancelButton);

		return await tcs.Task;
	}

	public class RawDownloadInfo
	{
		public readonly byte[] Bytes;
		public readonly HttpResponseMessage Response;

		public RawDownloadInfo(byte[] bytes, HttpResponseMessage response)
		{
			Bytes = bytes;
			Response = response;
		}
	}
}