using Godot;

namespace UX.Head;

using System.Collections;

using Core.UI;
using Core.Log;
using Core.Manager;

public sealed partial class SelectSitePanel : AlternativePanel
{
	[Export] private OptionButton _selectButton = null!;

	public override void _Ready()
	{
		base._Ready();

		SetSites(SiteManager.GetSites());
		_selectButton.ItemSelected += OnItemSelected;
	}

	public void SetSites(IList sites)
	{
		_selectButton.Clear();

		bool isAdded = false;
		foreach (string site in sites)
		{
			_selectButton.AddItem(site);
			isAdded = true;
		}

		if (isAdded) OnItemSelected(0);
	}

	private void OnItemSelected(long index)
	{
		string siteName = _selectButton.GetItemText((int)index);
		Log.Print($"Try select {siteName}");
		Global.Instance.SetSite(siteName);
	}
}
