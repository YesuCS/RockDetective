using System;
using System.Collections.Generic;

using com.yesuchum.RockDetective.Enums;

namespace com.yesuchum.RockDetective.Method
{
    public class PhaseState
    {
        public PhaseProgress Progress { get; set; }

        public DateTime? CompletedDateTime { get; set; }

        public string Summary { get; set; }

        public Dictionary<string, string> Data { get; set; } = new Dictionary<string, string>();
    }

    public enum PhaseProgress
    {
        NotStarted = 0,
        InProgress = 1,
        Complete = 2,
        Skipped = 3
    }

    public static class PhaseStateKey
    {
        public const string ChampionWaived = "ChampionWaived";
        public const string ChampionWaiveReason = "ChampionWaiveReason";
        public const string AttestNoUnverifiedDetails = "AttestNoUnverifiedDetails";
        public const string AttestNothingNotGermane = "AttestNothingNotGermane";
        public const string ClassifyScoreImpact = "ClassifyScoreImpact";
        public const string ClassifyScoreUrgency = "ClassifyScoreUrgency";
        public const string ClassifyScoreRecurrence = "ClassifyScoreRecurrence";
        public const string ClassifyScoreScope = "ClassifyScoreScope";
        public const string ChampionSignOffDateTime = "ChampionSignOffDateTime";
        public const string ChampionSignOffPersonAliasId = "ChampionSignOffPersonAliasId";
        public const string LoopBackCount = "LoopBackCount";
        public const string LastLoopBackDateTime = "LastLoopBackDateTime";
    }

    public class CasePhaseStates : Dictionary<CasePhase, PhaseState>
    {
        public PhaseState GetState( CasePhase phase )
        {
            if ( !ContainsKey( phase ) )
            {
                this[phase] = new PhaseState();
            }

            return this[phase];
        }
    }
}
