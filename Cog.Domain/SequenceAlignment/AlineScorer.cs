﻿using System;
using System.Collections.Generic;
using System.Linq;
using SIL.Collections;
using SIL.Machine;
using SIL.Machine.FeatureModel;
using SIL.Machine.NgramModeling;
using SIL.Machine.Statistics;

namespace SIL.Cog.Domain.SequenceAlignment
{
	public class AlineScorer : IPairwiseAlignmentScorer<Word, ShapeNode>
	{
		private const int MaxSoundChangeScore = 1000;
		private const int MaxSubstitutionScore = 3500;
		private const int MaxExpansionCompressionScore = 4500;
		private const int IndelCost = 1000;
		private const int VowelCost = 0;

		private readonly SegmentPool _segmentPool;
		private readonly IReadOnlySet<SymbolicFeature> _relevantConsFeatures;
		private readonly IReadOnlySet<SymbolicFeature> _relevantVowelFeatures; 
		private readonly IReadOnlyDictionary<SymbolicFeature, int> _featureWeights;
		private readonly IReadOnlyDictionary<FeatureSymbol, int> _valueMetrics;
		private readonly SoundClass[] _contextualSoundClasses;

		public AlineScorer(SegmentPool segmentPool, IEnumerable<SymbolicFeature> relevantVowelFeatures, IEnumerable<SymbolicFeature> relevantConsFeatures,
			IDictionary<SymbolicFeature, int> featureWeights, IDictionary<FeatureSymbol, int> valueMetrics, IEnumerable<SoundClass> contextualSoundClasses)
		{
			_segmentPool = segmentPool;
			_relevantVowelFeatures = new IDBearerSet<SymbolicFeature>(relevantVowelFeatures).ToReadOnlySet();
			_relevantConsFeatures = new IDBearerSet<SymbolicFeature>(relevantConsFeatures).ToReadOnlySet();
			_featureWeights = new Dictionary<SymbolicFeature, int>(featureWeights).ToReadOnlyDictionary();
			_valueMetrics = new Dictionary<FeatureSymbol, int>(valueMetrics).ToReadOnlyDictionary();
			_contextualSoundClasses = contextualSoundClasses.ToArray();
		}

		public IReadOnlySet<SymbolicFeature> RelevantVowelFeatures
		{
			get { return _relevantVowelFeatures; }
		}

		public IReadOnlySet<SymbolicFeature> RelevantConsonantFeatures
		{
			get { return _relevantConsFeatures; }
		} 

		public IReadOnlyDictionary<SymbolicFeature, int> FeatureWeights
		{
			get { return _featureWeights; }
		}

		public IReadOnlyDictionary<FeatureSymbol, int> ValueMetrics
		{
			get { return _valueMetrics; }
		}

		public int GetGapPenalty(Word sequence1, Word sequence2)
		{
			return -IndelCost;
		}

		public int GetInsertionScore(Word sequence1, ShapeNode p, Word sequence2, ShapeNode q)
		{
			return GetSoundChangeScore(sequence1, null, p, sequence2, q, null);
		}

		public int GetDeletionScore(Word sequence1, ShapeNode p, Word sequence2, ShapeNode q)
		{
			return GetSoundChangeScore(sequence1, p, null, sequence2, null, q);
		}

		public int GetSubstitutionScore(Word sequence1, ShapeNode p, Word sequence2, ShapeNode q)
		{
			return (MaxSubstitutionScore - (Delta(p.Annotation.FeatureStruct, q.Annotation.FeatureStruct) + V(p) + V(q))) + GetSoundChangeScore(sequence1, p, null, sequence2, q, null);
		}

		public int GetExpansionScore(Word sequence1, ShapeNode p, Word sequence2, ShapeNode q1, ShapeNode q2)
		{
			return (MaxExpansionCompressionScore - (Delta(p.Annotation.FeatureStruct, q1.Annotation.FeatureStruct) + Delta(p.Annotation.FeatureStruct, q2.Annotation.FeatureStruct)
				+ V(p) + Math.Max(V(q1), V(q2)))) + GetSoundChangeScore(sequence1, p, null, sequence2, q1, q2);
		}

		public int GetCompressionScore(Word sequence1, ShapeNode p1, ShapeNode p2, Word sequence2, ShapeNode q)
		{
			return (MaxExpansionCompressionScore - (Delta(p1.Annotation.FeatureStruct, q.Annotation.FeatureStruct) + Delta(p2.Annotation.FeatureStruct, q.Annotation.FeatureStruct)
				+ V(q) + Math.Max(V(p1), V(p2)))) + GetSoundChangeScore(sequence1, p1, p2, sequence2, q, null);
		}

		public int GetMaxScore1(Word sequence1, ShapeNode p, Word sequence2)
		{
			return GetMaxScore(p) + GetMaxSoundChangeScore(sequence1, p, sequence2);
		}

		public int GetMaxScore2(Word sequence1, Word sequence2, ShapeNode q)
		{
			return GetMaxScore(q) + GetMaxSoundChangeScore(sequence2, q, sequence1);
		}

		private int GetMaxSoundChangeScore(Word word, ShapeNode node, Word otherWord)
		{
			if (word.Variety == otherWord.Variety)
				return 0;

			VarietyPair varietyPair = word.Variety.VarietyPairs[otherWord.Variety];
			if (varietyPair.SoundChangeProbabilityDistribution == null)
				return 0;

			double prob;
			if (varietyPair.Variety1 == word.Variety)
			{
				SoundContext lhs = node.ToSoundContext(_segmentPool, _contextualSoundClasses);
				prob = varietyPair.DefaultCorrespondenceProbability;
				IProbabilityDistribution<Ngram<Segment>> probDist;
				if (varietyPair.SoundChangeProbabilityDistribution.TryGetProbabilityDistribution(lhs, out probDist) && probDist.Samples.Count > 0)
					prob = probDist.Samples.Max(nseg => probDist[nseg]);
			}
			else
			{
				Ngram<Segment> corr = _segmentPool.GetExisting(node);
				prob = varietyPair.SoundChangeProbabilityDistribution.Conditions.Count == 0 ? 0
					: varietyPair.SoundChangeProbabilityDistribution.Conditions.Max(lhs => varietyPair.SoundChangeProbabilityDistribution[lhs][corr]);
			}
			return (int) (MaxSoundChangeScore * prob);
		}

		private int GetMaxScore(ShapeNode node)
		{
			return MaxSubstitutionScore - (V(node) * 2);
		}

		public int Delta(FeatureStruct fs1, FeatureStruct fs2)
		{
			IEnumerable<SymbolicFeature> features = ((FeatureSymbol) fs1.GetValue(CogFeatureSystem.Type)) == CogFeatureSystem.VowelType
				&& ((FeatureSymbol) fs2.GetValue(CogFeatureSystem.Type)) == CogFeatureSystem.VowelType
				? _relevantVowelFeatures : _relevantConsFeatures;

			return features.Aggregate(0, (val, feat) => val + (Diff(fs1, fs2, feat) * _featureWeights[feat]));
		}

		private int Diff(FeatureStruct fs1, FeatureStruct fs2, SymbolicFeature feature)
		{
			SymbolicFeatureValue pValue;
			if (!fs1.TryGetValue(feature, out pValue))
				return 0;
			SymbolicFeatureValue qValue;
			if (!fs2.TryGetValue(feature, out qValue))
				return 0;

			int sum = 0;
			int count = 0;
			foreach (FeatureSymbol pSymbol in pValue.Values)
			{
				foreach (FeatureSymbol qSymbol in qValue.Values)
				{
					sum += Math.Abs(_valueMetrics[pSymbol] - _valueMetrics[qSymbol]);
					count++;
				}
			}

			return sum / count;
		}

		private int V(ShapeNode node)
		{
			return node.Annotation.Type() == CogFeatureSystem.VowelType ? VowelCost : 0;
		}

		private int GetSoundChangeScore(Word sequence1, ShapeNode p1, ShapeNode p2, Word sequence2, ShapeNode q1, ShapeNode q2)
		{
			if (sequence1.Variety == sequence2.Variety)
				return 0;

			VarietyPair varietyPair = sequence1.Variety.VarietyPairs[sequence2.Variety];

			if (varietyPair.SoundChangeProbabilityDistribution == null)
				return 0;

			if (sequence1.Variety == varietyPair.Variety2)
			{
				ShapeNode tempNode = p1;
				p1 = q1;
				q1 = tempNode;

				tempNode = p2;
				p2 = q2;
				q2 = tempNode;
			}

			Ngram<Segment> target;
			if (p1 == null)
			{
				target = new Ngram<Segment>();
			}
			else
			{
				Segment targetSegment = _segmentPool.GetExisting(p1);
				target = p2 == null ? targetSegment : new Ngram<Segment>(targetSegment, _segmentPool.GetExisting(p2));
			}

			Ngram<Segment> corr;
			if (q1 == null)
			{
				corr = new Ngram<Segment>();
			}
			else
			{
				Segment corrSegment = _segmentPool.GetExisting(q1);
				corr = q2 == null ? corrSegment : new Ngram<Segment>(corrSegment, _segmentPool.GetExisting(q2));
			}

			ShapeNode leftNode = p1 == null ? p2 : p1.GetPrev(NodeFilter);
			SoundClass leftEnv;
			if (leftNode == null || !_contextualSoundClasses.TryGetMatchingSoundClass(_segmentPool, leftNode, out leftEnv))
				leftEnv = null;
			ShapeNode pRight = p2 ?? p1;
			ShapeNode rightNode = pRight == null ? null : pRight.GetNext(NodeFilter);
			SoundClass rightEnv;
			if (rightNode == null || !_contextualSoundClasses.TryGetMatchingSoundClass(_segmentPool, rightNode, out rightEnv))
				rightEnv = null;

			var lhs = new SoundContext(leftEnv, target, rightEnv);
			IProbabilityDistribution<Ngram<Segment>> probDist;
			double prob = varietyPair.SoundChangeProbabilityDistribution.TryGetProbabilityDistribution(lhs, out probDist) ? probDist[corr]
				: varietyPair.DefaultCorrespondenceProbability;
			return (int) (MaxSoundChangeScore * prob);
		}

		private static bool NodeFilter(ShapeNode node)
		{
			return node.Type().IsOneOf(CogFeatureSystem.ConsonantType, CogFeatureSystem.VowelType, CogFeatureSystem.AnchorType);
		}
	}
}
