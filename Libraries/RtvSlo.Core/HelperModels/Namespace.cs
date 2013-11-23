using RtvSlo.Core.HelperEnums;

namespace RtvSlo.Core.HelperModels
{
    public class Namespace
    {
        public string Prefix { get; set; }

        public string FullPath { get; set; }

        public NamespaceStatusEnum Status { get; set; }

        #region Ctor

        public Namespace()
        {

        }

        public Namespace(string prefix, string fullPath, NamespaceStatusEnum status)
        {
            this.Prefix = prefix;
            this.FullPath = fullPath;
            this.Status = status;
        }

        #endregion Ctor
    }
}
