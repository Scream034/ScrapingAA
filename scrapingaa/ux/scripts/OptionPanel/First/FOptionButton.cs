using System;
using System.Collections.Generic;
using Core;
using Core.Components.Characteristics;
using Godot;
using UX.Components.Table;
using UX.Option.First.Services;

namespace UX.Option.First;

public partial class FOptionButton : OptionButton
{
	public Dictionary<string, IFService> AvailableServices = null!;

	[Export] public OptionPanel Parent = null!;
	[Export] public Button StartButton = null!;

	private Action? _serviceAction;

	public override void _EnterTree()
	{
		AvailableServices = new()
		{
			{ FProductApplicationService.Name, new FProductApplicationService() },
			{ FProductSearchService.Name, new FProductSearchService() },
			{ FProductTitleService.Name, new FProductTitleService() },
			{ FProductPhotoService.Name, new FProductPhotoService() },
			{ FProductAutoSortCharacteristics.Name, new FProductAutoSortCharacteristics() },
		};
	}

	public override void _Ready()
	{
		ItemSelected += OnItemSelected;

		foreach (var service in AvailableServices)
		{
			AddItem(service.Key);
		}

		StartButton.Pressed += OnStartButtonPressed;

		if (Selected == -1) OS.Alert("Нет загруженных действий (Bug)", "Жук!");
		else OnItemSelected(Selected);
	}

	protected void OnItemSelected(long index)
	{
		string item = GetItemText((int)index);
		var xService = AvailableServices[item];
		if (xService == null) throw new NullReferenceException($"Service \"${item}\" is not founded");

		if (xService is FProductApplicationService applicationService)
		{
			applicationService.Init(new FProductApplicationInitializeArguments(Parent.AdminInfo, Parent.TableView));
			_serviceAction = async () =>
			{
				await applicationService.Start();
			};
		}
		else if (xService is FProductSearchService searchService)
		{
			searchService.Init(new FProductSearchInitializeArguments(Global.Instance.Spider));
			_serviceAction = async () =>
			{
				if (Global.Instance.Manager == null)
					OS.Alert("Нет выбранных продуктов", "Ошибка!");
				else
					await searchService.Start(new FProductSearchStartArguments(Parent.TableView, Parent.TableView.Rows.Selected, Global.Instance.Manager.Parser.Domain!));
			};
		}
		else if (xService is FProductTitleService titleService)
		{
			titleService.Init();
			_serviceAction = async () =>
			{
				List<IProductBase<IBaseCharacteristicsParser>> rows = new();
				foreach (var row in Parent.TableView.Rows.Selected)
				{
					if (row is ProductTableRow productTableRow) rows.Add(productTableRow.Product);
				}

				await titleService.Start(new(Parent.TableView.Rows.Selected.ToList<ProductTableRow>()));
			};
		}
		else if (xService is FProductPhotoService photoService)
		{
			photoService.Init(new(Parent.TableView));
			_serviceAction = async () =>
			{
				await photoService.Start();
			};
		}
		else if (xService is FProductAutoSortCharacteristics autoSortCharacteristics)
		{
			autoSortCharacteristics.Init(new(Parent.TableView));
			_serviceAction = async () =>
			{
				await autoSortCharacteristics.Start();
			};
		}
	}

	private void OnStartButtonPressed()
	{
		if (_serviceAction == null) return;

		_serviceAction.Invoke();
	}
}