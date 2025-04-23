namespace Core.Components.Characteristics;

public sealed class OffsetVH
{
	public int Vertical { get; set; }
	public int Horizontal { get; set; }

	public OffsetVH(int vertical = 0, int horizontal = 0)
	{
		Vertical = vertical;
		Horizontal = horizontal;
	}

	public OffsetVH Clone()
	{
		return new OffsetVH(Vertical, Horizontal);
	}

	public override string ToString() => $"V:{Vertical}, H:{Horizontal}";
}

public sealed class CharacteristicOffset
{
	public OffsetVH Name { get; set; }
	public OffsetVH Value { get; set; }
	public OffsetVH General { get; set; }

	public CharacteristicOffset(OffsetVH? name = null, OffsetVH? value = null, OffsetVH? general = null)
	{
		Name = name ?? new OffsetVH();
		Value = value ?? new OffsetVH();
		General = general ?? new OffsetVH();
	}

	public CharacteristicOffset Clone()
	{
		return new CharacteristicOffset(Name.Clone(), Value.Clone(), General.Clone());
	}

	public override string ToString() => $"Name: {Name}; Value: {Value}; General: {General}";
}