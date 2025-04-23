using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Godot;

using Core;
using Core.Log;
using Core.Admin;
using Core.Components.Characteristics;

using UX.Components.Table;

namespace UX.Option.First.Services;

public class FProductApplicationService : IFService<FProductApplicationInitializeArguments, FServiceNoneType>
{
	public static string Name = "Применить";

	public AdminInfo AdminInfo { get; private set; } = null!;

	private TableView _tableView = null!;
	private bool _isInitialized = false;

	public void Init(FProductApplicationInitializeArguments args)
	{
		if (_isInitialized) return;

		AdminInfo = args.AdminInfo;

		_tableView = args.TableView;
		_isInitialized = true;
	}

	public async Task Start(FServiceNoneType xargs = new())
	{
		if (_tableView.Rows.Selected.Count <= 0)
		{
			Log.Error($"No products selected to add, total: {_tableView.Rows.Count}");
			OS.Alert("Не выбраны продукты для добавления", "Ошибка!");
			return;
		}

		if (Global.Instance.Admin == null || !Global.Instance.Admin.Info.Equals(AdminInfo))
		{
			Global.Instance.Admin = new(Global.Instance.Spider, AdminInfo);
		}

		var completedProducts = new List<IProductBase<IBaseCharacteristicsParser>>();
		bool shouldStop = false; // Флаг для остановки процесса

		foreach (ProductTableRow row in _tableView.Rows.Selected)
		{
			try
			{
				string? error = await Global.Instance.Admin.AddProductAsync(row.Product);
				if (error != null)
				{
					Log.Error($"Failed to add product {row.Product.Title}, {error}");
					OS.Alert($"Не удалось добавить продукт {row.Product.Title}\n{error}", "Ошибка");
					shouldStop = true; // Останавливаем процесс
					break;
				}
			}
			catch (Exception ex)
			{
				Log.Error($"Failed to add product {row.Product.Title}: {ex.Message} \n[{ex.StackTrace}]");
				row.Product.CompletedURL = "Ошибка";

				// Обработка ошибки открытия страницы администрирования
				bool continueProcess = await HandleAdminPageError();
				if (!continueProcess)
				{
					shouldStop = true; // Останавливаем процесс
					break;
				}
			}
			finally
			{
				completedProducts.Add(row.Product);
				_tableView.Rows.Selected.Remove(row);
			}
		}

		if (shouldStop)
		{
			OS.Alert("Применение данных остановлено. Результат экстренно сохраняется.", "Уведомление");
		}

		// Сохраняем текущий результат в Excel
		Global.Instance.SaveProductsToExcel(completedProducts);

		await Global.Instance.Spider.CloseCurrentPageAsync();
	}

	private async Task<bool> HandleAdminPageError()
	{
		Log.Print("Error occurred while opening admin page, trying to open it again");

		if (Global.Instance.Admin == null)
		{
			return false; // Останавливаем процесс
		}

		if (await Global.Instance.Admin.OpenPageAsync() != null)
		{
			Log.Print("Successfully opened admin page after ERROR");
			return true; // Продолжаем процесс
		}

		return false; // Останавливаем процесс
	}
}

public class FProductApplicationInitializeArguments : IFInitializeArguments
{
	public AdminInfo AdminInfo { get; set; }
	public TableView TableView { get; set; }

	public FProductApplicationInitializeArguments(AdminInfo adminInfo, TableView tableView)
	{
		AdminInfo = adminInfo;
		TableView = tableView;
	}
}