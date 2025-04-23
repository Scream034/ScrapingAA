namespace Core.Components.Smart.ASC.Internal;

public sealed class ASCharacteristicValue
{
	public double Value;
	public double IdfValue;

	public ASCharacteristicValue(double value = 1, double idfValue = 0.25)
	{
		Value = value;
		IdfValue = idfValue;
	}

	public static ASCharacteristicValue operator +(ASCharacteristicValue a, double value)
	{
		a.Value += value;
		return a;
	}

	public static ASCharacteristicValue operator /(ASCharacteristicValue a, double value)
	{
		a.Value /= value;
		return a;
	}
}