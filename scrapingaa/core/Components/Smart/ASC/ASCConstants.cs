namespace Core.Components.Smart.ASC.Internal;

using System.Collections.Generic;
using Core.Components.Smart.AEXU;

public sealed class ASCConstants
{
	public const float ScoreAEXUnitsCoefficient = 0.51f;
	public const float ScoreAEXUnitsDoubtfulCoefficient = 0.21213f;
	public const float ScoreDifferentFactor = 10f;
	public const int MinWordLength = 3;

	// Словарь весов для типов единиц измерения для каждой категории
	public static Dictionary<ProductCharacteristics.Type, Dictionary<AEXUnit.Type, float>> AEXUnitTypeWeights => new()
		{
			{ ProductCharacteristics.Type.Technical, new Dictionary<AEXUnit.Type, float>()
				{
					{ AEXUnit.Type.Length, 1f },
					{ AEXUnit.Type.Area, 1f },
					{ AEXUnit.Type.Volume, 1f },
					{ AEXUnit.Type.Weight, 1f },
					{ AEXUnit.Type.Time, 0.5f },
					{ AEXUnit.Type.Temperature, 1f },
					{ AEXUnit.Type.Frequency, 1f },
					{ AEXUnit.Type.Density, 1f },
					{ AEXUnit.Type.Force, 1f },
					{ AEXUnit.Type.Torque, 1f },
					{ AEXUnit.Type.Resistance, 0.5f },
					{ AEXUnit.Type.Capacitance, 1f },
					{ AEXUnit.Type.Inductance, 1f },
					{ AEXUnit.Type.Luminosity, 1f },
					{ AEXUnit.Type.Angle, 1f },
					{ AEXUnit.Type.Ratio, 1f },
				}
			},
			{ ProductCharacteristics.Type.Performance, new Dictionary<AEXUnit.Type, float>()
				{
					{ AEXUnit.Type.Time, 0.5f },
					{ AEXUnit.Type.Speed, 1f },
					{ AEXUnit.Type.Power, 1f },
					{ AEXUnit.Type.Energy, 1f },
					{ AEXUnit.Type.Pressure, 1f },
					{ AEXUnit.Type.Voltage, 1f },
					{ AEXUnit.Type.Current, 1f },
					{ AEXUnit.Type.Resistance, 0.5f },
				}
			},
		};
}