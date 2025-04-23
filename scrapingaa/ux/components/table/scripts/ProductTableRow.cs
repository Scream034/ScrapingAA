namespace UX.Components.Table;

using Godot;

using Core;
using Core.Log;
using Core.Components.Characteristics;
using UX.ProductOption;


public sealed partial class ProductTableRow : TableRow
{
	public static class Meta
	{
		public static class CharacteristicRow
		{
			public const string CanMove = "need";
			public const string LastColor = "lastColor";
			public const string OldKey = "oldKey";
			public const string CategoryType = "categoryType";
		}
	}

	public IProductBase<IBaseCharacteristicsParser> Product;

	public bool IsEmptyCharacteristics
	{
		get => Product.CharacteristicsParser.Characteristics.Storage.Count == 0;
		set
		{
			
			if (value)
			{
				Button button = new() { Name = "EmptyCharacteristicsButton", Text = "◬" };
				button.Pressed += OnEmptyCharacteristicsButtonPressed;
				button.Modulate = new Color(0, 0, 1, 1f);
				button.TooltipText = "Пустой список характеристик";
				AddChild(button);
			}
			else
			{
				GetNodeOrNull<Button>("EmptyCharacteristicsButton")?.QueueFree();
			}
		}
	}

	public bool IsEmptyImages
	{
		get => Product.SaveManager.ImagePaths.Count == 0;
		set
		{
			if (value)
			{
				Button button = new() { Name = "EmptyImagesButton", Text = "◬" };
				button.Pressed += OnEmptyImagesButtonPressed;
				button.Modulate = new Color(1, 1, 0, 1f);
				button.TooltipText = "Пустой список изображений";
				AddChild(button);
			}
			else
			{
				GetNodeOrNull<Button>("EmptyImagesButton")?.QueueFree();
			}
		}
	}

	public bool IsIncorrectTitle
	{
		get => Product.SaveManager.ImagePaths.Count == 0;
		set
		{
			if (value)
			{
				Button button = new() { Name = "IncorrectTitleButton", Text = "◬" };
				button.Pressed += OnIncorrectTitleButtonPressed;
				button.Modulate = new Color(0, 1, 1, 1f);
				button.TooltipText = "Некорректное название";
				AddChild(button);
			}
			else
			{
				GetNodeOrNull<Button>("IncorrectTitleButton")?.QueueFree();
			}
		}
	}


	public new string Name
	{
		get => GetName();
		set
		{
			if (Product.SaveManager.SetTitle(value) is string title)
			{
				TitleButton.Text = title;
				SetName(title);
			}
			else
			{
				Log.Error($"Failed to set title for product: {Product.SaveManager.DirectoryPath}");
			}
		}
	}

	public ProductTableRow(IProductBase<IBaseCharacteristicsParser> product) : base(product.Title!, product.URL!)
	{
		Product = product;

		Modulate = product.IsBroken ? new Color(1, 0, 0, 1f) : new Color(1, 1, 1, 1f);

		IsEmptyCharacteristics = Product.CharacteristicsParser.Characteristics.Storage.Count == 0;
		IsEmptyImages = Product.SaveManager.ImagePaths.Count == 0;
		IsIncorrectTitle = Title == null || Title.Contains(Constants.IncorrectTitleString);
	}

	public override void Init()
	{
		if (Product.IsBroken)
		{
			Log.Error($"Product is broken: {Product.SaveManager.DirectoryPath}");
			OS.Alert("Невозможно открыть сломанный продукт", "Ошибка");
			return;
		}

		var optionPanel = ProductOptionPanel.Create(this);
		GetTree().Root.AddChild(optionPanel);
	}

	private void OnEmptyCharacteristicsButtonPressed()
	{
		OS.Alert("Пустой список характеристик");
	}

	private void OnEmptyImagesButtonPressed()
	{
		OS.Alert("Пустой список изображений");
	}

	private void OnIncorrectTitleButtonPressed()
	{
		OS.Alert("Неверный заголовок");
	}
}