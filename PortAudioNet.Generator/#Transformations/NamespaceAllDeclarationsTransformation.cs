using Biohazrd;
using Biohazrd.Transformation;

namespace PortAudioNet.Generator;

internal sealed class NamespaceAllDeclarationsTransformation : TransformationBase
{
    protected override TransformationResult TransformDeclaration(TransformationContext context, TranslatedDeclaration declaration)
    {
        if (context.Parents.Length == 0 && declaration.Namespace is null)
        { return declaration with { Namespace = nameof(PortAudioNet) }; }
        else
        { return declaration; }
    }
}
