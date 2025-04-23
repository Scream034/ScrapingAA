namespace UX.Option.Internal;

using System;
using System.Linq;
using System.Collections.Generic;
using Godot;
using Core;
using Core.Log;
using Core.Extensions;
using Core.Components;
using Core.Components.Characteristics;

public sealed partial class OptionPanelSearchLineEdit : LineEdit
{
	public const float MinSimilarity = 1f;
	public const float MaxSimilarity = 10f;

	[Export] public OptionPanel OptionPanel = null!;

	public override void _Ready()
	{
		Connect(LineEdit.SignalName.TextSubmitted, new(this, nameof(OnTextSubmitted)));
	}

	public IEnumerable<IProductBase<IBaseCharacteristicsParser>> Search(string text)
	{
		List<Tuple<float, IProductBase<IBaseCharacteristicsParser>>> products = new();

		string[] mainWords = text.FindMatches(RegexPattern.Words);

		foreach (var product in OptionPanel.TableView.BufferedProducts)
		{
			if (string.IsNullOrEmpty(product.Title) || string.IsNullOrEmpty(product.URL))
			{
				continue;
			}
			else if (product.Title == text)
			{
				products.Add(new(MaxSimilarity, product));
				continue;
			}

			string[] targetWords = product.Title!.FindMatches(RegexPattern.Words);
			float similarity = StringUtils.CalculateTotalSimilarity(mainWords, targetWords);
			if (similarity > MinSimilarity)
			{
				products.Add(new(Mathf.Min(similarity, MaxSimilarity), product));
			}
		}

		return products.OrderByDescending(x => x.Item1).Select(x => x.Item2);
	}

	private void OnTextSubmitted(string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			OptionPanel.UpdateTable();
			return;
		}

		Log.Print($"Search: {text}");

		var products = Search(text);
		OptionPanel.TableView.UpdateRows(products);
		OptionPanel.TableView.GenerateButtonPages();

		Log.Print($"Search result: {products.Count()}");
		OS.Alert($"Найдено {products.Count()} товаров", "Результат поиска");
	}
}