using Biohazrd;
using Biohazrd.Transformation;

namespace PortAudioNet.Generator;

internal sealed class PaStreamHandleTransformation : TransformationBase
{
    protected override TransformationResult TransformTypedef(TransformationContext context, TranslatedTypedef declaration)
    {
        if (declaration is { Name: "PaStream", UnderlyingType: VoidTypeReference })
        {
            return new ExternallyDefinedTypeDeclaration(declaration.Name, declaration)
            {
                Namespace = nameof(PortAudioNet)
            };
        }

        return declaration;
    }
}
