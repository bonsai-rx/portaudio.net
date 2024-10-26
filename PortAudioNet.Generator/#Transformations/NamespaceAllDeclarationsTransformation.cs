using Biohazrd;
using Biohazrd.Transformation;

namespace PortAudioNet.Generator;

internal sealed class NamespaceAllDeclarationsTransformation : TransformationBase
{
    protected override TransformationResult TransformDeclaration(TransformationContext context, TranslatedDeclaration declaration)
    {
        if (context.Parents.Length > 0 || declaration.Namespace is not null)
            return declaration;

        string namespaceName = nameof(PortAudioNet);
        if (declaration.File.CSharpFriendlyName() is string namespaceSuffix)
            namespaceName += $".{namespaceSuffix}";

        return declaration with { Namespace = namespaceName };
    }
}
