using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParameterManager
{
    [AttributeUsage(AttributeTargets.Field,AllowMultiple =false,Inherited =false)]
    public class HelpAttribute : Attribute
    {
        public HelpAttribute(string description_in)
        {
            this.description = description_in;
        }

        protected string description;
        public string Description
        {
            get { return this.description; }            
        }

        public bool IsCheck { get; set; } = false;

        public bool IsHidden { get; set; } = false;

    }
}
