using System.Collections.Generic;

namespace com.yesuchum.RockDetective.ViewModels
{
    public class SkillSettingsInitBag
    {
        public bool CanEdit { get; set; }

        public List<SkillWordingItemBag> Items { get; set; } = new List<SkillWordingItemBag>();

        public Dictionary<string, string> Values { get; set; } = new Dictionary<string, string>();
    }

    public class SkillWordingItemBag
    {
        public string Key { get; set; }

        public string Group { get; set; }

        public string Label { get; set; }

        public string Default { get; set; }
    }

    public class SkillSettingsSaveResultBag
    {
        public Dictionary<string, string> Values { get; set; } = new Dictionary<string, string>();

        public bool IsAppliedToRockAgents { get; set; }
    }
}
