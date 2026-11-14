using System.Collections.Generic;

namespace com.yesuchum.RockDetective.ViewModels
{
    public class SkillSettingsInitBag
    {
        public bool CanEdit { get; set; }

        public string SkillUrl { get; set; }

        public SkillStatusBag Status { get; set; } = new SkillStatusBag();
    }

    public class SkillStatusBag
    {
        public List<SkillToolStatusBag> Tools { get; set; } = new List<SkillToolStatusBag>();

        public int AddedToolCount { get; set; }
    }

    public class SkillToolStatusBag
    {
        public string Guid { get; set; }

        public string Name { get; set; }

        public string StandardName { get; set; }

        public string Description { get; set; }

        public string State { get; set; }
    }
}
