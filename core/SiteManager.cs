using System.Collections.Generic;

using Godot;

using Core.Sites;

namespace Core.Manager;

using Core.Log;

public partial class SiteManager
{
	public IBaseSiteParser Parser { get; set; }

	public SiteManager(string siteName)
	{
		Parser = BaseSiteParser.Create(siteName)!;
	}

	public static List<string> GetSites()
	{
		List<string> sites = new();

		foreach (string site in DirAccess.GetDirectoriesAt("res://core/sites"))
		{
			sites.Add(System.IO.Path.GetFileName(site)); // Get the name of the directory
			Log.Print($"Found site: {site}");
		}

		return sites;
	}
}
