using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Playwright;

namespace Core.Components.Characteristics.SpecAvto;

using Core.Extensions;
using Core.Log;

public class SpecAvtoCharacteristicsParser_NormalA(SpecAvtoCharacteristicsParser parent) : SpecAvtoCharacteristicsParser_Any(parent)
{
	public static class XPath
	{
		public const string Block = "//div[contains(@class, 'section-block-with-props')]";
		public const string Name = "//div[@class='label']";
		public const string Value = "//div[@class='value']";
		public const string BlockTitle = "//div[@class='section-sub-header']";
	}

	public override async Task<bool> ParseAsync(IPage page, IOptions? options = null)
	{
		IReadOnlyList<IElementHandle> blocks = await page.QuerySelectorAllAsync(XPath.Block);
		if (blocks.Count == 0)
		{
			return false;
		}

		foreach (IElementHandle block in blocks)
		{
			IReadOnlyList<IElementHandle> elementsName = await block.QuerySelectorAllAsync(XPath.Name);
			IReadOnlyList<IElementHandle> elementsValue = await block.QuerySelectorAllAsync(XPath.Value);
			IElementHandle? elementBlockTitle = await block.QuerySelectorAsync(XPath.BlockTitle);
			if (elementsName.Count != elementsValue.Count)
			{
				Log.Error($"Count elementsName ({elementsName.Count}) != count elementsValue ({elementsValue.Count}) {page.Url}");
				continue;
			};

			string blockName = elementBlockTitle != null ? (await elementBlockTitle.TextContentAsync() ?? "").Trim().ToLower() : string.Empty;

			for (int i = 0; i < elementsName.Count; i++)
			{
				CharacteristicsItem? item = await CharacteristicsItem.Parse(elementsName[i], elementsValue[i]);
				if (item != null)
				{
					if (Parent.Characteristics.ContainsName(item.Name))
					{
						Log.Warning($"Characteristics item with name {item.Name} already exists");
						item.Name += " " + blockName.ToLowerFirstChar();
						Log.Print($"New name: {item.Name}");
					}

					Parent.Characteristics.Add(item);
				}
				else
				{
					Log.Warning($"Failed to parse characteristics item: {i}");
				}
			}
		}

		return true;
	}
}