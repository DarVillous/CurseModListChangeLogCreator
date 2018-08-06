using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CurseModListChangelogCreator2
{
    public class ModPackInformation
    {
        public ModPackInformation()
        {
            Mods = new List<CurseModListChangelogCreator2.ModVersion>();
        }
        public string Loader { get; internal set; }
        public string ModPackAuthor { get; internal set; }
        public string ModPackVersion { get; internal set; }
        public string MinecraftVersion { get; internal set; }
        public string ProjectId { get; set; }

        public List<ModVersion> Mods {get; }

        public string ModListHtml { get; set; }
    }
}
