using System.Collections.Generic;
using Core;
using Core.UI.Window;
using Godot;

using UX.Option.First.Windows.EditTitles;

namespace UX.Option.First.Windows;

public partial class EditTitlesWindow : BaseWindow
{
	[Export] public EditTitlesHandler Handler = null!;

	public override void _Ready()
	{
		// TODO: Добавить точку входа для редактирования названий
#if false
		List<IProductBase<IBaseCharacteristicsParser>> products = new();

		for (int i = 0; i < 1024; i++)
		{
			IProductBase<IBaseCharacteristicsParser> product = new();
			product.Title = $"Товар {i} крутой ага";

			products.Add(product);
		}

		Handler.Initialize(products);
#endif
	}

	public static EditTitlesWindow Create(string title = "Редактирование названий")
	{
		PackedScene scene = GD.Load<PackedScene>("res://ux/windows/edit_titles.tscn");
		var window = scene.Instantiate<EditTitlesWindow>();
		window.Title = title;
		return window;
	}
}