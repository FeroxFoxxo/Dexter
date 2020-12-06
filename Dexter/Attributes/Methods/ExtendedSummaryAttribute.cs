using System;

namespace Dexter.Attributes.Methods {

    [AttributeUsage(AttributeTargets.Method, Inherited = true)]
    public sealed class ExtendedSummaryAttribute : Attribute {

        public string ExtendedSummary { get; private set; }

        public ExtendedSummaryAttribute (string ExtendedSummary) {
            this.ExtendedSummary = ExtendedSummary;
        }

    }

}
