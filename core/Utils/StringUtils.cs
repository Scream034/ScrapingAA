namespace Core.Components;

using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Log;
using Extensions;

public static partial class StringUtils
{
	private static string FormatSentences_ProcessParentheses(string input)
	{
		MatchCollection matches = Regex.Matches(input, RegexPattern.InPerhaps);
		foreach (Match match in matches)
		{
			// Форматируем содержимое в скобках рекурсивно
			string innerContent = match.Value.Trim('(', ')');
			string formattedContent = FormatSentences(innerContent);
			input = input.Replace(match.Value, $"({formattedContent})");
		}
		return input;
	}

	public static string FormatSentences(string input)
	{
		// Удаляем лишние пробелы в конце строки
		input = input.Trim();

		// Обрабатываем строки в скобках рекурсивно
		input = FormatSentences_ProcessParentheses(input);

		string[] sentences = Regex.Split(input, RegexPattern.FinalPunctuationMarks, RegexOptions.None);
		string result = string.Empty;

		for (int i = 0; i < sentences.Length; i++)
		{
			if (string.IsNullOrWhiteSpace(sentences[i]))
				continue;

			if (i % 2 == 0)
			{
				string sentence = sentences[i];

				if (sentence.Length > 0)
				{
					string[] words = sentence.FindMatches(RegexPattern.Words);

					string? firstWord = words.FirstOrDefault(x => !x.ContainsArithmeticOperations() && !IsStringInBrackets(sentence, x));
					if (string.IsNullOrEmpty(firstWord))
					{
						sentence = Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(sentence);
					}
					else
					{
						sentence = sentence.Replace(firstWord, Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(firstWord.ToLower()));
					}

					foreach (string word in words.Where(x => x != firstWord && !x.ContainsArithmeticOperations() && !x.AreAllCharactersSame(true) && !IsStringInBrackets(sentence, x)))
					{
						sentence = sentence.Replace(word, word.ToLower());
					}
				}

				result += sentence;
				if (i + 1 < sentences.Length)
				{
					result += sentences[i + 1]; // Добавляем знак препинания
				}
			}
		}


		return result.Trim(); // Очищаем результат от лишних пробелов
	}

	public static bool IsStringInBrackets(string input, string target)
	{
		// Регулярное выражение для поиска строки в скобках
		string pattern = $@"\(([^()]*)\)"; // Находим содержимое в круглых скобках

		MatchCollection matches = Regex.Matches(input, pattern);

		foreach (Match match in matches)
		{
			// Проверяем, содержится ли целевая строка в содержимом скобок
			if (match.Groups[1].Value.Contains(target, StringComparison.OrdinalIgnoreCase))
			{
				return true; // Найдено
			}
		}

		return false; // Не найдено
	}

	/// <summary>
	/// Убирает лишние символы и убирает двоеточие на конце, если оно имеется
	/// </summary>
	public static string RemoveEnderSymbols(string text) => Regex.Replace(text, RegexPattern.EnderSymbols, string.Empty, RegexOptions.IgnoreCase);

	public static string RemoveSubEnderSymbols(string text) => Regex.Replace(text, RegexPattern.SubEnderSymbols, string.Empty, RegexOptions.IgnoreCase);

	/// <summary>
	/// Метод для нахождения самой длиной общей подстроки
	/// </summary>
	public static string LongestCommonSubstring(string str1, string str2)
	{
		int[,] lcsuff = new int[str1.Length + 1, str2.Length + 1];
		int length = 0; // длина максимальной общей подстроки
		int endIndex = 0; // индекс конца общей подстроки в str1

		for (int i = 1; i <= str1.Length; i++)
		{
			for (int j = 1; j <= str2.Length; j++)
			{
				if (str1[i - 1] == str2[j - 1])
				{
					lcsuff[i, j] = lcsuff[i - 1, j - 1] + 1;

					if (lcsuff[i, j] > length)
					{
						length = lcsuff[i, j];
						endIndex = i; // индекс конца
					}
				}
			}
		}

		// возвращаем конечную подстроку
		return str1.Substring(endIndex - length, length);
	}

	private static void LevenshteinSimilarity_NormalizeSentence(ref string input)
	{
		StringBuilder result = new (input.Length);
		foreach (var c in input.ToLower())
		{
			if (char.IsLetter(c) || char.IsDigit(c))
			{
				result.Append(c);
			}
		}

		input = result.ToString();
	}

	public static float LevenshteinSimilarity(string str1, string str2)
	{
		if (string.IsNullOrEmpty(str1))
		{
			return string.IsNullOrEmpty(str2) ? 1 : 0;
		}
		else if (string.IsNullOrEmpty(str2))
		{
			return 0;
		}

		// Оставляем только
		LevenshteinSimilarity_NormalizeSentence(ref str1);
		LevenshteinSimilarity_NormalizeSentence(ref str2);

		if (str1 == str2)
		{
			return 1;
		}
		else if (str1.Length == 0 || str2.Length == 0)
		{
			return 0;
		}

		int[,] distanceMatrix = new int[str1.Length + 1, str2.Length + 1];

		// Инициализация матрицы
		for (int i = 0; i <= str1.Length; i++)
		{
			distanceMatrix[i, 0] = i;
		}
		for (int j = 0; j <= str2.Length; j++)
		{
			distanceMatrix[0, j] = j;
		}

		// Заполнение матрицы расстояний
		for (int i = 1; i <= str1.Length; i++)
		{
			for (int j = 1; j <= str2.Length; j++)
			{
				int cost = (str1[i - 1] == str2[j - 1]) ? 0 : 1; // Цена замены
				distanceMatrix[i, j] = Math.Min(
				Math.Min(
					distanceMatrix[i - 1, j] + 1,       // Удаление
					distanceMatrix[i, j - 1] + 1),      // Вставка
					distanceMatrix[i - 1, j - 1] + cost // Замена или совпадение
					);
			}
		}

		// Расстояние Левенштейна находится в правом нижнем углу матрицы
		int levenshteinDistance = distanceMatrix[str1.Length, str2.Length];

		// Нормализация для получения оценки схожести от 0 до 1
		int maxLength = Math.Max(str1.Length, str2.Length);
		if (maxLength == 0)
		{
			return 1; // Обе строки пустые, считаем их идентичными
		}
		return 1 - (float)levenshteinDistance / maxLength;
	}

	public static float GetDamerauLevenshteinSimilarity(string s1, string s2)
	{
		if (string.IsNullOrEmpty(s1))
		{
			return string.IsNullOrEmpty(s2) ? 1f : 0f; // Обе пустые - схожесть 1, только s1 пустая - схожесть 0
		}
		if (string.IsNullOrEmpty(s2))
		{
			return 0f; // Только s2 пустая - схожесть 0
		}

		int length1 = s1.Length;
		int length2 = s2.Length;

		// Создаем матрицу расстояний
		int[,] d = new int[length1 + 1, length2 + 1];

		// Инициализация первой строки и первого столбца
		for (int i = 0; i <= length1; i++)
		{
			d[i, 0] = i;
		}
		for (int j = 0; j <= length2; j++)
		{
			d[0, j] = j;
		}

		// Использование словаря для хранения последнего появления символа в строке 1
		Dictionary<char, int> charMap = new Dictionary<char, int>();

		// Заполнение матрицы
		for (int i = 1; i <= length1; i++)
		{
			for (int j = 1; j <= length2; j++)
			{
				int cost = (s1[i - 1] == s2[j - 1]) ? 0 : 1;
				int insertion = d[i, j - 1] + 1;
				int deletion = d[i - 1, j] + 1;
				int substitution = d[i - 1, j - 1] + cost;
				int transposition = int.MaxValue;

				// Проверка транспозиции
				if (i > 1 && j > 1 && s1[i - 1] == s2[j - 2] && s1[i - 2] == s2[j - 1])
				{
					transposition = d[i - 2, j - 2] + cost; // cost здесь всегда 1 или 0, как для substitution
				}

				d[i, j] = Math.Min(Math.Min(insertion, deletion), Math.Min(substitution, transposition));
			}
			charMap[s1[i - 1]] = i - 1; // Обновляем последнее появление символа из s1
		}

		int damerauLevenshteinDistance = d[length1, length2];

		// Нормализация расстояния для получения схожести
		int maxLength = Math.Max(length1, length2);
		if (maxLength == 0)
		{
			return 1f; // Обе строки пустые, схожесть 1
		}
		return 1f - (float)damerauLevenshteinDistance / maxLength; // Возвращаем нормализованную схожесть
	}

	public static float CalculateTotalSimilarity(string[] mainWords, string[] targetWords)
	{
		float totalSimilarity = 0;

		foreach (string mainWord in mainWords)
		{
			float currentMaxSimilarity = 0;

			foreach (string targetWord in targetWords)
			{
				float similarity = LevenshteinSimilarity(mainWord, targetWord);
				if (similarity > currentMaxSimilarity)
				{
					currentMaxSimilarity = similarity;
				}
			}

			totalSimilarity += currentMaxSimilarity;
		}

		return totalSimilarity;
	}

	/// <summary>
	/// Вычисляет максимальную схожесть между двумя строками разбивая строки на слова
	/// <b>Рекомендую использовать в технических названиях.</b>
	/// </summary>
	public static float CalculateMaxSimilarityByWords(string[] mainWords, string[] targetWords)
	{
		float maxSimilarity = 0;

		foreach (string mainWord in mainWords)
		{
			if (mainWord.Length > 20)
			{
				Log.Warning($"mainWord is too long: {mainWord}");
			}

			float currentMaxSimilarity = 0;

			foreach (string targetWord in targetWords)
			{
				if (targetWord.Length > 20)
				{
					Log.Warning($"targetWord is too long: {targetWord}");
				}

				float similarity = LevenshteinSimilarity(mainWord, targetWord);
				if (similarity > currentMaxSimilarity)
				{
					currentMaxSimilarity = similarity;
				}
			}

			if (currentMaxSimilarity > maxSimilarity)
			{
				maxSimilarity = currentMaxSimilarity;
			}
		}

		return maxSimilarity;
	}

	public static float CalculateMaxSimilarityByWords(string str1, string str2)
	{
		string[] mainWords = str1.FindMatches(RegexPattern.Words);
		string[] targetWords = str2.FindMatches(RegexPattern.Words);

		if (!mainWords.Any() || !targetWords.Any())
		{
			return 0;
		}

		return CalculateMaxSimilarityByWords(mainWords, targetWords);
	}


	public static List<string> GetWithoutWhiteSpace(string text)
	{
		List<string> words = new List<string>();
		StringBuilder buffer = new StringBuilder();

		for (int i = 0; i < text.Length; i++)
		{
			char c = text[i];

			if (!char.IsWhiteSpace(c))
			{
				buffer.Append(c);
			}
			else if (buffer.Length > 0)
			{
				words.Add(buffer.ToString());
				buffer.Clear();
			}
		}

		// Добавляем последнее слово, если есть в буфере после цикла
		if (buffer.Length > 0)
		{
			words.Add(buffer.ToString());
		}

		return words;
	}

	/// <summary>
	/// Удаляет: "1. ", "1) ", "100.200.300." - из строки также убирает пробелы в начале строки.
	/// </summary>
	/// <param name="text"></param>
	/// <param name="removeDotAfterNumber"></param>
	/// <returns></returns>
	public static string TrimLeadingNumbers(string text, bool removeDotAfterNumber = true) // Переименовал функцию снова
	{
		if (string.IsNullOrEmpty(text))
		{
			return text;
		}

		int startIndex = 0; // Индекс начала "полезной" части строки (после номера строки)
		int i = 0;

		// Пропускаем пробелы в начале строки
		while (i < text.Length && char.IsWhiteSpace(text[i]))
		{
			i++;
		}

		if (i == text.Length) // Если строка состоит только из пробелов
		{
			return text.TrimStart();
		}

		int numberEndIndex = -1; // Индекс конца номера строки (последней цифры)
		int dotIndex = -1;      // Индекс точки после цифры (если есть)

		int j = i;
		while (j < text.Length)
		{
			if (char.IsDigit(text[j]))
			{
				numberEndIndex = j;
				j++;
			}
			else if (removeDotAfterNumber && text[j] == '.')
			{
				dotIndex = j;
				break;
			}
			else if (char.IsWhiteSpace(text[j]))
			{
				startIndex = j;
				break;
			}
			else
			{
				startIndex = j;
				break;
			}
		}

		if (startIndex > 0) // Если startIndex > 0, значит, номер строки был найден и startIndex УЖЕ указывает на начало "полезной" части строки
		{
			return text.Substring(startIndex).TrimStart(); // *** Убрали "+ 1" из Substring и добавили TrimStart() для удаления возможных пробелов в начале "полезной" части ***
		}
		else if (dotIndex > 0 && removeDotAfterNumber) // Если точка найдена, но пробел не найден
		{
			return text.Substring(dotIndex + 1).TrimStart(); // *** Оставили "+ 1" для точки, но добавили TrimStart() ***
		}
		else // Если дошли до конца цикла, но не нашли ни пробела, ни точки (или точку не нужно удалять), и numberEndIndex != -1
		{
			if (numberEndIndex != -1)
				return text.Substring(numberEndIndex + 1).TrimStart(); // *** Оставили "+ 1" для numberEndIndex, но добавили TrimStart() ***
			else
				return text;
		}
	}
}