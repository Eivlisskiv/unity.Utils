﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Utilities.Reflection;

namespace UnityUtils.PropertyAttributes
{
	public class PolymorphicAttribute : PropertyAttribute
	{
		private static readonly Dictionary<Type, Type[]> subTypes = new Dictionary<Type, Type[]>();

		public readonly bool nullable;
		public string[] options { get; private set; }
		public Type baseType { get; private set; }
		public bool WasInitialized => types != null;
		private Type[] types;
		private ConstructorInfo[] constructors;

		public Type SelectedType => _index == -1 ? null : types?[_index];
		public int Index
		{
			get => nullable ? _index + 1 : _index;
			set => _index = nullable ? value - 1 : value;
		}

		private int _index;

		private object parent;
		private FieldInfo field;

		/// <summary>
		/// Uses the field's type as base type.
		/// </summary>
		/// <param name="nullable">Can the instance be null.</param>
		public PolymorphicAttribute(bool nullable = false)
		{
			this.nullable = nullable;
		}

		/// <summary>
		/// Defined base type.
		/// </summary>
		/// <param name="baseType">The base type the polymorphic objects must extend.</param>
		/// <param name="nullable">Can the instance be null.</param>
		public PolymorphicAttribute(Type baseType, bool nullable = false) : this(nullable)
		{
			Init(baseType);
		}

		private void Init(Type baseType)
		{
			this.baseType = baseType;
			Type unityType = typeof(UnityEngine.Object);
			types = subTypes.TryGetValue(baseType, out var _types) ? _types :
				subTypes[baseType] = baseType.GetImplementations().Where(t => !unityType.IsAssignableFrom(t)).ToArray();

			int k = 0;
			options = new string[types.Length + (nullable ? 1 : 0)];
			if (nullable)
				options[k++] = "Null Reference";
			for (int i = 0; i < types.Length; i++, k++)
				options[k] = types[i].Name;

			constructors = types.Select(t =>
				t.GetConstructor(new Type[] { baseType }) ?? t.GetConstructor(new Type[0])
			).ToArray();
		}

		public void SetFieldInfo(object parent, FieldInfo field, int listIndex)
		{
			this.parent = parent;
			this.field = field;

			if (types == null) //did not init;
			{
				Type baseType = field.FieldType;
				
				if (baseType.IsArray)
				{
					baseType = baseType.GetElementType();
				}
				else if (baseType.IsGenericType) //Generic collections
				{
					Type[] generics = baseType.GetGenericArguments();
					baseType = generics[0];
				}

				Init(baseType);
			}

			object currentValue = GetFieldValue(listIndex);

			_index = currentValue == null ? -1 : Array.IndexOf(types, currentValue.GetType());
		}

		public bool ChangeIndex(int typeIndex, int listIndex, bool setField, out object value)
		{
			if (typeIndex == Index)
			{
				value = null;
				return false;
			}

			Index = typeIndex;

			if (_index == -1)
			{
				value = null;
				if (setField) 
				{ 
					SetFieldValue(value, listIndex);
				}

				return true;
			}

			value = ChangeType(types[_index], listIndex, setField);
			return true;
		}

		private object ChangeType(Type type, int listIndex, bool setValue)
		{
			if (field == null) 
				return null;

			object currentValue = GetFieldValue(listIndex);

			//Type was not truly changed;
			if (currentValue?.GetType() == type) 
				return currentValue;

			//Will use reference
			if (type.Inherits(typeof(UnityEngine.Object)))
			{
				var generic = typeof(PolymorphicUnityObjectReference<>);
				var polyType = generic.BuildGeneric(type);
				return Activator.CreateInstance(polyType);
			}

			object value = Create(type, currentValue);
			if (setValue)
				SetFieldValue(value, listIndex);
			return value;
		}

		private object Create(Type type, object currentValue)
		{
			if (type.IsStruct())
			{
				return Activator.CreateInstance(type);
			}

			ConstructorInfo constructorInfo = constructors[_index];
			if (constructorInfo == null)
			{
				Debug.LogWarning(string.Format("{0} does not have an empty constructor or a constructor with a {1} parameter.",
					type.Name, baseType.Name));
				return null;
			}

			return constructorInfo.GetParameters().Length == 0
				? constructorInfo.Invoke(new object[] { })
				: constructorInfo.Invoke(new object[] { currentValue });
		}

		public object GetFieldValue(int listIndex)
		{
			object value = field.GetValue(parent);
			if (listIndex != -1 && value is IList list)
				return listIndex < list.Count ? list[listIndex] : null;
			return value;
		}

		private void SetFieldValue(object value, int listIndex)
		{
			if (listIndex == -1)
			{
				field.SetValue(parent, value);
				return;
			}

			if (field.GetValue(parent) is IList list)
				list[listIndex] = value;
		}
	}
}
