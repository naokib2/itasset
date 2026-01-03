using System;
using System.Management;
using System.Security.Principal;

namespace itasset {
    public static class SharePermissionReader {

        public static NormalizedPermissions GetNormalizedSharePermissions(string shareName) {
            var perms = new NormalizedPermissions();

            try {
                var path = "Win32_LogicalShareSecuritySetting.Name='" + shareName.Replace("'", "''") + "'";
                using (var mo = new ManagementObject(path)) {
                    var outParams = mo.InvokeMethod("GetSecurityDescriptor", null, null);
                    if (outParams == null) return perms;
                    var rvObj = outParams["ReturnValue"];
                    if (rvObj == null || (uint)rvObj != 0) return perms;

                    var sd = (ManagementBaseObject)outParams["Descriptor"];
                    if (sd == null) return perms;

                    var dacl = sd["DACL"] as ManagementBaseObject[];
                    if (dacl == null) return perms;

                    // We take the maximum ALLOW permission for each normalized principal (deny handling is minimal)
                    foreach (var ace in dacl) {
                        var aceTypeObj = ace["AceType"];
                        var maskObj = ace["AccessMask"];
                        var trusteeObj = ace["Trustee"];

                        if (maskObj == null || trusteeObj == null) continue;

                        uint mask = (uint)maskObj;
                        var p = PermissionUtils.FromShareAccessMask(mask);

                        bool isDeny = false;
                        if (aceTypeObj != null) {
                            // 0 = ACCESS_ALLOWED, 1 = ACCESS_DENIED
                            isDeny = ((uint)aceTypeObj) == 1;
                        }

                        var trustee = (ManagementBaseObject)trusteeObj;
                        string sidStr = null;
                        try {
                            if (trustee["SID"] != null) {
                                var sidBytes = (byte[])trustee["SID"];
                                sidStr = new SecurityIdentifier(sidBytes, 0).Value;
                            }
                        } catch { }

                        string name = null;
                        try {
                            if (trustee["Name"] != null) {
                                var dom = trustee["Domain"] == null ? "" : trustee["Domain"].ToString();
                                var nm = trustee["Name"].ToString();
                                name = string.IsNullOrEmpty(dom) ? nm : (dom + "\\" + nm);
                            }
                        } catch { }

                        var cat = PrincipalCategory.Categorize(sidStr, name);

                        if (cat == PrincipalCategory.Kind.None) continue;

                        if (isDeny && PermissionUtils.IsWrite(p)) {
                            // Minimal: if deny write for that principal, mark DENY
                            ApplyDeny(perms, cat);
                        } else if (!isDeny) {
                            ApplyAllowMax(perms, cat, p);
                        }
                    }
                }
            } catch {
                // swallow; leave NONE
            }

            return perms;
        }

        private static void ApplyAllowMax(NormalizedPermissions perms, PrincipalCategory.Kind cat, PermLevel p) {
            switch (cat) {
                case PrincipalCategory.Kind.Everyone:
                    perms.Everyone = PermissionUtils.Max(perms.Everyone, p);
                    break;
                case PrincipalCategory.Kind.AuthenticatedUsers:
                    perms.AuthenticatedUsers = PermissionUtils.Max(perms.AuthenticatedUsers, p);
                    break;
                case PrincipalCategory.Kind.Users:
                    perms.Users = PermissionUtils.Max(perms.Users, p);
                    break;
                case PrincipalCategory.Kind.Admins:
                    perms.Admins = PermissionUtils.Max(perms.Admins, p);
                    break;
            }
        }

        private static void ApplyDeny(NormalizedPermissions perms, PrincipalCategory.Kind cat) {
            switch (cat) {
                case PrincipalCategory.Kind.Everyone:
                    perms.Everyone = PermLevel.DENY;
                    break;
                case PrincipalCategory.Kind.AuthenticatedUsers:
                    perms.AuthenticatedUsers = PermLevel.DENY;
                    break;
                case PrincipalCategory.Kind.Users:
                    perms.Users = PermLevel.DENY;
                    break;
                case PrincipalCategory.Kind.Admins:
                    perms.Admins = PermLevel.DENY;
                    break;
            }
        }
    }
}
