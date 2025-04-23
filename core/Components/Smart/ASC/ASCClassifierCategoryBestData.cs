namespace Core.Components.Smart.ASC.Internal;

using System;
using System.Collections.Generic;
using Core.Components.Smart.AEXU;

public sealed record class ASCClassifierCategoryBestData
{
	public readonly ProductCharacteristics.Type FirstCategory;
	public readonly ProductCharacteristics.Type SecondCategory;
	public readonly double FirstCategoryScore;
	public readonly double SecondCategoryScore;

	public ASCClassifierCategoryBestData(ref ASCClassifierCategoryData data, in AEXUnits aexUnits)
	{
		// Ищем лучшую категорию
		foreach (var currentCategory in ProductCharacteristics.TypeNames.Keys)
		{
			if (currentCategory == ProductCharacteristics.Type.None || currentCategory == ProductCharacteristics.Type.Unknown) continue;

			double currentScore = data.Scores[currentCategory];

			if (data.IsKnownCategoryInternal(currentCategory))
			{
				// Пробуем извлечь ед.изм.
				double scoreUnits = _GetScoreForUnits(aexUnits, currentCategory, data.RawUnitsInName);
				if (scoreUnits == 0)
				{
					scoreUnits = _GetScoreForUnits(aexUnits, currentCategory, data.UnitsInName) * ASCConstants.ScoreAEXUnitsDoubtfulCoefficient;
				}

				scoreUnits += _GetScoreForUnits(aexUnits, currentCategory, data.UnitsInValue);

				// Умножаем на вес ед.изм.
				currentScore += scoreUnits * ASCConstants.ScoreAEXUnitsCoefficient;

				if (currentScore > FirstCategoryScore)
				{
					SecondCategoryScore = FirstCategoryScore;
					SecondCategory = FirstCategory;
					FirstCategoryScore = currentScore;
					FirstCategory = currentCategory;
				}
				else if (currentScore > SecondCategoryScore)
				{
					SecondCategoryScore = currentScore;
					SecondCategory = currentCategory;
				}
			}
		}
	}

	private float _GetScoreForUnits(in AEXUnits aexUnits, ProductCharacteristics.Type type, AEXUToken token)
	{
		float score = 0;
		if (type != ProductCharacteristics.Type.Technical && type != ProductCharacteristics.Type.Performance)
		{
			return score;
		}

		// Получаем ед.изм. из токена
		List<Tuple<float, AEXUnit>> units = aexUnits.Extract(token);

		if (ASCConstants.AEXUnitTypeWeights.TryGetValue(type, out var weights))
		{
			foreach (Tuple<float, AEXUnit> unitTuple in units)
			{
				if (weights.TryGetValue(unitTuple.Item2.Result, out var unitWeight))
				{
					score += unitWeight * unitTuple.Item1;
				}
			}
		}
		return score;
	}
}