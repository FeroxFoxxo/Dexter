using Dexter.Abstractions;
using System.Collections.Generic;

namespace Dexter.Configurations {

    /// <summary>
    /// The PFPConfiguration class contains details about what can be used as a PFP and where the
    /// PFP images are stored to be loaded up by the ProfileService of the ~changepfp command.
    /// </summary>
    public class PFPConfiguration : JSONConfiguration {

        /// <summary>
        /// The PFPDirectory is the location of the directory in which the profile pictures for Dexter are stored.
        /// </summary>
        public string PFPDirectory { get; set; }

        /// <summary>
        /// The PFPExtensions is a list of extensions that Discord.NET can use for an image.
        /// </summary>
        public List<string> PFPExtensions { get; set; }
    }

}
