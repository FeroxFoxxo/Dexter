using System;
namespace Dexter.Attributes
{

    /// <summary>
    /// The ExtendedSummary attribute provides extended information on a command's usage and summary.
    /// This summary only runs on `help [COMMAND]` rather than the generic `help` command, as the latter
    /// takes the generalized [Summary] attribute.
    /// </summary>
    /// <remarks>
    /// The constructor of the attribute sets the extended summary parameter to the variable.
    /// </remarks>
    /// <param name="extendedSummary">The Extended Summary of the command, which will be shown on the `help [COMMAND]` command.</param>

    [AttributeUsage(AttributeTargets.Method)]
    public class ExtendedSummaryAttribute(string extendedSummary) : Attribute
    {

        /// <summary>
        /// The Extended Summary is the string pertaining to the extended information of the command.
        /// </summary>

        public string ExtendedSummary { get; private set; } = extendedSummary;
    }

}
