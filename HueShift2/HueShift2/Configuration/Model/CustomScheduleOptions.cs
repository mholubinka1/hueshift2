using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HueShift2.Configuration.Model
{
    public class CustomScheduleOptions
    {
        public CustomTransition[] Programme { get; set; } = Array.Empty<CustomTransition>();

        public CustomScheduleOptions()
        {

        }

        public void SetDefaults()
        {
            Programme = Array.Empty<CustomTransition>();
            return;
        }
    }


}
