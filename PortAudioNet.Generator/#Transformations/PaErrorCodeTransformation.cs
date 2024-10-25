using Biohazrd;
using Biohazrd.Transformation;
using System.Diagnostics;

namespace PortAudioNet.Generator;

internal sealed class PaErrorCodeTransformation : TransformationBase
{
    private TranslatedEnum? PaErrorCode = null;
    private TranslatedTypedef? PaError = null;

    protected override TranslatedLibrary PreTransformLibrary(TranslatedLibrary library)
    {
        Debug.Assert(PaErrorCode is null);
        Debug.Assert(PaError is null);
        PaErrorCode = null;
        PaError = null;

        foreach (TranslatedDeclaration declaration in library.EnumerateRecursively())
        {
            if (declaration is TranslatedEnum { Name: nameof(PaErrorCode) } paErrorCode)
            {
                Debug.Assert(PaErrorCode is null);
                PaErrorCode = paErrorCode;
            }
            else if (declaration is TranslatedTypedef { Name: nameof(PaError) } paError)
            {
                Debug.Assert(PaError is null);
                PaError = paError;
            }
        }

        if (PaErrorCode is null || PaError is null)
        {
            PaErrorCode = null;
            PaError = null;
        }

        return library;
    }

    protected override TransformationResult TransformEnum(TransformationContext context, TranslatedEnum declaration)
    {
        if (!ReferenceEquals(declaration, PaErrorCode))
            return declaration;

        Debug.Assert(PaError is not null);
        return declaration with
        {
            Name = nameof(PaError),
            UnderlyingType = PaError.UnderlyingType,
            ReplacedDeclarations = [PaError],
        };
    }

    protected override TransformationResult TransformTypedef(TransformationContext context, TranslatedTypedef declaration)
    {
        if (ReferenceEquals(declaration, PaError))
            return null;

        return declaration;
    }

    protected override TransformationResult TransformConstant(TransformationContext context, TranslatedConstant declaration)
    {
        if (PaErrorCode is not null && declaration.Name is "paFormatIsSupported")
            return declaration with { Type = TranslatedTypeReference.Create(PaErrorCode) };

        return declaration;
    }
}
