﻿namespace Ecng.Xaml
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Linq;
	using System.Reflection;
	using System.Windows;
	using System.Windows.Controls;
	using System.Windows.Controls.Primitives;

	using Ecng.Common;
	using Ecng.Reflection;

	public partial class EntityPropertyEditor
	{
		/// <summary>
		/// <see cref="DependencyProperty"/> для <see cref="SelectedValue"/>.
		/// </summary>
		public static readonly DependencyProperty SelectedValueProperty = DependencyProperty.Register("SelectedValue", typeof(EntityProperty), typeof(EntityPropertyEditor),
			new PropertyMetadata(null, SelectedValueChanged));

		private static void SelectedValueChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
		{
			var value = (EntityProperty)args.NewValue;
			var ctrl = (EntityPropertyEditor)sender;

			ctrl.SelectedPropertyName = value?.Name;
		}

		/// <summary>
		/// Выбранное свойство.
		/// </summary>
		public EntityProperty SelectedValue
		{
			get { return (EntityProperty)GetValue(SelectedValueProperty); }
			set { SetValue(SelectedValueProperty, value); }
		}

		/// <summary>
		/// <see cref="DependencyProperty"/> для <see cref="SelectedPropertyName"/>.
		/// </summary>
		public static readonly DependencyProperty SelectedPropertyNameProperty = DependencyProperty.Register("SelectedPropertyName", typeof(string), typeof(EntityPropertyEditor),
			new PropertyMetadata(null, SelectedPropertyNameChanged));

		private static void SelectedPropertyNameChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
		{
			var value = (string)args.NewValue;
			var ctrl = (EntityPropertyEditor)sender;

			ctrl.SelectItem(value);
		}

		/// <summary>
		/// Имя выбранного свойства.
		/// </summary>
		public string SelectedPropertyName
		{
			get { return (string)GetValue(SelectedPropertyNameProperty); }
			set { SetValue(SelectedPropertyNameProperty, value); }
		}

		/// <summary>
		/// <see cref="DependencyProperty"/> для <see cref="SelectedValue"/>.
		/// </summary>
		public static readonly DependencyProperty ItemsProperty = DependencyProperty.Register("Items", typeof(IList<EntityProperty>), typeof(EntityPropertyEditor),
			new FrameworkPropertyMetadata(null, ItemsChanged));

		private static void ItemsChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
		{
			var value = (IEnumerable)args.NewValue;
			var ctrl = (EntityPropertyEditor)sender;

			ctrl.TreeView.ItemsSource = value;
		}

		/// <summary>
		/// Источник данных.
		/// </summary>
		public IList<EntityProperty> Items
		{
			get { return (IList<EntityProperty>)GetValue(ItemsProperty); }
			set { SetValue(ItemsProperty, value); }
		}

		public EntityPropertyEditor()
		{
			InitializeComponent();
			TreeView.ItemContainerGenerator.StatusChanged += OnItemContainerGeneratorStatusChanged;
        }

		private void TreeView_OnSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
		{
			SelectedValue = (EntityProperty)e.NewValue;
		}

		private void OnItemContainerGeneratorStatusChanged(object sender, EventArgs e)
		{
			if (TreeView.ItemContainerGenerator.Status != GeneratorStatus.ContainersGenerated)
				return;

			if (SelectedValue == null)
				return;

			var element = TreeView.ItemContainerGenerator.ContainerFromItem(SelectedValue);

			if (element != null)
				((TreeViewItem)element).IsSelected = true;
		}

		private void SelectItem(string name)
		{
			EntityProperty item = null;
			IEnumerable<EntityProperty> items = Items;

			string propName = null;

			foreach (var part in name.Split('.'))
			{
				propName = propName == null ? part : propName + "." + part;

				item = items.FirstOrDefault(i => i.Name == propName);

				if (item == null)
					break;

				items = item.Properties;
			}

			SelectedValue = item;

			var element = TreeView.ItemContainerGenerator.ContainerFromItem(item);

			if (element != null)
				((TreeViewItem)element).IsSelected = true;
		}
	}

	public class EntityProperty
	{
		public string Name { get; set; }

		public string DisplayName { get; set; }

		public EntityProperty Parent { get; set; }

		public IEnumerable<EntityProperty> Properties { get; set; }

		public string FullDisplayName => Parent == null ? DisplayName : "{0} -> {1}".Put(Parent.FullDisplayName, DisplayName);

		public override string ToString()
		{
			return "{0} ({1})".Put(Name, FullDisplayName);
		}
	}

	public static class EntityPropertyHelper
	{
		public static List<EntityProperty> GetEntityProperties(this Type initialType, Func<PropertyInfo, bool> filter = null)
		{
			if (initialType == null)
				throw new ArgumentNullException(nameof(initialType));

			List<EntityProperty> rootProperties = null;

			var processed = new HashSet<Type>();
			var stack = new Stack<Tuple<Type, EntityProperty>>();
			var root = Tuple.Create(initialType, (EntityProperty)null);

			filter = filter ?? (p => true);

			stack.Push(root);

			while (stack.Count > 0)
			{
				var item = stack.Pop();
				var type = item.Item1;
				var parent = item.Item2;

				if (processed.Contains(type))
					continue;

				var properties = new List<EntityProperty>();

				var propertyInfos = type
					.GetMembers<PropertyInfo>(BindingFlags.Public | BindingFlags.Instance)
					.Where(filter);

				foreach (var pi in propertyInfos)
				{
					var nameAttr = pi.GetAttribute<DisplayNameAttribute>();
					var displayName = nameAttr == null ? pi.Name : nameAttr.DisplayName;

					var prop = new EntityProperty
					{
						Name = (parent != null ? parent.Name + "." : string.Empty) + pi.Name,
						Parent = parent,
						DisplayName = displayName,
					};

					if (!pi.PropertyType.IsPrimitive() && !pi.PropertyType.IsNullable())
					{
						stack.Push(Tuple.Create(pi.PropertyType, prop));
					}

					properties.Add(prop);
				}

				processed.Add(type);

				if (parent != null)
				{
					parent.Properties = properties;
				}
				else
					rootProperties = properties;
			}

			return rootProperties;
		}

		public static object GetPropValue(this object entity, string name)
		{
			var value = entity;

			foreach (var part in name.Split('.'))
			{
				var info = value?.GetType().GetProperty(part);

				if (info == null)
					return null;

				value = info.GetValue(value, null);
			}

			return value;
		}
	}
}
