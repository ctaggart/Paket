using Nessos.UnionArgParser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using Microsoft.FSharp.Core;
using Paket;
using System.Diagnostics;
using Microsoft.FSharp.Collections;

namespace Paket.PowerShell.CS
{
    [Cmdlet("Paket", "Add")]
    class Add : Cmdlet
    {
        protected override void ProcessRecord()
        {
            var parser = UnionArgParser.Create<Commands.AddArgs>(FSharpOption<string>.None);
            Debug.WriteLine("parser: " + parser);
        }
    }

    class Program
    {
        static ParseResults<T> createParseResultsOfList<T>(UnionArgParser<T> parser, List<T> inputs) where T : IArgParserTemplate
        {
            var cliParams = parser.PrintCommandLine(ListModule.OfSeq(inputs));
            return parser.ParseCommandLine(FSharpOption<string[]>.Some(cliParams), FSharpOption<IExiter>.None, FSharpOption<bool>.None, FSharpOption<bool>.None, FSharpOption<bool>.None);
        }

        static void Main(string[] args)
        {
            var add = new Add();
            //add.
            foreach(var result in add.Invoke())
            {
                Debug.WriteLine(result);
            }
            Debug.WriteLine("done");
        }
    }
}
