# ResultProblem

https://olivervea.github.io/Olve.Utilities/api/Olve.Results.ResultProblem.html

Class. Represents a problem encountered during an operation.

Constructors:
- `ResultProblem(string message, params object[] args)` — message: format string. args: format arguments.
- `ResultProblem(Exception exception, string message, params object[] args)` — exception: causing exception.
- `ResultProblem(string formattedMessage, string[]? tags, int severity, string? source, string? exceptionSummary)` — for JSON deserialization.

Static fields:
- `string? DefaultSource` — default source for new problems
- `string[] DefaultTags` — default tags for new problems
- `int DefaultSeverity` — default severity for new problems
- `bool DefaultPrintDebug` — controls ToString() format

Properties:
- `string Message` — raw format string
- `string FormattedMessage` — formatted message with arguments applied
- `string[] Tags` — categorization tags
- `int Severity` — severity level (higher = more severe)
- `object[] Args` — format arguments
- `string? Source` — problem source
- `Exception? Exception` — causing exception
- `string? ExceptionSummary` — exception summary
- `ProblemOriginInformation OriginInformation` — auto-captured file/line

Methods:
- `string ToString()` — uses ToDebugString() if DefaultPrintDebug, else ToBriefString()
- `string ToBriefString()` — brief display, omits code locations
- `string ToDebugString()` — includes code locations
