using System;
using System.ComponentModel.DataAnnotations;

namespace RepoInspector
{
    public class RepoInspectorOptions
    {
        [Required]
        public TimeSpan DenialOfServiceBuffer { get; set; }

        [Required]
        public TimeSpan LoopDelay { get; set; }

        [Required]
        public TimeSpan HttpConnectionLifetime { get; set; }

        [Required]
        public string ScratchDirectory { get; set; }

        [Required]
        public string CachePath { get; set; }

        [Required]
        public string GitHubProductHeader { get; set; }
    }
}
