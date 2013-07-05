﻿using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Shapes;
using SIL.Cog.ViewModels;
using SIL.Collections;

namespace SIL.Cog.Views
{
	/// <summary>
	/// Interaction logic for WordsView.xaml
	/// </summary>
	public partial class WordsView
	{
		private readonly SimpleMonitor _monitor;

		public WordsView()
		{
			InitializeComponent();
			_monitor = new SimpleMonitor();
		}

		private void WordsView_OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			var vm = DataContext as WordsViewModel;
			if (vm == null)
				return;

			vm.SelectedWords.CollectionChanged += SelectedWords_CollectionChanged;
		}

		private void SelectedWords_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			var vm = (WordsViewModel) DataContext;
			if (_monitor.Busy)
				return;

			using (_monitor.Enter())
			{
				foreach (WordViewModel word in WordsListBox.SelectedItems.Cast<WordViewModel>().Except(vm.SelectedWords))
					ClearWordSelection(word);
				WordsListBox.SelectedItems.Clear();
				foreach (WordViewModel word in vm.SelectedWords)
					WordsListBox.SelectedItems.Add(word);
				if (vm.SelectedWords.Count > 0)
					WordsListBox.ScrollIntoView(vm.SelectedWords[0]);
			}
		}

		private void WordsListBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var vm = (WordsViewModel) DataContext;
			if (_monitor.Busy)
				return;

			using (_monitor.Enter())
			{
				foreach (WordViewModel word in e.RemovedItems)
				{
					ClearWordSelection(word);
					vm.SelectedWords.Remove(word);
				}
				foreach (WordViewModel word in e.AddedItems)
					vm.SelectedWords.Add(word);
			}
		}

		private void ClearWordSelection(WordViewModel word)
		{
			var item = (ListBoxItem) WordsListBox.ItemContainerGenerator.ContainerFromItem(word);
			if (item != null)
			{
				var wordListBox = item.FindVisualChild<ListBox>();
				if (wordListBox != null)
					wordListBox.UnselectAll();
			}
		}

		private void MarkerClicked(object sender, MouseButtonEventArgs e)
		{
			var rect = (Rectangle) sender;
			ScrollToWordPair((WordViewModel) rect.DataContext, ScrollViewer, WordsListBox);
		}

		private void ScrollToWordPair(WordViewModel wordPair, ScrollViewer sv, ItemsControl ic)
		{
			var cp = (FrameworkElement) ic.ItemContainerGenerator.ContainerFromItem(wordPair);
			var point = cp.TransformToAncestor(ic).Transform(new Point());
			sv.ScrollToVerticalOffset((point.Y + (cp.ActualHeight / 2)) - (sv.ActualHeight / 2));
		}

		private void Copy_OnExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			var vm = (WordsViewModel) DataContext;
			Clipboard.SetText(vm.SelectedWordsText);
		}

		private void SelectAll_OnExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			WordsListBox.SelectAll();
		}
	}
}
