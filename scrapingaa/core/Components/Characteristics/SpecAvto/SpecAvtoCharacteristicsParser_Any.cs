using System;
using System.Threading.Tasks;
using Microsoft.Playwright;

namespace Core.Components.Characteristics.SpecAvto;

public abstract class SpecAvtoCharacteristicsParser_Any
{
	public SpecAvtoCharacteristicsParser Parent { get; set; }

	public SpecAvtoCharacteristicsParser_Any(SpecAvtoCharacteristicsParser parent)
	{
		Parent = parent;
	}

	public virtual Task<bool> ParseAsync(IPage page, IOptions? options)
	{
		throw new NotImplementedException();
	}

	[Obsolete]
	public virtual Task<int> GetAverageCountOfCharacteristicsAsync(IPage page)
	{
		return Task.FromResult(0);
	}

	public interface IOptions { }
}