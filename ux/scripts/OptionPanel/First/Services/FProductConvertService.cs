namespace UX.Option.First.Services;

using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Internal;
using Core.Log;
using Godot;
using UX.Components.Table;

public sealed class FProductConvertService : IFService<FServiceNoneType, FProductConvertStartArguments>
{
	public static string Name = "Конвертировать продукты в новые";

	public void Init(FServiceNoneType xArgs = new())
	{
		GD.Print("FProductConvertService Init");
	}

	public async Task Start(FProductConvertStartArguments xArgs)
	{
		using ProductConvertManager manager = new();

		foreach (var row in xArgs.Products)
		{
			await manager.ConvertProductAsync(row.Product);
		}
	}
}

public class FProductConvertStartArguments(in List<ProductTableRow> products) : IFStartArguments
{
	public readonly List<ProductTableRow> Products = products;
}