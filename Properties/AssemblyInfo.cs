using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("CSGDemo")]
[assembly: AssemblyDescription("Demonstration of CSG algorithm")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Sander van Rossen & Matthew Baranowski")]
[assembly: AssemblyProduct("CSGDemo")]
[assembly: AssemblyCopyright("Copyright © 2010 Sander van Rossen & Matthew Baranowski")]
[assembly: AssemblyTrademark("CSGDemo")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("f236c767-678f-4c20-9282-d051a3c39657")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Build Number
//      Revision
//
[assembly: AssemblyVersion("1.0.0.201")]
[assembly: AssemblyFileVersion("1.0.0.201")]

#if SIGN_ASSEMBLY
[assembly: AssemblyKeyFile(@"../../../OpenTK.snk")]
#endif