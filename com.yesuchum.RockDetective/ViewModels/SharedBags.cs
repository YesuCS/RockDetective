using System;
using System.Collections.Generic;

namespace com.yesuchum.RockDetective.ViewModels
{
    public class CategoryOptionBag
    {
        public string Guid { get; set; }

        public string Name { get; set; }

        public string IconCssClass { get; set; }
    }

    public class PersonOptionBag
    {
        public string PersonAliasGuid { get; set; }

        public string FullName { get; set; }

        public string PhotoUrl { get; set; }
    }

    public class GateRequirementBag
    {
        public string Description { get; set; }

        public bool IsMet { get; set; }
    }

    public class GateResultBag
    {
        public int Phase { get; set; }

        public bool IsMet { get; set; }

        public List<GateRequirementBag> Requirements { get; set; } = new List<GateRequirementBag>();
    }

    public class PhaseDefinitionBag
    {
        public int Phase { get; set; }

        public string Name { get; set; }

        public string Guidance { get; set; }

        public List<string> Prompts { get; set; } = new List<string>();

        public bool IsAutoSkippedInSimpleMode { get; set; }

        public bool IsLoopPhase { get; set; }
    }

    public class PdfResultBag
    {
        public string FileName { get; set; }

        public string Base64 { get; set; }
    }

    public class PhaseStateBag
    {
        public int Phase { get; set; }

        public int Progress { get; set; }

        public DateTime? CompletedDateTime { get; set; }

        public string Summary { get; set; }

        public Dictionary<string, string> Data { get; set; } = new Dictionary<string, string>();
    }
}
