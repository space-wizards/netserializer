/*
 * Copyright 2015 Tomi Valkeinen
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace NetSerializer
{
	sealed class DictionarySerializer : IStaticTypeSerializer
	{
		public bool Handles(Type type)
		{
			if (!type.IsGenericType)
				return false;

			var genTypeDef = type.GetGenericTypeDefinition();

			return genTypeDef == typeof(Dictionary<,>) || genTypeDef == typeof(ImmutableDictionary<,>);
		}

		public IEnumerable<Type> GetSubtypes(Type type)
		{
			// Dictionary<K,V> is stored as KeyValuePair<K,V>[]

			var genArgs = type.GetGenericArguments();

			var serializedType = typeof(KeyValuePair<,>).MakeGenericType(genArgs).MakeArrayType();

			return new[] { serializedType };
		}

		public MethodInfo GetStaticWriter(Type type)
		{
			Debug.Assert(type.IsGenericType);

			if (!type.IsGenericType)
				throw new Exception();

			var genTypeDef = type.GetGenericTypeDefinition();

			Debug.Assert(genTypeDef == typeof(Dictionary<,>) || genTypeDef == typeof(ImmutableDictionary<,>));

			var containerType = this.GetType();

			var writer = Helpers.GetGenWriter(containerType, genTypeDef);

			var genArgs = type.GetGenericArguments();

			writer = writer.MakeGenericMethod(genArgs);

			return writer;
		}

		public MethodInfo GetStaticReader(Type type)
		{
			Debug.Assert(type.IsGenericType);

			if (!type.IsGenericType)
				throw new Exception();

			var genTypeDef = type.GetGenericTypeDefinition();

			Debug.Assert(genTypeDef == typeof(Dictionary<,>) || genTypeDef == typeof(ImmutableDictionary<,>));

			var containerType = this.GetType();

			var reader = Helpers.GetGenReader(containerType, genTypeDef);

			var genArgs = type.GetGenericArguments();

			reader = reader.MakeGenericMethod(genArgs);

			return reader;
		}

		private static void BaseWritePrimitive<TKey, TValue>(Serializer serializer, Stream stream, IReadOnlyDictionary<TKey, TValue> value)
		{
			if (value == null)
			{
				serializer.Serialize(stream, null);
				return;
			}

			var kvpArray = new KeyValuePair<TKey, TValue>[value.Count];

			int i = 0;
			foreach (var kvp in value)
				kvpArray[i++] = kvp;

			serializer.Serialize(stream, kvpArray);
		}

		public static void WritePrimitive<TKey, TValue>(Serializer serializer, Stream stream, Dictionary<TKey, TValue> value)
		{
			BaseWritePrimitive(serializer, stream, value);
		}

		public static void WritePrimitive<TKey, TValue>(Serializer serializer, Stream stream, ImmutableDictionary<TKey, TValue> value)
		{
			BaseWritePrimitive(serializer, stream, value);
		}

		public static void ReadPrimitive<TKey, TValue>(Serializer serializer, Stream stream, out Dictionary<TKey, TValue> value)
		{
			var kvpArray = (KeyValuePair<TKey, TValue>[])serializer.Deserialize(stream);

			if (kvpArray == null)
			{
				value = null;
				return;
			}

			value = new Dictionary<TKey, TValue>(kvpArray.Length);

			foreach (var kvp in kvpArray)
				value.Add(kvp.Key, kvp.Value);
		}

		public static void ReadPrimitive<TKey, TValue>(Serializer serializer, Stream stream, out ImmutableDictionary<TKey, TValue> value)
		{
			ReadPrimitive(serializer, stream, out Dictionary<TKey, TValue> builder);
			if (builder == null)
			{
				value = null;
				return;
			}

			value = builder.ToImmutableDictionary();
		}
	}
}
