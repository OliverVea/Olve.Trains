using Olve.Validation;
using Olve.Validation.Validators;

namespace Olve.Engine3D.DebugServer.Commands;

internal class RunCommandValidator : IValidator<RunCommandRequest>
{
    private static readonly StringValidator CommandValidator = new StringValidator()
        .CannotBeNullOrWhiteSpace()
        .CannotBeOneOf("invalid");
    
    public Result Validate(RunCommandRequest value)
    {
        var result =  CommandValidator.Validate(value.Command);

        return result;
    }
}

