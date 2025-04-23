namespace UX.ProductOption.Internal;

using System;
using System.Linq;
using System.Collections.Generic;
using Godot;
using Core;
using Core.Log;
using Core.Components;
using Core.Components.Characteristics;

public sealed partial class ProductPreviewBody : GridContainer, IProductBaseBodyValue
{
	public LineEditComponent[]? lineEditComponents;
	public OptionButtonComponent[]? optionButtonComponents;
	public ProductOptionPanel Parent = null!;

	public override void _EnterTree()
	{
		SizeFlagsHorizontal = SizeFlags.ExpandFill;
		SizeFlagsVertical = SizeFlags.ExpandFill;
		Columns = 2;
	}

	public void Initialize(ProductOptionPanel parent)
	{
		if (lineEditComponents != null || optionButtonComponents != null)
		{
			Update();
			return;
		}

		Parent = parent;

		lineEditComponents = [
			new("Конечный URL", lineEdit => {
				Parent.Product.CompletedURL = lineEdit.Text;
			}, lineEdit => {
				lineEdit.Text = Parent.Product.CompletedURL;
			}),

			new("Модель", lineEdit => {
				Parent.Product.CharacteristicsParser.Model = lineEdit.Text;
			}, lineEdit => {
				lineEdit.Text = Parent.Product.CharacteristicsParser.Model;
			}),

			new("Стоимость", lineEdit => {
				Parent.Product.CharacteristicsParser.Price = lineEdit.Text;
			}, lineEdit => {
				lineEdit.Text = Parent.Product.CharacteristicsParser.Price;
			}),

			new("Мощность, л.с", lineEdit => {
				Parent.Product.CharacteristicsParser.Power = lineEdit.Text;
			}, lineEdit => {
				lineEdit.Text = Parent.Product.CharacteristicsParser.Power;
			}),

			new("Объём двигателя", lineEdit => {
				Parent.Product.CharacteristicsParser.EngineVolume = lineEdit.Text;
			}, lineEdit => {
				lineEdit.Text = Parent.Product.CharacteristicsParser.EngineVolume;
			}),

			new("Количество мест", lineEdit => {
				Parent.Product.CharacteristicsParser.Seats = lineEdit.Text;
			}, lineEdit => {
				lineEdit.Text = Parent.Product.CharacteristicsParser.Seats;
			}),
		];

		optionButtonComponents = [
			new("Двигатель", BaseCharacteristicsParser.DisplayEngineTypes.Values.ToArray(), optionButton => {
				Parent.Product.CharacteristicsParser.Engine = BaseCharacteristicsParser.DisplayEngineTypes.FirstOrDefault(x => x.Value == optionButton.GetItemText(optionButton.Selected)).Key;
			}, optionButton => {
				for (int i = 0; i < optionButton.ItemCount; i++)
				{
					if (BaseCharacteristicsParser.DisplayEngineTypes.FirstOrDefault(x => x.Key == Parent.Product.CharacteristicsParser.Engine).Value == optionButton.GetItemText(i))
					{
						optionButton.Select(i);
						break;
					}
				}
			}),

			new("Коробка передач", BaseCharacteristicsParser.DisplayTransmissionTypes.Values.ToArray(), optionButton => {
				Parent.Product.CharacteristicsParser.Transmission = BaseCharacteristicsParser.DisplayTransmissionTypes.FirstOrDefault(x => x.Value == optionButton.GetItemText(optionButton.Selected)).Key;
			}, optionButton => {
				for (int i = 0; i < optionButton.ItemCount; i++)
				{
					if (BaseCharacteristicsParser.DisplayTransmissionTypes.FirstOrDefault(x => x.Key == Parent.Product.CharacteristicsParser.Transmission).Value == optionButton.GetItemText(i))
					{
						optionButton.Select(i);
						break;
					}
				}
			}),

			new("Привод", BaseCharacteristicsParser.DisplayDriveTypes.Values.ToArray(), optionButton => {
				Parent.Product.CharacteristicsParser.Drive = BaseCharacteristicsParser.DisplayDriveTypes.FirstOrDefault(x => x.Value == optionButton.GetItemText(optionButton.Selected)).Key;
			}, optionButton => {
				for (int i = 0; i < optionButton.ItemCount; i++)
				{
					if (BaseCharacteristicsParser.DisplayDriveTypes.FirstOrDefault(x => x.Key == Parent.Product.CharacteristicsParser.Drive).Value == optionButton.GetItemText(i))
					{
						optionButton.Select(i);
						break;
					}
				}
			})
		];

		CreateLineEdits(lineEditComponents);
		CreateOptionButtons(optionButtonComponents);
	}

	public void Update()
	{
		foreach (var component in lineEditComponents!)
		{
			component.OnUpdate(component.LineEdit!);
		}

		foreach (var component in optionButtonComponents!)
		{
			component.OnUpdate(component.OptionButton!);
		}
	}

	public void Destroy()
	{
		GD.Print("Immit");
	}

	public void CreateLineEdits(params LineEditComponent[] components)
	{
		foreach (var component in components)
		{
			VBoxContainer container = new()
			{
				Name = component.Name,
				SizeFlagsHorizontal = SizeFlags.ExpandFill,
			};

			Label label = new()
			{
				Text = component.Name
			};

			LineEdit lineEdit = new()
			{
				PlaceholderText = component.Name
			};
			lineEdit.TextChanged += (string _) => component.OnTextChanged(lineEdit);

			component.LineEdit = lineEdit;

			container.AddChild(label);
			container.AddChild(lineEdit);

			component.OnUpdate(lineEdit);

			AddChild(container);
		}
	}

	public void CreateOptionButtons(params OptionButtonComponent[] components)
	{
		foreach (var component in components)
		{
			VBoxContainer container = new()
			{
				Name = component.Name,
				SizeFlagsHorizontal = SizeFlags.ExpandFill,
			};

			Label label = new()
			{
				Text = component.Name
			};

			OptionButton optionButton = new();
			foreach (var item in component.Items)
			{
				optionButton.AddItem(item);
			}
			optionButton.ItemSelected += (long _) => component.OnItemSelected(optionButton);

			component.OptionButton = optionButton;

			container.AddChild(label);
			container.AddChild(optionButton);

			component.OnUpdate(optionButton);

			AddChild(container);
		}
	}

	public sealed record class LineEditComponent(string Name, Action<LineEdit> OnTextChanged, Action<LineEdit> OnUpdate)
	{
		public LineEdit? LineEdit { get; set; }
	};

	public sealed record class OptionButtonComponent(string Name, string[] Items, Action<OptionButton> OnItemSelected, Action<OptionButton> OnUpdate)
	{
		public OptionButton? OptionButton { get; set; }
	};
}