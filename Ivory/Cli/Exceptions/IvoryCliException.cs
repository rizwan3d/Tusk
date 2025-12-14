using System.Collections.ObjectModel;

namespace Ivory.Cli.Exceptions;

internal sealed class IvoryCliException : Exception
{
    private readonly List<Exception> _rollbackErrors = [];

    public IvoryCliException()
        : base("An error occurred while executing the command.")
    {
    }

    public IvoryCliException(string message)
        : base(message)
    {
    }

    public IvoryCliException(string message, Exception? inner)
        : base(message, inner)
    {
    }

    public IReadOnlyCollection<Exception> RollbackErrors => new ReadOnlyCollection<Exception>(_rollbackErrors);

    public void AddRollbackErrors(IEnumerable<Exception> errors)
    {
        foreach (var error in errors)
        {
            _rollbackErrors.Add(error);
        }
    }
}

