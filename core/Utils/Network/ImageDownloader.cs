namespace Core.Components.Network;
using System;
using System.Threading.Tasks;
using Godot;
using Core.Log;
using Core.UI.Window;

public static class ImageDownloader
{
	// Событие для уведомления о прогрессе
	public static event Action<float>? ProgressChanged;

	/// <summary>
	/// Скачивает изображение по указанному URL и возвращает его как Godot.Image.
	/// </summary>
	/// <param name="url">URL изображения.</param>
	/// <returns>Объект Godot.Image или null, если загрузка не удалась.</returns>
	public static async Task<Image?> DownloadAsync(string url)
	{
		ProgressChanged?.Invoke(0.0f);

		void OnProgressChanged(float percentage) => ProgressChanged?.Invoke(percentage);

		FileDownloader.DownloadProgressChanged += OnProgressChanged;

		var info = await FileDownloader.DownloadAsync(url);
		if (info == null)
		{
			Log.Print("Download failed");
			return null;
		}

		FileDownloader.DownloadProgressChanged -= OnProgressChanged;

		ProgressChanged?.Invoke(obj: 0.99f);

		try
		{
			// Попытка загрузить изображение как JPEG
			Image image = new Image();
			Error loadResult = image.LoadJpgFromBuffer(info.Bytes);

			if (loadResult != Error.Ok)
			{
				// Если не удалось загрузить как JPEG, попробуем как PNG
				loadResult = image.LoadPngFromBuffer(info.Bytes);

				if (loadResult != Error.Ok)
				{
					Log.Error($"Failed to load image from buffer: {loadResult}");
					return null;
				}
			}

			Log.Print($"Image loaded successfully from {url}");
			return image;
		}
		catch (Exception ex)
		{
			Log.Error($"Error while loading image from {url}: {ex.Message}");
			return null;
		}
		finally
		{
			ProgressChanged?.Invoke(1.0f);
		}
	}

	public static async Task<Image?> DownloadAsyncWithProgress(SceneTree tree, string url, string label, bool showCancelButton = false)
	{
		var tcs = new TaskCompletionSource<Image?>();

		// Создаем окно прогресса
		_ = ProgressWindow.ShowAndAwaitAsync(tree, label, async window =>
		{
			void OnProgressChanged(float progress)
			{
				window.SetProgress(progress);
			};

			// Подписываемся на событие прогресса
			ProgressChanged += OnProgressChanged;

			try
			{
				// Выполняем скачивание
				Image? result = await DownloadAsync(url);
				tcs.TrySetResult(result);
			}
			catch (Exception ex)
			{
				tcs.TrySetException(ex);
			}
			finally
			{
				// Отписываемся от события после завершения
				ProgressChanged -= OnProgressChanged;
			}
		}, showCancelButton);

		return await tcs.Task;
	}
}