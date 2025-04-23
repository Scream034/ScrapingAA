namespace Core.Internal;

using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Godot;
using Core.IO;
using Core.Log;
using Core.Components.Characteristics;

public sealed class ProductBaseSaveManager<TBaseCharacteristicsParser> where TBaseCharacteristicsParser : IBaseCharacteristicsParser
{
	public readonly IProductBase<TBaseCharacteristicsParser> Product;

	public List<string> ImagePaths { get; set; } = new();
	public string? DirectoryName;
	public string? DirectoryPath;

	public ProductBaseSaveManager(in IProductBase<TBaseCharacteristicsParser> product)
	{
		Product = product;
	}

	public bool UpdateDirectoryPath()
	{
		if (string.IsNullOrEmpty(DirectoryName))
		{
			Log.Error("DirectoryName is null");
			return false;
		}

		DirectoryName = ParseTitle(DirectoryName);
		// Если последний символ является ./,:;' и т.п., то удалить
		DirectoryName = char.IsSymbol(DirectoryName[^1]) ? DirectoryName[..^1] : DirectoryName;
		DirectoryPath = Path.Join(Constants.Path.Folder.Temp, DirectoryName);
		DirAccess.MakeDirRecursiveAbsolute(DirectoryPath);

		LoadImages();

		return true;
	}

	public string? SetTitle(string? title)
	{
		if (DirectoryName == title || string.IsNullOrEmpty(title) || string.IsNullOrEmpty(DirectoryPath)) return null;

		if (Path.Exists(DirectoryPath))
		{
			title = ParseTitle(title);

			string newPath = Path.Join(Constants.Path.Folder.Temp, title);
			if (Path.Exists(newPath)) return null;

			Error error = DirAccess.RenameAbsolute(DirectoryPath, Path.Join(Constants.Path.Folder.Temp, title));
			if (error is not Error.Ok)
			{
				Log.Error($"Error: {error}");
				return null;
			}

			DirectoryName = title;
			UpdateDirectoryPath();
		}

		return DirectoryName;
	}

	public async Task<bool> LoadAsync(string directoryName, string fileName)
	{
		string directoryPath = Path.Join(Constants.Path.Folder.Temp, directoryName);
		string infoFilePath = Path.Join(directoryPath, fileName);

		if (Path.Exists(directoryPath))
		{
			DirectoryName = directoryName;
			DirectoryPath = directoryPath;
		}
		else
		{
			Log.Error($"Directory not found: {directoryPath}");
			return false; // Или можно выбросить исключение
		}

		if (!Path.Exists(infoFilePath))
		{
			// Попытка загрузить из старой версии
			if (await ProductBaseSaveManagerOld.LoadAndReplaceAsync(Product, directoryName, Constants.PathName.File.InfoOld))
			{
				return true;
			}

			Log.Error($"Info file not found: {infoFilePath}");
			return false; // Или можно выбросить исключение, в зависимости от логики обработки ошибок
		}

		using System.IO.StreamReader reader = new(infoFilePath);
		Product.URL = await reader.ReadLineAsync();
		Product.CompletedURL = await reader.ReadLineAsync();
		Product.CharacteristicsParser.FromIOString(await reader.ReadToEndAsync());

		LoadImages();

		return true;
	}

	public async Task<bool> LoadAsync(string directoryName)
	{
		return await LoadAsync(directoryName, Constants.PathName.File.Info);
	}

	public bool LoadImages()
	{
		if (string.IsNullOrEmpty(DirectoryPath))
		{
			Log.Error("DirectoryPath is null");
			return false;
		}

		string path = Path.Join(DirectoryPath, Constants.PathName.Folder.Images);
		if (!Path.Exists(path))
		{
			return false;
		}

		ImagePaths = DirAccess.GetFilesAt(path).Where(static file => Constants.ImageExtensions.Contains(File.GetExtension(file).ToLower())).Select(file => Path.Join(path, file)).ToList();
		return true;
	}

	public async Task SaveAsync(string fileName)
	{
		if (string.IsNullOrEmpty(DirectoryPath))
		{
			Log.Error("DirectoryPath is null");
			return;
		}

		string path = Path.Join(DirectoryPath, fileName);
		try
		{
			using System.IO.StreamWriter writer = new(path);
			writer.WriteLine(Product.URL);
			writer.WriteLine(Product.CompletedURL);
			// Записываем характеристики без последнего Separator
			await writer.WriteAsync(Product.CharacteristicsParser.ToIOString());
		}
		catch (Exception ex)
		{
			Log.Error(ex.Message);
		}

		Log.Print($"Saved: {path}");
	}

	public async Task SaveAsync()
	{
		await SaveAsync(Constants.PathName.File.Info);
	}

	public async Task SavePreviousAsync()
	{
		await SaveAsync(Constants.PathName.File.InfoPrevious);
	}

	public async Task<bool> RestoreAsync()
	{
		if (CanRestore())
		{
			return await LoadAsync(DirectoryName!, Constants.PathName.File.InfoPrevious);
		}

		return false;
	}

	public bool SetIsAdded(bool isAdded)
	{
		if (string.IsNullOrEmpty(DirectoryPath)) return false;

		string path = Path.Join(DirectoryPath, Constants.PathName.File.IsAdded);
		Log.Print($"Set is added: {path}");

		if (isAdded)
		{
			FileAccess file = FileAccess.Open(path, FileAccess.ModeFlags.Write);
			file.StoreString(default);
			file.Close();
		}
		else
		{
			if (!Path.Exists(path))
			{
				return false;
			}
			Directory.Remove(DirectoryPath);
		}
		return true;
	}

	public bool Remove()
	{
		if (string.IsNullOrEmpty(DirectoryName))
		{
			Log.Error($"Not found DirectoryName");
			return false;
		}
		else if (string.IsNullOrEmpty(DirectoryPath))
		{
			Log.Error($"Not found DirectoryPath");
			return false;
		}
		else if (!Path.Exists(DirectoryPath))
		{
			Log.Error($"Not found with {DirectoryName}");
			return false;
		}

		return Directory.Remove(DirectoryPath);
	}

	public bool IsAdded()
	{
		if (string.IsNullOrEmpty(DirectoryPath)) return false;

		return Path.Exists(Path.Join(DirectoryPath, Constants.PathName.File.IsAdded));
	}

	public bool CanRestore()
	{
		return DirectoryPath != null && Path.Exists(Path.Join(DirectoryPath, Constants.PathName.File.InfoPrevious));
	}

	public ProductBaseSaveManager<TTCharacteristicsParser> Clone<TTCharacteristicsParser>(IProductBase<TTCharacteristicsParser> product) where TTCharacteristicsParser : IBaseCharacteristicsParser
	{
		return new(product)
		{
			ImagePaths = ImagePaths.ToList()
		};
	}

	public bool IsOld()
	{
		try
		{
			if (DirectoryName != null && Path.Exists(Path.Join(DirectoryName, Constants.PathName.File.InfoOld)) && !Path.Exists(Path.Join(DirectoryName, Constants.PathName.File.Info)))
			{
				return true;
			}
		}
		catch (Exception ex)
		{
			Log.Error(ex);
		}

		return false;
	}

	public static string ParseTitle(in string title) => System.Text.RegularExpressions.Regex.Replace(Constants.InvalidFileNameChars.Aggregate(title, static (current, c) => current.Replace(c, ' ')).Replace("   ", " "), @"\s+", " ").Trim();
}