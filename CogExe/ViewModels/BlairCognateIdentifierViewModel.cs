﻿using System.Diagnostics;
using SIL.Cog.Processors;
using SIL.Cog.Services;

namespace SIL.Cog.ViewModels
{
	public class BlairCognateIdentifierViewModel : ComponentSettingsViewModelBase
	{
		private bool _ignoreRegularInsertionDeletion;
		private bool _regularConsEqual;
		private readonly ComponentOptionsViewModel _similarSegments;

		public BlairCognateIdentifierViewModel(IDialogService dialogService, IImportService importService, CogProject project)
			: base("Blair", project)
		{
			_similarSegments = new ComponentOptionsViewModel("Similar segments", "Type", project, 0,
				new ThresholdSimilarSegmentIdentifierViewModel(Project), new ListSimilarSegmentIdentifierViewModel(dialogService, importService, Project));
			_similarSegments.PropertyChanged += ChildPropertyChanged;
		}

		public BlairCognateIdentifierViewModel(IDialogService dialogService, IImportService importService, CogProject project, BlairCognateIdentifier cognateIdentifier)
			: base("Blair", project)
		{
			_ignoreRegularInsertionDeletion = cognateIdentifier.IgnoreRegularInsertionDeletion;
			_regularConsEqual = cognateIdentifier.RegularConsonantEqual;

			IProcessor<VarietyPair> similarSegments = Project.VarietyPairProcessors["similarSegmentIdentifier"];
			if (similarSegments is ThresholdSimilarSegmentIdentifier)
			{
				_similarSegments = new ComponentOptionsViewModel("Similar segments", "Type", project, 0,
					new ThresholdSimilarSegmentIdentifierViewModel(Project, (ThresholdSimilarSegmentIdentifier) similarSegments),
					new ListSimilarSegmentIdentifierViewModel(dialogService, importService, Project));
			}
			else if (similarSegments is ListSimilarSegmentIdentifier)
			{
				_similarSegments = new ComponentOptionsViewModel("Similar segments", "Type", project, 1,
					new ThresholdSimilarSegmentIdentifierViewModel(Project),
					new ListSimilarSegmentIdentifierViewModel(dialogService, importService, Project, (ListSimilarSegmentIdentifier) similarSegments));
			}
			Debug.Assert(_similarSegments != null);
			_similarSegments.PropertyChanged += ChildPropertyChanged;
		}

		public override void AcceptChanges()
		{
			base.AcceptChanges();
			_similarSegments.AcceptChanges();
		}

		public bool IgnoreRegularInsertionDeletion
		{
			get { return _ignoreRegularInsertionDeletion; }
			set
			{
				Set(() => IgnoreRegularInsertionDeletion, ref _ignoreRegularInsertionDeletion, value);
				IsChanged = true;
			}
		}

		public bool RegularConsonantsEqual
		{
			get { return _regularConsEqual; }
			set
			{
				Set(() => RegularConsonantsEqual, ref _regularConsEqual, value);
				IsChanged = true;
			}
		}

		public ComponentOptionsViewModel SimilarSegments
		{
			get { return _similarSegments; }
		}

		public override void UpdateComponent()
		{
			_similarSegments.UpdateComponent();
			Project.VarietyPairProcessors["cognateIdentifier"] = new BlairCognateIdentifier(Project,
				_ignoreRegularInsertionDeletion, _regularConsEqual, "primary");
		}
	}
}
