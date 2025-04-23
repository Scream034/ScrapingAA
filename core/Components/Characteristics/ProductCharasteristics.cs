namespace Core.Components;

using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

using Log;

public class ProductCharacteristics
{
	public enum Type
	{
		None,
		Unknown, // Неизвестно
		Equipment, // Оснащение
		Salon, // Салон
		Technical, // Технические характеристики
		Performance, // Эксплуатационные показатели
		General // Общая информация
	}

	public event Action<Type, string, string>? OnAdded;
	public event Action<Type, string>? OnRemoved;

	public static Dictionary<Type, string> TypeNames = new()
	{
		{ Type.Unknown, "Unknown" },
		{ Type.Equipment, "Equipment" },
		{ Type.Salon, "Salon" },
		{ Type.Technical, "Technical" },
		{ Type.Performance, "Performance" },
		{ Type.General, "General" }
	};

	public static Dictionary<Type, string> DisplayTypeNames = new()
	{
		{ Type.Unknown, "Неизвестные характеристики" },
		{ Type.Equipment, "Оснащение" },
		{ Type.Salon, "Салон" },
		{ Type.Technical, "Технические характеристики" },
		{ Type.Performance, "Эксплуатационные показатели" },
		{ Type.General, "Общая информация" }
	};

	public Dictionary<Type, Dictionary<string, string>> Dictionary = new();

	/// <summary>
	/// Количество характеристик (Внутри каждой категории в общей сумме)
	/// </summary>
	public int Count => Dictionary.Sum(x => x.Value.Count);

	// Константы для сепараторов
	private const string CategorySeparator = "\n\uE000"; // Разделитель между категориями
	private const string KeyValueSeparator = "\uE000"; // Разделитель между ключами и значениями
	private const string CategoryEntrySeparator = "\uE000\n"; // Разделитель строк в категории

	public void Add(Type type, string key, string value)
	{
		if (!Dictionary.TryGetValue(type, out Dictionary<string, string>? characteristics))
		{
			characteristics = new Dictionary<string, string>();
			Dictionary[type] = characteristics;
		}

		characteristics[key] = value;
		OnAdded?.Invoke(type, key, value);
	}

	public void Add(Type type, Dictionary<string, string> characteristics)
	{
		if (!Dictionary.TryGetValue(type, out Dictionary<string, string>? existingCharacteristics))
		{
			existingCharacteristics = new();
			Dictionary[type] = existingCharacteristics;
		}

		foreach ((string? key, string? value) in characteristics)
		{
			existingCharacteristics[key] = value;
			OnAdded?.Invoke(type, key, value);
		}
	}

	public bool Remove(Type type, string key)
	{
		if (Dictionary.TryGetValue(type, out Dictionary<string, string>? characteristics) && characteristics.Remove(key))
		{
			if (characteristics.Count == 0)
			{
				Dictionary.Remove(type);
			}

			OnRemoved?.Invoke(type, key);

			return true;
		}
		return false;
	}

	public bool RemoveKey(string key)
	{
		return Dictionary.Any(category => category.Value.Remove(key));
	}

	public bool Contains(Type type, string key)
	{
		if (Dictionary.TryGetValue(type, out Dictionary<string, string>? characteristics))
		{
			return characteristics.ContainsKey(key);
		}
		return false;
	}

	public string? Get(Type type, string key)
	{
		if (Dictionary.TryGetValue(type, out Dictionary<string, string>? characteristics))
		{
			characteristics.TryGetValue(key, out var value);
			return value;
		}
		return null;
	}

	public Dictionary<string, string> Get(Type type)
	{
		return Dictionary.TryGetValue(type, out Dictionary<string, string>? characteristics) ? characteristics : new();
	}

	public int GetCount(Type type)
	{
		return Get(type).Count;
	}

	public ProductCharacteristics Clone()
	{
		var result = new ProductCharacteristics();
		foreach (var category in Dictionary)
		{
			result.Add(category.Key, category.Value.ToDictionary());
		}
		return result;
	}

	public new string ToString()
	{
		return string.Join("\n", Dictionary.Select(static x => $"{x.Key}\n{string.Join("\n", x.Value.Select(static y => $"{y.Key}: {y.Value}"))}"));
	}

	public string ToIOString()
	{
		return string.Join(CategorySeparator, Dictionary.Select(static category =>
			$"{category.Key}{CategoryEntrySeparator}" +
			string.Join(CategoryEntrySeparator, category.Value.Select(static pair => $"  {pair.Key}{KeyValueSeparator}{pair.Value}"))));
	}

	public void FromIOString(string input)
	{
		string[] categories = input.Split(new[] { CategorySeparator }, StringSplitOptions.RemoveEmptyEntries);
		try
		{
			foreach (var categoryBlock in categories)
			{
				string[] lines = categoryBlock.Split(new[] { CategoryEntrySeparator }, StringSplitOptions.RemoveEmptyEntries);
				if (lines.IsEmpty()) continue;

				string category = lines[0].Trim();
				Type typeKey = TypeNames.FirstOrDefault(x => x.Value == category).Key;
				if (typeKey == Type.None)
				{
					// Если не найден новый способ сохранения, то используем старый
					typeKey = DisplayTypeNames.FirstOrDefault(x => x.Value == category).Key;
					if (typeKey == Type.None)
					{
						Log.Error($"Unknown category: {category}");
						continue;
					};
				}

				for (int i = 1; i < lines.Length; i++)
				{
					string[] keyValue = lines[i].Split(new[] { KeyValueSeparator }, 2, StringSplitOptions.None);
					string key = keyValue[0].Trim();
					string value = keyValue[1].Trim();
					Add(typeKey, key, value);
				}
			}
		}
		catch (Exception ex)
		{
			Log.Error(ex, $"\nFailed to parse IO string: {input}");
		}
	}

	public IEnumerator<KeyValuePair<Type, Dictionary<string, string>>> GetEnumerator() => Dictionary.GetEnumerator();

	public string? this[Type category, string key]
	{
		get => Get(category, key);

		set
		{
			if (value != null) Add(category, key, value);
		}
	}
}


public static class ProductCharacteristicsType
{
	public static ProductCharacteristics.Type? FromStringOrNull(this string input)
	{
		// Ищем в типах по умолчанию
		var typeName = ProductCharacteristics.TypeNames.FirstOrDefault(x => x.Value == input, new(ProductCharacteristics.Type.None, "")).Key;
		if (typeName != ProductCharacteristics.Type.None) return typeName;

		// Ищем в типах для отображения
		typeName = ProductCharacteristics.DisplayTypeNames.FirstOrDefault(x => x.Value == input).Key;
		if (typeName == ProductCharacteristics.Type.None) return null;

		return typeName;
	}

	public static ProductCharacteristics.Type FromString(this string input)
	{
		// Ищем в типах по умолчанию
		var typeName = ProductCharacteristics.TypeNames.FirstOrDefault(x => x.Value == input, new(ProductCharacteristics.Type.None, "")).Key;
		if (typeName != ProductCharacteristics.Type.None) return typeName;

		// Ищем в типах для отображения
		typeName = ProductCharacteristics.DisplayTypeNames.FirstOrDefault(x => x.Value == input).Key;
		if (typeName == ProductCharacteristics.Type.None) throw new ArgumentException($"Unknown type: {input}");

		return typeName;
	}

	public static ProductCharacteristics.Type FromDisplayString(this string input)
	{
		return ProductCharacteristics.DisplayTypeNames.First(x => x.Value == input).Key;
	}

	public static ProductCharacteristics.Type FromDisplayStringOrDefault(this string input, ProductCharacteristics.Type defaultValue = ProductCharacteristics.Type.None)
	{
		return ProductCharacteristics.DisplayTypeNames.FirstOrDefault(x => x.Value == input, new(defaultValue, "")).Key;
	}

	public static string ToDisplayString(this ProductCharacteristics.Type type)
	{
		return ProductCharacteristics.DisplayTypeNames[type];
	}
}