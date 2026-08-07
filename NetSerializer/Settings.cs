/*
 * Copyright 2016 Tomi Valkeinen
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Collections.Generic;

namespace NetSerializer
{
	public class Settings
	{
		/// <summary>
		/// Maximum number of elements allowed in a deserialized collection.
		/// </summary>
		/// <remarks>
		/// This defends network deserialization from allocating attacker-controlled
		/// arrays, lists, dictionaries, and similar collection types.
		/// </remarks>
		public uint MaxCollectionLength = 1024 * 1024;

		/// <summary>
		/// Maximum number of bytes allowed in a deserialized byte array.
		/// </summary>
		public uint MaxByteArrayLength = 16 * 1024 * 1024;

		/// <summary>
		/// Maximum number of UTF-16 code units allowed in a deserialized string.
		/// </summary>
		public uint MaxStringLength = 16 * 1024 * 1024;

		/// <summary>
		/// Array of custom TypeSerializers
		/// </summary>
		public ITypeSerializer[] CustomTypeSerializers = new ITypeSerializer[0];

		/// <summary>
		/// Support IDeserializationCallback
		/// </summary>
		public bool SupportIDeserializationCallback = false;

		/// <summary>
		/// Support OnSerializing, OnSerialized, OnDeserializing, OnDeserialized attributes
		/// </summary>
		public bool SupportSerializationCallbacks = false;
	}
}
