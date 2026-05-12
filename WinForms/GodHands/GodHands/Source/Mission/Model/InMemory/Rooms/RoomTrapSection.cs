using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;

namespace GodHands {
    public class RoomTrapSection : InMemory {
        private int len;
        private List<RoomTrap> traps = new List<RoomTrap>();

        public RoomTrapSection(string url, int pos, int len, DirRec rec):
        base(url, pos, rec) {
            this.len = len;
            LoadEntries();
        }

        [ReadOnly(true)][Category(" INTERNAL")]
        public int LenSection {
            get { return len; }
            set {}
        }

        [ReadOnly(true)][Category(" INTERNAL")]
        public List<RoomTrap> Traps {
            get { return traps; }
            set {}
        }

        public bool HasEntries() {
            return traps.Count > 0;
        }

        public bool OpenSection(TreeNode root) {
            for (int i = 0; i < traps.Count; i++) {
                RoomTrap trap = traps[i];
                root.Nodes.Add(trap.GetUrl(), trap.GetText(), 37, 37);
            }
            return true;
        }

        private void LoadEntries() {
            traps.Clear();
            int pos = GetPos() - GetRec().LbaData*2048;
            for (int i = 0; i + 0x0B < len; i += 0x0C) {
                ushort trapId = RamDisk.GetU16(GetPos()+i+0x06);
                Skill skill = null;
                byte skillId;
                if (RoomTrap.TryGetMappedSkillId(trapId, out skillId)) {
                    skill = Model.Get("DB:Skills/Skill_" + skillId) as Skill;
                }
                if ((skill != null) && (skill.SkillCategory == "Trap")) {
                    string key = GetUrl()+"/Trap_"+traps.Count;
                    RoomTrap trap = new RoomTrap(key, pos + i, GetRec());
                    traps.Add(trap);
                }
            }
        }
    }
}
