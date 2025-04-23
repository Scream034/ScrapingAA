namespace Core.Components.Characteristics.SpecAvto;

using Log;
using Components;
using Extensions;
using Sites.SpecAvto;

using System;
using System.Web;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.Playwright;

public class SpecAvtoCharacteristicsParser_Special(SpecAvtoCharacteristicsParser parent) : SpecAvtoCharacteristicsParser_Any(parent)
{
	public interface IXPath
	{
		string Title { get; }
		string Title2 { get; }
		string Block { get; }
		string Value(int i);
	}

	public class XPathInstance : IXPath
	{
		public virtual string Title => "//div[@class='specifications-text']//tr[1]/th";
		public virtual string Title2 => "//div[@class='specifications-text']//tbody/tr[1]/*";
		public virtual string Block => "//div[@class='specifications-text']//table/tbody/tr";
		public virtual string _Value => "xpath=./*[$]";
		public virtual string Value(int i) => _Value.Replace("$", (i + 2).ToString());
	}

	public virtual IXPath XPath => new XPathInstance();

	public class Options : IOptions
	{
		public AddedWord AddedWord { get; set; } = AddedWord.None;
		public CharacteristicOffset Offset { get; set; } = new();
		public SpecAvtoProduct Product { get; set; }

		public Options(SpecAvtoProduct product)
		{
			Product = product;
		}
	}

	public enum AddedWord
	{
		None,
		UnitsOfMeasurement
	}

	public string[] AddValueWords { get; set; } = [];

	protected static string[][] InternalSplits = {
		["<p>", "</p>"],
		["<br>", "<br/>"],
		["•"]
	};

	public override async Task<bool> ParseAsync(IPage page, IOptions? xOptions)
	{
		if (xOptions is not Options options) return false;

		return await ParseAsyncInternal(page, options);
	}

	/// <summary>
	/// Формирует характеристики через получение следующих под хар-ик, который обычно в той строке.
	/// </summary>
	/// <param name="blocks">Текущий списое хар-ик</param>
	/// <param name="index">Индекс текущей хар-ики</param>
	/// <param name="options">Настройки парсинга</param>
	/// <param name="generalItem">Основная характеристика</param>
	/// <returns>Список хар-ик</returns>
	public async Task<List<SpecAvtoCharacteristicItem>> FormatByMultipleRowsAsync(IReadOnlyList<IElementHandle> blocks, ushort index, Options options, SpecAvtoCharacteristicItem generalItem)
	{
		List<SpecAvtoCharacteristicItem> list = new();

		string mainWord = FormatMainWord(generalItem.Name);

		index++; // Пропускаем текущую хар-ику
		for (; index < blocks.Count; index++)
		{
			IElementHandle block = blocks[index];
			SpecAvtoCharacteristicItem item = await SpecAvtoCharacteristicItem.ParseAsync(block, options.Offset);

			if (!IsCharacteristicSubName(item.Name) || string.IsNullOrWhiteSpace(item.Value))
			{
				break; // Это не внутренний параметр характеристики
			}

			item.Name = FormatSubName(mainWord, item.Name);
			item.Value = FormatSubValue(item.Value);

			list.Add(item);
		}

		return list;
	}

	/// <summary>
	/// Формирует хар-ики из строки, если хар-ика в одной строке.
	/// </summary>
	/// <param name="generalItem">Основная характеристика</param>
	/// <returns>Список хар-ик</returns>
	public List<SpecAvtoCharacteristicItem> FormatBySingleRow(SpecAvtoCharacteristicItem generalItem)
	{
		List<SpecAvtoCharacteristicItem> items = new();

		string[]? xNames = null;
		string[]? xValues = null;

		// Делим предполагаемые хар-ики на имена и значения
		foreach (string[] separator in InternalSplits)
		{
			xNames ??= SplitBy(generalItem.Name, separator);
			xValues ??= SplitBy(generalItem.Value, separator);
			if (xNames != null && xValues != null) break;
			else if (xNames != null && xNames.Length > 0 && xValues == null) // На случай если разная запись
			{
				Log.Warning("xNames != null && xValues == null");
				xValues = FormatBySingleRowInternalSplit(generalItem.Value);
				if (xValues == null)
				{
					Log.Warning("xValues == null");
				}

				break;
			}
			else if (xNames == null && xValues != null && xValues.Length > 0) // На случай если разная запись
			{
				xNames = FormatBySingleRowInternalSplit(generalItem.Name);
				if (xNames == null)
				{
					Log.Warning("xNames == null && xValues != null");
				}

				break;
			}
		}

		xNames ??= [generalItem.Name];
		xValues ??= [generalItem.Value];

		// Без дополнительных характеристик
		if (xNames.Length == 1 || xValues.Length == 1)
		{
			generalItem.Name = xNames.Length > 1 ? string.Join(", ", xNames!).Replace(":,", string.Empty) : FormatName(generalItem.Name);

			if (AddValueWords.Length == xValues.Length)
			{
				generalItem.Value = string.Empty;
				for (ushort i = 0; i < xValues.Length; i++)
				{
					generalItem.Value += xValues[i] + ' ' + AddValueWords[i] + ", ";
				}
				generalItem.Value = generalItem.Value[..^2];
			}
			else
			{
				if (xValues.Length > 1)
				{
					generalItem.Value = xValues.Aggregate((a, b) => FormatSubValue(a) + ", " + FormatSubValueInternal(b)) + ' ' + AddValueWords.FirstOrDefault(string.Empty);
				}
				else
				{
					generalItem.Value = FormatSubValue(generalItem.Value);
				}
			}

			items.Add(generalItem);
		}
		else if (xNames.Length > 1 && xValues.Length > 0)
		{
			// С дополнительными характеристиками

			// Проверяем структуру записи с главным именем и под-именами
			if (xNames.Length - 1 == xValues.Length && xNames.Length > xValues.Length)
			{
				xValues = xValues.Prepend(string.Empty).ToArray();
			}
			else
			{
				Log.Warning($"xNames.Length ({xNames.Length}) - 1 == xValues.Length ({xValues.Length}) && xNames.Length ({xNames.Length}) > xValues.Length ({xValues.Length})");
				return items;
			}

			string mainWord = FormatMainWord(xNames.First());

			for (ushort i = 1; i < xNames.Length; i++)
			{
				items.Add(new SpecAvtoCharacteristicItem
				(
					FormatSubName(mainWord, xNames[i]),
					FormatSubValue(xValues[i])
				));
			}
		}

		return items;
	}

	public virtual async Task<Tuple<IElementHandle?, IElementHandle?>> GetTitlesAsync(IPage page)
	{
		IReadOnlyList<IElementHandle> suggestions = await page!.QuerySelectorAllAsync(XPath.Title);
		if (suggestions.Count == 0)
		{
			suggestions = await page!.QuerySelectorAllAsync(XPath.Title2);
		}

		IElementHandle? nameElement = null;
		IElementHandle? valueElement = null;

		for (int i = 0; i < suggestions.Count; i++)
		{
			IElementHandle suggestion = suggestions[i];

			string? text = (await suggestion.InnerTextAsync()).RemoveHtmlTags().RemoveControlChars().ReduceWhitespace().Trim().ToLower().Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.None).FirstOrDefault();
			if (string.IsNullOrWhiteSpace(text)) continue;

			if (nameElement == null && (text.StartsWith("на") || text.StartsWith("техн")) && (text.EndsWith("ия") || text.EndsWith("ие")))
			{
				nameElement = suggestion;
			}
			else if (valueElement == null && text.StartsWith("знач"))
			{
				valueElement = suggestion;
			}
			else if (nameElement != null && valueElement != null)
			{
				break;
			}
		}

		return new(nameElement, valueElement);
	}

	protected virtual async Task<bool> ParseAsyncInternal(IPage page, Options options)
	{
		IReadOnlyList<IElementHandle> blocks = await page!.QuerySelectorAllAsync(XPath.Block);
		if (blocks.Count == 0)
		{
			return false;
		}

		var (titleNameElement, titleValueElement) = await GetTitlesAsync(page);
		if (titleNameElement != null || titleValueElement != null)
		{
			options.Offset.General.Vertical++;
			if (titleNameElement != null && await titleNameElement.GetAttributeAsync("colspan") is string colspanName && ushort.TryParse(colspanName, out ushort colspanNameValue) && colspanNameValue > 1)
			{
				options.Offset.Name.Horizontal += colspanNameValue - 1;
				options.Offset.Value.Horizontal += colspanNameValue - 1;
			}
		}

		for (ushort i = (ushort)options.Offset.General.Vertical; i < blocks.Count; i++)
		{
			IElementHandle block = blocks[index: i];

			// Добавляем слово к значению (пм. "Двигатель: 2.0" -> "Двигатель: 2.0 л")
			if (options.AddedWord != AddedWord.None && await block.QuerySelectorAsync(XPath.Value(options.Offset.Name.Horizontal)) is IElementHandle elementValue)
			{
				string html = (await elementValue.InnerHTMLAsync()).RemoveControlChars();
				AddValueWords = FormatBySingleRowInternalSplit(html) ?? [html.DecodeHtml().ReduceWhitespace().Trim()];
			}

			SpecAvtoCharacteristicItem generalItem = await SpecAvtoCharacteristicItem.ParseAsync(block, options.Offset);

			List<SpecAvtoCharacteristicItem> items = [generalItem];

			// Если название пусто - не валидно
			if (string.IsNullOrWhiteSpace(generalItem.Name)) continue;
			else if (generalItem.Value != null && string.IsNullOrWhiteSpace(generalItem.Value))
			{
				// Если значение пустое, а название имеется
				try
				{
					items = await FormatByMultipleRowsAsync(blocks, i, options, generalItem);
					i += (ushort)items.Count; // Пропускаю уже обработанные элементы
				}
				catch (Exception ex)
				{
					Log.Error($"When formatting by multiple rows: {ex.Message}");
				}
			}
			else
			{
				// Иначе пробуем парсить только эту строку
				try
				{
					items = FormatBySingleRow(generalItem);
				}
				catch (Exception ex)
				{
					Log.Error($"When formatting by single row: {ex.Message}");
				}
			}

			Parent.Characteristics.Add(items);
		}

		return true;
	}

	protected string[]? FormatBySingleRowInternalSplit(string html)
	{
		string[]? strings = null;

		foreach (string[] separator in InternalSplits)
		{
			strings ??= SplitBy(html, separator);
			if (strings != null) break;
		}

		return strings;
	}

	protected static bool IsCharacteristicSubName(string name) => name.StartsWith('-') || name.StartsWith('—') || name.IsFirstCharLowercase();

	protected string FormatSubValueInternal(string subValue) => subValue.RemoveHtmlTags().ReduceWhitespace().Trim();

	protected string FormatSubValue(string subValue) => FormatSubValueInternal(subValue) + (string.IsNullOrEmpty(AddValueWords.FirstOrDefault(string.Empty)) ? "" : ' ' + AddValueWords.First());

	protected static string FormatMainWord(string mainWord) => StringUtils.TrimLeadingNumbers(StringUtils.RemoveEnderSymbols(mainWord.Trim().RemoveHtmlTags().Trim())).Trim();

	protected static string FormatSubName(string mainWord, string subName) => StringUtils.FormatSentences((mainWord + ' ' + FormatName(subName)).RemoveHtmlTags());

	protected static string FormatName(string name) => StringUtils.TrimLeadingNumbers(name.RemoveHtmlTags().ReduceWhitespace()).Trim();

	public static string[]? SplitBy(string text, string[] separators) => separators.Any(text.Contains) ? text.Split(separators, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries) : null;
}