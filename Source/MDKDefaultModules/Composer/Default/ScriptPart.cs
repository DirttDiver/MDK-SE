using Microsoft.CodeAnalysis;

namespace Malware.MDKDefaultModules.Composer.Default
{
    /// <summary>
    /// Represents a part of a script. Script parts will be evaluated and joined together during build in order to
    /// create the final Space Engineers script.
    /// </summary>
    public abstract class ScriptPart
    {
        /// <summary>
        /// Create a new instance of <see cref="ScriptPart"/>
        /// </summary>
        /// <param name="document"></param>
        /// <param name="partRoot"></param>
        protected ScriptPart(Document document, SyntaxNode partRoot)
        {
            Document = document;
            PartRoot = partRoot;
        }

        /// <summary>
        /// The name of this document.
        /// </summary>
        public virtual string Name => Document?.Name;

        /// <summary>
        /// The document this part originated from
        /// </summary>
        public Document Document { get; }

        /// <summary>
        /// The root part that contains this part
        /// </summary>
        public SyntaxNode PartRoot { get; }

        /// <summary>
        /// Generates the script content that makes up this part
        /// </summary>
        /// <returns></returns>
        public abstract string GenerateContent();
    }
}
