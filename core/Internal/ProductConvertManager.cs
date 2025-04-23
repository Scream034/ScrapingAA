namespace Core.Internal;

using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Core.Components.Characteristics;
using Core.Log;
using Core.IO;
using System;

public sealed class ProductConvertManager : IDisposable
{
	public static bool NeedConvert(in IProductBase<IBaseCharacteristicsParser> product)
	{
		try
		{
			if (product.SaveManager.DirectoryName != null && Path.Exists(Path.Join(product.SaveManager.DirectoryName, Constants.PathName.File.InfoOld)) && !Path.Exists(Path.Join(product.SaveManager.DirectoryName, Constants.PathName.File.Info)))
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

	public async Task ConvertProductAsync(IProductBase<IBaseCharacteristicsParser> product)
	{
		if (!NeedConvert(product)) return;

		Log.Print($"Converting product: {product.Title}");
		await product.SaveManager.SaveAsync(Path.Join(product.SaveManager.DirectoryName!, Constants.PathName.File.InfoOld));
		Log.Print($"Converted product: {product.Title}");
	}

	public void Dispose()
	{
		GC.SuppressFinalize(this);
	}

	~ProductConvertManager() => Dispose();
}