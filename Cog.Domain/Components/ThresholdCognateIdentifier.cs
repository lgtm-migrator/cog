using SIL.Machine.Annotations;
using SIL.Machine.SequenceAlignment;

namespace SIL.Cog.Domain.Components
{
	public class ThresholdCognateIdentifier : ICognateIdentifier
	{
		private readonly double _threshold;

		public ThresholdCognateIdentifier(double threshold)
		{
			_threshold = threshold;
		}

		public double Threshold
		{
			get { return _threshold; }
		}

		public void UpdateCognicity(WordPair wordPair, IWordAlignerResult alignerResult)
		{
			int alignmentCount = 0;
			double totalAlignmentScore = 0.0;
			foreach (Alignment<Word, ShapeNode> alignment in alignerResult.GetAlignments())
			{
				totalAlignmentScore += alignment.NormalizedScore;
				alignmentCount++;
			}
			wordPair.CognicityScore = totalAlignmentScore / alignmentCount;
			wordPair.AreCognatePredicted = wordPair.CognicityScore >= _threshold;
		}
	}
}
