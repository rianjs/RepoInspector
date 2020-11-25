using System;

namespace RepoMan.Analysis.Scoring
{
    /// <summary>
    /// Full credit is given for pull requests closed in less than 2 business days, otherwise 10 points per days is deducted for every business day the pull
    /// request remains open. So if a pull request is open for 2 business days, a score of 40 is returned. If a pull request is open for 7 business days, a
    /// score of -10 is returned.
    /// </summary>
    public class BusinessDaysScorer :
        Scorer
    {
        public override double ScoreMultiplier => -10;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="businessDaysOpen"></param>
        /// <returns></returns>
        public new double GetScore(int businessDaysOpen)
        {
            if (businessDaysOpen < 0)
            {
                throw new ArgumentOutOfRangeException($"A pull request can't be open for a negative number of business days. Value: {businessDaysOpen}");
            }
            
            const int fullCreditDayLimit = 1;
            const int fullCredit = 50;
            if (businessDaysOpen <= fullCreditDayLimit)
            {
                return fullCredit;
            }
            
            var penaltyDays = businessDaysOpen - fullCreditDayLimit;
            var penalty = penaltyDays * ScoreMultiplier;
            return fullCredit + penalty;
        }
    }
}