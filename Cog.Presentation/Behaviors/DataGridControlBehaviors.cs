﻿using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;
using Xceed.Wpf.DataGrid;
using Xceed.Wpf.DataGrid.Views;

namespace SIL.Cog.Presentation.Behaviors
{
	public static class DataGridControlBehaviors
	{
		public static readonly DependencyProperty IsRowHeaderProperty = DependencyProperty.RegisterAttached("IsRowHeader", typeof(bool), typeof(DataGridControlBehaviors),
			new UIPropertyMetadata(false, OnIsRowHeaderChanged));

		private static void OnIsRowHeaderChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
		{
			var c = (ColumnBase) obj;
			if ((bool) e.NewValue)
			{
				c.ReadOnly = true;
				c.CanBeCurrentWhenReadOnly = false;
				c.AllowSort = false;
			}
		}

		public static void SetIsRowHeader(ColumnBase column, bool value)
		{
			column.SetValue(IsRowHeaderProperty, value);
		}

		public static bool GetIsRowHeader(ColumnBase column)
		{
			return (bool) column.GetValue(IsRowHeaderProperty);
		}

		private static readonly DependencyPropertyKey MergedHeadersPropertyKey = DependencyProperty.RegisterAttachedReadOnly("MergedHeaders", typeof(ObservableCollection<MergedHeader>),
			typeof(DataGridControlBehaviors), new FrameworkPropertyMetadata(new ObservableCollection<MergedHeader>()));

		public static readonly DependencyProperty MergedHeadersProperty = MergedHeadersPropertyKey.DependencyProperty;

		public static ObservableCollection<MergedHeader> GetMergedHeaders(DataGridControl dataGrid)
		{
			return (ObservableCollection<MergedHeader>) dataGrid.GetValue(MergedHeadersProperty);
		}

		public static readonly DependencyProperty IsRowVirtualizationEnabledProperty = DependencyProperty.RegisterAttached("IsRowVirtualizationEnabled", typeof(bool), typeof(DataGridControlBehaviors),
			new UIPropertyMetadata(true, OnIsRowVirtualizationEnabledChanged));

		private static void OnIsRowVirtualizationEnabledChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
		{
			var dataGrid = (DataGridControl) obj;
			if (dataGrid.IsLoaded)
			{
				SetRowVirtualization(dataGrid);
			}
			else
			{
				if (!((bool) e.NewValue))
					dataGrid.Loaded += dataGrid_Loaded;
				else
					dataGrid.Loaded -= dataGrid_Loaded;
			}
		}

		private static void dataGrid_Loaded(object sender, RoutedEventArgs e)
		{
			var dataGrid = (DataGridControl) sender;
			SetRowVirtualization(dataGrid);
			dataGrid.Loaded -= dataGrid_Loaded;
		}

		private static void SetRowVirtualization(DataGridControl dataGrid)
		{
			var scrollViewer = (TableViewScrollViewer) dataGrid.Template.FindName("PART_ScrollViewer", dataGrid);
			scrollViewer.CanContentScroll = GetIsRowVirtualizationEnabled(dataGrid);
		}

		public static void SetIsRowVirtualizationEnabled(DataGridControl dataGrid, bool value)
		{
			dataGrid.SetValue(IsRowVirtualizationEnabledProperty, value);
		}

		public static bool GetIsRowVirtualizationEnabled(DataGridControl dataGrid)
		{
			return (bool) dataGrid.GetValue(IsRowVirtualizationEnabledProperty);
		}

		public static readonly DependencyProperty AutoScrollOnSelectionProperty =
			DependencyProperty.RegisterAttached(
				"AutoScrollOnSelection", 
				typeof(bool), 
				typeof(DataGridControlBehaviors), 
				new UIPropertyMetadata(false, OnAutoScrollOnSelectionChanged));

		private static void OnAutoScrollOnSelectionChanged(DependencyObject depObj, DependencyPropertyChangedEventArgs e)
		{
			var dataGrid = depObj as DataGridControl;
			if (dataGrid == null)
				return;

			if (!(e.NewValue is bool))
				return;

			if ((bool) e.NewValue)
				dataGrid.SelectionChanged += DataGrid_SelectionChanged;
			else
				dataGrid.SelectionChanged -= DataGrid_SelectionChanged;
		}

		private static void DataGrid_SelectionChanged(object sender, DataGridSelectionChangedEventArgs e)
		{
			var dataGrid = (DataGridControl) sender;
			if (dataGrid != null && dataGrid.IsLoaded && dataGrid.SelectedItem != null)
			{
				dataGrid.Focus();
				dataGrid.Dispatcher.BeginInvoke(new Action(() => dataGrid.BringItemIntoView(dataGrid.SelectedItem)), DispatcherPriority.Background);
			}
		}

		public static bool GetAutoScrollOnSelection(DataGridControl datagrid)
		{
			return (bool) datagrid.GetValue(AutoScrollOnSelectionProperty);
		}

		public static void SetAutoScrollOnSelection(DataGridControl datagrid, bool value)
		{
			datagrid.SetValue(AutoScrollOnSelectionProperty, value);
		}

		public static readonly DependencyProperty IsInitialSelectionDisabledProperty =
			DependencyProperty.RegisterAttached(
				"IsInitialSelectionDisabled", 
				typeof(bool), 
				typeof(DataGridControlBehaviors), 
				new UIPropertyMetadata(false, OnIsInitialSelectionDisabledChanged));

		private static void OnIsInitialSelectionDisabledChanged(DependencyObject depObj, DependencyPropertyChangedEventArgs e)
		{
			var dataGrid = depObj as DataGridControl;
			if (dataGrid == null)
				return;

			if (!(e.NewValue is bool))
				return;

			if ((bool) e.NewValue)
				dataGrid.ItemsSourceChangeCompleted += DataGrid_ItemsSourceChangeCompleted;
			else
				dataGrid.ItemsSourceChangeCompleted -= DataGrid_ItemsSourceChangeCompleted;
		}

		private static void DataGrid_ItemsSourceChangeCompleted(object sender, EventArgs e)
		{
			var dataGrid = (DataGridControl) sender;
			dataGrid.SelectedItem = null;
		}

		public static void SetIsInitialSelectionDisabled(DataGridControl dataGrid, bool value)
		{
			dataGrid.SetValue(IsInitialSelectionDisabledProperty, value);
		}

		public static bool GetIsInitialSelectionDisabled(DataGridControl dataGrid)
		{
			return (bool) dataGrid.GetValue(IsInitialSelectionDisabledProperty);
		}
	}
}
