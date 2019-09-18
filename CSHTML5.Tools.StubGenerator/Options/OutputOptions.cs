using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StubGenerator.Common.Options
{
    public class OutputOptions
    {
        public OutputMethodOptions OutputMethodOptions { get; set; }

        public OutputPropertyOptions OutputPropertyOptions { get; set; }

        public OutputEventOptions OutputEventOptions { get; set; }

        public bool OutputOnlyPublicAndProtectedMembers { get; set; }

        public bool OutputFullTypeName { get; set; }

        public OutputOptions()
        {
            OutputEventOptions = OutputEventOptions.AUTO_IMPLEMENT;
            OutputMethodOptions = OutputMethodOptions.OUTPUT_RETURN_TYPE;
            OutputPropertyOptions = OutputPropertyOptions.OUTPUT_PRIVATE_FIELD;
            OutputOnlyPublicAndProtectedMembers = true;
            OutputFullTypeName = false;
        }

        public static OutputOptions Default
        {
            get
            {
                return new OutputOptions();
            }
        }

        public OutputOptions(OutputMethodOptions methodOptions, OutputPropertyOptions propertyOptions, OutputEventOptions eventOptions, bool writeFullTypeName, bool outputPrivateMembers)
        {
            OutputMethodOptions = methodOptions;
            OutputPropertyOptions = propertyOptions;
            OutputEventOptions = eventOptions;
            OutputOnlyPublicAndProtectedMembers = !outputPrivateMembers;
            OutputFullTypeName = writeFullTypeName;
        }

        public OutputOptions(bool implementMembers, bool outputOnlyPublicAndProtectedMembers, bool outputFullTypeName)
        {
            if (implementMembers)
            {
                OutputMethodOptions = OutputMethodOptions.OUTPUT_RETURN_TYPE;
                OutputPropertyOptions = OutputPropertyOptions.OUTPUT_PRIVATE_FIELD;
                OutputEventOptions = OutputEventOptions.AUTO_IMPLEMENT;
            }
            else
            {
                OutputMethodOptions = OutputMethodOptions.OUTPUT_NOT_IMPLEMENTED;
                OutputPropertyOptions = OutputPropertyOptions.OUTPUT_NOT_IMPLEMENTED;
                OutputEventOptions = OutputEventOptions.OUTPUT_NOT_IMPLEMENTED;
            }
            OutputOnlyPublicAndProtectedMembers = outputOnlyPublicAndProtectedMembers;
            OutputFullTypeName = outputFullTypeName;
        }
    }
}
