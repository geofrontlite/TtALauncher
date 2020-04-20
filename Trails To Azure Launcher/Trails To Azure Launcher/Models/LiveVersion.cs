using System;
using System.Collections.Generic;
using System.Text;

namespace Trails_To_Azure_Launcher.Models
{
    class LiveVersion
    {
        public String version { get; set; }
        public bool required { get; set; } = false;//Default to false. Up to the json file to override.
    }
}
