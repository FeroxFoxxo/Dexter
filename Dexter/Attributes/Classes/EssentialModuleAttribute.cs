using System;

namespace Dexter.Attributes.Classes {

    /// <summary>
    /// The EssentialModule attribute specifies that a given command module should be enabled in the CommandService
    /// as it is a vital part of the bots operations. These modules cannot be disabled and are applied to classes.
    /// </summary>
    
    [AttributeUsage(AttributeTargets.Class, Inherited = true)]
    public sealed class EssentialModuleAttribute : Attribute {}

}
