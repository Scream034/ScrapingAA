namespace UX.ProductOption;

using System.Collections.Generic;
using Godot;
using Core.Log;
using Internal;
using System;
using System.Linq;

public sealed partial class ProductOptionBody : Control
{
	public readonly Dictionary<string, IProductBaseBodyValue> ProductOptionTypes = new()
	{
		{ "Характеристики", new ProductCharacteristicsBody() },
		{ "Главная", new ProductPreviewBody() }
	};

	public readonly Shortcut ButtonBaseShortcut = new() { Events = new() { new InputEventKey() { Keycode = Key.Key1, Unicode = '1', ShiftPressed = true, Device = -1 } } };

	[Export] public string DefaultOption = "Характеристики";

	public ProductOptionPanel Parent = null!;
	public IProductBaseBodyValue? CurrentOptionType;

	public override void _EnterTree()
	{
		if (Parent.Buttons.ButtonsContainer == null)
		{
			throw new ArgumentException("Parent should be have OptionButton with Tabs");
		}

		SizeFlagsHorizontal = SizeFlags.ExpandFill;

		for (int i = 0; i < ProductOptionTypes.Count; i++)
		{
			var item = ProductOptionTypes.ElementAt(i);

			Button button = new()
			{
				Text = item.Key,
				SizeFlagsStretchRatio = Parent.Buttons.CloseButton.SizeFlagsStretchRatio + 0.01f,
				Shortcut = new() { Events = new() { new InputEventKey() { Keycode = Key.Kp1 + i, Device = -1 }, new InputEventKey() { Keycode = Key.Key1 + i, Device = -1 } } }
			};
			button.Pressed += () => OnOptionButtonPressed(item.Value);
			Parent.Buttons.ButtonsContainer.AddChild(button);
		}

		OnOptionButtonPressed(ProductOptionTypes[DefaultOption]);
	}

	private void OnOptionButtonPressed(IProductBaseBodyValue type)
	{
		if (CurrentOptionType != null)
		{
			CurrentOptionType.Destroy();
			RemoveChild((Node)CurrentOptionType);
		}

		type.Initialize(Parent);
		CurrentOptionType = type;
		AddChild((Node)CurrentOptionType);
	}
}
