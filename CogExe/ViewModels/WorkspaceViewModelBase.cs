﻿using System;
using System.Collections.ObjectModel;

namespace SIL.Cog.ViewModels
{
	public abstract class WorkspaceViewModelBase : InitializableViewModelBase
	{
		private readonly ObservableCollection<CogViewModelBase> _taskAreas; 

		protected WorkspaceViewModelBase(string displayName)
			: base(displayName)
		{
			_taskAreas = new ObservableCollection<CogViewModelBase>();
		}

		public ObservableCollection<CogViewModelBase> TaskAreas
		{
			get { return _taskAreas; }
		}

		public override bool SwitchView(Type viewType, object model)
		{
			return viewType == GetType();
		}
	}
}
