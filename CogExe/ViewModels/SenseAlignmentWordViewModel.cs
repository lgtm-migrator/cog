﻿using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using GalaSoft.MvvmLight;
using SIL.Machine;

namespace SIL.Cog.ViewModels
{
	public class SenseAlignmentWordViewModel : ViewModelBase
	{
		private readonly VarietyViewModel _variety;
		private readonly string _prefix;
		private readonly ReadOnlyCollection<string> _columns;
		private readonly string _suffix;

		public SenseAlignmentWordViewModel(Word word, AlignmentCell<ShapeNode> prefix, IEnumerable<AlignmentCell<ShapeNode>> columns, AlignmentCell<ShapeNode> suffix)
		{
			_variety = new VarietyViewModel(word.Variety);
			_prefix = prefix.StrRep();
			_columns = new ReadOnlyCollection<string>(columns.Select(cell => cell.IsNull ? "-" : cell.StrRep()).ToArray());
			_suffix = suffix.StrRep();
		}

		public VarietyViewModel Variety
		{
			get { return _variety; }
		}

		public string Prefix
		{
			get { return _prefix; }
		}

		public ReadOnlyCollection<string> Columns
		{
			get { return _columns; }
		}

		public string Suffix
		{
			get { return _suffix; }
		}
	}
}
