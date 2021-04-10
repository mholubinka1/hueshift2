using Q42.HueApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HueShift2.Model
{
    public class LightProperties
    {
        public string Id { get; private set; }
        public string Name { get; private set; }
        public string ModelId { get; private set; }

        public string ProductId { get; private set; }

        public LightProperties(Light networkLight)
        {
            this.Id = networkLight.Id;
            this.Name = networkLight.Name;
            this.ModelId = networkLight.ModelId;
            this.ProductId = networkLight.ProductId;
        }
    }
}
