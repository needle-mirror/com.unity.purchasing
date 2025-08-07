
namespace LipingShare.LCLib.Asn1Processor
{
    /// <summary>
    /// Represents the type of end of an indefinite length ASN.1 node.
    /// </summary>
    public enum Asn1EndOfIndefiniteLengthNodeType
    {
        /// <summary>
        /// Indicates the end of an indefinite length ASN.1 node, which is typically represented by a specific end-of-stream marker.
        /// </summary>
        EndOfStream,
        /// <summary>
        /// Indicates the end of an ASN.1 node footer, which is used to signify the end of a node in the context of ASN.1 encoding.
        /// </summary>
        EndOfNodeFooter,
        /// <summary>
        ///  Indicates that the ASN.1 node is not at the end, meaning it is still in progress or has not yet reached its conclusion.
        /// </summary>
        NotEnd,
    }
}
