using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Playwright;

namespace Core.Components.Characteristics;

using Core.Extensions;
using Core.Log;
using Godot;

public class SolyarkaCharacteristicsParser : BaseCharacteristicsParser
{
	public static class XPath
	{
		public const string Item = "xpath=/li";
		public const string BlockName = "xpath=/div/p";
		public const string Name = "xpath=/ul/li/p[1]";
		public const string Value = "xpath=/ul/li/p[2]";
	}

	public async Task ParseAsync(IElementHandle generalBlock)
	{
		IReadOnlyList<IElementHandle> blocks = await generalBlock.QuerySelectorAllAsync(XPath.Item);
		foreach (IElementHandle block in blocks)
		{
			IReadOnlyList<IElementHandle> names = await block.QuerySelectorAllAsync(XPath.Name);
			IReadOnlyList<IElementHandle> values = await block.QuerySelectorAllAsync(XPath.Value);
			IElementHandle? elementBlockName = await block.QuerySelectorAsync(XPath.BlockName);
			string blockName = elementBlockName != null ? (await elementBlockName.InnerTextAsync()).Trim() : string.Empty;

			if (names.Count != values.Count)
			{
				Log.Error($"Names and values count not equal: {names.Count} != {values.Count}");
				DisplayServer.Beep();
				OS.Alert($"Невозможно получить характеристики: {names.Count} != {values.Count}", "Ошибка!");
				continue;
			}

			for (int i = 0; i < names.Count; i++)
			{
				CharacteristicsItem? item = await CharacteristicsItem.Parse(names[i], values[i]);
				if (item != null)
				{
					if (Characteristics.ContainsName(item.Name))
					{
						Log.Warning($"Characteristics item with name {item.Name} already exists");
						item.Name += " " + blockName.ToLowerFirstChar();
						Log.Print($"New name: {item.Name}");
					}

					Characteristics.Add(item);
				}
				else
				{
					Log.Warning($"Failed to parse characteristics item: {i}");
				}
			}
		}
	}
}