using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace GodHands {
    public class ActorSeqActionInfo {
        public int Frame { get; set; }
        public string Name { get; set; }
        public List<int> Params { get; set; }
    }

    public class ActorSeqAnimation : BaseClass {
        private readonly ActorSeqSection section;
        private readonly string text;
        private readonly string summary;
        private readonly List<int> slotRefs;
        private readonly List<ActorSeqActionInfo> actions;

        public ActorSeqAnimation(
            string url,
            ActorSeqSection section,
            int id,
            int length,
            int baseAnimationId,
            int scaleFlags,
            List<int> slotRefs,
            List<ActorSeqActionInfo> actions,
            string label) : base(url, section.GetPos()) {
            this.section = section;
            this.AnimationId = id;
            this.Length = length;
            this.BaseAnimationId = baseAnimationId;
            this.ScaleFlags = scaleFlags;
            this.slotRefs = slotRefs;
            this.actions = actions;
            text = "Anim " + id.ToString("D2") + ((label.Length > 0) ? (" " + label) : "");
            summary = BuildSummary();
            Model.Add(url, this);
            Publisher.Register(this);
        }

        public override string GetText() {
            return text;
        }

        public ActorSeqSection Section {
            get { return section; }
        }

        [ReadOnly(true)]
        [Category("SEQ")]
        [DisplayName("Animation ID")]
        public int AnimationId { get; private set; }

        [ReadOnly(true)]
        [Category("SEQ")]
        [DisplayName("Length")]
        public int Length { get; private set; }

        [ReadOnly(true)]
        [Category("SEQ")]
        [DisplayName("Base Animation")]
        public int BaseAnimationId { get; private set; }

        [ReadOnly(true)]
        [Category("SEQ")]
        [DisplayName("Scale Flags")]
        public int ScaleFlags { get; private set; }

        [ReadOnly(true)]
        [Category("SEQ")]
        [DisplayName("Slots")]
        public string Slots {
            get {
                if (slotRefs.Count == 0) {
                    return "none";
                }
                return string.Join(", ", slotRefs.Select(x => x.ToString()).ToArray());
            }
        }

        [ReadOnly(true)]
        [Category("SEQ")]
        [DisplayName("Actions")]
        public string Actions {
            get {
                if (actions.Count == 0) {
                    return "none";
                }
                List<string> chunks = new List<string>();
                foreach (ActorSeqActionInfo action in actions) {
                    string suffix = "";
                    if (action.Params.Count > 0) {
                        suffix = "(" + string.Join(", ", action.Params.Select(x => x.ToString()).ToArray()) + ")";
                    }
                    chunks.Add(action.Frame + ":" + action.Name + suffix);
                }
                return string.Join(" | ", chunks.ToArray());
            }
        }

        public string GetSummaryText() {
            return summary;
        }

        private string BuildSummary() {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(text);
            sb.AppendLine("Source: " + section.GetText());
            sb.AppendLine("Length: " + Length + " frames");
            sb.AppendLine("Base Animation: " + BaseAnimationId);
            sb.AppendLine("Scale Flags: " + ScaleFlags);
            sb.AppendLine("Slots: " + Slots);
            sb.AppendLine("Actions: " + Actions);
            return sb.ToString().TrimEnd();
        }
    }

    public class ActorSeqSection : ActorModelSection {
        private static readonly Dictionary<int, string> actionNames = new Dictionary<int, string>() {
            { 0x01, "loop" },
            { 0x02, "attack?" },
            { 0x04, "0x04" },
            { 0x0A, "0x0a" },
            { 0x0B, "locomotion?" },
            { 0x0C, "0x0c" },
            { 0x0D, "0x0d" },
            { 0x0F, "0x0f" },
            { 0x13, "unlockBone" },
            { 0x14, "0x14" },
            { 0x15, "0x15" },
            { 0x16, "0x16" },
            { 0x17, "turnLeft?" },
            { 0x18, "turnRight?" },
            { 0x19, "0x19" },
            { 0x1A, "0x1a" },
            { 0x1B, "0x1b" },
            { 0x1C, "0x1c" },
            { 0x1D, "paralyze?" },
            { 0x24, "0x24" },
            { 0x27, "0x27" },
            { 0x34, "0x34" },
            { 0x35, "0x35" },
            { 0x36, "0x36" },
            { 0x37, "0x37" },
            { 0x38, "0x38" },
            { 0x39, "0x39" },
            { 0x3A, "disappear" },
            { 0x3B, "land" },
            { 0x3C, "adjustShadow" },
            { 0x3F, "0x3f" },
            { 0x40, "0x40" },
        };

        private static readonly Dictionary<int, int> actionParamCount = new Dictionary<int, int>() {
            { 0x01, 0 }, { 0x02, 0 }, { 0x04, 1 }, { 0x0A, 1 }, { 0x0B, 0 },
            { 0x0C, 1 }, { 0x0D, 0 }, { 0x0F, 1 }, { 0x13, 1 }, { 0x14, 1 },
            { 0x15, 1 }, { 0x16, 2 }, { 0x17, 0 }, { 0x18, 0 }, { 0x19, 0 },
            { 0x1A, 1 }, { 0x1B, 1 }, { 0x1C, 1 }, { 0x1D, 0 }, { 0x24, 2 },
            { 0x27, 4 }, { 0x34, 3 }, { 0x35, 5 }, { 0x36, 3 }, { 0x37, 1 },
            { 0x38, 1 }, { 0x39, 1 }, { 0x3A, 0 }, { 0x3B, 0 }, { 0x3C, 1 },
            { 0x3F, 0 }, { 0x40, 0 },
        };

        private readonly List<ActorSeqAnimation> animations = new List<ActorSeqAnimation>();
        private bool parsed = false;
        private string summaryText = "";

        public ActorSeqSection(
            string url,
            int pos,
            int len,
            DirRec rec,
            string displayText,
            string containerFile,
            int offsetInContainer,
            string exportName,
            ActorModelSectionKind kind) :
            base(url, pos, len, rec, displayText, containerFile, offsetInContainer, exportName, kind) {
        }

        [ReadOnly(true)]
        [Category("SEQ")]
        [DisplayName("Animations")]
        public int AnimationCount {
            get {
                EnsureParsed();
                return animations.Count;
            }
        }

        [ReadOnly(true)]
        [Category("SEQ")]
        [DisplayName("Slots")]
        public int NumSlots { get; private set; }

        [ReadOnly(true)]
        [Category("SEQ")]
        [DisplayName("Bones")]
        public int NumBones { get; private set; }

        [ReadOnly(true)]
        [Category("SEQ")]
        [DisplayName("Header Offset")]
        public string HeaderOffsetHex { get; private set; }

        public IList<ActorSeqAnimation> Animations {
            get {
                EnsureParsed();
                return animations.AsReadOnly();
            }
        }

        public string GetSummaryText() {
            EnsureParsed();
            return summaryText;
        }

        public string GetSequencePreference() {
            switch (SectionKind) {
            case ActorModelSectionKind.CommonSeq:
                return "common";
            case ActorModelSectionKind.BattleSeq:
                return "battle";
            default:
                return "auto";
            }
        }

        public bool OpenSection(TreeNode root) {
            EnsureParsed();
            foreach (ActorSeqAnimation animation in animations) {
                root.Nodes.Add(animation.GetUrl(), animation.GetText(), 29, 29);
            }
            return true;
        }

        private void EnsureParsed() {
            if (parsed) {
                return;
            }
            parsed = true;

            byte[] raw = RawBytes();
            if ((raw == null) || (raw.Length < 16)) {
                summaryText = "Unable to read sequence data.";
                return;
            }

            NumSlots = ReadU16(raw, 0);
            NumBones = ReadU8(raw, 2);
            int slotOffset = ReadU32(raw, 12) + 8;
            int headerOffset = slotOffset + NumSlots;
            HeaderOffsetHex = "0x" + headerOffset.ToString("X4");

            int denom = NumBones * 4 + 10;
            if ((denom <= 0) || (headerOffset < (16 + NumSlots))) {
                summaryText = "Sequence header is invalid.";
                return;
            }

            int numAnimations = (headerOffset - NumSlots - 16) / denom;
            if (numAnimations < 0) {
                numAnimations = 0;
            }

            List<int> slots = new List<int>();
            for (int i = 0; (i < NumSlots) && ((slotOffset + i) < raw.Length); i++) {
                slots.Add(ReadU8(raw, slotOffset + i));
            }

            int cursor = 16;
            for (int id = 0; id < numAnimations; id++) {
                if ((cursor + denom) > raw.Length) {
                    break;
                }

                int length = ReadU16(raw, cursor);
                int baseAnimationId = unchecked((sbyte)ReadU8(raw, cursor + 2));
                int scaleFlags = ReadU8(raw, cursor + 3);
                int ptrActions = ReadU16(raw, cursor + 4);
                List<int> slotRefs = new List<int>();
                for (int j = 0; j < slots.Count; j++) {
                    if (slots[j] == id) {
                        slotRefs.Add(j);
                    }
                }
                List<ActorSeqActionInfo> actions = ReadActions(raw, headerOffset, ptrActions, length);
                string label = BuildLabel(baseAnimationId, actions);
                string key = GetUrl() + "/Anim_" + id.ToString("D2");
                ActorSeqAnimation animation = new ActorSeqAnimation(
                    key,
                    this,
                    id,
                    length,
                    baseAnimationId,
                    scaleFlags,
                    slotRefs,
                    actions,
                    label
                );
                animations.Add(animation);
                cursor += denom;
            }

            summaryText = BuildSummaryText(slots);
        }

        private string BuildSummaryText(List<int> slots) {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(GetText());
            sb.AppendLine("Animations: " + animations.Count);
            sb.AppendLine("Slots: " + NumSlots);
            sb.AppendLine("Bones: " + NumBones);
            sb.AppendLine("Header Offset: " + HeaderOffsetHex);
            if (slots.Count > 0) {
                sb.AppendLine("Slot Table: " + string.Join(", ", slots.Select(x => x.ToString()).ToArray()));
            }
            foreach (ActorSeqAnimation animation in animations) {
                sb.AppendLine(animation.GetText());
            }
            return sb.ToString().TrimEnd();
        }

        private static string BuildLabel(int baseAnimationId, List<ActorSeqActionInfo> actions) {
            List<string> tags = new List<string>();
            bool hasLoop = actions.Any(x => x.Name == "loop");
            bool hasDisappear = actions.Any(x => x.Name == "disappear");
            bool hasAttack = actions.Any(x => x.Name == "attack?");
            bool hasMove = actions.Any(x => (x.Name == "locomotion?") || (x.Name == "turnLeft?") || (x.Name == "turnRight?"));

            if (hasLoop) {
                tags.Add("loop");
            }
            if (hasDisappear) {
                tags.Add("death-ish");
            }
            if (hasAttack) {
                tags.Add("attack?");
            }
            if (hasMove) {
                tags.Add("movement?");
            }
            if (baseAnimationId >= 0) {
                tags.Add("base:" + baseAnimationId);
            }

            if (tags.Count == 0) {
                return "";
            }
            return "[" + string.Join(" | ", tags.ToArray()) + "]";
        }

        private static List<ActorSeqActionInfo> ReadActions(byte[] raw, int headerOffset, int ptrActions, int length) {
            List<ActorSeqActionInfo> list = new List<ActorSeqActionInfo>();
            if (ptrActions <= 0) {
                return list;
            }

            int pos = headerOffset + ptrActions;
            while (pos < raw.Length) {
                int frame = ReadU8(raw, pos++);
                if (frame == 0xFF) {
                    break;
                }
                if (frame > length) {
                    break;
                }
                if (pos >= raw.Length) {
                    break;
                }

                int actionId = ReadU8(raw, pos++);
                if (actionId == 0x00) {
                    break;
                }

                string name = actionNames.ContainsKey(actionId)
                    ? actionNames[actionId]
                    : "0x" + actionId.ToString("X2");
                int paramCount = actionParamCount.ContainsKey(actionId)
                    ? actionParamCount[actionId]
                    : 0;
                List<int> pars = new List<int>();
                for (int i = 0; (i < paramCount) && (pos < raw.Length); i++) {
                    pars.Add(ReadU8(raw, pos++));
                }
                list.Add(new ActorSeqActionInfo() {
                    Frame = frame,
                    Name = name,
                    Params = pars,
                });
            }
            return list;
        }

        private static int ReadU8(byte[] raw, int pos) {
            if ((pos < 0) || (pos >= raw.Length)) {
                return 0;
            }
            return raw[pos];
        }

        private static int ReadU16(byte[] raw, int pos) {
            if ((pos + 1) >= raw.Length) {
                return 0;
            }
            return raw[pos] + raw[pos + 1] * 256;
        }

        private static int ReadU32(byte[] raw, int pos) {
            if ((pos + 3) >= raw.Length) {
                return 0;
            }
            return raw[pos]
                + raw[pos + 1] * 256
                + raw[pos + 2] * 65536
                + raw[pos + 3] * 16777216;
        }
    }
}
