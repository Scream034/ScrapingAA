namespace UX.ProductOption;

using Godot;
using Core;
using Core.Log;
using Core.Components.Characteristics;
using System.IO;
using UX.Components.Table;

public sealed partial class ProductOptionPanel : Control
{
	public const string ScenePath = "res://ux/product_option_panel.scn";

	[Export] public ProductOptionHeader Header = null!;
	[Export] public ProductOptionButtons Buttons = null!;
	[Export] public ProductOptionBody Body = null!;

	public IProductBase<IBaseCharacteristicsParser> Product = null!;
	public ProductTableRow Row = null!;

	public static ProductOptionPanel Create(ProductTableRow row)
	{
		var scene = ResourceLoader.Load(ScenePath) as PackedScene;
		if (scene == null)
		{
			throw new FileNotFoundException($"Scene path: {ScenePath}");
		}

		var optionPanel = scene.InstantiateOrNull<ProductOptionPanel>();
		if (optionPanel == null)
		{
			throw new InvalidDataException($"Scene path: {ScenePath}");
		}

		optionPanel.Row = row;
		optionPanel.Product = row.Product;

		return optionPanel;
	}

	public override void _EnterTree()
	{
		Header.Parent = this;
		Buttons.Parent = this;
		Body.Parent = this;
	}

	public void Close()
	{
		Log.Print($"Closed: {Product.Title}");
		QueueFree();
	}
}
