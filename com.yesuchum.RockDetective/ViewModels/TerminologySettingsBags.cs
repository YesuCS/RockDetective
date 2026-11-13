using System.Collections.Generic;

namespace com.yesuchum.RockDetective.ViewModels
{
    public class TerminologySettingsInitBag
    {
        public List<CategoryOptionBag> Categories { get; set; } = new List<CategoryOptionBag>();

        public List<TerminologyTermBag> Terms { get; set; } = new List<TerminologyTermBag>();

        public List<TerminologyPhaseBag> Phases { get; set; } = new List<TerminologyPhaseBag>();

        public List<TerminologyGateBag> Gates { get; set; } = new List<TerminologyGateBag>();

        public bool CanEdit { get; set; }
    }

    public class TerminologyTermBag
    {
        public string Key { get; set; }

        public string Label { get; set; }

        public string DefaultSingular { get; set; }
    }

    public class TerminologyPhaseBag
    {
        public string Key { get; set; }

        public int Phase { get; set; }

        public string DefaultName { get; set; }

        public string DefaultGuidance { get; set; }

        public List<string> DefaultPrompts { get; set; } = new List<string>();
    }

    public class TerminologyGateBag
    {
        public string Key { get; set; }

        public string PhaseName { get; set; }

        public string DefaultDescription { get; set; }
    }

    public class TerminologyValuesBag
    {
        public Dictionary<string, string> Terms { get; set; } = new Dictionary<string, string>();

        public Dictionary<string, TerminologyPhaseValueBag> Phases { get; set; } = new Dictionary<string, TerminologyPhaseValueBag>();

        public Dictionary<string, string> GateDescriptions { get; set; } = new Dictionary<string, string>();
    }

    public class TerminologyPhaseValueBag
    {
        public string Name { get; set; }

        public string Guidance { get; set; }

        public List<string> Prompts { get; set; } = new List<string>();
    }
}
