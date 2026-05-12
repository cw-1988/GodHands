using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace GodHands {
    public class RoomTrap : InMemory {
        // Room trap records use compact per-room codes, not direct DB:Skills IDs.
        private static readonly Dictionary<ushort, byte> TrapSkillIdByRawId =
        new Dictionary<ushort, byte>() {
            { 0x00, 0x06 }, // Death Vapor
            { 0x01, 0x07 }, // Eruption
            { 0x02, 0x08 }, // Freeze
            { 0x03, 0x09 }, // Gust
            { 0x04, 0x0A }, // Terra Thrust
            { 0x05, 0x0B }, // Holy Light
            { 0x06, 0x0C }, // Diabolos
            { 0x07, 0x0D }, // Poison Panel
            { 0x08, 0x0E }, // Paralysis Panel
            { 0x0B, 0x11 }, // Curse Panel
            { 0x0C, 0x12 }, // Heal Panel
            { 0x0D, 0x13 }, // Cure Panel
            { 0x0F, 0x15 }, // Trap Clear
        };

        public RoomTrap(string url, int pos, DirRec rec):
        base(url, pos, rec) {
        }

        public override int GetLen() {
            return 0x0C;
        }

        [ReadOnly(true)][Category(" TRAP")]
        public short TileX {
            get { return RamDisk.GetS16(GetPos()+0x00); }
            set {}
        }

        [ReadOnly(true)][Category(" TRAP")]
        public short TileY {
            get { return RamDisk.GetS16(GetPos()+0x02); }
            set {}
        }

        [ReadOnly(true)][Category(" TRAP")]
        public short TileZ {
            get { return RamDisk.GetS16(GetPos()+0x04); }
            set {}
        }

        [ReadOnly(true)][Category(" TRAP")]
        public ushort TrapId {
            get { return RamDisk.GetU16(GetPos()+0x06); }
            set {}
        }

        [ReadOnly(true)][Category(" TRAP")]
        public short State {
            get { return RamDisk.GetS16(GetPos()+0x08); }
            set {}
        }

        [ReadOnly(true)][Category(" TRAP")]
        public sbyte ArgA {
            get { return RamDisk.GetS8(GetPos()+0x0A); }
            set {}
        }

        [ReadOnly(true)][Category(" TRAP")]
        public sbyte ArgB {
            get { return RamDisk.GetS8(GetPos()+0x0B); }
            set {}
        }

        [ReadOnly(true)][Category(" TRAP")]
        public string TrapName {
            get {
                Skill skill = GetSkill();
                if (skill != null) {
                    return skill.Name;
                }
                return "Trap " + TrapId.ToString("X2");
            }
            set {}
        }

        [ReadOnly(true)][Category(" TRAP")]
        public string SkillCategory {
            get {
                Skill skill = GetSkill();
                if (skill != null) {
                    return skill.SkillCategory;
                }
                return "";
            }
            set {}
        }

        public bool IsTrap() {
            Skill skill = GetSkill();
            return (skill != null) && (skill.SkillCategory == "Trap");
        }

        public override string GetText() {
            return TrapName + " (" + TileX + ", " + TileY + ", " + TileZ + ")";
        }

        public static bool TryGetMappedSkillId(ushort trapId, out byte skillId) {
            return TrapSkillIdByRawId.TryGetValue(trapId, out skillId);
        }

        private Skill GetSkill() {
            byte skillId;
            if (!TryGetMappedSkillId(TrapId, out skillId)) {
                return null;
            }
            return Model.Get("DB:Skills/Skill_" + skillId) as Skill;
        }
    }
}
