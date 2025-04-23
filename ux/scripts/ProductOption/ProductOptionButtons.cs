namespace UX.ProductOption;

using Godot;
using Core.Log;
using Internal;
using Core.Components;

public sealed partial class ProductOptionButtons : ScrollContainer
{
	[Export] public BaseButton CloseButton = null!;
	[Export] public BaseButton RestoreButton = null!;
	[Export] public HBoxContainer ButtonsContainer = null!;
	[Export] public Label UnknownCharacteristicsLabel = null!;
	[Export] public Label AllCharacteristicsLabel = null!;

	public ProductOptionPanel Parent = null!;

	public override void _EnterTree()
	{
		CloseButton.Pressed += OnCloseButtonPressed;

		if (Parent.Product.SaveManager.CanRestore())
		{
			RestoreButton.Pressed += OnRestoreButtonPressed;
		}

		Parent.Product.CharacteristicsParser.Characteristics.Storage.OnAdded += OnCharacteristicAdded;
		Parent.Product.CharacteristicsParser.Characteristics.Storage.OnRemoved += OnCharacteristicRemoved;

		UnknownCharacteristicsLabel.Text = Parent.Product.CharacteristicsParser.Characteristics.Storage.GetCount(ProductCharacteristics.Type.Unknown).ToString();
		AllCharacteristicsLabel.Text = Parent.Product.CharacteristicsParser.Characteristics.Storage.Count.ToString();
	}

	private void OnCloseButtonPressed()
	{
		Parent.Close();
	}

	private async void OnRestoreButtonPressed()
	{
		if (await Parent.Product.SaveManager.RestoreAsync())
		{
			if (Parent.Body.CurrentOptionType is ProductCharacteristicsBody body)
			{
				body.UpdateCategories();
			}
			else
			{
				Log.Warning($"Undefined type: {Parent.Body.CurrentOptionType}");
			}
		}
	}

	private void OnCharacteristicAdded(ProductCharacteristics.Type type, string name, string value)
	{
		if (type == ProductCharacteristics.Type.Unknown)
		{
			UnknownCharacteristicsLabel.Text = (UnknownCharacteristicsLabel.Text.ToInt() + 1).ToString();
		}

		AllCharacteristicsLabel.Text = (AllCharacteristicsLabel.Text.ToInt() + 1).ToString();
		
	}

	private void OnCharacteristicRemoved(ProductCharacteristics.Type type, string name)
	{
		if (type == ProductCharacteristics.Type.Unknown)
		{
			UnknownCharacteristicsLabel.Text = (UnknownCharacteristicsLabel.Text.ToInt() - 1).ToString();
		}

		AllCharacteristicsLabel.Text = (AllCharacteristicsLabel.Text.ToInt() - 1).ToString();
	}
}