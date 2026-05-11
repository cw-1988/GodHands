using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace GodHands {
    public class RoomScriptOpcodeChoice {
        public RoomScriptOpcodeChoice(byte opcode, string name, int size) {
            Opcode = opcode;
            Name = name;
            Size = size;
        }

        public byte Opcode { get; private set; }
        public string Name { get; private set; }
        public int Size { get; private set; }

        public int ArgumentCount {
            get { return Math.Max(0, Size - 1); }
        }

        public string Label {
            get { return ToString(); }
        }

        public override string ToString() {
            return "0x" + Opcode.ToString("X2") + " " + Name;
        }
    }

    public class RoomScriptOpcodeEntry {
        public RoomScriptOpcodeEntry(int offset, byte opcode, string opcodeName, int size, byte[] args, bool truncated, bool unsafeAdvance) {
            Offset = offset;
            Opcode = opcode;
            OpcodeName = opcodeName;
            Size = size;
            Args = (args != null) ? (byte[])args.Clone() : new byte[0];
            Truncated = truncated;
            UnsafeAdvance = unsafeAdvance;
        }

        public int Offset { get; private set; }
        public byte Opcode { get; private set; }
        public string OpcodeName { get; private set; }
        public int Size { get; private set; }
        public byte[] Args { get; private set; }
        public bool Truncated { get; private set; }
        public bool UnsafeAdvance { get; private set; }

        public string ValuesHex {
            get { return HexBytes(Args); }
        }

        public string Label {
            get { return Offset.ToString("X4") + ": " + OpcodeName; }
        }

        private static string HexBytes(byte[] data) {
            if ((data == null) || (data.Length == 0)) {
                return "";
            }

            List<string> values = new List<string>();
            foreach (byte b in data) {
                values.Add(b.ToString("X2"));
            }
            return string.Join(" ", values.ToArray());
        }
    }

    public class RoomScriptSection : InMemory {
        private struct OpcodeSpec {
            public string Name;
            public int Size;

            public OpcodeSpec(string name, int size) {
                Name = name;
                Size = size;
            }
        }

        private static readonly Dictionary<byte, OpcodeSpec> opcodeSpecs = new Dictionary<byte, OpcodeSpec>() {
            { 0x00, new OpcodeSpec("nop", 0x01) },
            { 0x01, new OpcodeSpec("Opcode01", 0x0A) },
            { 0x02, new OpcodeSpec("Opcode02", 0x03) },
            { 0x03, new OpcodeSpec("Opcode03", 0x03) },
            { 0x04, new OpcodeSpec("Opcode04", 0x04) },
            { 0x05, new OpcodeSpec("Opcode05", 0x12) },
            { 0x06, new OpcodeSpec("Opcode06", 0x0B) },
            { 0x07, new OpcodeSpec("Opcode07", 0x0B) },
            { 0x08, new OpcodeSpec("Opcode08", 0x26) },
            { 0x09, new OpcodeSpec("Opcode09", 0x00) },
            { 0x0A, new OpcodeSpec("Opcode0A", 0x01) },
            { 0x0B, new OpcodeSpec("Opcode0B", 0x01) },
            { 0x0C, new OpcodeSpec("Opcode0C", 0x01) },
            { 0x0D, new OpcodeSpec("Opcode0D", 0x03) },
            { 0x0E, new OpcodeSpec("Opcode0E", 0x03) },
            { 0x0F, new OpcodeSpec("Opcode0F", 0x01) },
            { 0x10, new OpcodeSpec("DialogShow", 0x0B) },
            { 0x11, new OpcodeSpec("DialogText", 0x04) },
            { 0x12, new OpcodeSpec("DialogHide", 0x02) },
            { 0x13, new OpcodeSpec("Opcode13", 0x09) },
            { 0x14, new OpcodeSpec("Opcode14", 0x02) },
            { 0x15, new OpcodeSpec("Opcode15", 0x02) },
            { 0x16, new OpcodeSpec("SplashScreenChoose", 0x04) },
            { 0x17, new OpcodeSpec("SplashScreenLoad", 0x06) },
            { 0x18, new OpcodeSpec("SplashScreenShow", 0x07) },
            { 0x19, new OpcodeSpec("SplashScreenHide", 0x01) },
            { 0x1A, new OpcodeSpec("SplashScreenFadeIn", 0x01) },
            { 0x1B, new OpcodeSpec("Opcode1B", 0x04) },
            { 0x1C, new OpcodeSpec("Opcode1C", 0x02) },
            { 0x1D, new OpcodeSpec("Opcode1D", 0x02) },
            { 0x1E, new OpcodeSpec("Opcode1E", 0x04) },
            { 0x1F, new OpcodeSpec("Opcode1F", 0x05) },
            { 0x20, new OpcodeSpec("ModelLoad", 0x09) },
            { 0x21, new OpcodeSpec("Opcode21", 0x06) },
            { 0x22, new OpcodeSpec("ModelAnimate", 0x06) },
            { 0x23, new OpcodeSpec("ModelSetAnimations", 0x05) },
            { 0x24, new OpcodeSpec("ActorSfxPanVolumeControl", 0x07) },
            { 0x25, new OpcodeSpec("Opcode25", 0x09) },
            { 0x26, new OpcodeSpec("ModelPosition", 0x0C) },
            { 0x27, new OpcodeSpec("Opcode27", 0x07) },
            { 0x28, new OpcodeSpec("ModelMoveTo", 0x0A) },
            { 0x29, new OpcodeSpec("ModelMoveTo2", 0x07) },
            { 0x2A, new OpcodeSpec("Opcode2A", 0x07) },
            { 0x2B, new OpcodeSpec("Opcode2B", 0x06) },
            { 0x2C, new OpcodeSpec("Opcode2C", 0x06) },
            { 0x2D, new OpcodeSpec("Opcode2D", 0x07) },
            { 0x2E, new OpcodeSpec("ModelScale", 0x0A) },
            { 0x2F, new OpcodeSpec("ModeFree", 0x03) },
            { 0x30, new OpcodeSpec("ModelLoadAnimationsEx", 0x06) },
            { 0x31, new OpcodeSpec("ModelTint", 0x06) },
            { 0x32, new OpcodeSpec("Opcode32", 0x06) },
            { 0x33, new OpcodeSpec("ModelRotate", 0x0B) },
            { 0x34, new OpcodeSpec("Opcode34", 0x08) },
            { 0x35, new OpcodeSpec("Opcode35", 0x07) },
            { 0x36, new OpcodeSpec("Opcode36", 0x07) },
            { 0x37, new OpcodeSpec("Opcode37", 0x05) },
            { 0x38, new OpcodeSpec("ModelLookAt", 0x06) },
            { 0x39, new OpcodeSpec("ModelLookAtPosition", 0x0A) },
            { 0x3A, new OpcodeSpec("ModelLoadAnimations", 0x04) },
            { 0x3B, new OpcodeSpec("WaitForFile", 0x01) },
            { 0x3C, new OpcodeSpec("Opcode3C", 0x02) },
            { 0x3D, new OpcodeSpec("Opcode3D", 0x02) },
            { 0x3E, new OpcodeSpec("ModelIlluminate", 0x0A) },
            { 0x3F, new OpcodeSpec("Opcode3F", 0x04) },
            { 0x40, new OpcodeSpec("JumpFwdIfFlag", 0x06) },
            { 0x41, new OpcodeSpec("Opcode41", 0x06) },
            { 0x42, new OpcodeSpec("ModelControlViaScript", 0x03) },
            { 0x43, new OpcodeSpec("Opcode43", 0x01) },
            { 0x44, new OpcodeSpec("SetEngineMode", 0x02) },
            { 0x45, new OpcodeSpec("Opcode45", 0x03) },
            { 0x46, new OpcodeSpec("Opcode46", 0x03) },
            { 0x47, new OpcodeSpec("Opcode47", 0x04) },
            { 0x48, new OpcodeSpec("Opcode48", 0x01) },
            { 0x49, new OpcodeSpec("SetJumpBackCounter", 0x03) },
            { 0x4A, new OpcodeSpec("JumpBackIfCounter", 0x04) },
            { 0x4B, new OpcodeSpec("Opcode4B", 0x03) },
            { 0x4C, new OpcodeSpec("Opcode4C", 0x03) },
            { 0x4D, new OpcodeSpec("Opcode4D", 0x02) },
            { 0x4E, new OpcodeSpec("Opcode4E", 0x01) },
            { 0x4F, new OpcodeSpec("Opcode4F", 0x01) },
            { 0x50, new OpcodeSpec("ModelControlViaBattleMode", 0x04) },
            { 0x51, new OpcodeSpec("Opcode51", 0x04) },
            { 0x52, new OpcodeSpec("Opcode52", 0x05) },
            { 0x53, new OpcodeSpec("Opcode53", 0x03) },
            { 0x54, new OpcodeSpec("BattleOver", 0x04) },
            { 0x55, new OpcodeSpec("Opcode55", 0x02) },
            { 0x56, new OpcodeSpec("Opcode56", 0x03) },
            { 0x57, new OpcodeSpec("Opcode57", 0x04) },
            { 0x58, new OpcodeSpec("SetHeadsUpDisplayMode", 0x02) },
            { 0x59, new OpcodeSpec("Opcode59", 0x04) },
            { 0x5A, new OpcodeSpec("Opcode5A", 0x07) },
            { 0x5B, new OpcodeSpec("Opcode5B", 0x04) },
            { 0x5C, new OpcodeSpec("Opcode5C", 0x07) },
            { 0x5D, new OpcodeSpec("Opcode5D", 0x04) },
            { 0x5E, new OpcodeSpec("Opcode5E", 0x03) },
            { 0x5F, new OpcodeSpec("Opcode5F", 0x02) },
            { 0x60, new OpcodeSpec("Opcode60", 0x03) },
            { 0x61, new OpcodeSpec("Opcode61", 0x03) },
            { 0x62, new OpcodeSpec("Opcode62", 0x02) },
            { 0x63, new OpcodeSpec("Opcode63", 0x01) },
            { 0x64, new OpcodeSpec("RestoreRoomGeometry", 0x02) },
            { 0x65, new OpcodeSpec("SuppressRoomGeometry", 0x02) },
            { 0x66, new OpcodeSpec("Opcode66", 0x03) },
            { 0x67, new OpcodeSpec("Opcode67", 0x05) },
            { 0x68, new OpcodeSpec("LoadRoom", 0x0A) },
            { 0x69, new OpcodeSpec("LoadScene", 0x04) },
            { 0x6A, new OpcodeSpec("Opcode6A", 0x04) },
            { 0x6B, new OpcodeSpec("Opcode6B", 0x02) },
            { 0x6C, new OpcodeSpec("Opcode6C", 0x04) },
            { 0x6D, new OpcodeSpec("DisplayRoom", 0x02) },
            { 0x6E, new OpcodeSpec("Opcode6E", 0x01) },
            { 0x6F, new OpcodeSpec("Opcode6F", 0x02) },
            { 0x70, new OpcodeSpec("ModelColor", 0x08) },
            { 0x71, new OpcodeSpec("Opcode71", 0x04) },
            { 0x72, new OpcodeSpec("Opcode72", 0x03) },
            { 0x73, new OpcodeSpec("Opcode73", 0x02) },
            { 0x74, new OpcodeSpec("LoadRoomSection10", 0x02) },
            { 0x75, new OpcodeSpec("WaitForRoomSection10", 0x01) },
            { 0x76, new OpcodeSpec("FreeRoomSection10", 0x01) },
            { 0x77, new OpcodeSpec("Opcode77", 0x05) },
            { 0x78, new OpcodeSpec("EnableRoomMechanismUpdates", 0x02) },
            { 0x79, new OpcodeSpec("RoomMechanismControl", 0x03) },
            { 0x7A, new OpcodeSpec("SetRoomAmbientSoundSuspended", 0x02) },
            { 0x7B, new OpcodeSpec("Opcode7B", 0x05) },
            { 0x7C, new OpcodeSpec("SetFirstPersonView", 0x02) },
            { 0x7D, new OpcodeSpec("Opcode7D", 0x03) },
            { 0x7E, new OpcodeSpec("Opcode7E", 0x04) },
            { 0x7F, new OpcodeSpec("Opcode7F", 0x01) },
            { 0x80, new OpcodeSpec("Opcode80SharedStub", 0x05) },
            { 0x81, new OpcodeSpec("Opcode81", 0x04) },
            { 0x82, new OpcodeSpec("Opcode82", 0x05) },
            { 0x83, new OpcodeSpec("Opcode83", 0x01) },
            { 0x84, new OpcodeSpec("Opcode84", 0x03) },
            { 0x85, new OpcodeSpec("LoadSfxSlot", 0x03) },
            { 0x86, new OpcodeSpec("FreeSfxSlot", 0x02) },
            { 0x87, new OpcodeSpec("Opcode87", 0x02) },
            { 0x88, new OpcodeSpec("SetCurrentSfx", 0x02) },
            { 0x89, new OpcodeSpec("Opcode89", 0x01) },
            { 0x8A, new OpcodeSpec("Opcode8A", 0x00) },
            { 0x8B, new OpcodeSpec("Opcode8B", 0x00) },
            { 0x8C, new OpcodeSpec("Opcode8C", 0x00) },
            { 0x8D, new OpcodeSpec("Opcode8D", 0x00) },
            { 0x8E, new OpcodeSpec("Opcode8E", 0x00) },
            { 0x8F, new OpcodeSpec("Opcode8F", 0x05) },
            { 0x90, new OpcodeSpec("LoadMusicSlot", 0x03) },
            { 0x91, new OpcodeSpec("FreeMusic", 0x02) },
            { 0x92, new OpcodeSpec("MusicPlay", 0x04) },
            { 0x93, new OpcodeSpec("Opcode93", 0x03) },
            { 0x94, new OpcodeSpec("Opcode94", 0x03) },
            { 0x95, new OpcodeSpec("Opcode95", 0x03) },
            { 0x96, new OpcodeSpec("Opcode96", 0x01) },
            { 0x97, new OpcodeSpec("Opcode97", 0x02) },
            { 0x98, new OpcodeSpec("Opcode98", 0x01) },
            { 0x99, new OpcodeSpec("ClearMusicLoadSlot", 0x02) },
            { 0x9A, new OpcodeSpec("Opcode9A", 0x03) },
            { 0x9B, new OpcodeSpec("Opcode9B", 0x05) },
            { 0x9C, new OpcodeSpec("Opcode9C", 0x05) },
            { 0x9D, new OpcodeSpec("LoadSoundFileById", 0x02) },
            { 0x9E, new OpcodeSpec("ProcessSoundQueue", 0x01) },
            { 0x9F, new OpcodeSpec("Opcode9F", 0x02) },
            { 0xA0, new OpcodeSpec("OpcodeA0", 0x05) },
            { 0xA1, new OpcodeSpec("SplashScreenEffects", 0x02) },
            { 0xA2, new OpcodeSpec("CameraZoomIn", 0x02) },
            { 0xA3, new OpcodeSpec("OpcodeA3", 0x01) },
            { 0xA4, new OpcodeSpec("OpcodeA4", 0x01) },
            { 0xA5, new OpcodeSpec("OpcodeA5", 0x01) },
            { 0xA6, new OpcodeSpec("OpcodeA6", 0x02) },
            { 0xA7, new OpcodeSpec("OpcodeA7", 0x00) },
            { 0xA8, new OpcodeSpec("OpcodeA8", 0x02) },
            { 0xA9, new OpcodeSpec("OpcodeA9", 0x02) },
            { 0xAA, new OpcodeSpec("OpcodeAA", 0x05) },
            { 0xAB, new OpcodeSpec("OpcodeAB", 0x00) },
            { 0xAC, new OpcodeSpec("OpcodeAC", 0x00) },
            { 0xAD, new OpcodeSpec("OpcodeAD", 0x00) },
            { 0xAE, new OpcodeSpec("OpcodeAE", 0x00) },
            { 0xAF, new OpcodeSpec("OpcodeAF", 0x00) },
            { 0xB0, new OpcodeSpec("OpcodeB0", 0x00) },
            { 0xB1, new OpcodeSpec("OpcodeB1", 0x02) },
            { 0xB2, new OpcodeSpec("OpcodeB2", 0x08) },
            { 0xB3, new OpcodeSpec("OpcodeB3", 0x04) },
            { 0xB4, new OpcodeSpec("OpcodeB4", 0x07) },
            { 0xB5, new OpcodeSpec("OpcodeB5", 0x03) },
            { 0xB6, new OpcodeSpec("OpcodeB6", 0x07) },
            { 0xB7, new OpcodeSpec("OpcodeB7", 0x06) },
            { 0xB8, new OpcodeSpec("OpcodeB8", 0x01) },
            { 0xB9, new OpcodeSpec("OpcodeB9", 0x03) },
            { 0xBA, new OpcodeSpec("OpcodeBA", 0x02) },
            { 0xBB, new OpcodeSpec("OpcodeBB", 0x03) },
            { 0xBC, new OpcodeSpec("OpcodeBC", 0x01) },
            { 0xBD, new OpcodeSpec("OpcodeBD", 0x02) },
            { 0xBE, new OpcodeSpec("OpcodeBE", 0x01) },
            { 0xBF, new OpcodeSpec("OpcodeBF", 0x03) },
            { 0xC0, new OpcodeSpec("CameraDirection", 0x07) },
            { 0xC1, new OpcodeSpec("CameraSetAngle", 0x01) },
            { 0xC2, new OpcodeSpec("CameraLookAt", 0x03) },
            { 0xC3, new OpcodeSpec("OpcodeC3", 0x03) },
            { 0xC4, new OpcodeSpec("ModelAnimateObject", 0x04) },
            { 0xC5, new OpcodeSpec("OpcodeC5", 0x09) },
            { 0xC6, new OpcodeSpec("OpcodeC6", 0x00) },
            { 0xC7, new OpcodeSpec("OpcodeC7", 0x0A) },
            { 0xC8, new OpcodeSpec("OpcodeC8", 0x0B) },
            { 0xC9, new OpcodeSpec("OpcodeC9", 0x02) },
            { 0xCA, new OpcodeSpec("OpcodeCA", 0x0A) },
            { 0xCB, new OpcodeSpec("OpcodeCB", 0x03) },
            { 0xCC, new OpcodeSpec("OpcodeCC", 0x00) },
            { 0xCD, new OpcodeSpec("OpcodeCD", 0x00) },
            { 0xCE, new OpcodeSpec("OpcodeCE", 0x00) },
            { 0xCF, new OpcodeSpec("OpcodeCF", 0x00) },
            { 0xD0, new OpcodeSpec("CameraPosition", 0x07) },
            { 0xD1, new OpcodeSpec("SetCameraPosition", 0x01) },
            { 0xD2, new OpcodeSpec("OpcodeD2", 0x03) },
            { 0xD3, new OpcodeSpec("OpcodeD3", 0x03) },
            { 0xD4, new OpcodeSpec("CameraHeight", 0x04) },
            { 0xD5, new OpcodeSpec("OpcodeD5", 0x09) },
            { 0xD6, new OpcodeSpec("OpcodeD6", 0x00) },
            { 0xD7, new OpcodeSpec("OpcodeD7", 0x0A) },
            { 0xD8, new OpcodeSpec("OpcodeD8", 0x0B) },
            { 0xD9, new OpcodeSpec("OpcodeD9", 0x02) },
            { 0xDA, new OpcodeSpec("OpcodeDA", 0x0A) },
            { 0xDB, new OpcodeSpec("OpcodeDB", 0x03) },
            { 0xDC, new OpcodeSpec("OpcodeDC", 0x00) },
            { 0xDD, new OpcodeSpec("OpcodeDD", 0x00) },
            { 0xDE, new OpcodeSpec("OpcodeDE", 0x02) },
            { 0xDF, new OpcodeSpec("OpcodeDF", 0x02) },
            { 0xE0, new OpcodeSpec("CameraWait", 0x02) },
            { 0xE1, new OpcodeSpec("SetScreenEffectEnabled", 0x02) },
            { 0xE2, new OpcodeSpec("CameraRollTween", 0x06) },
            { 0xE3, new OpcodeSpec("ScreenEffectAngleTween", 0x06) },
            { 0xE4, new OpcodeSpec("ScreenEffectScaleTween", 0x05) },
            { 0xE5, new OpcodeSpec("ScreenEffectColorTween", 0x06) },
            { 0xE6, new OpcodeSpec("ScreenEffectOffsetTween", 0x05) },
            { 0xE7, new OpcodeSpec("SetScreenEffectMode", 0x02) },
            { 0xE8, new OpcodeSpec("OpcodeE8", 0x03) },
            { 0xE9, new OpcodeSpec("RecenterCamera", 0x03) },
            { 0xEA, new OpcodeSpec("CameraZoom", 0x04) },
            { 0xEB, new OpcodeSpec("CameraNearClip", 0x04) },
            { 0xEC, new OpcodeSpec("CameraFarClip", 0x04) },
            { 0xED, new OpcodeSpec("ScreenEffectParamPairTween", 0x05) },
            { 0xEE, new OpcodeSpec("OpcodeEE", 0x00) },
            { 0xEF, new OpcodeSpec("CameraOscillationControl", 0x06) },
            { 0xF0, new OpcodeSpec("Wait", 0x02) },
            { 0xF1, new OpcodeSpec("OpcodeF1", 0x02) },
            { 0xF2, new OpcodeSpec("OpcodeF2", 0x05) },
            { 0xF3, new OpcodeSpec("OpcodeF3", 0x02) },
            { 0xF4, new OpcodeSpec("ScriptCallSlotActive", 0x02) },
            { 0xF5, new OpcodeSpec("ScriptCall", 0x04) },
            { 0xF6, new OpcodeSpec("ScriptReturn", 0x02) },
            { 0xF7, new OpcodeSpec("OpcodeF7", 0x03) },
            { 0xF8, new OpcodeSpec("OpcodeF8", 0x01) },
            { 0xF9, new OpcodeSpec("OpcodeF9", 0x01) },
            { 0xFA, new OpcodeSpec("OpcodeFA", 0x01) },
            { 0xFB, new OpcodeSpec("OpcodeFB", 0x01) },
            { 0xFC, new OpcodeSpec("OpcodeFC", 0x01) },
            { 0xFD, new OpcodeSpec("OpcodeFD", 0x01) },
            { 0xFE, new OpcodeSpec("OpcodeFE", 0x01) },
            { 0xFF, new OpcodeSpec("return", 0x01) },
        };

        private static readonly List<RoomScriptOpcodeChoice> opcodeChoices = BuildOpcodeChoices();

        private readonly int len;
        private bool parsed = false;
        private string summaryText = "";
        private int sectionLen = 0;
        private int originalDialogPtr = 0;
        private int ptrUnknown1 = 0;
        private int ptrUnknown2 = 0;
        private ushort unk1 = 0;
        private ushort unk2 = 0;
        private ushort unk3 = 0;
        private ushort unk4 = 0;
        private byte[] dialogTail = new byte[0];
        private readonly List<RoomScriptOpcodeEntry> opcodes = new List<RoomScriptOpcodeEntry>();
        private readonly List<string> dialogs = new List<string>();

        public RoomScriptSection(string url, int pos, int len, DirRec rec):
        base(url, pos, rec) {
            this.len = len;
        }

        [ReadOnly(true)][Category(" INTERNAL")]
        public int LenSection {
            get { return len; }
            set {}
        }

        [ReadOnly(true)]
        [Category("SCRIPT")]
        [DisplayName("Section Length")]
        public string SectionLengthHex {
            get {
                EnsureParsed();
                return "0x" + sectionLen.ToString("X");
            }
        }

        [ReadOnly(true)]
        [Category("SCRIPT")]
        [DisplayName("Dialog Pointer")]
        public string DialogPointerHex {
            get {
                EnsureParsed();
                return "0x" + originalDialogPtr.ToString("X");
            }
        }

        [ReadOnly(true)]
        [Category("SCRIPT")]
        [DisplayName("Unknown Pointer 1")]
        public string UnknownPointer1Hex {
            get {
                EnsureParsed();
                return "0x" + ptrUnknown1.ToString("X");
            }
        }

        [ReadOnly(true)]
        [Category("SCRIPT")]
        [DisplayName("Unknown Pointer 2")]
        public string UnknownPointer2Hex {
            get {
                EnsureParsed();
                return "0x" + ptrUnknown2.ToString("X");
            }
        }

        [ReadOnly(true)]
        [Category("SCRIPT")]
        [DisplayName("Opcodes")]
        public int OpcodeCount {
            get {
                EnsureParsed();
                return opcodes.Count;
            }
        }

        [ReadOnly(true)]
        [Category("SCRIPT")]
        [DisplayName("Dialogs")]
        public int DialogCount {
            get {
                EnsureParsed();
                return dialogs.Count;
            }
        }

        public override int GetLen() {
            return len;
        }

        [Browsable(false)]
        public IList<RoomScriptOpcodeEntry> Opcodes {
            get {
                EnsureParsed();
                return opcodes.AsReadOnly();
            }
        }

        [Browsable(false)]
        public IList<string> Dialogs {
            get {
                EnsureParsed();
                return dialogs.AsReadOnly();
            }
        }

        [Browsable(false)]
        public IList<RoomScriptOpcodeChoice> OpcodeChoices {
            get {
                return opcodeChoices.AsReadOnly();
            }
        }

        public string GetSummaryText() {
            EnsureParsed();
            return summaryText;
        }

        public string GetDisplayText() {
            EnsureParsed();
            return "Script (" + opcodes.Count + ")";
        }

        public bool OpenSection(TreeNode root) {
            EnsureParsed();
            if (root != null) {
                root.Text = GetDisplayText();
            }
            return true;
        }

        public int GetArgumentCount(byte opcode) {
            OpcodeSpec spec = GetOpcodeSpec(opcode);
            if (spec.Size <= 0) {
                return 0;
            }
            return spec.Size - 1;
        }

        public bool TryAddOpcode(int index, byte opcode, out string error) {
            return TryInsertOpcode(index, opcode, new byte[Math.Max(0, GetArgumentCount(opcode))], out error);
        }

        public bool TryInsertOpcode(int index, byte opcode, byte[] args, out string error) {
            EnsureParsed();
            List<RoomScriptOpcodeEntry> next = CloneOpcodes();
            int safeIndex = Math.Max(0, Math.Min(index, next.Count));
            RoomScriptOpcodeEntry entry;
            if (!TryCreateEditableEntry(opcode, args, out entry, out error)) {
                return false;
            }
            next.Insert(safeIndex, entry);
            return TryWriteOpcodes(next, out error);
        }

        public bool TryDeleteOpcode(int index, out string error) {
            EnsureParsed();
            if ((index < 0) || (index >= opcodes.Count)) {
                error = "Opcode index is out of range.";
                return false;
            }

            List<RoomScriptOpcodeEntry> next = CloneOpcodes();
            next.RemoveAt(index);
            return TryWriteOpcodes(next, out error);
        }

        public bool TryMoveOpcode(int index, int newIndex, out string error) {
            EnsureParsed();
            if ((index < 0) || (index >= opcodes.Count)) {
                error = "Opcode index is out of range.";
                return false;
            }
            if ((newIndex < 0) || (newIndex >= opcodes.Count)) {
                error = "Target opcode index is out of range.";
                return false;
            }
            if (index == newIndex) {
                error = null;
                return true;
            }

            List<RoomScriptOpcodeEntry> next = CloneOpcodes();
            RoomScriptOpcodeEntry moving = next[index];
            next.RemoveAt(index);
            next.Insert(newIndex, CloneOpcode(moving));
            return TryWriteOpcodes(next, out error);
        }

        public bool TryUpdateOpcode(int index, byte opcode, byte[] args, out string error) {
            EnsureParsed();
            if ((index < 0) || (index >= opcodes.Count)) {
                error = "Opcode index is out of range.";
                return false;
            }

            List<RoomScriptOpcodeEntry> next = CloneOpcodes();
            RoomScriptOpcodeEntry entry;
            if (!TryCreateEditableEntry(opcode, args, out entry, out error)) {
                return false;
            }
            next[index] = entry;
            return TryWriteOpcodes(next, out error);
        }

        private void EnsureParsed() {
            if (parsed) {
                return;
            }
            parsed = true;

            opcodes.Clear();
            dialogs.Clear();
            dialogTail = new byte[0];
            summaryText = "";

            byte[] raw = RawBytes();
            if ((raw == null) || (raw.Length < 0x10)) {
                summaryText = "Unable to read script section.";
                return;
            }

            sectionLen = ReadU16(raw, 0x00);
            originalDialogPtr = ReadU16(raw, 0x02);
            ptrUnknown1 = ReadU16(raw, 0x04);
            ptrUnknown2 = ReadU16(raw, 0x06);
            unk1 = ReadU16(raw, 0x08);
            unk2 = ReadU16(raw, 0x0A);
            unk3 = ReadU16(raw, 0x0C);
            unk4 = ReadU16(raw, 0x0E);

            int safeDialogPtr = originalDialogPtr;
            if ((safeDialogPtr < 0x10) || (safeDialogPtr > raw.Length)) {
                safeDialogPtr = raw.Length;
            }
            originalDialogPtr = safeDialogPtr;

            int dialogLimit = raw.Length;
            if ((ptrUnknown1 > 0) && (ptrUnknown1 < dialogLimit)) {
                dialogLimit = ptrUnknown1;
            }
            if ((ptrUnknown2 > 0) && (ptrUnknown2 < dialogLimit)) {
                dialogLimit = ptrUnknown2;
            }
            if (dialogLimit < safeDialogPtr) {
                dialogLimit = raw.Length;
            }

            dialogTail = Slice(raw, safeDialogPtr, raw.Length - safeDialogPtr);
            dialogs.AddRange(ParseDialogs(raw, safeDialogPtr, dialogLimit));

            int offset = 0x10;
            while (offset < safeDialogPtr) {
                byte opcode = raw[offset];
                OpcodeSpec spec = GetOpcodeSpec(opcode);
                int size = spec.Size;
                if (size <= 0) {
                    opcodes.Add(new RoomScriptOpcodeEntry(
                        offset,
                        opcode,
                        spec.Name,
                        size,
                        new byte[0],
                        false,
                        true
                    ));
                    break;
                }

                int available = Math.Min(size, safeDialogPtr - offset);
                if (available <= 0) {
                    break;
                }

                bool truncated = available < size;
                byte[] args = Slice(raw, offset + 1, Math.Max(0, available - 1));
                opcodes.Add(new RoomScriptOpcodeEntry(
                    offset,
                    opcode,
                    spec.Name,
                    size,
                    args,
                    truncated,
                    false
                ));

                if (truncated) {
                    break;
                }
                offset += size;
            }

            summaryText = BuildSummaryText();
        }

        private string BuildSummaryText() {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(GetText());
            sb.AppendLine("Container: " + GetRec().GetFileName());
            sb.AppendLine("Offset: 0x" + GetPos().ToString("X8"));
            sb.AppendLine("Length: 0x" + len.ToString("X8") + " (" + len + ")");
            sb.AppendLine("Header Section Length: 0x" + sectionLen.ToString("X"));
            sb.AppendLine("Dialog Pointer: 0x" + originalDialogPtr.ToString("X"));
            sb.AppendLine("Unknown Pointer 1: 0x" + ptrUnknown1.ToString("X"));
            sb.AppendLine("Unknown Pointer 2: 0x" + ptrUnknown2.ToString("X"));
            sb.AppendLine("Opcodes: " + opcodes.Count);
            sb.AppendLine("Dialogs: " + dialogs.Count);
            return sb.ToString().TrimEnd();
        }

        private bool TryWriteOpcodes(List<RoomScriptOpcodeEntry> next, out string error) {
            EnsureParsed();

            List<byte> codeBytes = new List<byte>();
            foreach (RoomScriptOpcodeEntry entry in next) {
                OpcodeSpec spec = GetOpcodeSpec(entry.Opcode);
                if (spec.Size <= 0) {
                    error = "Opcode 0x" + entry.Opcode.ToString("X2") + " has unresolved size and cannot be serialized safely.";
                    return false;
                }
                int expectedArgCount = Math.Max(0, spec.Size - 1);
                if (entry.Args.Length != expectedArgCount) {
                    error = "Opcode 0x" + entry.Opcode.ToString("X2") + " expects " + expectedArgCount + " argument byte(s).";
                    return false;
                }

                codeBytes.Add(entry.Opcode);
                codeBytes.AddRange(entry.Args);
            }

            int newDialogPtr = 0x10 + codeBytes.Count;
            if (newDialogPtr + dialogTail.Length > len) {
                error = "Not enough free space in this script section for that edit.";
                return false;
            }

            byte[] output = new byte[len];
            int delta = newDialogPtr - originalDialogPtr;

            WriteU16(output, 0x00, (ushort)len);
            WriteU16(output, 0x02, (ushort)newDialogPtr);
            WriteU16(output, 0x04, (ushort)AdjustTailPointer(ptrUnknown1, delta));
            WriteU16(output, 0x06, (ushort)AdjustTailPointer(ptrUnknown2, delta));
            WriteU16(output, 0x08, unk1);
            WriteU16(output, 0x0A, unk2);
            WriteU16(output, 0x0C, unk3);
            WriteU16(output, 0x0E, unk4);

            for (int i = 0; i < codeBytes.Count; i++) {
                output[0x10 + i] = codeBytes[i];
            }
            Array.Copy(dialogTail, 0, output, newDialogPtr, dialogTail.Length);

            if (!UndoRedo.Exec(new BindArray(this, GetPos(), len, output))) {
                error = "Failed to write script section.";
                return false;
            }

            parsed = false;
            EnsureParsed();
            Publisher.Publish(this);
            error = null;
            return true;
        }

        private bool TryCreateEditableEntry(byte opcode, byte[] args, out RoomScriptOpcodeEntry entry, out string error) {
            OpcodeSpec spec = GetOpcodeSpec(opcode);
            int expectedArgCount = Math.Max(0, spec.Size - 1);
            if (spec.Size <= 0) {
                entry = null;
                error = "Opcode 0x" + opcode.ToString("X2") + " has unresolved size and is not editable yet.";
                return false;
            }
            if (args == null) {
                args = new byte[0];
            }
            if (args.Length != expectedArgCount) {
                entry = null;
                error = "Opcode 0x" + opcode.ToString("X2") + " expects " + expectedArgCount + " argument byte(s).";
                return false;
            }

            entry = new RoomScriptOpcodeEntry(0, opcode, spec.Name, spec.Size, args, false, false);
            error = null;
            return true;
        }

        private List<RoomScriptOpcodeEntry> CloneOpcodes() {
            List<RoomScriptOpcodeEntry> copy = new List<RoomScriptOpcodeEntry>();
            foreach (RoomScriptOpcodeEntry opcode in opcodes) {
                copy.Add(CloneOpcode(opcode));
            }
            return copy;
        }

        private static RoomScriptOpcodeEntry CloneOpcode(RoomScriptOpcodeEntry opcode) {
            return new RoomScriptOpcodeEntry(
                opcode.Offset,
                opcode.Opcode,
                opcode.OpcodeName,
                opcode.Size,
                opcode.Args,
                opcode.Truncated,
                opcode.UnsafeAdvance
            );
        }

        private int AdjustTailPointer(int pointer, int delta) {
            if ((pointer <= 0) || (pointer < originalDialogPtr)) {
                return pointer;
            }
            return pointer + delta;
        }

        private OpcodeSpec GetOpcodeSpec(byte opcode) {
            if (opcodeSpecs.ContainsKey(opcode)) {
                return opcodeSpecs[opcode];
            }
            return new OpcodeSpec("Opcode" + opcode.ToString("X2"), 1);
        }

        private static List<RoomScriptOpcodeChoice> BuildOpcodeChoices() {
            List<RoomScriptOpcodeChoice> choices = new List<RoomScriptOpcodeChoice>();
            foreach (KeyValuePair<byte, OpcodeSpec> pair in opcodeSpecs.OrderBy(x => x.Key)) {
                choices.Add(new RoomScriptOpcodeChoice(pair.Key, pair.Value.Name, pair.Value.Size));
            }
            return choices;
        }

        private static List<string> ParseDialogs(byte[] section, int ptrDialog, int dialogLimit) {
            List<string> result = new List<string>();
            if ((ptrDialog <= 0) || (ptrDialog + 2 > section.Length) || (ptrDialog >= dialogLimit)) {
                return result;
            }

            int count = ReadU16(section, ptrDialog);
            if (count == 0) {
                return result;
            }

            int start = ptrDialog + count * 2;
            for (int i = 0; i < count; i++) {
                if (start >= dialogLimit) {
                    result.Add("<truncated>");
                    continue;
                }

                int end = start;
                while ((end < dialogLimit) && (section[end] != 0xE7)) {
                    end++;
                }
                if (end < dialogLimit) {
                    end++;
                }
                result.Add(DecodeDialogText(section, start, end));
                start = end;
                while ((start < dialogLimit) && (section[start] == 0xEB)) {
                    start++;
                }
            }
            return result;
        }

        private static string DecodeDialogText(byte[] data, int start, int end) {
            if ((start < 0) || (end < start) || (start >= data.Length)) {
                return "";
            }

            StringBuilder sb = new StringBuilder();
            int limit = Math.Min(end, data.Length);
            for (int i = start; i < limit; i++) {
                byte b = data[i];
                if (b == 0xE7) {
                    break;
                }
                if (b == 0xEB) {
                    continue;
                }
                if (b == 0xE8) {
                    sb.Append(Environment.NewLine);
                    continue;
                }
                if (b == 0xFA) {
                    if ((i + 1) < limit) {
                        if ((sb.Length > 0) && !char.IsWhiteSpace(sb[sb.Length - 1])) {
                            sb.Append(' ');
                        }
                        i++;
                    }
                    continue;
                }
                if ((b == 0xF8) || (b == 0xF9) || (b == 0xFB) || (b == 0xFC) || (b == 0xFD) || (b == 0xFE)) {
                    if ((i + 1) < limit) {
                        i++;
                    }
                    continue;
                }
                if (b == 0xF6) {
                    continue;
                }

                byte ascii = Kildean.to_ascii[b];
                if (ascii != 0) {
                    Encoding enc = Encoding.GetEncoding("iso-8859-1");
                    sb.Append(enc.GetString(new byte[] { ascii }));
                }
            }

            string text = sb.ToString().Trim();
            while (text.StartsWith(Environment.NewLine)) {
                text = text.Substring(Environment.NewLine.Length).TrimStart();
            }
            return text;
        }

        private static ushort ReadU16(byte[] data, int offset) {
            if ((offset < 0) || ((offset + 1) >= data.Length)) {
                return 0;
            }
            return BitConverter.ToUInt16(data, offset);
        }

        private static byte[] Slice(byte[] data, int offset, int len) {
            if ((data == null) || (len <= 0) || (offset >= data.Length)) {
                return new byte[0];
            }

            int safeOffset = Math.Max(0, offset);
            int safeLen = Math.Min(len, data.Length - safeOffset);
            byte[] result = new byte[safeLen];
            Buffer.BlockCopy(data, safeOffset, result, 0, safeLen);
            return result;
        }

        private static void WriteU16(byte[] data, int offset, ushort value) {
            if ((offset < 0) || ((offset + 1) >= data.Length)) {
                return;
            }
            byte[] bytes = BitConverter.GetBytes(value);
            data[offset] = bytes[0];
            data[offset + 1] = bytes[1];
        }
    }
}
