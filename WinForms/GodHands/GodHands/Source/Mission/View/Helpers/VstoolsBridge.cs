using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;

namespace GodHands {
    public class VstoolsHttpServer {
        private readonly string root;
        private readonly HttpListener listener;
        private readonly Thread thread;

        public VstoolsHttpServer(string root, int port) {
            this.root = Path.GetFullPath(root);
            listener = new HttpListener();
            listener.Prefixes.Add("http://127.0.0.1:" + port + "/");
            listener.Start();
            BaseUrl = "http://127.0.0.1:" + port + "/";
            thread = new Thread(new ThreadStart(ListenLoop));
            thread.IsBackground = true;
            thread.Start();
        }

        public string BaseUrl { get; private set; }

        public string Root {
            get { return root; }
        }

        private void ListenLoop() {
            while (listener.IsListening) {
                try {
                    HttpListenerContext context = listener.GetContext();
                    ThreadPool.QueueUserWorkItem(new WaitCallback(ProcessRequest), context);
                } catch {
                    break;
                }
            }
        }

        private void ProcessRequest(object state) {
            HttpListenerContext context = state as HttpListenerContext;
            if (context == null) {
                return;
            }

            try {
                string relative = context.Request.Url.AbsolutePath.TrimStart('/');
                if (relative.Length == 0) {
                    relative = "index.html";
                }
                relative = relative.Replace('/', Path.DirectorySeparatorChar);

                string path = Path.GetFullPath(Path.Combine(root, relative));
                if (!path.StartsWith(root, StringComparison.OrdinalIgnoreCase) || (!File.Exists(path))) {
                    context.Response.StatusCode = 404;
                    context.Response.Close();
                    return;
                }

                byte[] data = File.ReadAllBytes(path);
                context.Response.ContentType = GuessContentType(Path.GetExtension(path));
                context.Response.ContentLength64 = data.Length;
                context.Response.OutputStream.Write(data, 0, data.Length);
                context.Response.OutputStream.Flush();
                context.Response.Close();
            } catch {
                try {
                    context.Response.StatusCode = 500;
                    context.Response.Close();
                } catch {}
            }
        }

        private static string GuessContentType(string ext) {
            switch ((ext ?? "").ToLowerInvariant()) {
            case ".html": return "text/html; charset=utf-8";
            case ".js": return "application/javascript; charset=utf-8";
            case ".css": return "text/css; charset=utf-8";
            case ".json": return "application/json; charset=utf-8";
            case ".png": return "image/png";
            case ".jpg":
            case ".jpeg": return "image/jpeg";
            case ".gif": return "image/gif";
            case ".svg": return "image/svg+xml";
            default: return "application/octet-stream";
            }
        }
    }

    public static class VstoolsBridge {
        private static VstoolsHttpServer server = null;
        private static readonly object embeddedRootLock = new object();
        private static string embeddedRoot = null;
        private static readonly string[] embeddedStaticFiles = new string[] {
            "index.html",
            "css/debug.css",
            "css/main.css",
            "dist/Akao.js",
            "dist/ARM.js",
            "dist/ARMRoom.js",
            "dist/Collada.js",
            "dist/FBC.js",
            "dist/FBT.js",
            "dist/FrameBuffer.js",
            "dist/GIM.js",
            "dist/MPD.js",
            "dist/MPDFace.js",
            "dist/MPDGroup.js",
            "dist/MPDMesh.js",
            "dist/P.js",
            "dist/Reader.js",
            "dist/SEQ.js",
            "dist/SEQAnimation.js",
            "dist/shp-seq-resolver.js",
            "dist/SHP.js",
            "dist/SOUND.js",
            "dist/Text.js",
            "dist/three.js",
            "dist/TIM.js",
            "dist/Viewer.js",
            "dist/VSTOOLS.js",
            "dist/WEP.js",
            "dist/WEPBone.js",
            "dist/WEPFace.js",
            "dist/WEPGroup.js",
            "dist/WEPPalette.js",
            "dist/WEPTextureMap.js",
            "dist/WEPVertex.js",
            "dist/ZND.js",
            "dist/ZUD.js",
            "dist/ui/ui-panel.js",
        };

        public static bool CanLaunch(object obj) {
            return (obj is Actor)
                || (obj is Room)
                || (obj is ActorModelSection)
                || (obj is ActorSeqAnimation)
                || (obj is ZUD);
        }

        public static bool Open(object obj) {
            try {
                string url;
                string error;
                if (!TryGetLaunchUrl(obj, out url, out error)) {
                    return Logger.Fail(error);
                }
                Process.Start(url);
                return Logger.Pass("Opened in vstools");
            } catch (Exception ex) {
                return Logger.Fail("Failed to open vstools: " + ex.Message);
            }
        }

        public static bool TryGetLaunchUrl(object obj, out string url, out string error) {
            url = null;
            error = null;

            try {
                string vstoolsRoot = FindVstoolsRoot();
                if (vstoolsRoot == null) {
                    error = "Could not find _refs/vstools next to this workspace.";
                    return false;
                }

                if (!EnsureServer(vstoolsRoot)) {
                    error = "Could not start local vstools server on localhost.";
                    return false;
                }

                url = BuildLaunchUrl(obj, vstoolsRoot);
                if (url == null) {
                    error = "Selected item cannot be opened in vstools.";
                    return false;
                }

                return true;
            } catch (Exception ex) {
                error = "Failed to prepare vstools: " + ex.Message;
                return false;
            }
        }

        private static bool EnsureServer(string vstoolsRoot) {
            if ((server != null) && (string.Compare(server.Root, Path.GetFullPath(vstoolsRoot), true) == 0)) {
                return true;
            }

            for (int port = 17890; port <= 17910; port++) {
                try {
                    server = new VstoolsHttpServer(vstoolsRoot, port);
                    return true;
                } catch {}
            }
            return Logger.Fail("Could not start local vstools server on localhost.");
        }

        private static string BuildLaunchUrl(object obj, string vstoolsRoot) {
            Actor actor = obj as Actor;
            Room room = obj as Room;
            ActorModelSection section = obj as ActorModelSection;
            ActorSeqAnimation animation = obj as ActorSeqAnimation;
            ZUD zud = obj as ZUD;
            if (animation != null) {
                section = animation.Section;
            }
            if (actor != null) {
                return BuildZudLaunchUrl(actor.GetZudRec(), vstoolsRoot, "auto", null);
            }
            if (room != null) {
                return BuildMpdLaunchUrl(room, vstoolsRoot);
            }
            if (zud != null) {
                string zudName = Path.GetFileName(zud.GetUrl());
                return BuildZudLaunchUrl(Model.GetRec(zudName), vstoolsRoot, "auto", null, zud.GetUrl());
            }
            if (section == null) {
                return null;
            }

            string bundleName = SafeName(section.GetUrl());
            string bundleDir = Path.Combine(vstoolsRoot, "data", "launches", "godhands", bundleName);
            if (!Directory.Exists(bundleDir)) {
                Directory.CreateDirectory(bundleDir);
            }

            if ((section.SectionKind == ActorModelSectionKind.CommonSeq)
            || (section.SectionKind == ActorModelSectionKind.BattleSeq)) {
                return BuildZudLaunchUrl(
                    section.GetRec(),
                    vstoolsRoot,
                    ((ActorSeqSection)section).GetSequencePreference(),
                    animation,
                    section.GetRec().GetUrl()
                );
            }

            string name = section.GetExportName();
            if (string.IsNullOrEmpty(name)) {
                name = section.GetText().Replace(' ', '_');
            }
            byte[] raw = section.RawBytes();
            if (raw == null) {
                return null;
            }
            File.WriteAllBytes(Path.Combine(bundleDir, name), raw);
            return server.BaseUrl
                + "index.html?file1="
                + Uri.EscapeDataString(ToViewerRelative(bundleName, name))
                + "&embedded=1";
        }

        private static string BuildMpdLaunchUrl(Room room, string vstoolsRoot) {
            DirRec zndRec = room.GetZndRec();
            DirRec mpdRec = room.GetMpdRec();
            if ((zndRec == null) || (mpdRec == null)) {
                return null;
            }

            string bundleName = SafeName(room.GetUrl());
            string bundleDir = Path.Combine(vstoolsRoot, "data", "launches", "godhands", bundleName);
            if (!Directory.Exists(bundleDir)) {
                Directory.CreateDirectory(bundleDir);
            }

            if (!WriteRecToBundle(bundleDir, zndRec.GetFileName(), zndRec)) {
                return null;
            }
            if (!WriteRecToBundle(bundleDir, mpdRec.GetFileName(), mpdRec)) {
                return null;
            }

            return server.BaseUrl
                + "index.html?file1="
                + Uri.EscapeDataString(ToViewerRelative(bundleName, zndRec.GetFileName()))
                + "&file2="
                + Uri.EscapeDataString(ToViewerRelative(bundleName, mpdRec.GetFileName()))
                + "&embedded=1";
        }

        private static byte[] ReadWholeFile(DirRec rec) {
            if (rec == null) {
                return null;
            }
            byte[] raw = new byte[rec.LenData];
            int pos = rec.LbaData * 2048;
            if (!RamDisk.Get(pos, rec.LenData, raw)) {
                return null;
            }
            return raw;
        }

        private static string ToViewerRelative(string bundleName, string fileName) {
            return "data/launches/godhands/" + bundleName + "/" + fileName;
        }

        private static bool WriteRecToBundle(string bundleDir, string fileName, DirRec rec) {
            byte[] raw = ReadWholeFile(rec);
            if (raw == null) {
                return false;
            }
            File.WriteAllBytes(Path.Combine(bundleDir, fileName), raw);
            return true;
        }

        private static string BuildZudLaunchUrl(
            DirRec zudRec,
            string vstoolsRoot,
            string seqPreference,
            ActorSeqAnimation animation,
            string sourceUrl = null) {
            if (zudRec == null) {
                return null;
            }

            string zudName = zudRec.GetFileName();
            byte[] zudRaw = ReadWholeFile(zudRec);
            if (zudRaw == null) {
                return null;
            }

            string bundleSeed = !string.IsNullOrEmpty(sourceUrl) ? sourceUrl : zudRec.GetUrl();
            string bundleName = SafeName(bundleSeed);
            string bundleDir = Path.Combine(vstoolsRoot, "data", "launches", "godhands", bundleName);
            if (!Directory.Exists(bundleDir)) {
                Directory.CreateDirectory(bundleDir);
            }

            File.WriteAllBytes(Path.Combine(bundleDir, zudName), zudRaw);

            StringBuilder url = new StringBuilder();
            url.Append(server.BaseUrl);
            url.Append("index.html?file1=");
            url.Append(Uri.EscapeDataString(ToViewerRelative(bundleName, zudName)));
            url.Append("&seq=");
            url.Append(Uri.EscapeDataString(string.IsNullOrEmpty(seqPreference) ? "auto" : seqPreference));
            if (animation != null) {
                url.Append("&anim=");
                url.Append(animation.AnimationId);
            }
            url.Append("&embedded=1");
            return url.ToString();
        }

        private static string FindVstoolsRoot() {
            string embedded = EnsureEmbeddedVstoolsRoot();
            if (embedded != null) {
                return embedded;
            }

            return FindExternalVstoolsRoot();
        }

        private static string FindExternalVstoolsRoot() {
            List<string> seeds = new List<string>();
            seeds.Add(AppDomain.CurrentDomain.BaseDirectory);
            seeds.Add(Environment.CurrentDirectory);

            foreach (string seed in seeds) {
                if (string.IsNullOrEmpty(seed)) {
                    continue;
                }

                DirectoryInfo dir = new DirectoryInfo(seed);
                while (dir != null) {
                    string probeA = Path.Combine(dir.FullName, "_refs", "vstools");
                    if (IsVstoolsRoot(probeA)) {
                        return probeA;
                    }

                    string probeB = Path.Combine(dir.FullName, "vstools");
                    if (IsVstoolsRoot(probeB)) {
                        return probeB;
                    }
                    dir = dir.Parent;
                }
            }
            return null;
        }

        private static string EnsureEmbeddedVstoolsRoot() {
            lock (embeddedRootLock) {
                if (!string.IsNullOrEmpty(embeddedRoot) && IsVstoolsRoot(embeddedRoot)) {
                    return embeddedRoot;
                }

                string baseDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                if (string.IsNullOrEmpty(baseDir)) {
                    baseDir = Path.GetTempPath();
                }
                if (string.IsNullOrEmpty(baseDir)) {
                    return null;
                }

                string root = Path.Combine(
                    baseDir,
                    "GodHands",
                    "EmbeddedVstools",
                    GetEmbeddedVersionStamp()
                );

                if (!ExtractEmbeddedVstools(root)) {
                    return null;
                }

                embeddedRoot = root;
                return embeddedRoot;
            }
        }

        private static string GetEmbeddedVersionStamp() {
            Assembly assembly = typeof(VstoolsBridge).Assembly;
            string version = "0";
            string lastWriteTicks = "0";

            try {
                Version assemblyVersion = assembly.GetName().Version;
                if (assemblyVersion != null) {
                    version = assemblyVersion.ToString();
                }
            } catch {}

            try {
                if (!string.IsNullOrEmpty(assembly.Location) && File.Exists(assembly.Location)) {
                    lastWriteTicks = File.GetLastWriteTimeUtc(assembly.Location).Ticks.ToString();
                }
            } catch {}

            return SafeName(version + "_" + lastWriteTicks);
        }

        private static bool ExtractEmbeddedVstools(string root) {
            try {
                Directory.CreateDirectory(root);
                foreach (string relativePath in embeddedStaticFiles) {
                    if (!ExtractEmbeddedFile(root, relativePath)) {
                        return false;
                    }
                }

                Directory.CreateDirectory(Path.Combine(root, "data", "launches", "godhands"));
                return IsVstoolsRoot(root);
            } catch {
                return false;
            }
        }

        private static bool ExtractEmbeddedFile(string root, string relativePath) {
            string targetPath = Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
            string targetDir = Path.GetDirectoryName(targetPath);
            if (!string.IsNullOrEmpty(targetDir) && !Directory.Exists(targetDir)) {
                Directory.CreateDirectory(targetDir);
            }

            string resourceName = "GodHands.EmbeddedVstools." + relativePath.Replace('/', '.');
            Assembly assembly = typeof(VstoolsBridge).Assembly;
            using (Stream input = assembly.GetManifestResourceStream(resourceName)) {
                if (input == null) {
                    return false;
                }

                using (FileStream output = new FileStream(targetPath, FileMode.Create, FileAccess.Write, FileShare.Read)) {
                    input.CopyTo(output);
                }
            }
            return true;
        }

        private static bool IsVstoolsRoot(string path) {
            return Directory.Exists(path)
                && File.Exists(Path.Combine(path, "index.html"))
                && (
                    File.Exists(Path.Combine(path, "dist", "Viewer.js"))
                    || File.Exists(Path.Combine(path, "src", "Viewer.js"))
                );
        }

        private static string SafeName(string value) {
            char[] invalid = Path.GetInvalidFileNameChars();
            StringBuilder sb = new StringBuilder();
            foreach (char ch in value) {
                bool bad = (ch == '/') || (ch == '\\') || (ch == ':');
                if (!bad) {
                    foreach (char invalidChar in invalid) {
                        if (ch == invalidChar) {
                            bad = true;
                            break;
                        }
                    }
                }
                sb.Append(bad ? '_' : ch);
            }
            return sb.ToString();
        }
    }
}
