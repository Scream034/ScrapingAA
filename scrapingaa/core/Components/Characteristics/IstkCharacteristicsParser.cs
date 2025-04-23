using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Playwright;

namespace Core.Components.Characteristics;

using Core.Log;

public class IstkCharacteristicsParser : BaseCharacteristicsParser
{
	// Для ISTK-сайта
	public static class XPath
	{
		public const string Row = "//tr";
		public const string Name = "//td[1]";
		public const string Value = "//td[2]";
	}

	public async Task AssertParseAsync(IElementHandle? elementBlock)
	{
		if (elementBlock == null)
		{
			Log.Warning("elementBlock (with characteristics) is null");
			return;
		}

		await ParseAsync(elementBlock);
	}

	public async Task ParseAsync(IElementHandle elementBlock)
	{
		IReadOnlyList<IElementHandle> rows = await elementBlock.QuerySelectorAllAsync(XPath.Row);
		foreach (IElementHandle row in rows)
		{
			IReadOnlyList<IElementHandle> names = await row.QuerySelectorAllAsync(XPath.Name);
			IReadOnlyList<IElementHandle> values = await row.QuerySelectorAllAsync(XPath.Value);

			if (names.Count != values.Count)
			{
				Log.Error($"Names and values count not equal: {names.Count} != {values.Count}");
				continue;
			}

			for (int i = 0; i < names.Count; i++)
			{
				CharacteristicsItem? item = await CharacteristicsItem.Parse(names[i], values[i]);
				if (item != null)
					Characteristics.Add(item);
				else
					Log.Warning($"Failed to parse characteristics item: {i}");
			}
		}
	}
}