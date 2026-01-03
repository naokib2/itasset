using System;
using System.Collections.Generic;

namespace itasset {
    public enum PermLevel {
        NONE = 0,
        READ = 1,
        WRITE = 2,
        CHANGE = 3,
        FULL = 4,
        DENY = -1 // handled separately
    }

    public static class PermissionUtils {
        public static string ToText(PermLevel p) {
            if (p == PermLevel.DENY) return "DENY";
            return p.ToString();
        }

        public static PermLevel Min(PermLevel a, PermLevel b) {
            // DENY dominates
            if (a == PermLevel.DENY || b == PermLevel.DENY) return PermLevel.DENY;
            return (PermLevel)Math.Min((int)a, (int)b);
        }

        public static bool IsWrite(PermLevel p) {
            return p == PermLevel.WRITE || p == PermLevel.CHANGE || p == PermLevel.FULL;
        }

        public static PermLevel FromShareAccessMask(uint mask) {
            // Well-known masks used by "Advanced Sharing" UI
            // FULL:   0x1F01FF
            // CHANGE: 0x1301BF
            // READ:   0x1200A9
            const uint FULL = 0x1F01FF;
            const uint CHANGE = 0x1301BF;
            const uint READ = 0x1200A9;

            if ((mask & FULL) == FULL) return PermLevel.FULL;
            if ((mask & CHANGE) == CHANGE) return PermLevel.CHANGE;
            if ((mask & READ) == READ) return PermLevel.READ;
            // fallback: detect any write-ish bits
            if ((mask & 0x00000002) != 0 || (mask & 0x00000004) != 0) return PermLevel.CHANGE;
            return PermLevel.READ;
        }

        public static PermLevel FromNtfsRights(System.Security.AccessControl.FileSystemRights rights) {
            // Rough mapping for auditing ransomware spread potential
            if ((rights & System.Security.AccessControl.FileSystemRights.FullControl) == System.Security.AccessControl.FileSystemRights.FullControl)
                return PermLevel.FULL;

            if ((rights & System.Security.AccessControl.FileSystemRights.Modify) == System.Security.AccessControl.FileSystemRights.Modify)
                return PermLevel.CHANGE;

            // Many combinations represent "write capability"
            var writeBits =
                System.Security.AccessControl.FileSystemRights.Write |
                System.Security.AccessControl.FileSystemRights.CreateFiles |
                System.Security.AccessControl.FileSystemRights.CreateDirectories |
                System.Security.AccessControl.FileSystemRights.AppendData |
                System.Security.AccessControl.FileSystemRights.WriteData |
                System.Security.AccessControl.FileSystemRights.WriteAttributes |
                System.Security.AccessControl.FileSystemRights.WriteExtendedAttributes |
                System.Security.AccessControl.FileSystemRights.Delete |
                System.Security.AccessControl.FileSystemRights.DeleteSubdirectoriesAndFiles;

            if ((rights & writeBits) != 0)
                return PermLevel.WRITE;

            // Read-ish
            return PermLevel.READ;
        }

        public static PermLevel MergeAllowDeny(PermLevel allowMax, bool hasDenyWrite) {
            if (hasDenyWrite) return PermLevel.DENY;
            return allowMax;
        }

        public static PermLevel Max(PermLevel a, PermLevel b) {
            if (a == PermLevel.DENY || b == PermLevel.DENY) return PermLevel.DENY;
            return (PermLevel)Math.Max((int)a, (int)b);
        }
    }
}
