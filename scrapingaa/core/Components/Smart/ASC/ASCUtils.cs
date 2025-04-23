namespace Core.Components.Smart.ASC.Internal;

using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using Core.Components.MyStem;

public static class ASCUtils
{
	public static readonly Lazy<ASCStopWords> StopWords = new(static () => new(Constants.Path.File.ASCStopWords), true); // Список стоп-слов
	public static readonly Lazy<ASCWhiteWords> WhiteWords = new(static () => new(Constants.Path.File.ASCWhiteWords), true); // Список белых слов
	public static Lazy<FastMyStem> MyStem = new(static () => new(new() { PrintOnlyLemmasAndGrammemes = true }), true);
	private static readonly ConcurrentDictionary<string, IEnumerable<string>> _lemmaCache = new();

	public static void Initialize()
	{
		if (IsInitialized()) return;

		// Загружаем стоп-слова и белые слова асинхронно, для ускорения загрузки
		Task taskStopWords = Task.Run(StopWords.Value.LoadFromFile);
		Task taskWhiteWords = Task.Run(WhiteWords.Value.LoadFromFile);

		Task.WaitAll(taskStopWords, taskWhiteWords);
	}

	/// <summary>
	/// Проверяет, инициализированы ли стоп-слова и белые слова. В теории может быть ошибка, если стоп-слова или белые слова не загрузились, а другой уже загрузились.
	/// </summary>
	/// <returns></returns>
	public static bool IsInitialized() => StopWords.IsValueCreated && WhiteWords.IsValueCreated;

	public static IEnumerable<string> Lemmatize(string text)
	{
		// **Кэширование лемм**
		if (_lemmaCache.TryGetValue(text, out var cachedLemmas)) // Проверяем, есть ли результат в кэше
		{
			return cachedLemmas; // Возвращаем закэшированный результат, если есть
		}

		// Если в кэше нет, вызываем MyStem.Analysis() и добавляем результат в кэш
		IEnumerable<string> lemmas = MyStem.Value.MultiAnalysis(text)
			.Replace("?", string.Empty)
			.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
			.Select(word => word.Split('|').First());

		_lemmaCache.TryAdd(text, lemmas); // Добавляем результат в кэш
		return lemmas; // Возвращаем полученный результат
	}

	/// <summary>
	/// Для использования функции требуется инициализация: <c>ASCUtils.Initialize()</c>
	/// </summary>
	public static bool IsNotValidWordInternal(string word) => StopWords.Value.Contains(word) || (word.Length < ASCConstants.MinWordLength && !WhiteWords.Value.Contains(word));
}