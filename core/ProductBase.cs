namespace Core.Sites;

using System.Linq;

using Core;
using Core.Components.Characteristics;
using Core.Internal;

public class ProductBase<TCharacteristicsParser> : IProductBase<TCharacteristicsParser> where TCharacteristicsParser : IBaseCharacteristicsParser
{
	public TCharacteristicsParser CharacteristicsParser { get; set; }
	public ProductBaseSaveManager<TCharacteristicsParser> SaveManager { get; set; }
	public bool IsBroken { get; set; }
	public string? URL { get; set; }
	public string? CompletedURL { get; set; }
	public string? Title
	{
		get => SaveManager.DirectoryName;
		set
		{
			SaveManager.SetTitle(value);
		}
	}

	public ProductBase(TCharacteristicsParser characteristicsParser)
	{
		CharacteristicsParser = characteristicsParser;
		SaveManager = new(this);
	}

	public virtual void CopyTo<TTCharacteristicsParser>(ProductBase<TTCharacteristicsParser> product) where TTCharacteristicsParser : IBaseCharacteristicsParser
	{
		product.SaveManager = SaveManager.Clone(product);
		product.URL = URL;
		product.CompletedURL = CompletedURL;
		product.IsBroken = IsBroken;
	}

	public static string ParseTitle(string title) => System.Text.RegularExpressions.Regex.Replace(Constants.InvalidFileNameChars.Aggregate(title, static (current, c) => current.Replace(c, ' ')).Replace("   ", " "), @"\s+", " ").Trim();
}