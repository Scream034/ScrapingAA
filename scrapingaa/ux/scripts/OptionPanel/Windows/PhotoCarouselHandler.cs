using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Core;
using Core.Log;
using Core.Components;
using UX.Components.Table;
using System.Linq;

namespace UX.Option.First.Windows.Photo
{
	public partial class PhotoCarouselHandler : Control
	{
		[Export] private HBoxContainer carouselContainer = null!;
		[Export] private TextureRect currentImage = null!;
		[Export] private Button previousButton = null!;
		[Export] private Button nextButton = null!;
		[Export] private Button deleteButton = null!;
		[Export] private Button openInExplorerButton = null!;
		[Export] private Button findInBrowserButton = null!;
		[Export] private Button addImageButton = null!;
		[Export] private Label infoLabel = null!;

		private List<(string Path, Texture2D Texture)> imageList = new(); // Список путей до изображений
		private int currentIndex = 0;
		private ProductTableRow? row = null; // Текущий продукт

		public override void _Ready()
		{
			SetupUI();

			// Отображение первого изображения, если оно имеется
			if (imageList.Count > 0)
			{
				ShowImageAtIndex(0);
			}
		}

		private void SetupUI()
		{
			// Подключение событий для кнопок
			previousButton.Pressed += OnPrevButtonPressed;
			nextButton.Pressed += OnNextButtonPressed;
			deleteButton.Pressed += OnDeleteButtonPressed;
			openInExplorerButton.Pressed += OnOpenInExplorerButtonPressed;
			findInBrowserButton.Pressed += OnFindInBrowserButtonPressed;
			addImageButton.Pressed += OnAddImageButtonPressed;

			// Обновление информации о текущем изображении
			UpdateInfoLabel();
		}

		private bool IsImageFile(string fileName)
		{
			return Constants.GodotImageExtensions.Any(fileName.EndsWith);
		}

		private void OnPrevButtonPressed()
		{
			currentIndex = Math.Max(0, currentIndex - 1);
			ShowImageAtIndex(currentIndex);
		}

		private void OnNextButtonPressed()
		{
			currentIndex = Math.Min(imageList.Count - 1, currentIndex + 1);
			ShowImageAtIndex(currentIndex);
		}

		private void OnDeleteButtonPressed()
		{
			if (currentIndex < 0 || currentIndex >= imageList.Count) return;

			var imagePath = imageList[currentIndex].Path;
			imageList.RemoveAt(currentIndex);

			if (row != null && row.Product.SaveManager.ImagePaths.Contains(imagePath))
			{
				row.Product.SaveManager.ImagePaths.Remove(imagePath); // Удаляем путь изображения из продукта
				row.IsEmptyImages = row.Product.SaveManager.ImagePaths.Count == 0;
			}

			DirAccess.RemoveAbsolute(imagePath); // Удаляем файл

			if (imageList.Count == 0)
			{
				currentImage.Texture = null;
				infoLabel.Text = "Нет изображений";
				return;
			}

			currentIndex = Math.Min(currentIndex, imageList.Count - 1);
			ShowImageAtIndex(currentIndex);

			Log.Print($"Image deleted: {imagePath}");
		}

		private void OnOpenInExplorerButtonPressed()
		{
			if (currentIndex < 0 || currentIndex >= imageList.Count)
			{
				Log.Error("No image selected to open in explorer.");
				return;
			}

			string filePath = ProjectSettings.GlobalizePath(imageList[currentIndex].Path).Replace("/", "\\");
			try
			{
				Process.Start(new ProcessStartInfo
				{
					FileName = "explorer.exe",
					Arguments = $"/select,\"{filePath}\"",
					UseShellExecute = true
				});
			}
			catch (Exception ex)
			{
				Log.Error($"Failed to open explorer: {ex.Message}");
			}
		}

		private void OnFindInBrowserButtonPressed()
		{
			if (row == null || string.IsNullOrEmpty(row.Title))
			{
				Log.Error("No product title to search.");
				return;
			}

			try
			{
				Process.Start(new ProcessStartInfo($"https://yandex.ru/images/search?from=tabbar^&text={row.Title}") { UseShellExecute = true });
			}
			catch (Exception ex)
			{
				Log.Error($"Failed to open browser: {ex.Message}");
			}
		}

		private void OnAddImageButtonPressed()
		{
			if (row == null)
			{
				Log.Error("No product selected to add images.");
				return;
			}

			Log.Print("Add image button pressed.");

			var window = GetImageWindow.Create();
			window.TreeExited += () =>
			{
				if (row.Product.SaveManager.DirectoryPath == null)
				{
					Log.Error($"Directory not found for product: {row.Title}");
					return;
				}
				else if (window.Handler.Image == null)
				{
					Log.Print($"No image selected to add.");
					return;
				}

				LoadImageAndSave(window.Handler.Image);

				SetProcess(true);
			};

			GetTree().Root.AddChild(window);
			window.PopupCentered();

			SetProcess(false);
		}

		private void UpdateInfoLabel()
		{
			if (row == null || currentIndex < 0 || currentIndex >= imageList.Count)
			{
				infoLabel.Text = $"{row?.Title ?? "Не выбран"}\nНет изображений";
				return;
			}

			infoLabel.Text = $"{row.Title}\nИзображение: {currentIndex + 1}/{imageList.Count}";
		}

		public void ShowImageAtIndex(int index)
		{
			if (index < 0 || index >= imageList.Count) return;

			currentImage.Texture = imageList[index].Texture;
			currentIndex = index;
			UpdateInfoLabel();
		}

		public void LoadImagesFromProduct(ProductTableRow xRow)
		{
			if (xRow == null)
			{
				Log.Error("ProductTableRow is null.");
				return;
			}

			ClearImages();
			row = xRow;

			foreach (var imagePath in row.Product.SaveManager.ImagePaths)
			{
				LoadImage(imagePath);
			}

			UpdateInfoLabel();
		}

		public void LoadImage(string filePath)
		{
			if (IsImageFile(filePath))
			{
				try
				{
					var texture = ImageTexture.CreateFromImage(Image.LoadFromFile(filePath));
					if (texture != null)
					{
						imageList.Add((filePath, texture));
					}
					Log.Print($"Image loaded: {filePath}");
				}
				catch (Exception ex)
				{
					Log.Error($"Failed to load image: {filePath}. Error: {ex.Message}");
					OS.Alert($"Не удалось загрузить изображение: {filePath}\n\n", "Ошибка!");
				}
			}
			else
			{
				Log.Error($"File is not an image: {filePath}");
				OS.Alert($"Файл не является изображением: {filePath}\n\n", "Ошибка!");
			}
		}

		public void LoadImageAndSave(Image image)
		{
			var texture = ImageTexture.CreateFromImage(image);
			if (texture == null)
			{
				Log.Error("Failed to create texture from image.");
				OS.Alert($"Не удалось создать текстуру из изображения.\n\n", "Ошибка!");
				return;
			}

			string filePath = System.IO.Path.Combine(row!.Product.SaveManager.DirectoryPath!, Constants.PathName.Folder.Images).Replace("\\", "/");

			DirAccess.MakeDirAbsolute(filePath);

			var fileData = image.SaveJpgToBuffer(1.0f);
			filePath = System.IO.Path.Combine(filePath, CryptoUtils.GetByteArraySHA256(fileData)!) + ".jpg";

			image.SaveJpg(filePath, 1.0f);

			row.Product.SaveManager.ImagePaths.Add(filePath);
			imageList.Add((filePath, texture));
			ShowImageAtIndex(imageList.Count - 1);
			row.IsEmptyImages = false;
		}

		public void ClearImages()
		{
			imageList.Clear();
			currentImage.Texture = null;
			currentIndex = -1;
			UpdateInfoLabel();
		}
	}
}