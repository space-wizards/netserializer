using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using NS = NetSerializer;

namespace NetSerializer.Benchmarks;

internal static class Program
{
	private const int PayloadElementCount = 512;

	public static void Main(string[] args)
	{
		var iterations = args.Contains("--quick") ? 5_000 : 100_000;
		var serializer = new NS.Serializer(new[] { typeof(BenchStruct), typeof(BenchMessage), typeof(IntArrayMessage) });
		var benchStruct = BenchStruct.Create();
		var benchMessage = BenchMessage.Create(PayloadElementCount);
		var intArrayMessage = IntArrayMessage.Create(PayloadElementCount);

		AssertRoundTrip(serializer, benchStruct, BenchStruct.AssertEqual);
		AssertRoundTrip(serializer, benchMessage, BenchMessage.AssertEqual);
		AssertRoundTrip(serializer, intArrayMessage, IntArrayMessage.AssertEqual);

		Console.WriteLine($"NetSerializer runtime benchmarks ({iterations:N0} iterations)");
		Console.WriteLine("Lower is better. Round-trip correctness is checked before timing.");
		Console.WriteLine();
		Console.WriteLine($"{"Scenario",-40} {"Elapsed",12}");
		Console.WriteLine(new string('-', 53));

		Run("BenchStruct direct serialize", iterations, () => SerializeDirect(serializer, benchStruct));
		Run("BenchStruct direct deserialize", iterations, () => DeserializeDirect<BenchStruct>(serializer, benchStruct));
		Run("BenchMessage object serialize", iterations, () => SerializeObject(serializer, benchMessage));
		Run("BenchMessage object deserialize", iterations, () => DeserializeObject(serializer, benchMessage));
		Run("BenchMessage direct serialize", iterations, () => SerializeDirect(serializer, benchMessage));
		Run("BenchMessage direct deserialize", iterations, () => DeserializeDirect<BenchMessage>(serializer, benchMessage));
		Run("IntArrayMessage object serialize", iterations, () => SerializeObject(serializer, intArrayMessage));
		Run("IntArrayMessage object deserialize", iterations, () => DeserializeObject(serializer, intArrayMessage));
	}

	private static void Run(string name, int iterations, Action action)
	{
		action();
		var stopwatch = Stopwatch.StartNew();
		for (var i = 0; i < iterations; i++)
		{
			action();
		}

		stopwatch.Stop();
		Console.WriteLine($"{name,-40} {Format(stopwatch.Elapsed),12}");
	}

	private static string Format(TimeSpan elapsed)
	{
		if (elapsed.TotalMilliseconds >= 1)
			return $"{elapsed.TotalMilliseconds:N2} ms";

		return $"{elapsed.TotalMicroseconds:N2} us";
	}

	private static void AssertRoundTrip<T>(NS.Serializer serializer, T value, Action<T, T> assertEqual)
	{
		var objectBytes = SerializeObject(serializer, value!);
		using (var stream = new MemoryStream(objectBytes))
		{
			var read = (T) serializer.Deserialize(stream);
			assertEqual(read, value);
		}

		var directBytes = SerializeDirect(serializer, value);
		using (var stream = new MemoryStream(directBytes))
		{
			serializer.DeserializeDirect(stream, out T read);
			assertEqual(read, value);
		}
	}

	private static byte[] SerializeObject(NS.Serializer serializer, object value)
	{
		using var stream = new MemoryStream();
		serializer.Serialize(stream, value);
		return stream.ToArray();
	}

	private static object DeserializeObject<T>(NS.Serializer serializer, T value)
	{
		var bytes = SerializeObject(serializer, value!);
		using var stream = new MemoryStream(bytes);
		return serializer.Deserialize(stream);
	}

	private static byte[] SerializeDirect<T>(NS.Serializer serializer, T value)
	{
		using var stream = new MemoryStream();
		serializer.SerializeDirect(stream, value);
		return stream.ToArray();
	}

	private static T DeserializeDirect<T>(NS.Serializer serializer, T value)
	{
		var bytes = SerializeDirect(serializer, value);
		using var stream = new MemoryStream(bytes);
		serializer.DeserializeDirect(stream, out T read);
		return read;
	}
}
