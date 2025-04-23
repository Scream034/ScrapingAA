namespace Core.Components.Characteristics;

using System.Collections.Generic;
using System.Linq;
using Core.Log;

public sealed class WebCharacteristics
{
	public const string DuplicateWord = "_";

	public ProductCharacteristics Storage = new();

	private string UpdateIfDuplicateName(string name)
	{
		while (ContainsName(name))
		{
			Log.Warning($"Duplicate characteristic name: {name}, adding {DuplicateWord} to it");
			name = DuplicateWord + name;
		}

		return name;
	}

	public void Add(string name, string value)
	{
		Storage.Add(ProductCharacteristics.Type.Unknown, UpdateIfDuplicateName(name), value);
	}

	public void Add(CharacteristicsItem item)
	{
		Storage.Add(ProductCharacteristics.Type.Unknown, UpdateIfDuplicateName(item.Name), item.Value);
	}

	public void Add(ProductCharacteristics.Type type, CharacteristicsItem item)
	{
		Storage.Add(type, UpdateIfDuplicateName(item.Name), item.Value);
	}

	public void Add(IEnumerable<CharacteristicsItem> items) => Storage.Add(ProductCharacteristics.Type.Unknown, items.ToDictionary(x => UpdateIfDuplicateName(x.Name), x => x.Value));

	public bool Remove(string name) => Storage.Remove(ProductCharacteristics.Type.Unknown, name);

	public bool Remove(CharacteristicsItem item) => Storage.Remove(ProductCharacteristics.Type.Unknown, item.Name);

	public bool ContainsName(string name) => Storage.Contains(ProductCharacteristics.Type.Unknown, name);

	public bool ContainsName(ProductCharacteristics.Type type, string name) => Storage.Contains(type, name);
}
