using System;
using System.Collections.Generic;
using System.Linq;

namespace NetSerializer.Benchmarks;

[Serializable]
internal struct BenchStruct
{
	public int Id;
	public long Tick;
	public float X;
	public float Y;
	public double Rotation;

	public static BenchStruct Create()
	{
		return new BenchStruct
		{
			Id = 42,
			Tick = 123456789,
			X = 12.5f,
			Y = -8.25f,
			Rotation = 1.25,
		};
	}

	public static void AssertEqual(BenchStruct actual, BenchStruct expected)
	{
		if (actual.Id != expected.Id ||
		    actual.Tick != expected.Tick ||
		    actual.X != expected.X ||
		    actual.Y != expected.Y ||
		    actual.Rotation != expected.Rotation)
		{
			throw new InvalidOperationException("BenchStruct round-trip mismatch.");
		}
	}
}

[Serializable]
internal sealed class BenchMessage
{
	public int Sequence;
	public BenchStruct Coordinates;
	public int[] EntityIds = [];
	public string[] Names = [];
	public Dictionary<int, BenchStruct> Components = new();

	public static BenchMessage Create(int elementCount)
	{
		var message = new BenchMessage
		{
			Sequence = 123,
			Coordinates = BenchStruct.Create(),
			EntityIds = new int[elementCount],
			Names = new string[elementCount / 8],
		};

		for (var i = 0; i < message.EntityIds.Length; i++)
		{
			message.EntityIds[i] = i * 3;
		}

		for (var i = 0; i < message.Names.Length; i++)
		{
			message.Names[i] = $"entity-{i}";
		}

		for (var i = 0; i < elementCount / 16; i++)
		{
			message.Components[i] = new BenchStruct
			{
				Id = i,
				Tick = i * 1000L,
				X = i + 0.25f,
				Y = -i - 0.5f,
				Rotation = i * 0.125,
			};
		}

		return message;
	}

	public static void AssertEqual(BenchMessage actual, BenchMessage expected)
	{
		if (actual.Sequence != expected.Sequence)
			throw new InvalidOperationException("BenchMessage sequence mismatch.");

		BenchStruct.AssertEqual(actual.Coordinates, expected.Coordinates);

		if (!actual.EntityIds.SequenceEqual(expected.EntityIds))
			throw new InvalidOperationException("BenchMessage entity-id array mismatch.");

		if (!actual.Names.SequenceEqual(expected.Names))
			throw new InvalidOperationException("BenchMessage names array mismatch.");

		if (actual.Components.Count != expected.Components.Count)
			throw new InvalidOperationException("BenchMessage component count mismatch.");

		foreach (var (key, value) in expected.Components)
		{
			BenchStruct.AssertEqual(actual.Components[key], value);
		}
	}
}

[Serializable]
internal sealed class IntArrayMessage
{
	public int[] Values = [];

	public static IntArrayMessage Create(int elementCount)
	{
		var message = new IntArrayMessage
		{
			Values = new int[elementCount],
		};

		for (var i = 0; i < message.Values.Length; i++)
		{
			message.Values[i] = i * 7;
		}

		return message;
	}

	public static void AssertEqual(IntArrayMessage actual, IntArrayMessage expected)
	{
		if (!actual.Values.SequenceEqual(expected.Values))
			throw new InvalidOperationException("IntArrayMessage array mismatch.");
	}
}
