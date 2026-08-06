using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using NUnit.Framework.Interfaces;

#nullable enable

namespace NetSerializer.UnitTests;

[TestFixture]
[TestOf(typeof(Serializer))]
public sealed class RoundTripTest
{
	private static readonly Type[] Types =
	[
		typeof(object),
		typeof(Guid),
		typeof(int),
		typeof(LargeStruct),
		typeof(U8Message),
		typeof(S16Message),
		typeof(S32Message),
		typeof(S64Message),
		typeof(DecimalMessage),
		typeof(NullableDecimalMessage),
		typeof(PrimitivesMessage),
		typeof(StructMessage),
		typeof(BoxedPrimitivesMessage),
		typeof(ByteArrayMessage),
		typeof(IntArrayMessage),
		typeof(StringMessage),
		typeof(DictionaryMessage),
		typeof(ComplexMessage),
		typeof(TriDimArrayCustomSerializersMessage),
		typeof(SimpleClass),
		typeof(SimpleClass2),
	];

	[TestCaseSource(nameof(Cases))]
	public void RoundTrips(IRoundTripCase testCase)
	{
		testCase.Run(CreateSerializer());
	}

	private static IEnumerable<ITestCaseData> Cases()
	{
		yield return Case("object", new RoundTripCase<object>(() => new object(), (a, b) => Assert.That(b.GetType(), Is.EqualTo(a.GetType()))));
		yield return Case("Guid", new RoundTripCase<Guid>(() => Guid.Parse("f6308807-c312-4ec1-b5d5-4dd9ec67cd8d"), AssertEqual));
		yield return Case("Guid direct", new RoundTripCase<Guid>(() => Guid.Parse("f6308807-c312-4ec1-b5d5-4dd9ec67cd8d"), AssertEqual, direct: true));
		yield return Case("int", new RoundTripCase<int>(() => -123456, AssertEqual));
		yield return Case("int direct", new RoundTripCase<int>(() => -123456, AssertEqual, direct: true));
		yield return Case("LargeStruct", new RoundTripCase<LargeStruct>(LargeStruct.Create, LargeStruct.AssertEqual));
		yield return Case("LargeStruct direct", new RoundTripCase<LargeStruct>(LargeStruct.Create, LargeStruct.AssertEqual, direct: true));
		yield return Case("U8Message", new RoundTripCase<U8Message>(U8Message.Create, U8Message.AssertEqual));
		yield return Case("U8Message direct", new RoundTripCase<U8Message>(U8Message.Create, U8Message.AssertEqual, direct: true));
		yield return Case("S16Message", new RoundTripCase<S16Message>(S16Message.Create, S16Message.AssertEqual));
		yield return Case("S32Message", new RoundTripCase<S32Message>(S32Message.Create, S32Message.AssertEqual));
		yield return Case("S64Message", new RoundTripCase<S64Message>(S64Message.Create, S64Message.AssertEqual));
		yield return Case("DecimalMessage", new RoundTripCase<DecimalMessage>(DecimalMessage.Create, DecimalMessage.AssertEqual));
		yield return Case("NullableDecimalMessage", new RoundTripCase<NullableDecimalMessage>(NullableDecimalMessage.Create, NullableDecimalMessage.AssertEqual));
		yield return Case("PrimitivesMessage", new RoundTripCase<PrimitivesMessage>(PrimitivesMessage.Create, PrimitivesMessage.AssertEqual));
		yield return Case("StructMessage", new RoundTripCase<StructMessage>(StructMessage.Create, StructMessage.AssertEqual));
		yield return Case("BoxedPrimitivesMessage", new RoundTripCase<BoxedPrimitivesMessage>(BoxedPrimitivesMessage.Create, BoxedPrimitivesMessage.AssertEqual));
		yield return Case("ByteArrayMessage", new RoundTripCase<ByteArrayMessage>(ByteArrayMessage.Create, ByteArrayMessage.AssertEqual));
		yield return Case("IntArrayMessage", new RoundTripCase<IntArrayMessage>(IntArrayMessage.Create, IntArrayMessage.AssertEqual));
		yield return Case("StringMessage", new RoundTripCase<StringMessage>(StringMessage.Create, StringMessage.AssertEqual));
		yield return Case("DictionaryMessage", new RoundTripCase<DictionaryMessage>(DictionaryMessage.Create, DictionaryMessage.AssertEqual));
		yield return Case("ComplexMessage", new RoundTripCase<ComplexMessage>(ComplexMessage.Create, ComplexMessage.AssertEqual));
		yield return Case("TriDimArrayCustomSerializersMessage", new RoundTripCase<TriDimArrayCustomSerializersMessage>(TriDimArrayCustomSerializersMessage.Create, TriDimArrayCustomSerializersMessage.AssertEqual));
	}

	private static ITestCaseData Case<T>(string name, RoundTripCase<T> testCase)
	{
		return new TestCaseData(testCase).SetName(name);
	}

	private static Serializer CreateSerializer()
	{
		return new Serializer(Types, new Settings
		{
			CustomTypeSerializers = [new TriDimArrayCustomSerializer()],
		});
	}

	private static byte[] Serialize<T>(Serializer serializer, T value, bool direct)
	{
		using var stream = new MemoryStream();
		if (direct)
			serializer.SerializeDirect(stream, value);
		else
			serializer.Serialize(stream, value);

		return stream.ToArray();
	}

	private static T Deserialize<T>(Serializer serializer, byte[] bytes, bool direct)
	{
		using var stream = new MemoryStream(bytes, writable: false);
		if (direct)
		{
			serializer.DeserializeDirect(stream, out T value);
			return value;
		}

		return (T)serializer.Deserialize(stream);
	}

	private static void AssertEqual<T>(T a, T b)
	{
		Assert.That(b, Is.EqualTo(a));
	}

	public interface IRoundTripCase
	{
		void Run(Serializer serializer);
	}

	public sealed class RoundTripCase<T> : IRoundTripCase
	{
		public RoundTripCase(Func<T> create, Action<T, T> assertEqual, bool direct = false)
		{
			Create = create;
			AssertEqual = assertEqual;
			Direct = direct;
		}

		public Func<T> Create { get; }
		public Action<T, T> AssertEqual { get; }
		public bool Direct { get; }

		public void Run(Serializer serializer)
		{
			var value = Create();
			var bytes = Serialize(serializer, value, Direct);
			var read = Deserialize<T>(serializer, bytes, Direct);

			AssertEqual(value, read);
		}
	}

	private sealed class TriDimArrayCustomSerializer : IStaticTypeSerializer
	{
		public bool Handles(Type type)
		{
			return type == typeof(int[,,]);
		}

		public IEnumerable<Type> GetSubtypes(Type type)
		{
			return [];
		}

		public MethodInfo GetStaticWriter(Type type)
		{
			return typeof(TriDimArrayCustomSerializer).GetMethod(
				nameof(WritePrimitive),
				BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.ExactBinding,
				null,
				[typeof(Stream), type],
				null)!;
		}

		public MethodInfo GetStaticReader(Type type)
		{
			return typeof(TriDimArrayCustomSerializer).GetMethod(
				nameof(ReadPrimitive),
				BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.ExactBinding,
				null,
				[typeof(Stream), type.MakeByRefType()],
				null)!;
		}

		private static void WritePrimitive(Stream stream, int[,,]? value)
		{
			if (value == null)
			{
				Primitives.WritePrimitive(stream, 0u);
				return;
			}

			var l1 = value.GetLength(0);
			var l2 = value.GetLength(1);
			var l3 = value.GetLength(2);

			Primitives.WritePrimitive(stream, (uint)l1 + 1);
			Primitives.WritePrimitive(stream, (uint)l2);
			Primitives.WritePrimitive(stream, (uint)l3);

			for (var z = 0; z < l1; z++)
			for (var y = 0; y < l2; y++)
			for (var x = 0; x < l3; x++)
				Primitives.WritePrimitive(stream, value[z, y, x]);
		}

		private static void ReadPrimitive(Stream stream, out int[,,]? value)
		{
			Primitives.ReadPrimitive(stream, out uint l1);
			if (l1 == 0)
			{
				value = null;
				return;
			}

			l1--;
			Primitives.ReadPrimitive(stream, out uint l2);
			Primitives.ReadPrimitive(stream, out uint l3);

			value = new int[l1, l2, l3];
			for (var z = 0; z < l1; z++)
			for (var y = 0; y < l2; y++)
			for (var x = 0; x < l3; x++)
				Primitives.ReadPrimitive(stream, out value[z, y, x]);
		}
	}

	[Serializable]
	private struct LargeStruct
	{
		public ulong A, B, C, D, E, F, G, H;

		public static LargeStruct Create()
		{
			return new LargeStruct { A = 1, B = 2, C = 3, D = 4, E = 5, F = 6, G = 7, H = 8 };
		}

		public static void AssertEqual(LargeStruct a, LargeStruct b)
		{
			Assert.That(b, Is.EqualTo(a));
		}
	}

	[Serializable]
	private sealed class U8Message
	{
		private byte _value = 42;

		public static U8Message Create() => new();
		public static void AssertEqual(U8Message a, U8Message b) => Assert.That(b._value, Is.EqualTo(a._value));
	}

	[Serializable]
	private sealed class S16Message
	{
		private short _value = -1234;

		public static S16Message Create() => new();
		public static void AssertEqual(S16Message a, S16Message b) => Assert.That(b._value, Is.EqualTo(a._value));
	}

	[Serializable]
	private sealed class S32Message
	{
		private int _value = -123456;

		public static S32Message Create() => new();
		public static void AssertEqual(S32Message a, S32Message b) => Assert.That(b._value, Is.EqualTo(a._value));
	}

	[Serializable]
	private sealed class S64Message
	{
		private long _value = -123456789;

		public static S64Message Create() => new();
		public static void AssertEqual(S64Message a, S64Message b) => Assert.That(b._value, Is.EqualTo(a._value));
	}

	[Serializable]
	private sealed class DecimalMessage
	{
		private decimal _value = 123.456m;

		public static DecimalMessage Create() => new();
		public static void AssertEqual(DecimalMessage a, DecimalMessage b) => Assert.That(b._value, Is.EqualTo(a._value));
	}

	[Serializable]
	private sealed class NullableDecimalMessage
	{
		private decimal? _value = 123.456m;
		private decimal? _null = null;

		public static NullableDecimalMessage Create() => new();
		public static void AssertEqual(NullableDecimalMessage a, NullableDecimalMessage b)
		{
			Assert.That(b._value, Is.EqualTo(a._value));
			Assert.That(b._null, Is.EqualTo(a._null));
		}
	}

	private enum MyEnum
	{
		A,
		B,
		C,
	}

	[Serializable]
	private sealed class PrimitivesMessage
	{
		private bool _bool = true;
		private byte _byte = 255;
		private sbyte _sbyte = -12;
		private char _char = 'x';
		private ushort _ushort = 65530;
		private short _short = -1234;
		private uint _uint = 123456;
		private int _int = -123456;
		private ulong _ulong = 123456789;
		private long _long = -123456789;
		private float _single = 1.5f;
		private double _double = -2.25d;
		private MyEnum _enum = MyEnum.C;
		private DateTime _date = new(2026, 8, 6, 12, 34, 56, DateTimeKind.Utc);

		public static PrimitivesMessage Create() => new();

		public static void AssertEqual(PrimitivesMessage a, PrimitivesMessage b)
		{
			Assert.That(b._bool, Is.EqualTo(a._bool));
			Assert.That(b._byte, Is.EqualTo(a._byte));
			Assert.That(b._sbyte, Is.EqualTo(a._sbyte));
			Assert.That(b._char, Is.EqualTo(a._char));
			Assert.That(b._ushort, Is.EqualTo(a._ushort));
			Assert.That(b._short, Is.EqualTo(a._short));
			Assert.That(b._uint, Is.EqualTo(a._uint));
			Assert.That(b._int, Is.EqualTo(a._int));
			Assert.That(b._ulong, Is.EqualTo(a._ulong));
			Assert.That(b._long, Is.EqualTo(a._long));
			Assert.That(BitConverter.SingleToUInt32Bits(b._single), Is.EqualTo(BitConverter.SingleToUInt32Bits(a._single)));
			Assert.That(BitConverter.DoubleToUInt64Bits(b._double), Is.EqualTo(BitConverter.DoubleToUInt64Bits(a._double)));
			Assert.That(b._enum, Is.EqualTo(a._enum));
			Assert.That(b._date, Is.EqualTo(a._date));
		}
	}

	[Serializable]
	private struct MyStruct1
	{
		public int A;
		public float B;
	}

	[Serializable]
	private struct MyStruct2
	{
		public MyStruct1 A;
		public long B;
	}

	[Serializable]
	private sealed class StructMessage
	{
		private MyStruct2 _value = new() { A = new MyStruct1 { A = 123, B = 1.5f }, B = 456 };

		public static StructMessage Create() => new();
		public static void AssertEqual(StructMessage a, StructMessage b) => Assert.That(b._value, Is.EqualTo(a._value));
	}

	[Serializable]
	private sealed class BoxedPrimitivesMessage
	{
		private object _bool = true;
		private object _byte = (byte)42;
		private object _int = -123456;
		private object _long = -123456789L;
		private object _enum = MyEnum.B;

		public static BoxedPrimitivesMessage Create() => new();

		public static void AssertEqual(BoxedPrimitivesMessage a, BoxedPrimitivesMessage b)
		{
			Assert.That(b._bool, Is.EqualTo(a._bool));
			Assert.That(b._byte, Is.EqualTo(a._byte));
			Assert.That(b._int, Is.EqualTo(a._int));
			Assert.That(b._long, Is.EqualTo(a._long));
			Assert.That(b._enum, Is.EqualTo(a._enum));
		}
	}

	[Serializable]
	private sealed class ByteArrayMessage
	{
		private byte[] _value = [1, 2, 3, 4, 5];

		public static ByteArrayMessage Create() => new();
		public static void AssertEqual(ByteArrayMessage a, ByteArrayMessage b) => Assert.That(b._value, Is.EqualTo(a._value));
	}

	[Serializable]
	private sealed class IntArrayMessage
	{
		private int[] _value = [1, -2, 3, -4, 5];

		public static IntArrayMessage Create() => new();
		public static void AssertEqual(IntArrayMessage a, IntArrayMessage b) => Assert.That(b._value, Is.EqualTo(a._value));
	}

	[Serializable]
	private sealed class StringMessage
	{
		private string _value = "hello";
		private string _empty = "";
		private string? _null = null;

		public static StringMessage Create() => new();

		public static void AssertEqual(StringMessage a, StringMessage b)
		{
			Assert.That(b._value, Is.EqualTo(a._value));
			Assert.That(b._empty, Is.EqualTo(a._empty));
			Assert.That(b._null, Is.EqualTo(a._null));
		}
	}

	[Serializable]
	private abstract class SimpleClassBase
	{
		public int BaseValue = 10;
	}

	private interface IMyTest
	{
	}

	[Serializable]
	private sealed class SimpleClass : SimpleClassBase
	{
		private int _value = 20;

		public void AssertEqual(SimpleClass other)
		{
			Assert.That(other.BaseValue, Is.EqualTo(BaseValue));
			Assert.That(other._value, Is.EqualTo(_value));
		}
	}

	[Serializable]
	private sealed class SimpleClass2 : IMyTest
	{
		private int _value = 30;

		public void AssertEqual(SimpleClass2 other)
		{
			Assert.That(other._value, Is.EqualTo(_value));
		}
	}

	[Serializable]
	private sealed class DictionaryMessage
	{
		private Dictionary<int, int> _intMap = new() { [1] = -1, [2] = -2 };
		private Dictionary<string, SimpleClass2> _objectMap = new() { ["a"] = new SimpleClass2(), ["b"] = new SimpleClass2() };

		public static DictionaryMessage Create() => new();

		public static void AssertEqual(DictionaryMessage a, DictionaryMessage b)
		{
			Assert.That(b._intMap, Is.EqualTo(a._intMap));
			Assert.That(b._objectMap.Keys, Is.EquivalentTo(a._objectMap.Keys));
			foreach (var key in a._objectMap.Keys)
				a._objectMap[key].AssertEqual(b._objectMap[key]);
		}
	}

	[Serializable]
	private sealed class ComplexMessage
	{
		private S16Message _message = new();
		private SimpleClass _sealedClass = new();
		private SimpleClassBase _abstractField = new SimpleClass();
		private IMyTest _interfaceField = new SimpleClass2();

		public static ComplexMessage Create() => new();

		public static void AssertEqual(ComplexMessage a, ComplexMessage b)
		{
			S16Message.AssertEqual(a._message, b._message);
			a._sealedClass.AssertEqual(b._sealedClass);
			((SimpleClass)a._abstractField).AssertEqual((SimpleClass)b._abstractField);
			((SimpleClass2)a._interfaceField).AssertEqual((SimpleClass2)b._interfaceField);
		}
	}

	[Serializable]
	private sealed class TriDimArrayCustomSerializersMessage
	{
		private int[,,] _value =
		{
			{ { 1, 2 }, { 3, 4 } },
			{ { 5, 6 }, { 7, 8 } },
		};

		public static TriDimArrayCustomSerializersMessage Create() => new();

		public static void AssertEqual(TriDimArrayCustomSerializersMessage a, TriDimArrayCustomSerializersMessage b)
		{
			Assert.That(b._value.Rank, Is.EqualTo(a._value.Rank));
			Assert.That(b._value.GetLength(0), Is.EqualTo(a._value.GetLength(0)));
			Assert.That(b._value.GetLength(1), Is.EqualTo(a._value.GetLength(1)));
			Assert.That(b._value.GetLength(2), Is.EqualTo(a._value.GetLength(2)));

			for (var z = 0; z < a._value.GetLength(0); z++)
			for (var y = 0; y < a._value.GetLength(1); y++)
			for (var x = 0; x < a._value.GetLength(2); x++)
				Assert.That(b._value[z, y, x], Is.EqualTo(a._value[z, y, x]));
		}
	}
}
