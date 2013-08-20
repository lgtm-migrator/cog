using System;
using System.Collections.Generic;
using System.Linq;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using SIL.Cog.Applications.Services;
using SIL.Cog.Domain;
using SIL.Cog.Domain.Clusterers;
using SIL.Collections;

namespace SIL.Cog.Applications.ViewModels
{
	public class SimilarityMatrixViewModel : WorkspaceViewModelBase
	{
		private readonly IProjectService _projectService;
		private readonly IExportService _exportService;
		private readonly IAnalysisService _analysisService;
		private ReadOnlyList<SimilarityMatrixVarietyViewModel> _varieties;
		private readonly List<Variety> _modelVarieties;
		private bool _isEmpty;
		private SimilarityMetric _similarityMetric;

		public SimilarityMatrixViewModel(IProjectService projectService, IExportService exportService, IAnalysisService analysisService)
			: base("Similarity Matrix")
		{
			_projectService = projectService;
			_exportService = exportService;
			_analysisService = analysisService;
			_modelVarieties = new List<Variety>();

			_projectService.ProjectOpened += _projectService_ProjectOpened;

			Messenger.Default.Register<DomainModelChangedMessage>(this, msg => ResetVarieties());
			Messenger.Default.Register<ComparisonPerformedMessage>(this, msg => CreateSimilarityMatrix());

			TaskAreas.Add(new TaskAreaCommandGroupViewModel("Similarity metric",
				new TaskAreaCommandViewModel("Lexical", new RelayCommand(() => SimilarityMetric = SimilarityMetric.Lexical)),
				new TaskAreaCommandViewModel("Phonetic", new RelayCommand(() => SimilarityMetric = SimilarityMetric.Phonetic))));
			TaskAreas.Add(new TaskAreaItemsViewModel("Common tasks",
				new TaskAreaCommandViewModel("Perform comparison", new RelayCommand(PerformComparison))));
			TaskAreas.Add(new TaskAreaItemsViewModel("Other tasks",
				new TaskAreaCommandViewModel("Export this matrix", new RelayCommand(Export))));
		}

		private void _projectService_ProjectOpened(object sender, EventArgs e)
		{
			ResetVarieties();
			if (_projectService.Project.VarietyPairs.Count > 0)
				CreateSimilarityMatrix();
		}

		private void ResetVarieties()
		{
			if (IsEmpty)
				return;
			_modelVarieties.Clear();
			Varieties = new ReadOnlyList<SimilarityMatrixVarietyViewModel>(new SimilarityMatrixVarietyViewModel[0]);
			IsEmpty = true;
		}

		private void PerformComparison()
		{
			if (_projectService.Project.Varieties.Count == 0 || _projectService.Project.Senses.Count == 0)
				return;

			ResetVarieties();
			_analysisService.CompareAll(this);
		}

		private void Export()
		{
			if (!_isEmpty)
				_exportService.ExportSimilarityMatrix(this, _similarityMetric);
		}

		private void CreateSimilarityMatrix()
		{
			var optics = new Optics<Variety>(variety => variety.VarietyPairs.Select(pair =>
				{
					double score = 0;
					switch (_similarityMetric)
					{
						case SimilarityMetric.Lexical:
							score = pair.LexicalSimilarityScore;
							break;
						case SimilarityMetric.Phonetic:
							score = pair.PhoneticSimilarityScore;
							break;
					}
					return Tuple.Create(pair.GetOtherVariety(variety), 1.0 - score);
				}).Concat(Tuple.Create(variety, 0.0)), 2);
			_modelVarieties.AddRange(optics.ClusterOrder(_projectService.Project.Varieties).Select(oe => oe.DataObject));
			SimilarityMatrixVarietyViewModel[] vms = _modelVarieties.Select(v => new SimilarityMatrixVarietyViewModel(_similarityMetric, _modelVarieties, v)).ToArray();
			Varieties = new ReadOnlyList<SimilarityMatrixVarietyViewModel>(vms);
			IsEmpty = false;
		}

		public SimilarityMetric SimilarityMetric
		{
			get { return _similarityMetric; }
			set
			{
				if (Set(() => SimilarityMetric, ref _similarityMetric, value))
				{
					ResetVarieties();
					CreateSimilarityMatrix();
				}
			}
		}

		public bool IsEmpty
		{
			get { return _isEmpty; }
			set { Set(() => IsEmpty, ref _isEmpty, value); }
		}

		public ReadOnlyList<SimilarityMatrixVarietyViewModel> Varieties
		{
			get { return _varieties; }
			private set { Set(() => Varieties, ref _varieties, value); }
		}
	}
}
