namespace Core.Components.Smart.ASC;

using Core.Log;
using Core.Components.Smart.AEXU;
using Internal;

public sealed class ASCClassifier
{
	public readonly AEXUnits AEXU;
	public readonly ASCFrequenciesData Frequencies;

	private bool _isInitialized = false;

	public ASCClassifier(AEXUnits aexu, ASCFrequenciesData frequencies)
	{
		AEXU = aexu;
		Frequencies = frequencies;
	}

	public void Initialize()
	{
		if (_isInitialized) return;

		AEXU.LoadUnits();
		Frequencies.LoadFromFile();
		ASCUtils.Initialize();
		_isInitialized = true;
	}

	public bool IsInitialized() => _isInitialized;

	public ProductCharacteristics.Type Classify(in string characteristicName, in string characteristicValue)
	{
		if (!IsInitialized())
		{
			Log.Warning("AEX is not initialized");
			Initialize();
		}

		ASCClassifierCategoryData data = new(characteristicName, characteristicValue, new(), new(), new());

		return Classify(data);
	}

	public ProductCharacteristics.Type Classify(ASCClassifierCategoryData data)
	{
		// Вычисляем TF-IDF для каждой категории
		CalculateScores(ref data);

		// Ищем лучшую категорию
		ASCClassifierCategoryBestData bestData = new(ref data, AEXU);

		double ambiguityThreshold = bestData.SecondCategoryScore * bestData.FirstCategoryScore / ASCConstants.ScoreDifferentFactor;

		if (bestData.FirstCategoryScore > 0)
		{
			// Log.Print($"Best category: {bestData.FirstCategory} with score {bestData.FirstCategoryScore:F4}\nSecond best category: {bestData.SecondCategory} with score {bestData.SecondCategoryScore:F4}"); // Форматированный вывод score
			double ambiguityScore = bestData.FirstCategoryScore - bestData.SecondCategoryScore;

			// Log.Print($"Ambiguity score: {ambiguityScore:F4} ({ambiguityThreshold})");
			if (ambiguityScore >= ambiguityThreshold)
			{
				return bestData.FirstCategory;
			}
			else
			{
				return ProductCharacteristics.Type.Unknown;
			}
		}
		else
		{
			return ProductCharacteristics.Type.Unknown;
		}
	}

	private void CalculateScores(ref ASCClassifierCategoryData data)
	{
		foreach (var categoryType in ProductCharacteristics.TypeNames.Keys)
		{
			if (categoryType == ProductCharacteristics.Type.None || categoryType == ProductCharacteristics.Type.Unknown) continue;

			double categoryScore = 0;
			int knownWordCount = 0;
			int unknownWordCount = 0;

			// Вычисляем вероятность слов для категории
			if (Frequencies.TryGetValue(categoryType, out var categoryFrequencyDict))
			{
				foreach (string word in data.WordsInName)
				{
					if (ASCUtils.IsNotValidWordInternal(word)) continue;

					if (categoryFrequencyDict.TryGetValue(word, out var value))
					{
						// **Используем TF-IDF: умножаем частоту слова на его IDF**
						categoryScore += value.Value * value.IdfValue; // Умножаем TF на IDF и суммируем
																													 // Log.Print($"Word ({categoryType}): {word}\n\tTF: {value.Value}\n\tIDF: {value.IdfValue}\n\tNew score: {categoryScore}");

						knownWordCount++;
					}
					else
					{
						unknownWordCount++;
					}
				}
			}

			data.Scores[categoryType] = categoryScore;
			data.KnownWordCounts[categoryType] = knownWordCount;
			data.UnknownWordCounts[categoryType] = unknownWordCount;
		}
	}
}