
namespace UX.Option.First.Services;

using System;
using System.Linq;
using System.Threading.Tasks;

using Core;
using Core.Log;
using Core.UI.Window;
using Components.Table;
using Godot;
using Core.Components.Smart.ASC;

public sealed class FProductAutoSortCharacteristics : IFService<FProductAutoSortCharacteristicsInitializeArguments, FServiceNoneType>
{
	public static string Name => "Авто сортировка хар-ик";
	public ASCClassifier? Classifier { get; private set; }

	private ProductsTableView _tableView = null!;

	public void Init(FProductAutoSortCharacteristicsInitializeArguments args)
	{
		_tableView = args.TableView ?? throw new ArgumentNullException(nameof(args.TableView));
		Classifier = new(new (Constants.Path.File.AEXSingle, Constants.Path.File.AEXFull), new (Constants.Path.Folder.ASC, Constants.PathName.CustomExtension.Freq));
	}

	public async Task Start(FServiceNoneType args = default)
	{
		var selected = _tableView.Rows.Selected.ToList<ProductTableRow>().Select(x => x.Product);
		float count = selected.Count();
		if (count == 0) return;
		else if (!Classifier!.IsInitialized())
		{
			Classifier.Initialize();
		}

		ProgressWindow window = ProgressWindow.Create("Авто сортировка хар-ик");
		Global.Instance.GetTree().Root.AddChild(window);
		window.Show();

		int counter = 0;
		var tree = Global.Instance.GetTree();

		foreach (var product in selected)
		{
			// Сохраняем копию
			await product.SaveManager.SavePreviousAsync();

			var storagesClone = product.CharacteristicsParser.Characteristics.Storage.Clone();

			foreach (var storage in storagesClone)
			{
				foreach (var characteristic in storage.Value)
				{
					var suggestionType = Classifier.Classify(characteristic.Key, characteristic.Value);

					product.CharacteristicsParser.Characteristics.Storage.Remove(storage.Key, characteristic.Key);
					product.CharacteristicsParser.Characteristics.Storage.Add(suggestionType, characteristic.Key, characteristic.Value);
				}
			}

			counter++;
			Log.Print($"Auto sort characteristics: {counter}/{count} ({((float)counter) / count * 100}%)");
			window.CallDeferred(ProgressWindow.MethodName.SetProgress, counter / count);
			_ = product.SaveManager.SaveAsync();
			await Global.Instance.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
		}

		window.QueueFree();

		Log.Print("Completed auto sort characteristics");
		OS.Alert("Авто сортировка хар-ик", "Завершено");

		return;
	}
}

public class FProductAutoSortCharacteristicsInitializeArguments : IFInitializeArguments
{
	public ProductsTableView TableView { get; }

	public FProductAutoSortCharacteristicsInitializeArguments(ProductsTableView tableView)
	{
		TableView = tableView ?? throw new ArgumentNullException(nameof(tableView));
	}
}