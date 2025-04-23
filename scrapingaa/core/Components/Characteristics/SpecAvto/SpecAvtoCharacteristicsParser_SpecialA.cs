namespace Core.Components.Characteristics.SpecAvto;

using System;
using System.Threading.Tasks;
using System.Collections.Generic;

using Microsoft.Playwright;

public class SpecAvtoCharacteristicsParser_SpecialA(SpecAvtoCharacteristicsParser parent) : SpecAvtoCharacteristicsParser_Special(parent)
{
	public new class XPathInstance : SpecAvtoCharacteristicsParser_Special.XPathInstance
	{
		public override string Block => "//div[@class='box-list']/div[contains(@class, 'list-item')]";
	}

	public override IXPath XPath => new XPathInstance();

	[Obsolete("", true)]
	public override async Task<int> GetAverageCountOfCharacteristicsAsync(IPage page)
	{
		int count = 0;

		IReadOnlyList<IElementHandle> rows = await page.QuerySelectorAllAsync(XPath.Block);
		foreach (IElementHandle row in rows)
		{
			count += (await row.QuerySelectorAllAsync("xpath=./*")).Count;
		}

		return count / rows.Count;
	}
}