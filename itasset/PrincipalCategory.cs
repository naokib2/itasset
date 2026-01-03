using System;

namespace itasset {
    public static class PrincipalCategory {
        public enum Kind {
            None = 0,
            Everyone = 1,
            AuthenticatedUsers = 2,
            Users = 3,
            Admins = 4
        }

        public static Kind Categorize(string sid, string name) {
            // Prefer SID when available (stable)
            if (!string.IsNullOrWhiteSpace(sid)) {
                if (sid == "S-1-1-0") return Kind.Everyone;
                if (sid == "S-1-5-11") return Kind.AuthenticatedUsers;
                if (sid == "S-1-5-32-545") return Kind.Users; // BUILTIN\Users
                if (sid == "S-1-5-32-544") return Kind.Admins; // BUILTIN\Administrators
                // Domain Users ends with -513 (RID 513). We can't know full domain SID, so use suffix match.
                if (sid.EndsWith("-513", StringComparison.OrdinalIgnoreCase)) return Kind.Users;
                // Domain Admins ends with -512 (RID 512)
                if (sid.EndsWith("-512", StringComparison.OrdinalIgnoreCase)) return Kind.Admins;
            }

            if (!string.IsNullOrWhiteSpace(name)) {
                var n = name.Trim();

                if (n.Equals("Everyone", StringComparison.OrdinalIgnoreCase)) return Kind.Everyone;
                if (n.EndsWith("\\Everyone", StringComparison.OrdinalIgnoreCase)) return Kind.Everyone;

                if (n.Equals("Authenticated Users", StringComparison.OrdinalIgnoreCase)) return Kind.AuthenticatedUsers;
                if (n.EndsWith("\\Authenticated Users", StringComparison.OrdinalIgnoreCase)) return Kind.AuthenticatedUsers;

                if (n.Equals("BUILTIN\\Users", StringComparison.OrdinalIgnoreCase)) return Kind.Users;
                if (n.EndsWith("\\Domain Users", StringComparison.OrdinalIgnoreCase)) return Kind.Users;

                if (n.Equals("BUILTIN\\Administrators", StringComparison.OrdinalIgnoreCase)) return Kind.Admins;
                if (n.EndsWith("\\Domain Admins", StringComparison.OrdinalIgnoreCase)) return Kind.Admins;
            }

            return Kind.None;
        }
    }
}
