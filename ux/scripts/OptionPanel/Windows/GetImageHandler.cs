using System;
using System.Threading.Tasks;
using Godot;
using Core.Components;
using System.Linq;
using Core.Log;
using Core.Components.Network;

namespace UX.Option.First.Windows.GetImage;

public partial class GetImageHandler : Control
{
	public static string LastDirectoryFilePath = "res://llim";

	public event Action<Image?> ImageChanged = null!;

	[Export] public TextureRect TextureRect = null!;
	[Export] public Button LoadFileButton = null!;
	[Export] public Button LoadFileFromClipboardButton = null!;
	[Export] public Button ClearButton = null!;

	public Image? Image
	{
		get => _image; set
		{
			if (value == null)
			{
				TextureRect.Texture = null;
			}
			else
			{
				TextureRect.Texture = ImageTexture.CreateFromImage(value);
			}

			ImageChanged?.Invoke(value);
			_image = value;
		}
	}
	private Image? _image;

	public override void _Ready()
	{
		LoadFileButton.Pressed += LoadImageFromFile;
		LoadFileFromClipboardButton.Pressed += async () => await LoadImageFromClipboardAsync();
		ClearButton.Pressed += OnClearButtonPressed;
	}

	public void LoadImageFromFile()
	{
		Log.Print("Try to load image from file");
		SetProcessInput(false);
		SetProcessUnhandledInput(false);

		var lastDirectoryFile = FileAccess.Open(LastDirectoryFilePath, FileAccess.ModeFlags.Read);
		string lastDirectory = lastDirectoryFile == null ? "" : lastDirectoryFile.GetAsText();
		DisplayServer.FileDialogShow("Загрузить из файла изображение", lastDirectory, "", false, DisplayServer.FileDialogMode.OpenFile, new string[] { "*.png,*.jpg,*.jpeg,*.bmp,*.tga,*.webp;Image Files" }, new(this, nameof(OnFileSelected)));
		lastDirectoryFile?.Close();
	}

	public async Task LoadImageFromClipboardAsync()
	{
		if (!DisplayServer.ClipboardHas()) return;

		Log.Print("Try to load image from clipboard");
		if (DisplayServer.ClipboardHasImage())
		{
			Image = DisplayServer.ClipboardGetImage();
			Log.Print("Image loaded from clipboard");
		}
		else
		{
			var clipboard = DisplayServer.ClipboardGet();
			if (clipboard == null) return;

			Image = await ImageDownloader.DownloadAsyncWithProgress(GetTree(), clipboard, "Скачивание");
			Log.Print($"Image loaded from clipboard url: {clipboard}");

			if (Image == null) {
				Log.Print("Image is null");
				OS.Alert("Не удалось загрузить изображение из буфера обмена", "Ошибка!");
			}
		}
	}

	public void Clear()
	{
		Image = null;
		TextureRect.Texture = null;
	}

	private void OnFileSelected(bool status, string[] paths, int _)
	{
		if (!status || paths.Length == 0) return;

		SetProcessInput(true);
		SetProcessUnhandledInput(true);

		string path = paths.First();
		Log.Print($"Selected file: {path}");

		Image = Image.LoadFromFile(path);

		FileAccess lastDirectoryFile = FileAccess.Open(LastDirectoryFilePath, FileAccess.ModeFlags.Write);
		lastDirectoryFile.StoreString(System.IO.Path.GetDirectoryName(path));
		lastDirectoryFile.Close();
	}

	private void OnClearButtonPressed()
	{
		Log.Print("Clear button pressed");
		Clear();
	}
}