using Myria.Lib.Core.Entities.Skills;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Myria.Lib.Core.Systems.Events
{
    public class SkillLearnedEventArgs : EventArgs
    {
        public Skill Skill { get; }
        public int? NewLevel { get; }   // optional: if you add skill levels later

        public SkillLearnedEventArgs(Skill skill, int? newLevel = null)
        {
            Skill = skill ?? throw new ArgumentNullException(nameof(skill));
            NewLevel = newLevel;
        }

    }

}
