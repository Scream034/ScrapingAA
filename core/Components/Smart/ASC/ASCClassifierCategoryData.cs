namespace Core.Components.Smart.ASC.Internal;

using System.Collections.Generic;
using Core.Components.MyStem;
using Core.Components.Smart.AEXU;

public sealed record class ASCClassifierCategoryData
{
	public readonly string CharacteristicName;
	public readonly string CharacteristicValue;
	public readonly Dictionary<ProductCharacteristics.Type, double> Scores;
	public readonly Dictionary<ProductCharacteristics.Type, int> KnownWordCounts;
	public readonly Dictionary<ProductCharacteristics.Type, int> UnknownWordCounts;
	public readonly AEXUToken RawUnitsInName;
	public readonly AEXUToken UnitsInName;
	public readonly AEXUToken UnitsInValue;
	public readonly IEnumerable<string> WordsInName;

	public ASCClassifierCategoryData(in string CharacteristicName, in string CharacteristicValue, in Dictionary<ProductCharacteristics.Type, double> Scores, in Dictionary<ProductCharacteristics.Type, int> KnownWordCounts, in Dictionary<ProductCharacteristics.Type, int> UnknownWordCounts)
	{
		this.CharacteristicName = CharacteristicName;
		this.CharacteristicValue = CharacteristicValue;
		this.Scores = Scores;
		this.KnownWordCounts = KnownWordCounts;
		this.UnknownWordCounts = UnknownWordCounts;

		RawUnitsInName = new(CharacteristicName);
		UnitsInName = new(CharacteristicName);
		UnitsInValue = new(CharacteristicValue, true);
		WordsInName = ASCUtils.Lemmatize(CharacteristicName);
	}

	/// <summary>
	/// Проверяет, является ли категория известной. <b>Метод не учитывает проверки на существование категории</b>
	/// </summary>
	public bool IsKnownCategoryInternal(ProductCharacteristics.Type category)
	{
		return KnownWordCounts[category] > 0 && KnownWordCounts[category] >= UnknownWordCounts[category];
	}
}