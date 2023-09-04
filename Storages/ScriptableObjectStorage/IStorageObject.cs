﻿using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityUtils.Storages
{
	public abstract class StorageObject : ScriptableObject
	{
		public abstract Type TableType { get; }
	}

	public interface IStorageObject<TKey, TValue>
	{
		TValue this[TKey key] { get; set; }
		void Delete(TKey key);
	}

	public class StorageObject<TKey, TValue> : StorageObject, IStorageObject<TKey, TValue>
	{
		public override Type TableType => typeof(TValue);

		public TValue this[TKey key] 
		{ 
			get
			{
				return values.TryGetValue(key, out TValue value) ? value : default;
			}
			set
			{
				if (value == null)
				{
					Delete(key);
				}
				else
				{
					values[key] = value;
				}
			}
		}

		[SerializeField]
		private Serialized.Dictionary<TKey, TValue> values;

		public void Delete(TKey key) => values.Remove(key);
	}
}
