namespace Core.Internal;

using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Godot;
using Core.IO;
using Core.Log;
using Core.Components.Characteristics;

public static class ProductBaseSaveManagerOld
{
	public static async Task<bool> LoadAndReplaceAsync<TBaseCharacteristicsParser>(IProductBase<TBaseCharacteristicsParser> product, string directoryName, string fileName) where TBaseCharacteristicsParser : IBaseCharacteristicsParser
	{
		string directoryPath = Path.Join(Constants.Path.Folder.Temp, directoryName);
		string infoFilePath = Path.Join(directoryPath, fileName);

		if (Path.Exists(directoryPath))
		{
			product.SaveManager.DirectoryName = directoryName;
			product.SaveManager.DirectoryPath = directoryPath;
		}
		else
		{
			Log.Error($"Directory not found: {directoryPath}");
			return false; // Или можно выбросить исключение
		}

		if (!Path.Exists(infoFilePath))
		{
			Log.Error($"Info file not found: {infoFilePath}");
			return false; // Или можно выбросить исключение, в зависимости от логики обработки ошибок
		}

		using System.IO.StreamReader reader = new(infoFilePath);
		product.URL = await reader.ReadLineAsync();
		product.CharacteristicsParser.Characteristics.Storage.FromIOString(await reader.ReadToEndAsync());

		product.SaveManager.LoadImages();

		await product.SaveManager.SaveAsync();

		return true;
	}
}