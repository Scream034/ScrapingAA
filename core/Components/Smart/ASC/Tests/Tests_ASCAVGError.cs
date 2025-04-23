namespace Core.Components.Smart.ASC.Internal.Tests;

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Core.Components.Characteristics;
using Core.Components.MyStem;
using Core.Log;

public sealed class Tests_ASCAVGError
{
	public readonly ASCClassifier Classifier;
	public IProductBase<IBaseCharacteristicsParser>[]? Products;

	public Tests_ASCAVGError(in ASCClassifier classifier)
	{
		Classifier = classifier;
		Classifier.Initialize();
	}

	public float Test()
	{
		if (Products == null)
		{
			throw new NullReferenceException("Products is null");
		}

		int errors = 0;
		int total = 0;
		int index = 0;
		int totalProducts = Products.Length;

		Stopwatch stopwatch = Stopwatch.StartNew();

		// Собираем характеристики в предполагаемом виде
		foreach (var product in Products)
		{
			index++;

			foreach (var storage in product.CharacteristicsParser.Characteristics.Storage.Dictionary)
			{
				if (storage.Key == ProductCharacteristics.Type.Unknown) continue;
				else if (storage.Value.Count == 0) continue;

				foreach (var characteristic in storage.Value)
				{
					ASCClassifierCategoryData data = new(characteristic.Key, characteristic.Value, new(), new(), new());
					ProductCharacteristics.Type suggestion = Classifier.Classify(characteristic.Key, characteristic.Value);
					if (suggestion != storage.Key)
					{
						errors++;
					}

					total++;
				}
			}

			// Log.Print($"Remaining: {totalProducts - index}");
		}

		stopwatch.Stop();

		float errorsPercentage = (float)errors / total;

		Log.Print($"Total: {total} ({totalProducts})");
		Log.Print($"Errors: {errors} ({errorsPercentage * 100}%)");
		Log.Print($"Time: {stopwatch.ElapsedMilliseconds}ms (~ AVG: {stopwatch.ElapsedMilliseconds / totalProducts}ms)");

		return errorsPercentage;
	}
}