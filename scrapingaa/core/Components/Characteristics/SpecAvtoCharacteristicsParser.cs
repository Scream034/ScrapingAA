using System;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace Core.Components.Characteristics;

using System.Web;
using Core.Components.Characteristics.SpecAvto;
using Core.Extensions;
using Core.Log;
using Core.Components;

public class SpecAvtoCharacteristicsParser : BaseCharacteristicsParser
{
	private readonly SpecAvtoCharacteristicsParser_Any[] parsers;

	public static class XPath
	{
		public const string Block = "xpath=/tr";
		public const string BlockTitle = "//div[@class='section-sub-header']";
		public const string Name = "//div[@class='list-items']/div[@class='item']/div[1]";
		public const string Value = "//div[@class='list-items']/div[@class='item']/div[2]";
	}

	public SpecAvtoCharacteristicsParser()
	{
		parsers = [
			new SpecAvtoCharacteristicsParser_NormalA(this),
			new SpecAvtoCharacteristicsParser_Special(this),
			new SpecAvtoCharacteristicsParser_SpecialA(this)
		];
	}

	public async Task ParseAsync(IPage page, SpecAvtoCharacteristicsParser_Any.IOptions? options = null)
	{
		foreach (var parser in parsers)
		{
			if (await parser.ParseAsync(page, options))
			{
				return; // Парсинг завершен
			}
		}
	}

	[Obsolete]
	public async Task<int> GetAverageCountOfCharacteristicsAsync(IPage page)
	{
		foreach (var parser in parsers)
		{
			int count = await parser.GetAverageCountOfCharacteristicsAsync(page);
			if (count > 0)
			{
				return count; // Парсинг завершен
			}
		}

		return 0;
	}
}

public class SpecAvtoCharacteristicItem : CharacteristicsItem
{
	public static class XPath
	{
		public static string Name(int index) => $"xpath=./*[{index + 1}]";

		// +1 -> Первые идут названия хар-ик
		public static string Value(int index) => $"xpath=./*[{index + 2}]";
	}

	public SpecAvtoCharacteristicItem(string name, string value) : base(name, value) { }

	public static async Task<SpecAvtoCharacteristicItem> ParseAsync(IElementHandle block, CharacteristicOffset offset)
	{
		SpecAvtoCharacteristicItem item = new(string.Empty, string.Empty);

		try
		{
			IElementHandle? elementName = await block.QuerySelectorAsync(XPath.Name(offset.Name.Horizontal));
			IElementHandle? elementValue = await block.QuerySelectorAsync(XPath.Value(offset.Value.Horizontal));

			if (elementName != null)
			{
				item.Name = FormatName(await elementName.InnerHTMLAsync());
			}

			// Обработка elementValue
			if (elementValue != null)
			{
				item.Value = FormatValue(await elementValue.InnerHTMLAsync());
			}

			// Логирование ошибок
			if (elementName == null && elementValue == null)
			{
				Log.Warning($"Both elementName and elementValue are null for offset {offset}");
			}
			else if (elementValue == null)
			{
				Log.Warning($"elementValue is null for offset {offset} ({item.Name})");
			}
		}
		catch (Exception ex)
		{
			Log.Error($"When parsing spec avto characteristic item ({offset}): {ex.Message}");
		}

		return item;
	}

	public new static string FormatName(string? name) => name != null ? StringUtils.RemoveSubEnderSymbols(name.DecodeHtml().RemoveControlChars().Trim()).Trim() : string.Empty;

	public new static string FormatValue(string? value) => value != null ? Regex.Replace(value, @"(<br\s*/?>|<p>\s*</p>|<div>\s*</div>)\s*$", string.Empty, RegexOptions.IgnoreCase).DecodeHtml().RemoveControlChars().Trim() : string.Empty;
}