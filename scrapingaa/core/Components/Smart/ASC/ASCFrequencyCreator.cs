namespace Core.Components.Smart.ASC.Internal;

using System;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
using Core.Log;
using Core.Components.Characteristics;

public sealed class ASCFrequencyCreator
{
	private readonly ASCFrequenciesData _data;

	public ASCFrequencyCreator(in ASCFrequenciesData data)
	{
		_data = data;
		ASCUtils.Initialize();
	}

	public void Create(in IEnumerable<IProductBase<IBaseCharacteristicsParser>> products, int? totalCount = null)
	{
		totalCount ??= products.Count();
		int counter = 0;

		_data.Clear();

		Stopwatch stopwatch = new();
		stopwatch.Start();

		foreach(var product in products)
		{
			int currentCounter = System.Threading.Interlocked.Increment(ref counter);
			Log.Print($"{currentCounter}/{totalCount}");

			foreach (var storage in product.CharacteristicsParser.Characteristics.Storage)
			{
				if (storage.Key == ProductCharacteristics.Type.None || storage.Key == ProductCharacteristics.Type.Unknown) continue;

				var categoryFrequencyDict = _data.GetOrAdd(storage.Key, (_) => new()); // Потокобезопасное получение/создание

				foreach (var characteristic in storage.Value)
				{
					IEnumerable<string> words = ASCUtils.Lemmatize(characteristic.Key.ToLower()); // Используем кэшированный Lemmatize

					foreach (string word in words)
					{
						if (ASCUtils.IsNotValidWordInternal(word)) continue;
						categoryFrequencyDict.AddOrUpdate(word, new ASCharacteristicValue(), static (_, value) => value + 1); // Потокобезопасное обновление частот
					}
				}
			}
		}

		stopwatch.Stop();
		Log.Print($"Elapsed time (CreateFrequencies): {stopwatch.ElapsedMilliseconds} ms\nStarting normalization");

		stopwatch.Restart();
		Normalize();
		stopwatch.Stop();
		Log.Print($"Elapsed time (NormalizeFrequencies): {stopwatch.ElapsedMilliseconds} ms\nStarting IDF calculation");

		stopwatch.Restart();
		CalculateIDFValues();
		stopwatch.Stop();
		Log.Print($"Elapsed time (CalculateIDFValues): {stopwatch.ElapsedMilliseconds} ms\nFinished");

		Log.Print($"Total elapsed time: {stopwatch.ElapsedMilliseconds} ms");
	}

	public void Normalize()
	{
		Parallel.ForEach(_data, storage =>
		{
			double totalFrequency = storage.Value.Values.Count;

			Log.Print(totalFrequency);

			foreach (var wordEntry in storage.Value)
			{
				storage.Value[wordEntry.Key] = wordEntry.Value / totalFrequency;
			}
		});
	}

	public void CalculateIDFValues()
	{
		double totalCategories = ProductCharacteristics.TypeNames.Count - 1; // Общее количество категорий (без Unknown)
		Dictionary<string, ASCharacteristicValue> all = new();

		// 1. Собери все уникальные слова из всех категорий
		foreach (var categoryFrequencies in _data.Values)
		{
			foreach (var frequency in categoryFrequencies)
			{
				if (!all.ContainsKey(frequency.Key))
				{
					all.Add(frequency.Key, frequency.Value);
				}
			}
		}

		// 2. Для каждого слова вычисли IDF
		foreach (var pair in all)
		{
			double totalWeightInCategories = 0;
			foreach (var categoryFrequencies in _data.Values)
			{
				if (categoryFrequencies.TryGetValue(pair.Key, out var value))
				{
					totalWeightInCategories += value.Value;
				}
			}

			// Формула IDF (с логарифмом по основанию 10)
			pair.Value.IdfValue = Math.Log10(totalCategories / totalWeightInCategories);
		}

		Log.Print($"IDF values calculated for {all.Count} unique words.");
	}


}