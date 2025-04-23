using System.Threading.Tasks;
using Core.Extensions;
using Core.Components;
using Microsoft.Playwright;

namespace Core.Components.Characteristics;

public class CharacteristicsItem
{
	public string Name { get; set; }
	public string Value { get; set; }

	public CharacteristicsItem(string name, string value)
	{
		Name = name;
		Value = value;
	}

	public static async Task<CharacteristicsItem?> Parse(IElementHandle elementName, IElementHandle elementValue)
	{
		string name = FormatName(await elementName.InnerTextAsync());
		string value = FormatValue(await elementValue.InnerTextAsync());

		if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(value))
			return null;

		return new CharacteristicsItem(name, value);
	}

	public static string FormatName(string? name) => name != null ? StringUtils.RemoveSubEnderSymbols(name.RemoveControlChars().DecodeHtml().RemoveHtmlTags()).CapitalizeFirstWord() : string.Empty;

	public static string FormatValue(string? value) => value != null ? value.RemoveControlChars().DecodeHtml().RemoveHtmlTags() : string.Empty;
}