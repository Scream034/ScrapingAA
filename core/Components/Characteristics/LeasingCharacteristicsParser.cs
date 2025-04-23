using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Playwright;

namespace Core.Components.Characteristics;

using System;
using Core.Extensions;
using Core.Log;

public class LeasingCharacteristicsParser : BaseCharacteristicsParser
{
	// Для Leasing-сайта
	public static class XPath
	{
		public const string Block = "//div[@class='l-catalog-card__chars-box']";
		public const string BlockTitle = "xpath=div[contains(@class, 'l-catalog-card__chars-caption')]/span";
		public const string Row = "//div[@class='l-catalog-card__chars-item']";
		public const string Name = "xpath=div[1]/span";
		public const string Value = "xpath=div[2]/span";
	}

	public async Task AssertParseAsync(IPage page)
	{
		IReadOnlyList<IElementHandle> blocks = await page.QuerySelectorAllAsync(XPath.Block);
		if (blocks.Count == 0)
		{
			Log.Warning("blocks (with characteristics) is empty");
			return;
		}

		await ParseAsync(blocks);
	}

	public async Task ParseAsync(IReadOnlyList<IElementHandle> blocks)
	{
		foreach (var block in blocks)
		{
			IElementHandle? elementBlockTitle = await block.QuerySelectorAsync(XPath.BlockTitle);
			if (elementBlockTitle != null)
			{
				string blockTitle = await elementBlockTitle.TextContentAsync() ?? "_";

				IReadOnlyList<IElementHandle> rows = await block.QuerySelectorAllAsync(XPath.Row);
				await ParseAsyncInternal(rows, blockTitle);
			}
			else
			{
				Log.Warning("Block title is null");
			}
		}
	}

	public async Task ParseAsyncInternal(IReadOnlyList<IElementHandle> rows, string blockTitle)
	{
		ProductCharacteristics.Type blockType = ProductCharacteristicsType.FromDisplayStringOrDefault(blockTitle, ProductCharacteristics.Type.Unknown);

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
				{
					item.Name = Characteristics.ContainsName(item.Name) ? $"{blockTitle} {item.Name.ToLowerFirstChar()}" : item.Name;

					if (!Characteristics.ContainsName(blockType, item.Name))
					{
						Characteristics.Add(blockType, item);
					}
					else
					{
						Log.Warning($"Already contains name: {item.Name} in {blockType}");
					}
				}
				else
				{
					Log.Warning($"Failed to parse characteristics item: {i}");
				}
			}
		}
	}
}