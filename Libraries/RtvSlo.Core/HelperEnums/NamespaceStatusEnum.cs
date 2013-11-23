
namespace RtvSlo.Core.HelperEnums
{
    public enum NamespaceStatusEnum : int
    {
        /// <summary>
        /// Namespace included in our triple store
        /// </summary>
        Internal = 1,

        /// <summary>
        /// Namespace needed for connecting to other triple stores
        /// </summary>
        External = 2
    }
}
