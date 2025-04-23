using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Core.Extensions;
using Core.Components;
using UX.Components.Table;

namespace UX.Option.First.Services.Title;

public class ProductProcessor
{
	// Кэш для регулярных выражений
	private readonly Dictionary<string, Regex> _regexCache = new();

	public void ProcessProducts(List<ProductTableRow> products, List<string> titles)
	{
		if (titles == null || titles.Count == 0) return;

		// Предобработка: создаем нормализованный список слов
		var normalizedTitles = titles
				.Where(title => !string.IsNullOrWhiteSpace(title))
				.Select(title => title.Trim().ToLower())
				.Distinct()
				.ToList();

		foreach (var product in products)
		{
			if (string.IsNullOrWhiteSpace(product.Title))
				continue;

			string originalTitle = product.Title;
			string lowerProduct = originalTitle.ToLower();
			string? matchedTitle = null;

			// Проверяем все формы слова для слова
			foreach (var entry in normalizedTitles)
			{
				if (TryMatchWord(lowerProduct, entry, out matchedTitle))
					break;
			}

			if (matchedTitle != null)
			{
				// Заменяем найденную форму на нормальную форму и приводим остальную часть к нижнему регистру
				string replacement = titles.First(t => t.ToLower() == matchedTitle);
				string normalizedTitle = MoveAndNormalize(originalTitle, matchedTitle, titles[0] + ' ');
				product.Name = normalizedTitle.Trim();
			}
			else
			{
				// Если форма не найдена, добавляем ключевое слово в начало
				string defaultTitle = titles.FirstOrDefault() ?? string.Empty;
				if (!string.IsNullOrEmpty(defaultTitle))
				{
					product.Name = $"{defaultTitle} {originalTitle.ToLowerFirstChar()}";
				}
			}
		}
	}

	/// <summary>
	/// Пытается найти слово в строке с использованием регулярного выражения.
	/// </summary>
	private bool TryMatchWord(string input, string word, out string? matchedWord)
	{
		matchedWord = null;

		// Получаем регулярное выражение из кэша или создаем новое
		if (!_regexCache.TryGetValue(word, out Regex? regex))
		{
			regex = new Regex($@"\b{Regex.Escape(word)}\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
			_regexCache[word] = regex;
		}

		Match match = regex.Match(input);
		if (match.Success)
		{
			matchedWord = match.Value;
			return true;
		}

		return false;
	}

	/// <summary>
	/// Перемещает найденное слово в начало строки, удаляет его из середины и приводит остальную часть к нижнему регистру.
	/// </summary>
	private string MoveAndNormalize(string input, string wordToReplace, string replacement)
	{
		// Создаем регулярное выражение для замены
		Regex regex = _regexCache[wordToReplace];

		// Удаляем все вхождения слова из строки
		string result = regex.Replace(input, "").Trim();

		// Приводим оставшуюся часть строки к нижнему регистру
		result = result.ToLower();

		// Добавляем нормальную форму слова в начало
		return $"{replacement} {result}".Trim();
	}
}