﻿using UnityEngine;
using UnityEngine.EventSystems;
using UnityUtils.PropertyAttributes;
using UnityUtils.UI.Selectable.Groups;

namespace UnityUtils.UI.Selectable
{
	public delegate void OnPointerDelegate(PointerEventData data);

	public class Button : UnityEngine.UI.Button, ISelectableInput
	{
		public event OnPointerDelegate PointerDownEvent;
		public event OnPointerDelegate PointerUpEvent;
		public event OnPointerDelegate PointerEnterEvent;
		public event OnPointerDelegate PointerExitEvent;
		public event OnPointerDelegate PointerPressEvent;

		[field: SerializeField]
		public int Id { get; set; }

		public ISelectableGroup Group
		{
			get => _group ?? group;
			set => _group = value;
		}

		[SerializeField]
		private SingleButtonGroup group;
		[SerializeReference]
		private ISelectableGroup _group;

		[field: SerializeField]
		public bool IsToggle { get; private set; }

		[SerializeReference, Polymorphic]
		[InspectorName("Events")]
		private IButtonAnimations[] animations;

		protected override void OnDestroy()
		{
			base.OnDestroy();
			PointerDownEvent = null;
			PointerUpEvent = null;
			PointerEnterEvent = null;
			PointerExitEvent = null;
			PointerPressEvent = null;
			onClick.RemoveAllListeners();
		}

		public override void OnPointerDown(PointerEventData eventData)
		{
			base.OnPointerDown(eventData);
			PointerDownEvent?.Invoke(eventData);
		}
		public override void OnPointerUp(PointerEventData eventData)
		{
			base.OnPointerUp(eventData);
			PointerUpEvent?.Invoke(eventData);
		}
		public override void OnPointerEnter(PointerEventData eventData)
		{
			base.OnPointerEnter(eventData);
			PointerEnterEvent?.Invoke(eventData);
			//force highlight, when "selected" base ignores state change; smh
			UpdateAnimations(ButtonState.Highlighted, true);
		}
		public override void OnPointerExit(PointerEventData eventData)
		{
			base.OnPointerExit(eventData);
			PointerExitEvent?.Invoke(eventData);
		}
		public override void OnPointerClick(PointerEventData eventData)
		{
			base.OnPointerClick(eventData);
			PointerPressEvent?.Invoke(eventData);
			Group?.Toggle(this);
		}
		protected override void DoStateTransition(SelectionState state, bool instant)
		{
			if (state == SelectionState.Selected && !IsToggle)
				return;

			base.DoStateTransition(state, instant);
			UpdateAnimations(state switch
			{
				SelectionState.Highlighted => ButtonState.Highlighted,
				SelectionState.Pressed => ButtonState.Pressed,
				SelectionState.Selected => ButtonState.Selected,
				SelectionState.Disabled => ButtonState.Disabled,
				_ => ButtonState.Normal
			}, !instant);
		}
		public virtual void OnGroupSelected() 
		{
			UpdateAnimations(ButtonState.GroupSelected, true);
		}
		public virtual void OnGroupDeselected() 
		{
			UpdateAnimations(ButtonState.GroupDeselected, true);
		}

		private void UpdateAnimations(ButtonState state, bool animate)
		{
			if (animations == null || !Application.isPlaying)
				return;

			for (int i = 0; i < animations.Length; i++)
			{
				IButtonAnimations animation = animations[i];
				animation?.DoStateTransition(state, animate);
			}
		}
	}
}
