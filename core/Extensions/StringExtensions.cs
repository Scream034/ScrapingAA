using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Web;

namespace Core.Extensions;

public static class StringExtensions
{
	public static string Replace(this string input, char[] replaces, char replaceWith)
	{
		if (string.IsNullOrWhiteSpace(input))
		{
			return input;
		}

		StringBuilder buffer = new();
		foreach (char c in input)
		{
			if (replaces.Contains(c))
			{
				if (replaceWith != char.MinValue)
					buffer.Append(replaceWith);
			}
			else
			{
				buffer.Append(c);
			}
		}

		return buffer.ToString();
	}

	/// <summary>
	/// Разделяет строку на подстроки на основе заданного условия для каждого символа.
	/// </summary>
	/// <param name="input">Исходная строка для разделения.</param>
	/// <param name="condition">Условие, которое определяет точку разделения. Возвращает true, если символ является точкой разделения.</param>
	/// <returns>Коллекция строк, полученных в результате разделения.</returns>
	public static IEnumerable<string> SplitByCondition(this string input, Func<char, bool> condition)
	{
		if (string.IsNullOrWhiteSpace(input))
		{
			yield break; // Возвращаем пустую коллекцию для пустой или null строки
		}

		StringBuilder currentPart = new StringBuilder();
		foreach (char character in input)
		{
			if (condition(character))
			{
				// Условие выполнено - это точка разделения
				if (currentPart.Length > 0)
				{
					yield return currentPart.ToString(); // Возвращаем накопленную часть
					currentPart.Clear(); // Начинаем новую часть
				}
				// Если нужно включать символ, на котором произошло разделение, в отдельную подстроку,
				// можно добавить: yield return character.ToString();
			}
			else
			{
				currentPart.Append(character); // Добавляем символ к текущей части
			}
		}

		// Возвращаем последнюю часть, если она есть
		if (currentPart.Length > 0)
		{
			yield return currentPart.ToString();
		}
	}

	public static string Reverse(this string original)
	{
		StringBuilder buffer = new();

		for (int i = original.Length - 1; i >= 0; i--)
		{
			buffer.Append(original[i]);
		}

		return buffer.ToString();
	}

	public static string CapitalizeFirstWord(this string str)
	{
		if (string.IsNullOrEmpty(str))
			return str; // Возвращаем строку как есть, если она пуста или равна null

		// Разделяем строку на слова, используя пробел в качестве разделителя
		var words = str.Split(' ', 2); // Разделяем только на 2 части: первое слово и остальная часть

		// Приводим первое слово к заглавной букве
		words[0] = char.ToUpper(words[0][0]) + words[0].Substring(1);

		// Объединяем слова обратно в строку
		return string.Join(" ", words);
	}

	public static string ToLowerFirstChar(this string input)
	{
		if (string.IsNullOrEmpty(input))
			return input;

		return char.ToLower(input[0]) + input.Substring(1);
	}

	public static string RemoveControlChars(this string input)
	{
		StringBuilder buffer = new();

		foreach (char c in input)
		{
			if (!char.IsControl(c))
			{
				buffer.Append(c);
			}
		}

		return buffer.ToString();
	}

	/// <summary>
	/// Декодирует HTML строку. Заменяя HTML-сущности на их соответствующие символы.
	/// </summary>
	/// <param name="input">Исходная строка.</param>
	/// <returns>Декодированная строка.</returns>
	public static string DecodeHtml(this string input)
	{
		return HttpUtility.HtmlDecode(input);
	}

	/// <summary>
	/// Удаляет все HTML-теги и их содержимое из строки (без использования Regex).
	/// </summary>
	/// <param name="input">Исходная строка.</param>
	/// <returns>Строка без HTML-тегов и их содержимого.</returns>
	public static string RemoveHtmlTags(this string input)
	{
		if (string.IsNullOrEmpty(input))
		{
			return input; // Пустая строка - ничего не делаем
		}

		StringBuilder result = new StringBuilder();
		bool isInsideTag = false; // Флаг, показывающий, находимся ли мы внутри HTML-тега

		foreach (char currentChar in input)
		{
			switch (currentChar)
			{
				case '<':
					isInsideTag = true; // Встретили открывающий тег - переходим внутрь тега
					break;
				case '>':
					isInsideTag = false; // Встретили закрывающий тег - выходим из тега
					break;
				default:
					if (!isInsideTag) // Если мы НЕ внутри тега
					{
						result.Append(currentChar); // Добавляем символ к результату
					}
					break;
			}
		}

		return result.ToString();
	}

	/// <summary>
	/// Удаляет все строчные HTML-теги из строки.
	/// </summary>
	/// <param name="input">Исходная строка.</param>
	/// <param name="onlyStringElements">Удалять элементы: br, p, div?</param>
	/// <returns>Строка без строчных HTML-тегов.</returns>
	public static string RemoveHtmlTags(this string input, bool onlyStringElements)
	{
		if (string.IsNullOrEmpty(input)) return input;
		if (onlyStringElements)
			return Regex.Replace(input, RegexPattern.HTMLStringTags, string.Empty);
		else
			return Regex.Replace(input, RegexPattern.HTMLTags, string.Empty);
	}

	/// <summary>
	/// Находит все позиции указанного символа в строке.
	/// </summary>
	/// <param name="input">Исходная строка.</param>
	/// <param name="character">Искомый символ.</param>
	/// <returns>Список индексов, где встречается символ.</returns>
	public static List<int> FindAllPositions(this string input, char character)
	{
		if (string.IsNullOrEmpty(input))
			return new List<int>(); // Возвращаем пустой список, если строка null или пустая.

		var positions = new List<int>();
		for (int i = 0; i < input.Length; i++)
		{
			if (input[i] == character)
			{
				positions.Add(i);
			}
		}
		return positions;
	}

	public static string TrimAroundDelimiters(this string text, char[] delimiters, char[] charsToTrim)
	{
		if (string.IsNullOrEmpty(text))
		{
			return text; // Пустая строка - ничего не делаем
		}

		StringBuilder result = new (text.Trim(charsToTrim)); // Trim начала и конца строки от charsToTrim (пробелов и т.д.)

		for (int i = 0; i < result.Length; i++)
		{
			if (delimiters.Contains(result[i])) // Если текущий символ - разделитель
			{
				// Удаление символов charsToTrim слева от разделителя
				int leftIndex = i - 1;
				while (leftIndex >= 0 && charsToTrim.Contains(result[leftIndex]))
				{
					result.Remove(leftIndex, 1);
					i--; // Сдвигаем индекс разделителя влево, т.к. строка стала короче
					leftIndex--;
				}

				// Удаление символов charsToTrim справа от разделителя
				int rightIndex = i + 1;
				while (rightIndex < result.Length && charsToTrim.Contains(result[rightIndex]))
				{
					result.Remove(rightIndex, 1);
					rightIndex++;
				}

				// Если символы charsToTrim были удалены, обновляем индекс
				if (result.Length < text.Length)
				{
					i--;
					text = result.ToString();
				}
			}
		}

		return result.ToString();
	}

	public static string TrimEnd(this string input, params string[] trimStrings)
	{
		if (input == null) throw new ArgumentNullException(nameof(input));
		if (trimStrings == null || trimStrings.Length == 0) return input;

		// Пока строка заканчивается на одну из строк из trimStrings, обрезаем её
		while (true)
		{
			bool trimmed = false;
			foreach (var trimString in trimStrings)
			{
				if (string.IsNullOrEmpty(trimString)) continue;

				if (input.EndsWith(trimString))
				{
					input = input.Substring(0, input.Length - trimString.Length);
					trimmed = true;
					break; // Прерываем цикл foreach, чтобы проверить снова
				}
			}

			if (!trimmed) break; // Если ничего не обрезали, выходим из цикла
		}

		return input;
	}

	public static bool Contains(this string input, char[] chars, StringComparison comparison = StringComparison.Ordinal) => chars.Any(x => input.Contains(x, comparison));

	public static bool Contains(this string input, string[] strings, StringComparison comparison = StringComparison.Ordinal) => strings.Any(x => input.Contains(x, comparison));

	public static bool Contains(this string input, string[][] strings, StringComparison comparison = StringComparison.Ordinal) => strings.Any(s => input.Contains(s, comparison));

	public static bool IsFirstCharLowercase(this string input)
	{
		// Проверяем, что строка не пустая
		if (string.IsNullOrEmpty(input))
		{
			return false; // либо вы можете выбросить исключение
		}

		// Проверяем, является ли первый символ строчной буквой
		return char.IsLower(input[0]);
	}

	public static bool AreAllCharactersSame(this string input, bool ignoreCase = false)
	{
		if (string.IsNullOrEmpty(input))
		{
			return false;
		}

		char firstChar = ignoreCase ? char.ToLower(input[0]) : input[0];
		return input.All(c => ignoreCase ? char.ToLower(c) == firstChar : c == firstChar);
	}

	public static string ReduceWhitespace(this string value)
	{
		StringBuilder newString = new StringBuilder();
		bool previousIsWhitespace = false;
		for (int i = 0; i < value.Length; i++)
		{
			if (char.IsWhiteSpace(value[i]))
			{
				if (previousIsWhitespace)
				{
					continue;
				}

				previousIsWhitespace = true;
			}
			else
			{
				previousIsWhitespace = false;
			}

			newString.Append(value[i]);
		}

		return newString.ToString();
	}

	public static bool ContainsArithmeticOperations(this string input)
	{
		return Regex.IsMatch(input, RegexPattern.ArithmeticOperations);
	}

	public static string[] FindMatches(this string input, string regex, RegexOptions options = RegexOptions.None)
	{
		return Regex.Matches(input, regex, options).Select(match => match.Value.Trim()).ToArray();
	}

	public static string[] FindMatches(this string input, string regex, int group, RegexOptions options = RegexOptions.None)
	{
		return Regex.Matches(input, regex, options).Select(match => match.Groups[group].Value.Trim()).ToArray();
	}
}