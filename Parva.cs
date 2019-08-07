
/* Generic driver frame file for Coco/R for C#.
   Pat Terry 13 June 2004 (p.terry@ru.ac.za)
   Modify this to suit your own purposes - hints are given below! */

//  ----------------------- you may need to change the "using" clauses:

using System;
using System.IO;
using System.Text;

namespace Parva {

	public class Parva {

		public static void Main (string[] args) {
			bool mergeErrors = false;
			string inputName = null;

			// ------------------------ you may need to process command line parameters:

			for (int i = 0; i < args.Length; i++) {
				if (args[i].ToLower() == "-l") mergeErrors = true;
				else inputName = args[i];
			}
			if (inputName == null) {
				Console.WriteLine("No input file specified");
				System.Environment.Exit(1);
			}

			int pos = inputName.LastIndexOf('/');
			if (pos < 0) pos = inputName.LastIndexOf('\\');
			string dir = inputName.Substring(0, pos+1);

			Scanner.Init(inputName);
			Errors.Init(inputName, dir, mergeErrors);
			//  ----------------------- add other initialization if required:
			Parser.Parse();
			Errors.Summarize();
			//  ----------------------- add other finalization if required:
		}

	} // end driver

} // end namespace
