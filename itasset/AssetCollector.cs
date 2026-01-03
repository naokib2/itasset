using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Net.NetworkInformation;
using System.Threading.Tasks;

namespace itasset {
    public static class AssetCollectorRuntime {
        public static Task<AssetInfo> CollectAsync() {
            return Task.Run(new Func<AssetInfo>(Collect));
        }

        public static AssetInfo Collect() {
            var info = new AssetInfo();

            // Host/Domain/Manufacturer/Model/RAM
            using (var cs = new ManagementObjectSearcher("SELECT * FROM Win32_ComputerSystem")) {
                var mo = cs.Get().Cast<ManagementObject>().FirstOrDefault();
                if (mo != null) {
                    info.Hostname = Environment.MachineName;
                    info.Manufacturer = mo["Manufacturer"] == null ? null : mo["Manufacturer"].ToString();
                    info.Model = mo["Model"] == null ? null : mo["Model"].ToString();
                    info.Domain = mo["Domain"] == null ? null : mo["Domain"].ToString();
                    if (mo["TotalPhysicalMemory"] != null) {
                        double bytes = Convert.ToDouble(mo["TotalPhysicalMemory"]);
                        info.RAM_GB = Math.Round(bytes / (1024D * 1024D * 1024D), 2);
                    }
                }
            }

            // Serial
            using (var bios = new ManagementObjectSearcher("SELECT SerialNumber FROM Win32_BIOS")) {
                var mo = bios.Get().Cast<ManagementObject>().FirstOrDefault();
                info.SerialNumber = mo == null ? null : (mo["SerialNumber"] == null ? null : mo["SerialNumber"].ToString());
            }

            // OS
            using (var os = new ManagementObjectSearcher("SELECT Caption, Version FROM Win32_OperatingSystem")) {
                var mo = os.Get().Cast<ManagementObject>().FirstOrDefault();
                if (mo != null) {
                    info.OS = mo["Caption"] == null ? null : mo["Caption"].ToString();
                    info.OSVersion = mo["Version"] == null ? null : mo["Version"].ToString();
                }
            }

            // CPU
            using (var cpu = new ManagementObjectSearcher("SELECT Name FROM Win32_Processor")) {
                var mo = cpu.Get().Cast<ManagementObject>().FirstOrDefault();
                info.CPU = mo == null ? null : (mo["Name"] == null ? null : mo["Name"].ToString());
            }

            // Disk total (fixed)
            double totalGb = 0;
            using (var ld = new ManagementObjectSearcher("SELECT Size, DriveType FROM Win32_LogicalDisk")) {
                foreach (ManagementObject mo in ld.Get()) {
                    if (mo["DriveType"] != null && Convert.ToInt32(mo["DriveType"]) == 3 && mo["Size"] != null)
                        totalGb += Convert.ToDouble(mo["Size"]) / (1024D * 1024D * 1024D);
                }
            }
            info.Disk_TotalGB = (int)Math.Round(totalGb, 0);

            // GPU
            var gpus = new List<string>();
            using (var vc = new ManagementObjectSearcher("SELECT Name FROM Win32_VideoController")) {
                foreach (ManagementObject mo in vc.Get()) {
                    var n = mo["Name"] == null ? null : mo["Name"].ToString();
                    if (!string.IsNullOrWhiteSpace(n)) gpus.Add(n);
                }
            }
            info.GPU = string.Join(";", gpus.Distinct());

            // IP / MAC
            var ips = new List<string>();
            foreach (var nic in NetworkInterface.GetAllNetworkInterfaces().Where(n => n.OperationalStatus == OperationalStatus.Up)) {
                var p = nic.GetIPProperties();
                foreach (var ua in p.UnicastAddresses) {
                    if (ua.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork) {
                        var ip = ua.Address.ToString();
                        if (!ip.StartsWith("169.254.")) ips.Add(ip);
                    }
                }
            }
            info.IP = string.Join(";", ips.Distinct());

            var macs = new List<string>();
            foreach (var nic in NetworkInterface.GetAllNetworkInterfaces().Where(n => n.OperationalStatus == OperationalStatus.Up)) {
                var pa = nic.GetPhysicalAddress();
                if (pa != null) {
                    var s = pa.ToString();
                    if (!string.IsNullOrWhiteSpace(s)) {
                        var parts = new List<string>();
                        for (int i = 0; i < s.Length / 2; i++) {
                            parts.Add(s.Substring(i * 2, 2));
                        }
                        macs.Add(string.Join(":", parts));
                    }
                }
            }
            info.MAC = string.Join(";", macs.Distinct());

            // TPM
            try {
                var scope = new ManagementScope(@"\\.\root\CIMV2\Security\MicrosoftTpm"); scope.Connect();
                using (var s = new ManagementObjectSearcher(scope, new ObjectQuery("SELECT * FROM Win32_Tpm"))) {
                    info.TPM = s.Get().Count > 0 ? "True" : "False";
                }
            }
            catch {
                info.TPM = "";
            }

            // SecureBoot
            try {
                var scope = new ManagementScope(@"\\.\root\wmi"); scope.Connect();
                foreach (var cls in new[] { "MS_SecureBoot", "MSFT_SecureBoot" }) {
                    using (var s = new ManagementObjectSearcher(scope, new ObjectQuery("SELECT * FROM " + cls))) {
                        foreach (ManagementObject mo in s.Get()) {
                            if (mo.Properties["SecureBootEnabled"] != null) {
                                info.SecureBoot = Convert.ToString(mo["SecureBootEnabled"]);
                                goto SECBOOT_DONE;
                            }
                        }
                    }
                }
            SECBOOT_DONE:;
            }
            catch {
                info.SecureBoot = "";
            }

            // BitLocker（switch式→従来switch）
            try {
                var scope = new ManagementScope(@"\\.\root\CIMV2\Security\MicrosoftVolumeEncryption"); scope.Connect();
                using (var s = new ManagementObjectSearcher(scope, new ObjectQuery("SELECT ProtectionStatus FROM Win32_EncryptableVolume"))) {
                    foreach (ManagementObject mo in s.Get()) {
                        var st = mo["ProtectionStatus"]; // 0=Unknown, 1=Off, 2=On
                        if (st != null) {
                            var v = Convert.ToInt32(st);
                            string label;
                            switch (v) {
                                case 0: label = "Unknown"; break;
                                case 1: label = "Off"; break;
                                case 2: label = "On"; break;
                                default: label = v.ToString(); break;
                            }
                            info.BitLocker = label;
                            break;
                        }
                    }
                }
            }
            catch {
                info.BitLocker = "";
            }

            // AV：Security Center（3rd-party）
            try {
                var scope = new ManagementScope(@"\\.\root\SecurityCenter2"); scope.Connect();
                using (var s = new ManagementObjectSearcher(scope, new ObjectQuery("SELECT * FROM AntiVirusProduct"))) {
                    var names = new List<string>();
                    var states = new List<string>();
                    foreach (ManagementObject av in s.Get()) {
                        var name = av["displayName"] == null ? null : av["displayName"].ToString();
                        var stateObj = av["productState"]; // uint32
                        if (!string.IsNullOrWhiteSpace(name)) names.Add(name);
                        if (stateObj != null) {
                            try {
                                var u = Convert.ToUInt32(stateObj);
                                states.Add("0x" + u.ToString("X"));
                            }
                            catch {
                                states.Add(stateObj.ToString());
                            }
                        }
                    }
                    info.AV_Products = string.Join("|", names.Distinct());
                    info.AV_ProductStates = string.Join("|", states);
                }
            }
            catch {
                info.AV_Products = "";
                info.AV_ProductStates = "";
            }

            // AV：Microsoft Defender
            try {
                var scope = new ManagementScope(@"\\.\root\Microsoft\Windows\Defender"); scope.Connect();
                using (var s = new ManagementObjectSearcher(scope, new ObjectQuery("SELECT * FROM MSFT_MpComputerStatus"))) {
                    foreach (ManagementObject mo in s.Get()) {
                        info.Defender_Enabled = mo["AntivirusEnabled"] == null ? null : mo["AntivirusEnabled"].ToString();
                        info.Defender_RTEnabled = mo["RealTimeProtectionEnabled"] == null ? null : mo["RealTimeProtectionEnabled"].ToString();
                        info.Defender_EngineVer = mo["AMEngineVersion"] == null ? null : mo["AMEngineVersion"].ToString();

                        var ts = mo["AntispywareSignatureLastUpdated"] ?? mo["AntivirusSignatureLastUpdated"];
                        if (ts != null) {
                            DateTime dt;
                            if (DateTime.TryParse(ts.ToString(), out dt))
                                info.Defender_SigDate = dt.ToString("yyyy-MM-dd");
                        }
                        break;
                    }
                }
            }
            catch {
                info.Defender_Enabled = "";
                info.Defender_RTEnabled = "";
                info.Defender_SigDate = "";
                info.Defender_EngineVer = "";
            }

            // Const
            info.Category = "PC/Laptop";
            info.LifecycleStatus = "In Service";
            info.LastInventoryDate = DateTime.Now.ToString("yyyy-MM-dd");

            CollectWindowsUpdateLatestDate(info);


            // SMB 共有（管理共有を除外）＋ Share Permission（詳細な共有のアクセス許可）＋ NTFS ACL ＋ 実効権限＋リスク判定
            try {
                using (var sh = new ManagementObjectSearcher("SELECT Name, Path, Type FROM Win32_Share WHERE Type = 0")) {
                    foreach (ManagementObject mo in sh.Get()) {
                        var shareName = mo["Name"] == null ? null : mo["Name"].ToString();
                        var sharePath = mo["Path"] == null ? null : mo["Path"].ToString();
                        if (string.IsNullOrWhiteSpace(shareName) || string.IsNullOrWhiteSpace(sharePath))
                            continue;

                        // 管理共有 / 隠し共有 を除外（末尾 $）
                        if (shareName.EndsWith("$", StringComparison.OrdinalIgnoreCase))
                            continue;

                        var audit = new ShareAuditInfo();
                        audit.ShareName = shareName;
                        audit.SharePath = sharePath;

                        // --- Share Permission (Advanced Sharing -> Permissions) ---
                        var sharePerms = SharePermissionReader.GetNormalizedSharePermissions(shareName);
                        audit.Share_Everyone = PermissionUtils.ToText(sharePerms.Everyone);
                        audit.Share_AuthenticatedUsers = PermissionUtils.ToText(sharePerms.AuthenticatedUsers);
                        audit.Share_Users = PermissionUtils.ToText(sharePerms.Users);
                        audit.Share_Admins = PermissionUtils.ToText(sharePerms.Admins);

                        // --- NTFS (Security tab) ---
                        var ntfsPerms = NtfsPermissionReader.GetNormalizedNtfsPermissions(sharePath);
                        audit.NTFS_Everyone = PermissionUtils.ToText(ntfsPerms.Everyone);
                        audit.NTFS_AuthenticatedUsers = PermissionUtils.ToText(ntfsPerms.AuthenticatedUsers);
                        audit.NTFS_Users = PermissionUtils.ToText(ntfsPerms.Users);
                        audit.NTFS_Admins = PermissionUtils.ToText(ntfsPerms.Admins);
                        audit.NTFS_OtherWrite = ntfsPerms.OtherWrite ? "TRUE" : "FALSE";

                        // --- Effective (Share ∧ NTFS) ---
                        audit.Effective_Everyone = PermissionUtils.ToText(PermissionUtils.Min(sharePerms.Everyone, ntfsPerms.Everyone));
                        audit.Effective_AuthenticatedUsers = PermissionUtils.ToText(PermissionUtils.Min(sharePerms.AuthenticatedUsers, ntfsPerms.AuthenticatedUsers));
                        audit.Effective_Users = PermissionUtils.ToText(PermissionUtils.Min(sharePerms.Users, ntfsPerms.Users));

                        // --- Ransomware risk evaluation ---
                        RiskEvaluator.Evaluate(audit);

                        info.ShareAudits.Add(audit);
                    }
                }
            }
            catch {
                // 共有列挙に失敗しても、他の収集は継続
            }
            return info;
        }

        // Windows Update
        private static void CollectWindowsUpdateLatestDate(AssetInfo info) {
            try {
                DateTime latest = DateTime.MinValue;
                using (var searcher = new System.Management.ManagementObjectSearcher(
                    "SELECT InstalledOn FROM Win32_QuickFixEngineering")) {
                    foreach (System.Management.ManagementObject mo in searcher.Get()) {
                        var raw = mo["InstalledOn"]?.ToString();
                        if (string.IsNullOrWhiteSpace(raw)) continue;
                        if (DateTime.TryParse(raw, out DateTime dt)) {
                            if (dt > latest) latest = dt;
                        }
                    }
                }
                info.WindowsUpdateLatestDate =
                    latest == DateTime.MinValue ? "N/A" : latest.ToString("yyyy-MM-dd");
            }
            catch {
                info.WindowsUpdateLatestDate = "ERROR";
            }
        }
    }
}
