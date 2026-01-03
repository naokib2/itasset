using System;
using System.Collections.Generic;
using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;

namespace itasset {
    public static class NtfsPermissionReader {

        public static NormalizedPermissions GetNormalizedNtfsPermissions(string folderPath) {
            var perms = new NormalizedPermissions();

            try {
                if (string.IsNullOrWhiteSpace(folderPath)) return perms;
                if (!Directory.Exists(folderPath)) return perms;

                var ds = Directory.GetAccessControl(folderPath);
                var rules = ds.GetAccessRules(true, true, typeof(SecurityIdentifier));

                // Track max allow and deny-write existence per category
                var allow = new Dictionary<PrincipalCategory.Kind, PermLevel>();
                var denyWrite = new Dictionary<PrincipalCategory.Kind, bool>();

                foreach (FileSystemAccessRule r in rules) {
                    if (r == null || r.IdentityReference == null) continue;

                    var sid = r.IdentityReference.Value;
                    // translate name best-effort for suffix checks
                    string name = null;
                    try { name = ((SecurityIdentifier)r.IdentityReference).Translate(typeof(NTAccount)).ToString(); } catch { }

                    var cat = PrincipalCategory.Categorize(sid, name);

                    // If not normalized principal, check if this is a write-capable principal and count as OtherWrite
                    if (cat == PrincipalCategory.Kind.None) {
                        if (r.AccessControlType == AccessControlType.Allow) {
                            var pOther = PermissionUtils.FromNtfsRights(r.FileSystemRights);
                            if (PermissionUtils.IsWrite(pOther)) perms.OtherWrite = true;
                        }
                        continue;
                    }

                    var p = PermissionUtils.FromNtfsRights(r.FileSystemRights);

                    if (r.AccessControlType == AccessControlType.Deny && PermissionUtils.IsWrite(p)) {
                        denyWrite[cat] = true;
                        continue;
                    }

                    if (r.AccessControlType == AccessControlType.Allow) {
                        PermLevel cur;
                        if (!allow.TryGetValue(cat, out cur)) cur = PermLevel.NONE;
                        allow[cat] = PermissionUtils.Max(cur, p);
                    }
                }

                perms.Everyone = PermissionUtils.MergeAllowDeny(GetAllow(allow, PrincipalCategory.Kind.Everyone), GetDeny(denyWrite, PrincipalCategory.Kind.Everyone));
                perms.AuthenticatedUsers = PermissionUtils.MergeAllowDeny(GetAllow(allow, PrincipalCategory.Kind.AuthenticatedUsers), GetDeny(denyWrite, PrincipalCategory.Kind.AuthenticatedUsers));
                perms.Users = PermissionUtils.MergeAllowDeny(GetAllow(allow, PrincipalCategory.Kind.Users), GetDeny(denyWrite, PrincipalCategory.Kind.Users));
                perms.Admins = PermissionUtils.MergeAllowDeny(GetAllow(allow, PrincipalCategory.Kind.Admins), GetDeny(denyWrite, PrincipalCategory.Kind.Admins));
            } catch {
                // swallow
            }

            return perms;
        }

        private static PermLevel GetAllow(Dictionary<PrincipalCategory.Kind, PermLevel> d, PrincipalCategory.Kind k) {
            PermLevel v;
            return d.TryGetValue(k, out v) ? v : PermLevel.NONE;
        }
        private static bool GetDeny(Dictionary<PrincipalCategory.Kind, bool> d, PrincipalCategory.Kind k) {
            bool v;
            return d.TryGetValue(k, out v) ? v : false;
        }
    }
}
