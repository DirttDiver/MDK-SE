using Microsoft.CodeAnalysis;

namespace Malware.MDKDefaultModules.Composer.Default
{
    /// <summary>
    /// Represents a script part which consists of an extension class; that is a class that is defined outside of the Program.
    /// </summary>
    public class ExtensionScriptPart : ScriptPart
    {
        /// <summary>
        /// Creates a new instance of <see cref="ExtensionScriptPart"/>
        /// </summary>
        /// <param name="document"></param>
        /// <param name="partRoot"></param>
        public ExtensionScriptPart(Document document, SyntaxNode partRoot) : base(document, partRoot)
        { }

        /// <inheritdoc />
        public override string GenerateContent()
        {
            return PartRoot.ToFullString();
        }
    }
}
