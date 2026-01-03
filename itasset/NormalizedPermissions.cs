namespace itasset {
    public class NormalizedPermissions {
        public PermLevel Everyone { get; set; } = PermLevel.NONE;
        public PermLevel AuthenticatedUsers { get; set; } = PermLevel.NONE;
        public PermLevel Users { get; set; } = PermLevel.NONE;
        public PermLevel Admins { get; set; } = PermLevel.NONE;
        public bool OtherWrite { get; set; } = false;
    }
}
