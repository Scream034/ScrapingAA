namespace Core.Components.Smart.AEXU;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Core.Extensions;

/// <summary>
/// Algorithm Extraction Unit Token
/// </summary>
public sealed class AEXUToken
{
	public static readonly char[] BlackList = ['^', '*', '\''];
	public static readonly char[] CharsToTrim = [' '];

	public List<string> Tokens { get; set; } = new List<string>();

	public AEXUToken(IEnumerable<string> tokens)
	{
		Tokens.AddRange(tokens);
	}

	public AEXUToken(in string text, in bool withoutDigit = false)
	{
		Main(text, withoutDigit: withoutDigit);
	}

	public void Main(string text, in bool withoutDigit = false)
	{
		text = text.TrimAroundDelimiters(BlackList, CharsToTrim).Replace(BlackList, char.MinValue).ReduceWhitespace();

		StringBuilder buffer = new(text);

		// Соединяем цифры и буквы
		int lastLetterIndex = -1;
		for (int j = 0; j < buffer.Length; j++)
		{
			char c = buffer[j];
			if (!char.IsWhiteSpace(c))
			{
				if (char.IsLetter(c) || char.IsPunctuation(c))
				{
					lastLetterIndex = j;
				}
				else if (char.IsDigit(c) && lastLetterIndex != j - 1 && lastLetterIndex != -1)
				{
					lastLetterIndex++;
					buffer[lastLetterIndex] = c;
					buffer.Remove(j, 1);
					j--;
				}
			}
		}

		// Разделяем по пробелу
		int start = 0;
		bool lastHasSpace = false;
		for (int j = 0; j < buffer.Length; j++)
		{
			char c = buffer[j];
			if (c == ' ')
			{
				if (!lastHasSpace)
				{
					Tokens.Add(buffer.ToString(start, j - start));
					start = j + 1;
					lastHasSpace = true;
				}
				else
				{
					start++;
				}
			}
		}

		if (buffer.Length == 0)
		{
			return;
		}

		if (buffer[0] == ' ')
		{
			buffer.Remove(0, 1);
			start -= 2;
		}
		if (buffer[buffer.Length - 1] == ' ')
		{
			buffer.Remove(buffer.Length - 1, 1);
			start -= 2;
		}

		Tokens.Add(buffer.ToString(start, buffer.Length - start));

		if (Tokens.Count > 0 && withoutDigit)
		{
			Tokens = Tokens.Select(static x => char.IsDigit(x[0]) && char.IsDigit(x[x.Length - 1]) ? "" : x).Where(x => !string.IsNullOrEmpty(x)).ToList();
		}
	}

	public void Clear()
	{
		Tokens.Clear();
	}

	public override string ToString()
	{
		return string.Join(" ", Tokens);
	}
}