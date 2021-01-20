using Dexter.Abstractions;
using System.Collections.Generic;

namespace Dexter.Configurations {

    /// <summary>
    /// The ProfilingConfiguration class contains details about what can be used as a PFP and where the
    /// PFP images are stored to be loaded up by the ProfileService of the ~changepfp command.
    /// It also contains information about database saving, particularly when databases should be saved.
    /// </summary>
    
    public class ProfilingConfiguration : JSONConfig {

        /// <summary>
        /// The PFPDirectory is the location of the directory in which the profile pictures for Dexter are stored.
        /// </summary>
        
        public string PFPDirectory { get; set; }

        /// <summary>
        /// The PFPExtensions is a list of extensions that Discord.NET can use for an image.
        /// </summary>
        
        public List<string> PFPExtensions { get; set; }

        /// <summary>
        /// The amount of seconds between each automatic profile picture switch.
        /// </summary>

        public int SecTillProfiling { get; set; }

        /// <summary>
        /// The numerical ID of the channel database data is to be backed up to.
        /// </summary>

        public ulong DatabaseBackupChannel { get; set; }

    }

}
