using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace GodHands {
    public enum ActorModelSectionKind {
        CharacterShp,
        WeaponWep,
        ShieldWep,
        CommonSeq,
        BattleSeq,
    }

    public class ActorModelSection : InMemory {
        private readonly string displayText;
        private readonly int len;
        private readonly string containerFile;
        private readonly int offsetInContainer;
        private readonly string exportName;
        private readonly ActorModelSectionKind kind;

        public ActorModelSection(
            string url,
            int pos,
            int len,
            DirRec rec,
            string displayText,
            string containerFile,
            int offsetInContainer,
            string exportName,
            ActorModelSectionKind kind) : base(url, pos, rec) {
            this.displayText = displayText;
            this.len = len;
            this.containerFile = containerFile;
            this.offsetInContainer = offsetInContainer;
            this.exportName = exportName;
            this.kind = kind;
        }

        public override string GetText() {
            return displayText;
        }

        public override int GetLen() {
            return len;
        }

        public override string GetExportName() {
            return exportName;
        }

        [Browsable(false)]
        public ActorModelSectionKind SectionKind {
            get { return kind; }
        }

        [ReadOnly(true)]
        [Category("Section")]
        [DisplayName("Container")]
        public string ContainerFile {
            get { return containerFile; }
        }

        [ReadOnly(true)]
        [Category("Section")]
        [DisplayName("Offset In ZUD")]
        public string OffsetInContainer {
            get { return "0x" + offsetInContainer.ToString("X8"); }
        }

        [ReadOnly(true)]
        [Category("Section")]
        [DisplayName("Length")]
        public int Length {
            get { return len; }
        }
    }

    public class Actor : InMemory {
        private Zone zone;
        private ZUD zud;
        private DirRec zudRec;

        public Actor(string url, int pos, DirRec rec,
        Zone zone, int zoneid, int actorid, DirRec zud_rec):
        base(url, pos, rec) {
            this.zone = zone;
            this.ZoneId = zoneid;
            this.ActorId = actorid;
            zudRec = zud_rec;
            zud = Model.zuds[zud_rec.GetUrl()];
        }

        [ReadOnly(true)]
        [Category(" INTERNAL")]
        [DisplayName("Zone ID")]
        [Description("Zone number")]
        public int ZoneId { get; set; }

        [ReadOnly(true)]
        [Category(" INTERNAL")]
        [DisplayName("Actor ID")]
        [Description("Actor number inside this zone")]
        public int ActorId { get; set; }

        public override string GetText() {
            return Name;
        }

        public override int GetLen() {
            return 0x464;
        }

        public string GetZndFileName() {
            return GetRec().GetFileName();
        }

        private int GetZudField32(int offset) {
            return RamDisk.GetS32(zud.GetPos() + offset);
        }

        private string DescribeZudSection(int ptrOffset, int lenOffset) {
            int ptr = GetZudField32(ptrOffset);
            int len = GetZudField32(lenOffset);
            if (len <= 0) {
                return "";
            }
            return zudRec.GetFileName() + " +0x" + ptr.ToString("X8") + " (" + len + " bytes)";
        }

        private ActorModelSection CreateSection(string url, string label, string extension, int ptrOffset, int lenOffset, ActorModelSectionKind kind) {
            int ptr = GetZudField32(ptrOffset);
            int len = GetZudField32(lenOffset);
            if (len <= 0) {
                return null;
            }

            string stem = Path.GetFileNameWithoutExtension(zudRec.GetFileName());
            string exportName = stem + "_" + label.Replace(' ', '_') + extension;
            return new ActorModelSection(url, ptr, len, zudRec, label, zudRec.GetFileName(), ptr, exportName, kind);
        }

        public ActorModelSection CreateCharacterShp(string url) {
            return CreateSection(url, "Character SHP", ".SHP", 0x08, 0x0C, ActorModelSectionKind.CharacterShp);
        }

        public ActorModelSection CreateWeaponWep(string url) {
            return CreateSection(url, "Weapon WEP", ".WEP", 0x10, 0x14, ActorModelSectionKind.WeaponWep);
        }

        public ActorModelSection CreateShieldWep(string url) {
            return CreateSection(url, "Shield WEP", ".WEP", 0x18, 0x1C, ActorModelSectionKind.ShieldWep);
        }

        public ActorSeqSection CreateCommonSeq(string url) {
            int ptr = GetZudField32(0x20);
            int len = GetZudField32(0x24);
            if (len <= 0) {
                return null;
            }
            string stem = Path.GetFileNameWithoutExtension(zudRec.GetFileName());
            string exportName = stem + "_SEQ_Common.SEQ";
            return new ActorSeqSection(url, ptr, len, zudRec, "SEQ Common", zudRec.GetFileName(), ptr, exportName, ActorModelSectionKind.CommonSeq);
        }

        public ActorSeqSection CreateBattleSeq(string url) {
            int ptr = GetZudField32(0x28);
            int len = GetZudField32(0x2C);
            if (len <= 0) {
                return null;
            }
            string stem = Path.GetFileNameWithoutExtension(zudRec.GetFileName());
            string exportName = stem + "_SEQ_Battle.SEQ";
            return new ActorSeqSection(url, ptr, len, zudRec, "SEQ Battle", zudRec.GetFileName(), ptr, exportName, ActorModelSectionKind.BattleSeq);
        }

        [ReadOnly(true)]
        [Category("Model")]
        [DisplayName("ZUD File")]
        public string ZudFile {
            get { return zudRec.GetFileName(); }
        }

        public DirRec GetZudRec() {
            return zudRec;
        }

        [ReadOnly(true)]
        [Category("Model")]
        [DisplayName("SHP")]
        public string CharacterShp {
            get { return DescribeZudSection(0x08, 0x0C); }
        }

        [ReadOnly(true)]
        [Category("Model")]
        [DisplayName("WEP Weapon")]
        public string WeaponWep {
            get { return DescribeZudSection(0x10, 0x14); }
        }

        [ReadOnly(true)]
        [Category("Model")]
        [DisplayName("WEP Shield")]
        public string ShieldWep {
            get { return DescribeZudSection(0x18, 0x1C); }
        }

        [ReadOnly(true)]
        [Category("Model")]
        [DisplayName("SEQ Common")]
        public string CommonSeq {
            get { return DescribeZudSection(0x20, 0x24); }
        }

        [ReadOnly(true)]
        [Category("Model")]
        [DisplayName("SEQ Battle")]
        public string BattleSeq {
            get { return DescribeZudSection(0x28, 0x2C); }
        }

        [Category("Stats")]
        [DisplayName("Unknown_01")]
        [Description("No information")]
        public byte Unknown_01 {
            get { return RamDisk.GetU8(GetPos()+0x00); }
            set { UndoRedo.Exec(new BindU8(this, 0x00, value)); }
        }

        [Category("Stats")]
        [DisplayName("Unknown_02")]
        [Description("No information")]
        public byte Unknown_02 {
            get { return RamDisk.GetU8(GetPos()+0x01); }
            set { UndoRedo.Exec(new BindU8(this, 0x01, value)); }
        }

        [Category("Stats")]
        [DisplayName("3D Effects")]
        [Description("3D model special effects")]
        public byte Index3DFX {
            get { return RamDisk.GetU8(GetPos()+0x02); }
            set { UndoRedo.Exec(new BindU8(this, 0x02, value)); }
        }

        [Category("Stats")]
        [DisplayName("Unknown_04")]
        [Description("No information")]
        public byte Unknown_04 {
            get { return RamDisk.GetU8(GetPos()+0x03); }
            set { UndoRedo.Exec(new BindU8(this, 0x03, value)); }
        }

        [Category("Stats")]
        [DisplayName("Name")]
        [Description("Name of the character (max 24 letters)")]
        public string Name {
            get {
                byte[] kildean = new byte[0x18];
                RamDisk.Get(GetPos()+0x04, 0x18, kildean);
                return Kildean.ToAscii(kildean);
            }
            set {
                string clip = value.Substring(0, Math.Min(0x18, value.Length));
                byte[] kildean = Kildean.ToKildean(clip, 0x18);
                UndoRedo.Exec(new BindArray(this, GetPos()+0x04, 0x18, kildean));
            }
        }

        [Category("Stats")]
        [DisplayName("HP")]
        [Description("Health points (depleted when taking damage)")]
        public ushort HP {
            get { return RamDisk.GetU16(GetPos()+0x1C); }
            set { UndoRedo.Exec(new BindU16(this, 0x1C, value)); }
        }

        [Category("Stats")]
        [DisplayName("MP")]
        [Description("Magic points (depleted by casting spells)")]
        public ushort MP {
            get { return RamDisk.GetU16(GetPos()+0x1E); }
            set { UndoRedo.Exec(new BindU16(this, 0x1E, value)); }
        }

        [Category("Stats")]
        [DisplayName("INT")]
        [Description("Intelligence (affects ability to cast magic)")]
        public byte INT {
            get { return RamDisk.GetU8(GetPos()+0x20); }
            set { UndoRedo.Exec(new BindU8(this, 0x20, value)); }
        }

        [Category("Stats")]
        [DisplayName("AGL")]
        [Description("Agility (affects ability to evade attacks)")]
        public byte AGL {
            get { return RamDisk.GetU8(GetPos()+0x21); }
            set { UndoRedo.Exec(new BindU8(this, 0x21, value)); }
        }

        [Category("Stats")]
        [DisplayName("STR")]
        [Description("Agility (affects ability to deal damage)")]
        public byte STR {
            get { return RamDisk.GetU8(GetPos()+0x22); }
            set { UndoRedo.Exec(new BindU8(this, 0x22, value)); }
        }

        [Category("Stats")]
        [DisplayName("Unknown_05")]
        [Description("No information")]
        public byte Unknown_05 {
            get { return RamDisk.GetU8(GetPos()+0x23); }
            set { UndoRedo.Exec(new BindU8(this, 0x23, value)); }
        }

        [Category("Stats")]
        [DisplayName("Unknown_06")]
        [Description("No information")]
        public byte Unknown_06 {
            get { return RamDisk.GetU8(GetPos()+0x24); }
            set { UndoRedo.Exec(new BindU8(this, 0x24, value)); }
        }

        [Category("Stats")]
        [DisplayName("Carry Speed")]
        [Description("Walking speed whilst carrying crates")]
        public byte CarrySpeed {
            get { return RamDisk.GetU8(GetPos()+0x25); }
            set { UndoRedo.Exec(new BindU8(this, 0x25, value)); }
        }

        [Category("Stats")]
        [DisplayName("Unknown_07")]
        [Description("No information")]
        public byte Unknown_07 {
            get { return RamDisk.GetU8(GetPos()+0x26); }
            set { UndoRedo.Exec(new BindU8(this, 0x26, value)); }
        }


        [Category("Stats")]
        [DisplayName("Run Speed")]
        [Description("Speed whilst running")]
        public byte RunSpeed {
            get { return RamDisk.GetU8(GetPos()+0x27); }
            set { UndoRedo.Exec(new BindU8(this, 0x27, value)); }
        }

        [Category("Stats")]
        [DisplayName("Unknown_08")]
        [Description("No information")]
        public byte Unknown_08 {
            get { return RamDisk.GetU8(GetPos()+0x28); }
            set { UndoRedo.Exec(new BindU8(this, 0x28, value)); }
        }

        [Category("Stats")]
        [DisplayName("Unknown_09")]
        [Description("No information")]
        public byte Unknown_09 {
            get { return RamDisk.GetU8(GetPos()+0x29); }
            set { UndoRedo.Exec(new BindU8(this, 0x29, value)); }
        }

        [Category("Stats")]
        [DisplayName("Unknown_10")]
        [Description("No information")]
        public byte Unknown_10 {
            get { return RamDisk.GetU8(GetPos()+0x2A); }
            set { UndoRedo.Exec(new BindU8(this, 0x2A, value)); }
        }

        [Category("Stats")]
        [DisplayName("Unknown_11")]
        [Description("No information")]
        public byte Unknown_11 {
            get { return RamDisk.GetU8(GetPos()+0x2B); }
            set { UndoRedo.Exec(new BindU8(this, 0x2B, value)); }
        }

        [Category("Stats")]
        [DisplayName("Enemy ID")]
        [Description("Enemy identifier (used by MPD files)")]
        public int EnemyID {
            get { return RamDisk.GetS32(GetPos()+0x460); }
            set { UndoRedo.Exec(new BindS32(this, 0x460, value)); }
        }
    }
}
